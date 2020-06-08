using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Reflection;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace AbeckDev.DoorController.DeviceClient
{
    class Program
    {

        static Microsoft.Azure.Devices.Client.DeviceClient deviceClient;

        string IotCéntralGlobalDeviceEndpoint;
        string IotCentralScopeId;
        string IotCentralDeviceId;
        string IotCentralPrimaryKey;

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
                          ProvisioningDeviceClient.Create("GlobalDeviceEndpoint", "ScopeID", security, transport);

                Console.WriteLine($"RegistrationID = {security.GetRegistrationID()}");

                Console.Write("ProvisioningClient RegisterAsync...");
                DeviceRegistrationResult result = await provClient.RegisterAsync();

                Console.WriteLine($"{result.Status}");

                return result;
            }
        }


        static void Main(string[] args)
        {
            string version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Console.WriteLine($"Starting DoorController Device Client: {version}");

            try
            {
                using (var security = new SecurityProviderSymmetricKey("DeviceId", "PrimaryKey", null))
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
                greenMessage("Device successfully connected to AZure IoT Central!");

                Console.WriteLine("Now what? ...");
                Console.ReadLine();

                //SendDevicePropertiesAsync().GetAwaiter().GetResult();

                //Console.Write("Register settings changed handler...");
                //s_deviceClient.SetDesiredPropertyUpdateCallbackAsync(HandleSettingChanged, null).GetAwaiter().GetResult();
                //Console.WriteLine("Done");

                //cts = new CancellationTokenSource();

                //// Create a handler for the direct method calls.
                //s_deviceClient.SetMethodHandlerAsync("GoToCustomer", CmdGoToCustomer, null).Wait();
                //s_deviceClient.SetMethodHandlerAsync("Recall", CmdRecall, null).Wait();

                //SendTruckTelemetryAsync(rand, cts.Token);

                //Console.WriteLine("Press any key to exit...");
                //Console.ReadKey();
                //cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
            }
        }
    }
}
