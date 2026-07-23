using AbeckDev.DoorController.DeviceClient.Model;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static AbeckDev.DoorController.DeviceClient.Service.ConsoleHelperService;

namespace AbeckDev.DoorController.DeviceClient.Service
{
    public class DeviceService
    {
        private Status _deviceStatus;
        private readonly IotHubDeviceClient _deviceClient;
        private readonly int _intervalInMilliseconds;
        private readonly List<DoorRegistration> _doorRegistrations;
        private readonly string _deviceLocation;
        private readonly int _coolDownIntervallMilliseconds;

        public DeviceService(
            Status deviceStatus,
            IotHubDeviceClient deviceClient,
            int intervalInMilliseconds,
            List<DoorRegistration> doorRegistrations,
            string deviceLocation,
            int coolDownIntervallMilliseconds)
        {
            _deviceStatus = deviceStatus;
            _deviceClient = deviceClient;
            _intervalInMilliseconds = intervalInMilliseconds;
            _doorRegistrations = doorRegistrations;
            _deviceLocation = deviceLocation;
            _coolDownIntervallMilliseconds = coolDownIntervallMilliseconds;
        }

        public void SetDeviceStatus(Status status) => _deviceStatus = status;

        public async Task SendDevicePropertiesAsync()
        {
            _deviceStatus = Status.ready;
            var reportedProperties = new ReportedProperties();
            reportedProperties["Location"] = _deviceLocation;
            reportedProperties["cooldownIntervalInMiliSec"] = _coolDownIntervallMilliseconds;
            reportedProperties["updateIntervalInMiliSec"] = _intervalInMilliseconds;
            reportedProperties["Version"] = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            reportedProperties["DeviceStatus"] = _deviceStatus.ToString();

            await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, CancellationToken.None);
            greenMessage($"Sent device properties: {JsonSerializer.Serialize(reportedProperties)}");
        }

        public async Task SendDeviceErrorTelemetryAsync(string message = "")
        {
            redMessage("Something went wrong. Will report error immediately.");
            _deviceStatus = Status.error;

            await SendTelemetryAndPropertiesAsync(message);
        }

        public async Task SendDeviceSuccessTelemetryAsync(string message = "")
        {
            greenMessage("Sending successful telemetry event to IoT Hub");
            _deviceStatus = Status.ready;

            await SendTelemetryAndPropertiesAsync(message);
        }

        public async Task SendDeviceTelemetryAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Console.WriteLine("[Device Update]");
                var doorActionString = "";
                string registeredDoorsReport = "";
                foreach (var door in _doorRegistrations)
                {
                    doorActionString += door.Name + ": " + door.ActionCount + "\n";
                    registeredDoorsReport += $"{door.Name}: ID={door.ID}, SystemCode={door.SystemCode}, DeviceCode={door.DeviceCode}; \n";
                    door.ActionCount = 0;
                }

                var telemetryDataPoint = new
                {
                    ReportedDeviceStatus = _deviceStatus.ToString(),
                    DoorActions = doorActionString,
                    RegisteredDoors = registeredDoorsReport,
                };
                var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
                var telemetryMessage = new TelemetryMessage(Encoding.ASCII.GetBytes(telemetryMessageString));
                await _deviceClient.SendTelemetryAsync(telemetryMessage, token);

                await SendDevicePropertiesAsync();
                greenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");

                await Task.Delay(_intervalInMilliseconds, token);
            }
        }

        private async Task SendTelemetryAndPropertiesAsync(string message)
        {
            var telemetryDataPoint = new
            {
                ReportedDeviceStatus = _deviceStatus.ToString(),
                EventMessage = message,
            };
            var telemetryMessageString = JsonSerializer.Serialize(telemetryDataPoint);
            var telemetryMessage = new TelemetryMessage(Encoding.ASCII.GetBytes(telemetryMessageString));
            await _deviceClient.SendTelemetryAsync(telemetryMessage, CancellationToken.None);
            await SendDevicePropertiesAsync();
        }
    }
}
