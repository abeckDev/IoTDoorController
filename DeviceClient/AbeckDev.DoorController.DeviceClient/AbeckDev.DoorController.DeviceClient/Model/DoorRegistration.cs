using System;
using System.Collections.Generic;
using System.Text;

namespace AbeckDev.DoorController.DeviceClient.Model
{
    public class DoorRegistration 
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public string SystemCode { get; set; } = null;

        public int DeviceCode { get; set; } = 0;

        public string Decimalcode { get; set; } = null;

        public int ActionCount { get; set; } = 0;

        public bool isLocked { get; set; } = false;

    }

    public enum Status
    {
        ready,
        offline,
        error
    }
}
