using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robot
{
    class ConfigSerialPort
    {
        public string CommName { get; set; } = "COM4";
        public int ComBaudrate { get; set; } = 115200;
        public Parity Parity { get; set; } = Parity.None;
        public byte DataByte { get; set; } = 8;
        public StopBits StopByte { get; set; } = StopBits.One;

        public ConfigSerialPort() { }

    }
}
