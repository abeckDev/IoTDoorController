using System;
using System.Collections.Generic;
using System.Text;

namespace AbeckDev.DoorController.DeviceClient.Model
{
    public class DoorRegistration
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public string SystemCode { get; set; }

        public int DeviceCode { get; set; }

        public int ActionCount { get; set; } = 0;
    }
}
