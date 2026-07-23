using System.Threading;

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

        public SemaphoreSlim CommandSemaphore { get; } = new(1, 1);

    }

    public enum Status
    {
        ready,
        offline,
        error
    }
}
