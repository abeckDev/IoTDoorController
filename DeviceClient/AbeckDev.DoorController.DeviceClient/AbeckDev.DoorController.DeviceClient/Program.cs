using AbeckDev.DoorController.DeviceClient.Extension;
using AbeckDev.DoorController.DeviceClient.Model;
using AbeckDev.DoorController.DeviceClient.Service;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AbeckDev.DoorController.DeviceClient
{
    internal class Program
    {
        private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true)).CreateLogger<Program>();

        private static Status DeviceStatus = Status.ready;
        private static IotHubDeviceClient deviceClient = null!;
        private static int intervalInMilliseconds = 900000;
        private static int coolDownintervallMilliseconds = 5000;
        private static List<DoorRegistration> doorRegistrations = [];
        private static string DeviceLocation = "Unknown";
        private static DeviceService deviceService = null!;

        private const string DefaultDoorCommandName = "DoorCommand";
        private const string DecimalCodeScriptPath = "/opt/doorControllerHelper/senddecimalcode.sh";
        private static string directMethodName = DefaultDoorCommandName;

        private static async Task Main(string[] args)
        {
#if DEBUG
            intervalInMilliseconds = 300000;
#endif
            string version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            Logger.LogInformation("Starting DoorController Device Client: {Version}", version);
            Logger.LogInformation("Reading Configuration");

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            DeviceLocation = configuration["DeviceLocation"] ?? DeviceLocation;
            coolDownintervallMilliseconds = configuration.GetValue<int?>("CooldownIntervalInMilliseconds") ?? coolDownintervallMilliseconds;
            intervalInMilliseconds = configuration.GetValue<int?>("UpdateIntervalInMilliseconds") ?? intervalInMilliseconds;
            doorRegistrations = DoorService.DoorRegistrationBuilder(configuration);

            try
            {
                deviceClient = await BuildDeviceClientAsync(configuration);
                Logger.LogInformation("Device successfully connected to Azure IoT Hub");

                deviceService = new DeviceService(DeviceStatus, deviceClient, intervalInMilliseconds, doorRegistrations, DeviceLocation, coolDownintervallMilliseconds);
                await deviceService.SendDevicePropertiesAsync();

                directMethodName = configuration["DirectMethodName"] ?? DefaultDoorCommandName;
                await deviceClient.SetDirectMethodCallbackAsync(CmdDoorAction, CancellationToken.None);

                using var cts = new CancellationTokenSource();
                await deviceService.SendDeviceTelemetryAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fatal startup error");
            }
        }

        private static async Task<IotHubDeviceClient> BuildDeviceClientAsync(IConfiguration configuration)
        {
            var ioTHubConnectionString = configuration["IoTHubConnectionString"];
            if (!string.IsNullOrWhiteSpace(ioTHubConnectionString))
            {
                Logger.LogInformation("Using V2 direct IoT Hub device connection string authentication.");
                return new IotHubDeviceClient(ioTHubConnectionString, new IotHubClientOptions());
            }

            var dpsIdScope = configuration["DpsIdScope"];
            var dpsRegistrationId = configuration["DpsRegistrationId"];
            var dpsSymmetricKey = configuration["DpsSymmetricKey"];
            var dpsGlobalDeviceEndpoint = configuration["DpsGlobalDeviceEndpoint"] ?? "global.azure-devices-provisioning.net";

            if (!string.IsNullOrWhiteSpace(dpsIdScope) &&
                !string.IsNullOrWhiteSpace(dpsRegistrationId) &&
                !string.IsNullOrWhiteSpace(dpsSymmetricKey))
            {
                Logger.LogInformation("Using V2 DPS provisioning for IoT Hub.");
                return await ProvisionClientFromDpsAsync(dpsGlobalDeviceEndpoint, dpsIdScope, dpsRegistrationId, dpsSymmetricKey);
            }

            var legacyGlobalDeviceEndpoint = configuration["IotCentralGlobalDeviceEndpoint"];
            var legacyScopeId = configuration["IotCentralScopeId"];
            var legacyDeviceId = configuration["IotCentralDeviceId"];
            var legacyPrimaryKey = configuration["IotCentralPrimaryKey"];

            if (!string.IsNullOrWhiteSpace(legacyGlobalDeviceEndpoint) &&
                !string.IsNullOrWhiteSpace(legacyScopeId) &&
                !string.IsNullOrWhiteSpace(legacyDeviceId) &&
                !string.IsNullOrWhiteSpace(legacyPrimaryKey))
            {
                Logger.LogWarning("Using legacy IoT Central-compatible DPS configuration (V1 compatibility mode).");
                return await ProvisionClientFromDpsAsync(legacyGlobalDeviceEndpoint, legacyScopeId, legacyDeviceId, legacyPrimaryKey);
            }

            throw new InvalidOperationException("Missing IoT Hub configuration. Configure IoTHubConnectionString (recommended) or DPS values (DpsIdScope/DpsRegistrationId/DpsSymmetricKey).");
        }

        private static async Task<IotHubDeviceClient> ProvisionClientFromDpsAsync(string globalEndpoint, string idScope, string registrationId, string symmetricKey)
        {
            using var security = new SecurityProviderSymmetricKey(registrationId, symmetricKey, null);
            using var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly);
            var provisioningClient = ProvisioningDeviceClient.Create(globalEndpoint, idScope, security, transport);

            var registrationResult = await provisioningClient.RegisterAsync(CancellationToken.None);
            if (registrationResult.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                throw new InvalidOperationException("DPS registration failed.");
            }

            var assignedDeviceId = registrationResult.DeviceId ?? registrationId;
            var dpsConnectionString = $"HostName={registrationResult.AssignedHub};DeviceId={assignedDeviceId};SharedAccessKey={security.GetPrimaryKey()}";
            return new IotHubDeviceClient(dpsConnectionString, new IotHubClientOptions());
        }

        private static async Task<DirectMethodResponse> CmdDoorAction(DirectMethodRequest methodRequest)
        {
            if (deviceService == null)
            {
                return new DirectMethodResponse(500)
                {
                    Payload = Encoding.UTF8.GetBytes("{\"CommandResponse\":\"Device service unavailable\"}"),
                };
            }

            try
            {
                if (!string.Equals(methodRequest.MethodName, directMethodName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Unsupported direct method '{methodRequest.MethodName}'. Expected '{directMethodName}'.");
                }

                if (!methodRequest.TryGetPayload<int>(out var doorNumber))
                {
                    throw new InvalidOperationException("Door payload must be numeric.");
                }

                var door = DoorService.GetDoorById(doorRegistrations, doorNumber);
                if (door == null)
                {
                    throw new InvalidOperationException($"Door {doorNumber} is not a registered door.");
                }

                await door.CommandSemaphore.WaitAsync();
                try
                {
                    if (!DoorService.isDecimalcodeMode(door))
                    {
                        throw new InvalidOperationException($"Door {door.Name} is not configured for decimal code mode.");
                    }

                    if (!DoorService.IsNumericDecimalCode(door.Decimalcode))
                    {
                        throw new InvalidOperationException($"Door {door.Name} has a non-numeric decimal code.");
                    }

                    Logger.LogInformation("Opening door {DoorName} ({DoorId}) using decimal code sender.", door.Name, door.ID);
                    var result = await DecimalCodeScriptPath.RunProcessAsync(door.Decimalcode);
                    Logger.LogInformation("{CommandResult}", result.Trim());
                    door.ActionCount++;
                    await Task.Delay(coolDownintervallMilliseconds);
                }
                finally
                {
                    door.CommandSemaphore.Release();
                }

                DeviceStatus = Status.ready;
                deviceService.SetDeviceStatus(DeviceStatus);
                string resultMsg = "{\"CommandResponse\":\"Executed direct method successfully\"}";
                await deviceService.SendDeviceSuccessTelemetryAsync(resultMsg);
                return new DirectMethodResponse(200)
                {
                    Payload = Encoding.UTF8.GetBytes(resultMsg),
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while executing direct method {MethodName}", methodRequest.MethodName);
                DeviceStatus = Status.error;
                deviceService.SetDeviceStatus(DeviceStatus);
                string errorMsg = "{\"CommandResponse\":\"Error in Method " + methodRequest.MethodName + ": " + ex.Message + "\"}";
                await deviceService.SendDeviceErrorTelemetryAsync(errorMsg);
                return new DirectMethodResponse(500)
                {
                    Payload = Encoding.UTF8.GetBytes(errorMsg),
                };
            }
        }
    }
}
