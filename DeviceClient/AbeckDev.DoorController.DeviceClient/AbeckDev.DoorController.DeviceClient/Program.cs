using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AbeckDev.DoorController.DeviceClient
{
    class Program
    {
        enum Status
        {
            ready,
            offline,
            error
        }
        static Status DeviceStatus = Status.ready;
        static Microsoft.Azure.Devices.Client.DeviceClient deviceClient;
        static TwinCollection reportedProperties = new TwinCollection();
        static int intervalInMilliseconds = 60000;
        static string IotCentralGlobalDeviceEndpoint;
        static string IotCentralScopeId;
        static string IotCentralDeviceId;
        static string IotCentralPrimaryKey;
        static string DeviceLocation;

        static void colorMessage(string text, ConsoleColor clr)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        static void greenMessage(string text)
        {
            colorMessage(text, ConsoleColor.Green);
        }

        static void redMessage(string text)
        {
            colorMessage(text, ConsoleColor.Red);
        }

        public static async Task<DeviceRegistrationResult> RegisterDeviceAsync(SecurityProviderSymmetricKey security)
        {
            Console.WriteLine("Register device...");

            using (var transport = new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly))
            {
                ProvisioningDeviceClient provClient =
                          ProvisioningDeviceClient.Create(IotCentralGlobalDeviceEndpoint, IotCentralScopeId, security, transport);

                Console.WriteLine($"RegistrationID = {security.GetRegistrationID()}");

                Console.Write("ProvisioningClient RegisterAsync...");
                DeviceRegistrationResult result = await provClient.RegisterAsync();

                Console.WriteLine($"{result.Status}");

                return result;
            }
        }

        static async Task SendDevicePropertiesAsync()
        {
            reportedProperties["Location"] = DeviceLocation;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            greenMessage($"Sent device properties: {JsonSerializer.Serialize(reportedProperties)}");
        }

        static Task<MethodResponse> CmdDoorAction(MethodRequest methodRequest, object userContext)
        {
            //Door needs to be triggered

            //Extract payload string
            var payloadString = Encoding.UTF8.GetString(methodRequest.Data).Replace("\"", "");

            int doorNumber = Int32.Parse(payloadString);
            Console.WriteLine($"Received the command to Open Door{doorNumber}");
            try
            {
                //Do Implementation for Open Door


                DeviceStatus = Status.ready;
                // Acknowledge the direct method call with a 200 success message.
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            catch (Exception ex)
            {
                // Error Handling
                string errorMsg = "{\"result\":\"Error in Methond " + methodRequest.Name + ": " + ex.Message + "\"}";
                SendErrorTelemetryAsync(errorMsg);
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(errorMsg), 500));
            }
        }

        static async void SendErrorTelemetryAsync(string message = "")
        {
            redMessage("Something went wrong. Will report error immediatly!");
            DeviceStatus = Status.error;
            //Send Telemetry
            var telemetryDataPoint = new
            {
                DeviceStatus = DeviceStatus.ToString(),
                ErrorMessage = message,
            };
            var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
            var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
            await deviceClient.SendEventAsync(telemetryMessage);
            greenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");

        }

        static async void SendTelemetryAsync(CancellationToken token)
        {
            while (true)
            {
                //Do update stuff
                Console.WriteLine("Time for an Update!");
                Console.WriteLine($"I am still standing at {DeviceLocation}");
                
                //Send Telemetry
                var telemetryDataPoint = new
                {
                    DeviceStatus = DeviceStatus.ToString(),
                };
                var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
                var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
                await deviceClient.SendEventAsync(telemetryMessage);
                greenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");

                //Wait a minute for next run
                await Task.Delay(intervalInMilliseconds);

            }
        }

        static void Main(string[] args)
        {
            string version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Console.WriteLine($"Starting DoorController Device Client: {version}");

            //Read Config
            Console.WriteLine("Reading Configuration");
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            IotCentralGlobalDeviceEndpoint = configuration["IotCentralGlobalDeviceEndpoint"];
            IotCentralScopeId = configuration["IotCentralScopeId"];
            IotCentralDeviceId = configuration["IotCentralDeviceId"];
            IotCentralPrimaryKey = configuration["IotCentralPrimaryKey"];
            DeviceLocation = configuration["DeviceLocation"];
            //Loaded Configuration

            try
            {
                using (var security = new SecurityProviderSymmetricKey(IotCentralDeviceId, IotCentralPrimaryKey, null))
                {
                    DeviceRegistrationResult result = RegisterDeviceAsync(security).GetAwaiter().GetResult();
                    if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                    {
                        Console.WriteLine("Failed to register Device");
                        return;
                    }

                    IAuthenticationMethod auth = new DeviceAuthenticationWithRegistrySymmetricKey(result.DeviceId, (security as SecurityProviderSymmetricKey).GetPrimaryKey());
                    deviceClient = Microsoft.Azure.Devices.Client.DeviceClient.Create(result.AssignedHub, auth, TransportType.Mqtt);
                }
                greenMessage("Device successfully connected to Azure IoT Central!");

                //Send Device Properties
                SendDevicePropertiesAsync().GetAwaiter().GetResult();

                CancellationTokenSource cts = new CancellationTokenSource();

                //Create Handler for direct method call
                deviceClient.SetMethodHandlerAsync("DoorCommand", CmdDoorAction, null).Wait();

                //Start Telemetry Send Loop
                SendTelemetryAsync(cts.Token);

                Console.WriteLine("Now what? ...");
                Console.ReadKey();
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
            }
        }
    }
}
