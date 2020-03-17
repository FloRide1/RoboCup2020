using System.Linq;
using Alea;

namespace DeviceQuery
{
    class Program
    {
        static void Main()
        {
            var devices = Device.Devices;
            var numGpus = devices.Length;
            foreach (var device in devices)
            {
                device.Print();

                // note that device ids for all GPU devices in a system does not need to be continuous
                var id = device.Id;
                var arch = device.Arch;
                var numMultiProc = device.Attributes.MultiprocessorCount;
            }

            // all device ids
            var deviceIds = devices.Select(device => device.Id);
        }
    }
}
