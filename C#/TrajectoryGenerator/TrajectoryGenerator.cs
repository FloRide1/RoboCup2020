using AdvancedTimers;
using EventArgsLibrary;
using PerceptionManagement;
using System;
using Utilities;
using WorldMap;

namespace TrajectoryGenerator
{
    public class TrajectoryPlanner
    {
        int robotId = 0;

        Location currentLocation;
        Location wayPointLocation;
        Location ghostLocation;

        GameState currentGameState = GameState.STOPPED;

        double FreqEch = 30;

        double accelAngulaireMax = 4; //en rad.s-2
        double accelLineaireMax = 2; //en m.s-2
        double vitesseAngulaireMax = 2;//en rad.s-1
        double vitesseLineaireMax = 1;//en m.s-1



        public TrajectoryPlanner(int id)
        {
            robotId = id;
            InitPositionPID();
        }

        public void InitRobotPosition(double x, double y, double theta)
        {
            currentLocation = new Location(x, y, theta, 0, 0, 0);
            wayPointLocation = new Location(x, y, theta, 0, 0, 0);
            ghostLocation = new Location(x, y, theta, 0, 0, 0);
        }
        
        //Input Events
        public void OnWaypointReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            wayPointLocation = e.Location;
        }

        public void OnPhysicalPositionReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (robotId == e.RobotId)
            {
                currentLocation = e.Location;
                CalculateGhostPosition();
                PIDPosition();
                //CalculateSpeedOrders();
            }
        }

        public void OnGameStateChangeReceived(object sender, EventArgsLibrary.GameStateArgs e)
        {
            if (e.RobotId == robotId)
            {
                currentGameState = e.gameState;
            }
        }

        void CalculateGhostPosition()
        {
            if (wayPointLocation == null)
                return;
            if (ghostLocation == null)
                return;

            //Calcul du cap du Waypoint dans le référentiel du terrain
            if (wayPointLocation.X - ghostLocation.X != 0)
                wayPointLocation.Theta = Math.Atan2(wayPointLocation.Y - ghostLocation.Y, wayPointLocation.X - ghostLocation.X);
            else
                wayPointLocation.Theta = Math.Atan2(wayPointLocation.Y - ghostLocation.Y, 0.0001);

            //Calcul du cap du Robot dans le référentiel du terrain
            //Attention, le cap est différent de l'orientation du robot !
            double CapRobotRefRobot;
            if (ghostLocation.Vx != 0)
                CapRobotRefRobot = Math.Atan2(ghostLocation.Vy, ghostLocation.Vx);
            else
                CapRobotRefRobot = Math.Atan2(ghostLocation.Vy, 0.0001);

            double CapRobotRefTerrain = CapRobotRefRobot + ghostLocation.Theta;

            wayPointLocation.Theta = Toolbox.ModuloByAngle(CapRobotRefRobot, wayPointLocation.Theta);

            //Calcul de l'éart de cap
            double ecartCap = wayPointLocation.Theta - CapRobotRefTerrain;

            //Calcul de la distance au WayPoint
            double distanceWayPoint = Math.Sqrt(Math.Pow(wayPointLocation.Y - ghostLocation.Y, 2) + Math.Pow(wayPointLocation.X - ghostLocation.X, 2));

            //Calcul de la vitesse linéaire du robot
            double vitesseLineaireRobot = Math.Sqrt(Math.Pow(ghostLocation.Vx, 2) + Math.Pow(ghostLocation.Vy, 2));

            //Vitesse souhaitée au passage du WayPoint (permet de définir des Waypoint terminaux avec arrêt ou transistoires)
            double vitesseLineaireWaypointSouhaitee = Math.Sqrt(Math.Pow(wayPointLocation.Vx, 2) + Math.Pow(wayPointLocation.Vy, 2));

            //Calcul de la vitesse maximum permettant de passer le WayPoint à la vitesse voulue
            double vitesseMaxParRapportAuWaypoint = Math.Sqrt(Math.Pow(vitesseLineaireWaypointSouhaitee, 2) + 2 * accelLineaireMax * distanceWayPoint);
            vitesseMaxParRapportAuWaypoint = Math.Min(vitesseMaxParRapportAuWaypoint, vitesseLineaireMax); //Limitation à VMax

            //Calcul de la vitesse lineaire cible en prenant en compte l'ecart de cap pour éviter des rayons de courbure trop grands
            double vitesseLineaireCible = Math.Max(0, vitesseMaxParRapportAuWaypoint * Math.Cos(ecartCap));
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
                nouveauCapRobot = wayPointLocation.Theta + ghostLocation.Theta;
            else
            {
                double capCible = wayPointLocation.Theta + ghostLocation.Theta;
                capCible = Toolbox.ModuloByAngle(CapRobotRefTerrain, capCible);
                vitesseAngulaireMax = 1.0;
                if (capCible >= CapRobotRefTerrain)
                    CapRobotRefTerrain = Math.Min(capCible, CapRobotRefTerrain + vitesseAngulaireMax / FreqEch);
                else
                    CapRobotRefTerrain = Math.Max(capCible, CapRobotRefTerrain - vitesseAngulaireMax / FreqEch);
                nouveauCapRobot = CapRobotRefTerrain + KpAng * ecartCap / FreqEch;
            }


            ////On traite à présent l'orientation angulaire du robot pour l'aligner sur l'angle demandé
            //wayPointLocation.Theta = Toolbox.ModuloByAngle(ghostLocation.Theta, wayPointLocation.Theta);
            //double ecartOrientationRobot = wayPointLocation.Theta - ghostLocation.Theta;
            //ghostLocation.Vtheta = 100.0 * ecartOrientationRobot / FreqEch;

            

            if (currentGameState != GameState.STOPPED)
            {
                //On génère les vitesses dans le référentiel du robot.
                ghostLocation.Vx = nouvelleVitesseLineaire * Math.Cos(ghostLocation.Theta - nouveauCapRobot);
                ghostLocation.Vy = nouvelleVitesseLineaire * -Math.Sin(ghostLocation.Theta - nouveauCapRobot);
                ghostLocation.Vtheta = 0.2;

                //Nouvelle orientation du robot
                //ghostLocation.Vtheta = 50* (wayPointLocation.Theta - ghostLocation.Theta)/FreqEch;

                ghostLocation.X += ghostLocation.Vx / FreqEch;
                ghostLocation.Y += ghostLocation.Vy / FreqEch;
                //ghostLocation.Theta += ghostLocation.Vtheta / FreqEch;
                //ghostLocation.Theta = Toolbox.ModuloByAngle(nouveauCapRobot, ghostLocation.Theta);
            }
            else
            {
                //Si on est à l'arrêt, on ne change rien
            }

            OnGhostLocation(robotId, ghostLocation);

        }

        AsservissementPID PID_X;
        AsservissementPID PID_Y;
        AsservissementPID PID_Theta;
        void InitPositionPID()
        {
            PID_X  = new AsservissementPID(FreqEch, 100.0, 50, 0, 100);
            PID_Y = new AsservissementPID(FreqEch, 100.0, 50, 0, 100);
            PID_Theta = new AsservissementPID(FreqEch, 1, 0, 0, 0);
        }

        void PIDPosition()
        {
            double vx = PID_X.CalculatePIDoutput(ghostLocation.X - currentLocation.X);
            double vy = PID_Y.CalculatePIDoutput(ghostLocation.Y - currentLocation.Y);
            double vtheta = PID_Theta.CalculatePIDoutput(ghostLocation.Theta - currentLocation.Theta);

            OnSpeedConsigneToRobot(robotId, (float)vx, (float)vy, (float)vtheta);
        }


        //void CalculateSpeedOrders()
        //{
        //    if (wayPointLocation == null)
        //        return;
        //    if (currentLocation == null)
        //        return;

        //    //Calcul du cap du Waypoint dans le référentiel du terrain
        //    double CapWayPointRefTerrain;
        //    if (wayPointLocation.X - currentLocation.X != 0)
        //        CapWayPointRefTerrain = Math.Atan2(wayPointLocation.Y - currentLocation.Y, wayPointLocation.X - currentLocation.X);
        //    else
        //        CapWayPointRefTerrain = Math.Atan2(wayPointLocation.Y - currentLocation.Y, 0.0001);

        //    //Calcul du cap du Robot dans le référentiel du terrain
        //    double CapRobotRefRobot;
        //    if (currentLocation.Vx != 0)
        //        CapRobotRefRobot = Math.Atan2(currentLocation.Vy, currentLocation.Vx);
        //    else
        //        CapRobotRefRobot = Math.Atan2(currentLocation.Vy, 0.0001);

        //    double CapRobotRefTerrain = CapRobotRefRobot + currentLocation.Theta;

        //    //Calcul de l'éart de cap
        //    double ecartCap = CapWayPointRefTerrain - CapRobotRefTerrain;
        //    ecartCap = Toolbox.Modulo2PiAngleRad(ecartCap);

        //    //Calcul de la distance au WayPoint
        //    double distanceWayPoint = Math.Sqrt(Math.Pow(wayPointLocation.Y - currentLocation.Y, 2) + Math.Pow(wayPointLocation.X - currentLocation.X, 2));

        //    //Calcul de la vitesse linéaire du robot
        //    double vitesseLineaireRobot = Math.Sqrt(Math.Pow(currentLocation.Vx, 2) + Math.Pow(currentLocation.Vy, 2));

        //    //Vitesse souhaitée au passage du WayPoint (permet de définir des Waypoint terminaux avec arrêt ou transistoires)
        //    double vitesseLineaireWaypointSouhaitee = Math.Sqrt(Math.Pow(wayPointLocation.Vx, 2) + Math.Pow(wayPointLocation.Vy, 2));

        //    //Calcul de la vitesse maximum permettant de passer le WayPoint à la vitesse voulue
        //    double vitesseMaxParRapportAuWaypoint = Math.Sqrt(Math.Pow(vitesseLineaireWaypointSouhaitee, 2) + 2 * accelLineaireMax * distanceWayPoint);
        //    vitesseMaxParRapportAuWaypoint = Math.Min(vitesseMaxParRapportAuWaypoint, vitesseLineaireMax); //Limitation à VMax

        //    //Calcul de la vitesse lineaire cible en prenant en compte l'ecart de cap pour éviter des rayons de courbure trop grands
        //    double vitesseLineaireCible = Math.Max(0, vitesseMaxParRapportAuWaypoint * Math.Cos(ecartCap));
        //    //Si le wayPoint est derrière le robot, on a normalement une vitesse linéaire cible nulle
        //    //    mais pour permettre que le robot tourne, on met un minimum de vitesse dans ce cas
        //    if (Math.Cos(ecartCap) < 0)
        //        vitesseLineaireCible = 0.2;

        //    //Calcul de la nouvelle vitesse lineaire en rampes sur la consigne vitesse lineaire cible (prise en compte du freinage ou de l'acceleration)
        //    double nouvelleVitesseLineaire;
        //    if (vitesseLineaireCible >= vitesseLineaireRobot)
        //        nouvelleVitesseLineaire = Math.Min(vitesseLineaireCible, vitesseLineaireRobot + accelLineaireMax / FreqEch);
        //    else
        //        nouvelleVitesseLineaire = Math.Max(vitesseLineaireCible, vitesseLineaireRobot - accelLineaireMax / FreqEch);

        //    //Si la vitesse Obtenue est faible et que l'on est très proche du WayPoint, on la réduit à 0 pour éviter les micro-mouvements
        //    if (nouvelleVitesseLineaire <= 0.2 && distanceWayPoint < 0.02)
        //        nouvelleVitesseLineaire = 0;

        //    //Calcul du nouveau Cap Robot : si la vitesse est faible, le nouveau cap est la direction du waypoint, sinon on tourne progressivement.
        //    double KpAng = 1.0;
        //    double nouveauCapRobot;
        //    if (nouvelleVitesseLineaire <= 0.2)
        //        nouveauCapRobot = CapWayPointRefTerrain;
        //    else
        //    {
        //        double capCible = CapWayPointRefTerrain;
        //        capCible = Toolbox.ModuloByAngle(CapRobotRefTerrain, capCible);
        //        vitesseAngulaireMax = 1.0;
        //        if (capCible >= CapRobotRefTerrain)
        //            CapRobotRefTerrain = Math.Min(capCible, CapRobotRefTerrain + vitesseAngulaireMax / FreqEch);
        //        else
        //            CapRobotRefTerrain = Math.Max(capCible, CapRobotRefTerrain - vitesseAngulaireMax / FreqEch);
        //        nouveauCapRobot = CapRobotRefTerrain + KpAng * ecartCap / FreqEch;
        //    }

        //    //On génère les vitesses à transmettre dans le référentiel du robot.
        //    double newVx = nouvelleVitesseLineaire * Math.Cos(currentLocation.Theta - nouveauCapRobot);
        //    double newVy = nouvelleVitesseLineaire * -Math.Sin(currentLocation.Theta - nouveauCapRobot);

        //    //On traite à présent l'orientation angulaire du robot pour l'aligner sur l'angle demandé
        //    double ecartOrientationRobot = wayPointLocation.Theta - currentLocation.Theta;
        //    double newVTheta = 30.0 * ecartOrientationRobot / FreqEch;

        //    OnSpeedConsigneToRobot(robotId, (float)newVx, (float)newVy, (float)newVTheta);
        //}

        //Output events
        public event EventHandler<SpeedConsigneArgs> OnSpeedConsigneEvent;
        public virtual void OnSpeedConsigneToRobot(int id, float vx, float vy, float vtheta)
        {
            var handler = OnSpeedConsigneEvent;
            if (handler != null)
            {
                handler(this, new SpeedConsigneArgs { RobotId = id, Vx = vx, Vy = vy, Vtheta = vtheta });
            }
        }


        public event EventHandler<LocationArgs> OnGhostLocationEvent;
        public virtual void OnGhostLocation(int id, Location loc)
        {
            var handler = OnGhostLocationEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location=loc});
            }
        }
    }
}
