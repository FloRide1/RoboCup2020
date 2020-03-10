using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace SensorSimulator
{
    public class SensorSimulator
    {
        int robotId;
        Location currentLocation = new Location(0, 0, 0, 0, 0, 0);

        public SensorSimulator(int id)
        {
            robotId = id;
        }

        //Input events
        public void OnPhysicalRobotPositionReceived(object sender, LocationArgs e)
        {
            //On calcule la perception simulée de position d'après le retour du simulateur physique directement
            //On réel on utilisera la triangulation lidar et la caméra
            if (robotId == e.RobotId)
            {
                //On construit les datas simulées en ajoutant du bruit et des dérives
                double xCamLidarSimu = e.Location.X;
                double yCamLidarSimu = e.Location.Y; 
                double thetaCamLidarSimu = e.Location.Theta;
                double vxOdoSimu = e.Location.Vx;
                double vyOdoSimu = e.Location.Vy;
                double vthetaOdoSimu = e.Location.Vtheta;
                double vthetaGyroSimu = e.Location.Vtheta;

                OnCamLidarSimulatedRobotPosition(robotId, xCamLidarSimu, yCamLidarSimu, thetaCamLidarSimu);
                OnOdometrySimulatedRobotSpeed(robotId, vxOdoSimu, vyOdoSimu, vthetaOdoSimu);
                OnGyroSimulatedRobotSpeed(robotId, vthetaGyroSimu);
            }
        }

        //Output events
        public event EventHandler<PositionArgs> OnCamLidarSimulatedRobotPositionEvent;
        public virtual void OnCamLidarSimulatedRobotPosition(int id, double x, double y, double theta)
        {
            var handler = OnCamLidarSimulatedRobotPositionEvent;
            if (handler != null)
            {
                handler(this, new PositionArgs { RobotId = id, X = x, Y = y, Theta = theta });
            }
        }

        public event EventHandler<SpeedArgs> OnOdometrySimulatedRobotSpeedEvent;
        public virtual void OnOdometrySimulatedRobotSpeed(int id, double vx, double vy, double vtheta)
        {
            var handler = OnOdometrySimulatedRobotSpeedEvent;
            if (handler != null)
            {
                handler(this, new SpeedArgs { RobotId = id, Vx = vx, Vy = vy, Vtheta = vtheta });
            }
        }

        public event EventHandler<GyroArgs> OnGyroSimulatedRobotSpeedEvent;
        public virtual void OnGyroSimulatedRobotSpeed(int id, double vtheta)
        {
            var handler = OnGyroSimulatedRobotSpeedEvent;
            if (handler != null)
            {
                handler(this, new GyroArgs { RobotId = id, Vtheta = vtheta });
            }
        }
    }
}
