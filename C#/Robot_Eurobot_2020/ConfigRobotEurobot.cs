using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robot
{
    class ConfigRobotEurobot
    {

        public RobotMode RobotMode { get; set; } = RobotMode.Standard;
        public bool UsingPhysicalSimulator { get; set; } = true;
        public bool UsingXBoxController { get; set; } = false;
        public bool UsingLidar { get; set; } = true;
        public bool UsingLogging { get; set; } = false;
        public bool UsingLogReplay { get; set; } = false;
        public bool usingRobotInterface { get; set; } = true;
        public bool usingReplayNavigator { get; set; } = true;
        public ConfigRobotEurobot() { }
    }
}
