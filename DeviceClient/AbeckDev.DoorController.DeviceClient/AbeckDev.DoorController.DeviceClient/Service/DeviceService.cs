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
using System.Reflection;

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
        int coolDownintervallMilliseconds;

        public DeviceService(Status DeviceStatus, Microsoft.Azure.Devices.Client.DeviceClient deviceClient, int intervallInMiliseconds, List<DoorRegistration> doorRegistrations, string IotCentralGlobalDeviceEndpoint, string IotCentralScopeId, string IotCentralDeviceId, string IotCentralPrimaryKey, string DeviceLocation, int coolDownintervallMilliseconds)
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
            this.coolDownintervallMilliseconds = coolDownintervallMilliseconds;
        }


        /// <summary>
        /// Send the Device Properties to Azure IoT Hub
        /// </summary>
        /// <returns></returns>
        public async Task SendDevicePropertiesAsync()
        {
            DeviceStatus = Status.ready;
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["Location"] = DeviceLocation;
            reportedProperties["cooldownIntervalInMiliSec"] = coolDownintervallMilliseconds;
            reportedProperties["updateIntervalInMiliSec"] = intervalInMilliseconds;
            reportedProperties["Version"] = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            reportedProperties["DeviceStatus"] = DeviceStatus.ToString();
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            greenMessage($"Sent device properties: {JsonSerializer.Serialize(reportedProperties)}");
        }


        /// <summary>
        /// Will send a telemetry message with an error flag to Azure IoT Central
        /// </summary>
        /// <param name="message">The Message to send</param>
        public async void SendDeviceErrorTelemetryAsync(string message = "")
        {
            redMessage("Something went wrong. Will report error immediatly!");
            DeviceStatus = Status.error;
            
            //Send Telemetry
            var telemetryDataPoint = new
            {
                ReportedDeviceStatus = DeviceStatus.ToString(),
                EventMessage = message,
            };
            var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
            var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
            await deviceClient.SendEventAsync(telemetryMessage);

            //Update Properties
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["Location"] = DeviceLocation;
            reportedProperties["cooldownIntervalInMiliSec"] = coolDownintervallMilliseconds;
            reportedProperties["updateIntervalInMiliSec"] = intervalInMilliseconds;
            reportedProperties["Version"] = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            reportedProperties["DeviceStatus"] = DeviceStatus.ToString();
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        /// <summary>
        /// Will send a telemetry message with a success flag to Azure IoT Central
        /// </summary>
        /// <param name="message"></param>
        public async void SendDeviceSuccessTelemetryAsync(string message = "")
        {
            greenMessage("Sending successfull Telemetry Event to ioT Central");
            DeviceStatus = Status.ready;
            //Send Telemetry
            var telemetryDataPoint = new
            {
                ReportedDeviceStatus = DeviceStatus.ToString(),
                EventMessage = message,
            };
            var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
            var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
            await deviceClient.SendEventAsync(telemetryMessage);

            //Update Properties
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["Location"] = DeviceLocation;
            reportedProperties["cooldownIntervalInMiliSec"] = coolDownintervallMilliseconds;
            reportedProperties["updateIntervalInMiliSec"] = intervalInMilliseconds;
            reportedProperties["Version"] = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            reportedProperties["DeviceStatus"] = DeviceStatus.ToString();
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }

        /// <summary>
        /// The loop which will keep sending telemtry messages every <see cref="intervalInMilliseconds"/> to Azure IoT Central
        /// </summary>
        /// <param name="token"></param>
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
                    ReportedDeviceStatus = DeviceStatus.ToString(),
                    DoorActions = doorActionString,
                    RegisteredDoors = registeredDoorsReport,
                };
                var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
                var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));
                await deviceClient.SendEventAsync(telemetryMessage);
                //Send Properties 
                await SendDevicePropertiesAsync();
                greenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");

                //Wait for next run
                await Task.Delay(intervalInMilliseconds);

            }
        }
    }
}
