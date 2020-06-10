using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AbeckDev.DoorController.DeviceClient.Service
{
    public static class DeviceService
    {
        public static async Task<DeviceRegistrationResult> RegisterDeviceAsync(SecurityProviderSymmetricKey security, string IotCentralGlobalDeviceEndpoint, string IotCentralScopeId)
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

        static async Task SendDevicePropertiesAsync(string DeviceLocation)
        {
            TwinCollection reportedProperties = new TwinCollection();

            reportedProperties["Location"] = DeviceLocation;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            greenMessage($"Sent device properties: {JsonSerializer.Serialize(reportedProperties)}");
        }
    }
}
