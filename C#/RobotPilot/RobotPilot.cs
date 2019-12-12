using Constants;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldMap;

namespace RobotPilot
{
    public class RobotPilot
    {
        string Name = "";
        public RobotPilot(string robotName)
        {
            Name = robotName;
        }

        public void SendSpeedConsigneToRobot()
        {
            OnSpeedConsigneToRobot(Name, (float)0.5, (float)0.2, (float)0.02);
        }

        public void SendSpeedConsigneToMotor()
        {
            OnSpeedConsigneToMotor((float)0.1, MotorControlName.MotorLeft);
        }

        Random rand = new Random();
        public void SendPositionFromKalmanFilter()
        {
            Location loc = new Location((float)(0.100 + rand.Next(-30, 30) / 100.0), (float)(0.1 + rand.Next(-30, 30) / 100.0), (float)(rand.NextDouble() * Math.PI / 12), 0, 0, 0);
            OnSendPositionFromKalmanFilter("Robot1Team1", loc);
        }

        //Events générés en sortie
        public delegate void SpeedConsigneEventHandler(object sender, SpeedConsigneArgs e);
        public event EventHandler<SpeedConsigneArgs> OnSpeedConsigneEvent;
        public virtual void OnSpeedConsigneToRobot(string name, float vx, float vy, float vtheta)
        {
            var handler = OnSpeedConsigneEvent;
            if (handler != null)
            {
                handler(this, new SpeedConsigneArgs {RobotName=name, Vx = vx, Vy = vy, Vtheta = vtheta });
            }
        }


        public delegate void SpeedConsigneToMotorEventHandler(object sender, SpeedConsigneToMotorArgs e);
        public event EventHandler<SpeedConsigneToMotorArgs> OnSpeedConsigneToMotorEvent;
        public virtual void OnSpeedConsigneToMotor(float speed, MotorControlName motor)
        {
            var handler = OnSpeedConsigneToMotorEvent;
            if (handler != null)
            {
                handler(this, new SpeedConsigneToMotorArgs { V = speed, MotorNumber = (byte)motor});
            }
        }
        

        public delegate void SendPositionFromKalmanFilterEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnSendPositionFromKalmanFilterEvent;
        public virtual void OnSendPositionFromKalmanFilter(string name, Location location )
        {
            var handler = OnSendPositionFromKalmanFilterEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotName = name, Location = location});
            }
        }
    }
}
