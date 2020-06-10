using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using static AbeckDev.DoorController.DeviceClient.Service.ConsoleHelperService;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using AbeckDev.DoorController.DeviceClient.Model;
using Microsoft.Azure.Devices.Client;
using System.Threading;

namespace AbeckDev.DoorController.DeviceClient.Service
{
    public class DeviceService
    {
        Status DeviceStatus;
        Microsoft.Azure.Devices.Client.DeviceClient deviceClient;
        int intervalInMilliseconds = 900000;
        List<DoorRegistration> doorRegistrations;
        string IotCentralGlobalDeviceEndpoint;
        string IotCentralScopeId;
        string IotCentralDeviceId;
        string IotCentralPrimaryKey;
        string DeviceLocation;

        public DeviceService(Status DeviceStatus, Microsoft.Azure.Devices.Client.DeviceClient deviceClient, int intervallInMiliseconds, List<DoorRegistration> doorRegistrations, string IotCentralGlobalDeviceEndpoint, string IotCentralScopeId, string IotCentralDeviceId, string IotCentralPrimaryKey, string DeviceLocation)
        {
            this.DeviceStatus = DeviceStatus;
            this.deviceClient = deviceClient;
            this.intervalInMilliseconds = intervallInMiliseconds;
            this.doorRegistrations = doorRegistrations;
            this.IotCentralGlobalDeviceEndpoint = IotCentralGlobalDeviceEndpoint;
            this.IotCentralScopeId = IotCentralScopeId;
            this.IotCentralDeviceId = IotCentralDeviceId;
            this.IotCentralPrimaryKey = IotCentralPrimaryKey;
            this.DeviceLocation = DeviceLocation;

        }

        public async Task SendDevicePropertiesAsync()
        {
            TwinCollection reportedProperties = new TwinCollection();

            reportedProperties["Location"] = DeviceLocation;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            greenMessage($"Sent device properties: {JsonSerializer.Serialize(reportedProperties)}");
        }


        public async void SendDeviceErrorTelemetryAsync(string message = "")
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


        public async void SendDeviceSuccessTelemetryAsync(string message = "")
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

        public async void SendDeviceTelemetryAsync(CancellationToken token)
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
                    door.ActionCount = 0;
                }

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
    }
}
