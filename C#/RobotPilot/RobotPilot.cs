using Constants;
using EventArgsLibrary;
using System;
using Utilities;

namespace RobotPilot
{
    public class RobotPilot
    {
        int RobotId = 0;
        public RobotPilot(int robotId)
        {
            RobotId = robotId;
        }
        
        Random rand = new Random();
        public void SendPositionFromKalmanFilter()
        {
            Location loc = new Location((float)(0.100 + rand.Next(-30, 30) / 100.0), (float)(0.1 + rand.Next(-30, 30) / 100.0), (float)(rand.NextDouble() * Math.PI / 12), 0, 0, 0);
            OnSendPositionFromKalmanFilter((int)TeamId.Team1+1, loc);
        }

        //Events générés en sortie
        public delegate void SpeedConsigneEventHandler(object sender, SpeedConsigneArgs e);
        public event EventHandler<SpeedConsigneArgs> OnSpeedConsigneEvent;
        public virtual void OnSpeedConsigneToRobot(int id, float vx, float vy, float vtheta)
        {
            var handler = OnSpeedConsigneEvent;
            if (handler != null)
            {
                handler(this, new SpeedConsigneArgs {RobotId=id, Vx = vx, Vy = vy, Vtheta = vtheta });
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
        public virtual void OnSendPositionFromKalmanFilter(int id, Location location )
        {
            var handler = OnSendPositionFromKalmanFilterEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = location});
            }
        }
    }
}
