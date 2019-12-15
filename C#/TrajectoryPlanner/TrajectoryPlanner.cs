using AdvancedTimers;
using EventArgsLibrary;
using PerceptionManagement;
using System;
using Utilities;
using WorldMap;

namespace TrajectoryGeneration
{
    public class TrajectoryPlanner
    {
        double FreqEch = 50;

        string robotName = "";

        Location currentLocation;
        Location wayPointLocation;

        double fSampling = 50;

        double accelAngulaireMax = 2; //en rad.s-2
        double accelLineaireMax = 2; //en m.s-2
        double vitesseAngulaireMax = 0.8;//en rad.s-1
        double vitesseLineaireMax = 3;//en m.s-1

        //HighFreqTimer highFrequencyTimer;


        public TrajectoryPlanner(string name)
        {
            robotName = name;
            //highFrequencyTimer = new HighFreqTimer(fSampling);
            //highFrequencyTimer.Tick += HighFrequencyTimer_Tick; ;
            //highFrequencyTimer.Start();
        }

        //private void HighFrequencyTimer_Tick(object sender, EventArgs e)
        //{
        //    //Calcul des nouvelles vitesses consigne
        //    double currentHeading = 
        //    //throw new NotImplementedException();
        //}

        public void OnPhysicalPositionReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if(robotName == e.RobotName)
            {
                currentLocation = e.Location;
                CalculateSpeedOrders();
            }            
        }

        void CalculateSpeedOrders()
        {
            if (wayPointLocation == null)
                return;
            if (currentLocation == null)
                return;

            //Calcul du cap du Waypoint dans le référentiel du terrain
            double CapWayPointRefTerrain; 
            if (wayPointLocation.X - currentLocation.X != 0)
                CapWayPointRefTerrain = Math.Atan2(wayPointLocation.Y - currentLocation.Y, wayPointLocation.X - currentLocation.X);
            else
                CapWayPointRefTerrain = Math.Atan2(wayPointLocation.Y - currentLocation.Y, 0.0001);

            //Calcul du cap du Robot dans le référentiel du terrain
            double CapRobotRefRobot;            
            if(currentLocation.Vx!=0)
                CapRobotRefRobot = Math.Atan2(currentLocation.Vy, currentLocation.Vx);
            else
                CapRobotRefRobot = Math.Atan2(currentLocation.Vy, 0.0001);

            double CapRobotRefTerrain = CapRobotRefRobot + currentLocation.Theta;

            //Calcul de l'éart de cap
            double ecartCap = CapWayPointRefTerrain - CapRobotRefTerrain;
            ecartCap = Toolbox.Modulo2PiAngleRad(ecartCap);

            //Calcul de la distance au WayPoint
            double distanceWayPoint = Math.Sqrt(Math.Pow(wayPointLocation.Y - currentLocation.Y, 2) + Math.Pow(wayPointLocation.X - currentLocation.X, 2));

            //Calcul de la vitesse linéaire du robot
            double vitesseLineaireRobot = Math.Sqrt(Math.Pow(currentLocation.Vx, 2) + Math.Pow(currentLocation.Vy, 2));

            //Vitesse souhaitée au passage du WayPoint (permet de définir des Waypoint terminaux avec arrêt ou transistoires)
            double vitesseLineaireWaypointSouhaitee = Math.Sqrt(Math.Pow(wayPointLocation.Vx, 2) + Math.Pow(wayPointLocation.Vy, 2));

            //Calcul de la vitesse maximum permettant de passer le WayPoint à la vitesse voulue
            double vitesseMaxParRapportAuWaypoint = Math.Sqrt(Math.Pow(vitesseLineaireWaypointSouhaitee, 2)+2*accelLineaireMax*distanceWayPoint);
            vitesseMaxParRapportAuWaypoint = Math.Min(vitesseMaxParRapportAuWaypoint, vitesseLineaireMax); //Limitation à VMax
            
            //Calcul de la vitesse lineaire cible en prenant en compte l'ecart de cap pour éviter des rayons de courbure trop grands
            double vitesseLineaireCible = Math.Max(0,vitesseMaxParRapportAuWaypoint * Math.Cos(ecartCap));
            //Si le wayPoint est derrière le robot, on a normalement une vitesse linéaire cible nulle
            //    mais pour permettre que le robot tourne, on met un minimum de vitesse dans ce cas
            if (Math.Cos(ecartCap) < 0)
                vitesseLineaireCible = 0.2;
                       
            //Calcul de la nouvelle vitesse lineaire en rampes sur la consigne vitesse lineaire cible (prise en compte du freinage ou de l'acceleration)
            double nouvelleVitesseLineaire;
            if (vitesseLineaireCible >= vitesseLineaireRobot)
                nouvelleVitesseLineaire = Math.Min(vitesseLineaireCible, vitesseLineaireRobot + accelLineaireMax / FreqEch);
            else
                nouvelleVitesseLineaire = Math.Max(vitesseLineaireCible, vitesseLineaireRobot - accelLineaireMax / FreqEch);

            //Si la vitesse Obtenue est faible et que l'on est très proche du WayPoint, on la réduit à 0 pour éviter les micro-mouvements
            if (nouvelleVitesseLineaire <= 0.2 && distanceWayPoint < 0.02)
                nouvelleVitesseLineaire = 0;
                        
            //Calcul du nouveau Cap Robot : si la vitesse est faible, le nouveau cap est la direction du waypoint, sinon on tourne progressivement.
            double KpAng = 1.0;
            double nouveauCapRobot;
            if (nouvelleVitesseLineaire <= 0.2)
                nouveauCapRobot = CapWayPointRefTerrain;
            else
            {
                double capCible = CapWayPointRefTerrain;
                capCible = Toolbox.ModuloByAngle(CapRobotRefTerrain, capCible);
                vitesseAngulaireMax = 1.0;
                if (capCible >= CapRobotRefTerrain)
                    CapRobotRefTerrain = Math.Min(capCible, CapRobotRefTerrain + vitesseAngulaireMax / FreqEch);
                else
                    CapRobotRefTerrain = Math.Max(capCible, CapRobotRefTerrain - vitesseAngulaireMax / FreqEch);
                nouveauCapRobot = CapRobotRefTerrain + KpAng * ecartCap / FreqEch;
            }

            //On génère les vitesses à transmettre dans le référentiel du robot.
            double newVx = nouvelleVitesseLineaire * Math.Cos(currentLocation.Theta - nouveauCapRobot);
            double newVy = nouvelleVitesseLineaire * -Math.Sin(currentLocation.Theta - nouveauCapRobot);

            //On traite à présent l'orientation angulaire du robot pour l'aligner sur l'angle demandé
            double ecartOrientationRobot = wayPointLocation.Theta - currentLocation.Theta;
            double newVTheta = 30.0 * ecartOrientationRobot / FreqEch;

            OnSpeedConsigneToRobot(robotName, (float)newVx, (float)newVy, (float)newVTheta);
        }

        //Input Events
        public void OnWaypointReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            wayPointLocation = e.Location;
        }

        //Output events
        public delegate void SpeedConsigneEventHandler(object sender, SpeedConsigneArgs e);
        public event EventHandler<SpeedConsigneArgs> OnSpeedConsigneEvent;
        public virtual void OnSpeedConsigneToRobot(string name, float vx, float vy, float vtheta)
        {
            var handler = OnSpeedConsigneEvent;
            if (handler != null)
            {
                handler(this, new SpeedConsigneArgs { RobotName = name, Vx = vx, Vy = vy, Vtheta = vtheta });
            }
        }
    }
}
