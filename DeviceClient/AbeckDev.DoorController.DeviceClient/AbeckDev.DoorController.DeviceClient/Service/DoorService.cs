using AbeckDev.DoorController.DeviceClient.Model;
using static AbeckDev.DoorController.DeviceClient.Service.ConsoleHelperService;
using static AbeckDev.DoorController.DeviceClient.Service.DeviceService;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;

namespace AbeckDev.DoorController.DeviceClient.Service
{
    public static class DoorService
    {
        /// <summary>
        /// Will register all doors to the Application based on the Config input
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static List<DoorRegistration> DoorRegistrationBuilder(IConfiguration configuration)
        {
            List<DoorRegistration> doorRegistrations = new List<DoorRegistration>();
            var valuesSection = configuration.GetSection("RemoteDoors");
            foreach (IConfigurationSection section in valuesSection.GetChildren())
            {
                try
                {
                    DoorRegistration door = new DoorRegistration();
                    door.Name = section.GetValue<string>("Name");
                    door.ID = section.GetValue<int>("ID");
                    door.SystemCode = section.GetValue<string>("SystemCode");
                    door.DeviceCode = section.GetValue<int>("DeviceCode");
                    Console.WriteLine($"Found Registration for Door: {door.Name}");
                    doorRegistrations.Add(door);
                    Console.WriteLine($"Successfully added door {door.Name} with ID {door.ID} and SystemCode {door.SystemCode} + {door.DeviceCode} to active door registration.");

                }
                catch (Exception ex)
                {
                    redMessage($"Error while Adding door {section.GetValue<string>("Name")}");
                    redMessage("Error Message: " + ex.Message);
                    redMessage("SKIPPING!");
                }
            }
            return doorRegistrations;
        }
    }
}
