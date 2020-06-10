using AbeckDev.DoorController.DeviceClient.Model;
using AbeckDev.DoorController.DeviceClient.Service;
using static AbeckDev.DoorController.DeviceClient.Service.ConsoleHelperService;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace AbeckDev.DoorController.DeviceClient
{
    class Program
    {
        
        static Status DeviceStatus = Status.ready;
        static Microsoft.Azure.Devices.Client.DeviceClient deviceClient;
        static int intervalInMilliseconds = 900000;
        static List<DoorRegistration> doorRegistrations;
        static string IotCentralGlobalDeviceEndpoint;
        static string IotCentralScopeId;
        static string IotCentralDeviceId;
        static string IotCentralPrimaryKey;
        static string DeviceLocation;

        
        static Task<MethodResponse> CmdDoorAction(MethodRequest methodRequest, object userContext)
        {
            //Door needs to be triggered
            //Extract payload string
            var payloadString = Encoding.UTF8.GetString(methodRequest.Data).Replace("\"", "");

            int doorNumber = Int32.Parse(payloadString);
            Console.WriteLine($"Received the command to Open Door{doorNumber}");
            try
            {
                //Check if door exists
                if (!doorRegistrations.Exists(d => d.ID == doorNumber) && doorNumber != 10)
                {
                    //Door does not exist
                    throw new Exception($"Door {doorNumber} is not a registered door!");
                }

                Console.WriteLine($"Opening door {doorNumber}");
                //ToDo: Open the Door Code


                DeviceStatus = Status.ready;
                // Acknowledge the direct method call with a 200 success message.
                string resultMsg = "{\"CommandResponse\":\"Executed direct method: " + methodRequest.Name + "successfully\" }";
                SendDeviceSuccessTelemetryAsync(resultMsg);
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(resultMsg), 200));
            }
            catch (Exception ex)
            {
                // Error Handling
                string errorMsg = "{\"CommandResponse\":\"Error in Method " + methodRequest.Name + ": " + ex.Message + "\"}";
                SendDeviceErrorTelemetryAsync(errorMsg);
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(errorMsg), 500));
            }
        }

        static async void SendDeviceErrorTelemetryAsync(string message = "")
        {
            redMessage("Something went wrong. Will report error immediatly!");
            DeviceStatus = Status.error;
            //Send Telemetry
            var telemetryDataPoint = new
            {
                DeviceStatus = DeviceStatus.ToString(),
                EventMessage = message,
            };
            var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
            var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
            await deviceClient.SendEventAsync(telemetryMessage);
            greenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");
        }

        static async void SendDeviceSuccessTelemetryAsync(string message = "")
        {
            greenMessage("Sending successfull Telemetry Event to ioT Central");
            DeviceStatus = Status.ready;
            //Send Telemetry
            var telemetryDataPoint = new
            {
                DeviceStatus = DeviceStatus.ToString(),
                EventMessage = message,
            };
            var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
            var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
            await deviceClient.SendEventAsync(telemetryMessage);
        }

        static async void SendTelemetryAsync(CancellationToken token)
        {
            while (true)
            {
                //Do update stuff
                Console.WriteLine("Time for an Update!");
                Console.WriteLine($"I am still standing at {DeviceLocation}");

                //Build Door Action String
                var doorActionString = "";
                string registeredDoorsReport = "";
                foreach (var door in doorRegistrations)
                {
                    doorActionString += door.Name + ": " + door.ActionCount + "\n";
                    registeredDoorsReport += $"{door.Name}: ID={door.ID}, SystemCode={door.SystemCode}, DeviceCode={door.DeviceCode}; \n";
                }

                //Build Saved Frequencies

                //Send Telemetry
                var telemetryDataPoint = new
                {
                    DeviceStatus = DeviceStatus.ToString(),
                    DoorActions = doorActionString,
                    RegisteredDoors = registeredDoorsReport,
                };
                var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
                var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
                await deviceClient.SendEventAsync(telemetryMessage);
                greenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");

                //Wait a minute for next run
                await Task.Delay(intervalInMilliseconds);

            }
        }


        //Needs dependencie
        public static void ResetDoorActionCounter()
        {
            foreach (var door in doorRegistrations)
            {
                door.ActionCount = 0;
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
            doorRegistrations = DoorService.DoorRegistrationBuilder(configuration);
            //Loaded Configuration

            try
            {
                using (var security = new SecurityProviderSymmetricKey(IotCentralDeviceId, IotCentralPrimaryKey, null))
                {
                    DeviceRegistrationResult result = DeviceService.RegisterDeviceAsync(security,IotCentralGlobalDeviceEndpoint,IotCentralScopeId).GetAwaiter().GetResult();
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

                //Cancellation Event
                Console.ReadLine();
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
