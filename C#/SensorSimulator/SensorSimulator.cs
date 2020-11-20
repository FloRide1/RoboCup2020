using EventArgsLibrary;
using System;
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

        Random rand = new Random();

        int imgSubSamplingCounter = 0;
        //Input events
        public void OnPhysicalRobotPositionReceived(object sender, LocationArgs e)
        {
            imgSubSamplingCounter++;
            //On calcule la perception simulée de position d'après le retour du simulateur physique directement
            //On réel on utilisera la triangulation lidar et la caméra
            if (robotId == e.RobotId)
            {
                //On construit les datas simulées en ajoutant du bruit et des dérives
                double erreurPositionCamLidarMax = 0;// 0.5; //en metre
                double erreurAngleCamLidarMax = 0;// 0.2; //en radian
                double erreurVitesseLineaireOdoMax = 0;// 0.1;
                double erreurVitesseAngulaireOdoMax = 0;// 0.1;
                double erreurVitesseAngulaireGyroMax = 0; // 0.05;

                double xCamLidarSimu = e.Location.X + (rand.NextDouble() - 0.5) * 2* erreurPositionCamLidarMax;
                double yCamLidarSimu = e.Location.Y + (rand.NextDouble() - 0.5) * 2* erreurPositionCamLidarMax;
                double thetaCamLidarSimu = e.Location.Theta + (rand.NextDouble() - 0.5) * erreurAngleCamLidarMax;
                double vxOdoSimu = e.Location.Vx + (rand.NextDouble() - 0.5) * 2 * erreurVitesseLineaireOdoMax;
                double vyOdoSimu = e.Location.Vy + (rand.NextDouble() - 0.5) * 2 * erreurVitesseLineaireOdoMax;
                double vthetaOdoSimu = e.Location.Vtheta + (rand.NextDouble() - 0.5) * 2 * erreurVitesseAngulaireOdoMax;
                double vthetaGyroSimu = e.Location.Vtheta + (rand.NextDouble() - 0.5) * 2 * erreurVitesseAngulaireGyroMax; 
                
                if (imgSubSamplingCounter % 5 == 0)
                {
                    OnCamLidarSimulatedRobotPosition(robotId, xCamLidarSimu, yCamLidarSimu, thetaCamLidarSimu);
                }
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

        public event EventHandler<PolarSpeedArgs> OnOdometrySimulatedRobotSpeedEvent;
        public virtual void OnOdometrySimulatedRobotSpeed(int id, double vx, double vy, double vtheta)
        {
            var handler = OnOdometrySimulatedRobotSpeedEvent;
            if (handler != null)
            {
                handler(this, new PolarSpeedArgs { RobotId = id, Vx = vx, Vy = vy, Vtheta = vtheta });
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
