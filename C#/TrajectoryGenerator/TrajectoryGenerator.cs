using AdvancedTimers;
using Constants;
using EventArgsLibrary;
using PerceptionManagement;
using PerformanceMonitorTools;
using System;
using System.Diagnostics;
using Utilities;
using WorldMap;

namespace TrajectoryGenerator
{
    public class TrajectoryPlanner
    {
        int robotId = 0;
        GameMode gameMode;
        bool isUsingXboxControler = false;

        Location currentLocationRefTerrain;
        Location wayPointLocation;
        Location ghostLocationRefTerrain;

        GameState currentGameState = GameState.PLAYING;

        //double FreqEch = 30.0;

        //double accelLineaireMax = 0.1; //en m.s-2
        //double accelRotationCapVitesseMax = 2* Math.PI * 0.25; //en rad.s-2
        //double accelRotationOrientationRobotMax = 2 * Math.PI * 0.1; //en rad.s-2

        //double vitesseLineaireMax = 0.3; //en m.s-1
        //double vitesseRotationCapVitesseMax = 2*Math.PI * 0.4; //en rad.s-1
        //double vitesseRotationOrientationRobotMax = 2*Math.PI * 0.1; //en rad.s-1

        double accelLineaireMax = 4; //en m.s-2
        double accelRotationCapVitesseMax = 2 * Math.PI * 1.0; //en rad.s-2
        double accelRotationOrientationRobotMax = 2 * Math.PI * 1.0; //en rad.s-2

        double vitesseLineaireMax = 2; //en m.s-1
        double vitesseRotationCapVitesseMax = 2 * Math.PI * 2.0; //en rad.s-1
        double vitesseRotationOrientationRobotMax = 2 * Math.PI * 2.0; //en rad.s-1


        double capVitesseRefTerrain = 0;
        double vitesseRotationCapVitesse = 0;


        AsservissementPID PID_X;
        AsservissementPID PID_Y;
        AsservissementPID PID_Theta;
        
        System.Timers.Timer PidConfigUpdateTimer;

        public TrajectoryPlanner(int id, GameMode gameMode)
        {
            this.gameMode = gameMode;
            robotId = id;
            InitRobotPosition(0, 0, 0);
            InitPositionPID();

            switch (this.gameMode)
            {
                case GameMode.RoboCup:
                    {
                        accelLineaireMax = 4; //en m.s-2
                        accelRotationCapVitesseMax = 2 * Math.PI * 1.0; //en rad.s-2
                        accelRotationOrientationRobotMax = 2 * Math.PI * 1.0; //en rad.s-2

                        vitesseLineaireMax = 3; //en m.s-1
                        vitesseRotationCapVitesseMax = 3 * Math.PI * 2.0; //en rad.s-1
                        vitesseRotationOrientationRobotMax = 2 * Math.PI * 2.0; //en rad.s-1
                    }
                    break;
                case GameMode.Eurobot:
                    {
                        accelLineaireMax = 0.1; //en m.s-2
                        accelRotationCapVitesseMax = 0.8 * Math.PI * 1.0; //en rad.s-2
                        accelRotationOrientationRobotMax = 0.8 * Math.PI * 1.0; //en rad.s-2

                        vitesseLineaireMax = 0.8; //en m.s-1
                        vitesseRotationCapVitesseMax = 0.8 * Math.PI * 2.0; //en rad.s-1
                        vitesseRotationOrientationRobotMax = 0.8 * Math.PI * 2.0; //en rad.s-1
                    }
                    break;
            }
        }

        void InitPositionPID()
        {
            PID_X = new AsservissementPID(20.0, 10.0, 0, 100, 100, 1);
            PID_Y = new AsservissementPID(20.0, 10.0, 0, 100, 100, 1);
            PID_Theta = new AsservissementPID(20.0, 10.0, 0, 5*Math.PI, 5*Math.PI, Math.PI); 

            PidConfigUpdateTimer = new System.Timers.Timer(1000);
            PidConfigUpdateTimer.Elapsed += PositionPidConfigUpdateTimer_Elapsed;
            PidConfigUpdateTimer.Start();

        }

        private void PositionPidConfigUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            switch (gameMode)
            {
                case GameMode.RoboCup:
                    {
                        PID_X.Init(kp: 0.0, ki: 0.0, kd: 0.0, 10, 10, 10);
                        PID_Y.Init(kp: 0.0, ki: 0.0, kd: 0.0, 10, 10, 10);
                        PID_Theta.Init(kp: 12.0, ki: 0.0, kd: 1.0, 10, 10, 10);
                        //PID_X.Init(kp: 50.0, ki: 0.0, kd: 4.0, 10, 10, 10);
                        //PID_Y.Init(kp: 50.0, ki: 0.0, kd: 4.0, 10, 10, 10);
                        //PID_Theta.Init(kp: 12.0, ki: 0.0, kd: 1.0, 10, 10, 10);
                    }
                    break;
                case GameMode.Eurobot:
                    {
                        PID_X.Init(kp: 0.0, ki: 0.0, kd: 1.0, 10, 10, 10);
                        PID_Y.Init(kp: 0.0, ki: 0.0, kd: 0.0, 10, 10, 10);
                        PID_Theta.Init(kp: 1.0, ki: 0.0, kd: 1.0, 10, 10, 10);
                    }
                    break;
            }
        }


        public void ChangeUseOfXBoxController(object sender, BoolEventArgs e)
        {
            isUsingXboxControler = e.value;
        }

        public void InitRobotPosition(double x, double y, double theta)
        {
            Location old_currectLocation = currentLocationRefTerrain;
            currentLocationRefTerrain = new Location(x, y, theta, 0, 0, 0);
            wayPointLocation = new Location(x, y, theta, 0, 0, 0);
            ghostLocationRefTerrain = new Location(x, y, theta, 0, 0, 0);
            if(old_currectLocation == null)
                OnCollision(robotId, currentLocationRefTerrain);
            else if (Toolbox.Distance(new PointD(old_currectLocation.X, old_currectLocation.Y), new PointD(currentLocationRefTerrain.X, currentLocationRefTerrain.Y)) > 0.5)
                OnCollision(robotId, currentLocationRefTerrain);
            PIDPositionReset();
        }
        
        //Input Events
        public void OnWaypointReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            wayPointLocation = e.Location;
        }


        Stopwatch swTimeInterPhysicalPositionReceived = new Stopwatch();
        double InstantPhysicalPositionReceived_1 = 0;
        public void OnPhysicalPositionReceived(object sender, EventArgsLibrary.LocationArgs e)
        {           
            if (!swTimeInterPhysicalPositionReceived.IsRunning)
                swTimeInterPhysicalPositionReceived.Start();

            double InstantPhysicalPositionReceived = swTimeInterPhysicalPositionReceived.ElapsedMilliseconds;
            double elapsedTimeBetweenSamples = (InstantPhysicalPositionReceived - InstantPhysicalPositionReceived_1)/1000;
            InstantPhysicalPositionReceived_1 = InstantPhysicalPositionReceived;

            if (robotId == e.RobotId)
            {
                currentLocationRefTerrain = e.Location;
                CalculateGhostPosition(elapsedTimeBetweenSamples);
                PIDPosition(elapsedTimeBetweenSamples);
            }
        }

        public void OnGameStateChangeReceived(object sender, EventArgsLibrary.GameStateArgs e)
        {
            if (e.RobotId == robotId)
            {
                currentGameState = e.gameState;
            }
        }

        void PIDPositionReset()
        {
            if (PID_X != null && PID_Y != null && PID_Theta != null)
            {
                PID_X.ResetPID(0);
                PID_Y.ResetPID(0);
                PID_Theta.ResetPID(0);
            }
        }
        void PIDPosition(double elapsedTimeBetweenSamples)
        {
            if (ghostLocationRefTerrain != null)
            {
                double erreurXRefTerrain = ghostLocationRefTerrain.X - currentLocationRefTerrain.X;
                double erreurYRefTerrain = ghostLocationRefTerrain.Y - currentLocationRefTerrain.Y;
                currentLocationRefTerrain.Theta = Toolbox.ModuloByAngle(ghostLocationRefTerrain.Theta, currentLocationRefTerrain.Theta);
                double erreurTheta = ghostLocationRefTerrain.Theta - currentLocationRefTerrain.Theta;

                //Changement de repère car les asservissements se font dans le référentiel du robot
                double erreurXRefRobot = erreurXRefTerrain * Math.Cos(currentLocationRefTerrain.Theta) + erreurYRefTerrain * Math.Sin(currentLocationRefTerrain.Theta);
                double erreurYRefRobot = -erreurXRefTerrain * Math.Sin(currentLocationRefTerrain.Theta) + erreurYRefTerrain * Math.Cos(currentLocationRefTerrain.Theta);

                ////For testing purpose only
                //double vxGhostRefRobot = ghostLocationRefTerrain.Vx * Math.Cos(currentLocationRefTerrain.Theta) + ghostLocationRefTerrain.Vy * Math.Sin(currentLocationRefTerrain.Theta);
                //double vyGhostRefRobot = -ghostLocationRefTerrain.Vx * Math.Sin(currentLocationRefTerrain.Theta) + ghostLocationRefTerrain.Vy * Math.Cos(currentLocationRefTerrain.Theta);
                //double vxRefRobot = vxGhostRefRobot;
                //double vyRefRobot = vyGhostRefRobot;
                //double vtheta = ghostLocationRefTerrain.Vtheta;

                //Console.WriteLine("Temps entre sample Odometry : " + elapsedTimeBetweenSamples.ToString() + " ms");

                double vxRefRobot = PID_X.CalculatePIDoutput(erreurXRefRobot, elapsedTimeBetweenSamples);
                double vyRefRobot = PID_Y.CalculatePIDoutput(erreurYRefRobot, elapsedTimeBetweenSamples);
                double vtheta = PID_Theta.CalculatePIDoutput(erreurTheta, elapsedTimeBetweenSamples);

                //On regarde si la position du robot est proche de la position du ghost
                double seuilToleranceEcartGhost = 1.0;
                if (Math.Sqrt(Math.Pow(erreurXRefTerrain, 2) + Math.Pow(erreurYRefTerrain, 2)+ Math.Pow(erreurTheta/2,2)) < seuilToleranceEcartGhost)
                {
                    //Si c'est le cas, le robot n'a pas rencontré de problème, on envoie les vitesses consigne.
                    OnSpeedConsigneToRobot(robotId, (float)vxRefRobot, (float)vyRefRobot, (float)vtheta);
                }
                else
                {
                    if (!isUsingXboxControler)
                    {
                        //Sinon, le robot a rencontré un obstacle ou eu un problème, on arrête le robot et on réinitialise les correcteurs et la ghostLocation
                        OnCollision(robotId, currentLocationRefTerrain);
                        OnSpeedConsigneToRobot(robotId, 0, 0, 0);

                        ghostLocationRefTerrain = currentLocationRefTerrain;
                        PIDPositionReset();
                        OnPidSpeedReset(robotId);
                    }
                }

                PolarPidCorrectionArgs correction = new PolarPidCorrectionArgs();
                correction.CorrPx = PID_X.correctionP;
                correction.CorrIx = PID_X.correctionI;
                correction.CorrDx = PID_X.correctionD;
                correction.CorrPy = PID_Y.correctionP;
                correction.CorrIy = PID_Y.correctionI;
                correction.CorrDy = PID_Y.correctionD;
                correction.CorrPTheta = PID_Theta.correctionP;
                correction.CorrITheta = PID_Theta.correctionI;
                correction.CorrDTheta = PID_Theta.correctionD;
                OnMessageToDisplayPositionPidCorrection(correction);
            }


            PolarPIDSetupArgs PositionPidSetup = new PolarPIDSetupArgs();
            PositionPidSetup.P_x = PID_X.Kp;
            PositionPidSetup.I_x = PID_X.Ki;
            PositionPidSetup.D_x = PID_X.Kd;
            PositionPidSetup.P_y = PID_Y.Kp;
            PositionPidSetup.I_y = PID_Y.Ki;
            PositionPidSetup.D_y = PID_Y.Kd;
            PositionPidSetup.P_theta = PID_Theta.Kp;
            PositionPidSetup.I_theta = PID_Theta.Ki;
            PositionPidSetup.D_theta = PID_Theta.Kd;
            PositionPidSetup.P_x_Limit = PID_X.ProportionalLimit;
            PositionPidSetup.I_x_Limit = PID_X.IntegralLimit;
            PositionPidSetup.D_x_Limit = PID_X.DerivationLimit;
            PositionPidSetup.P_y_Limit = PID_Y.ProportionalLimit;
            PositionPidSetup.I_y_Limit = PID_Y.IntegralLimit;
            PositionPidSetup.D_y_Limit = PID_Y.DerivationLimit;
            PositionPidSetup.P_theta_Limit = PID_Theta.ProportionalLimit;
            PositionPidSetup.I_theta_Limit = PID_Theta.IntegralLimit;
            PositionPidSetup.D_theta_Limit = PID_Theta.DerivationLimit;

            OnMessageToDisplayPositionPidSetup(PositionPidSetup);
        }

        void CalculateGhostPosition(double ElapsedTimeBetweenCalculation)
        {

            TrajectoryGeneratorMonitor.TrajectoryGeneratorReceived();
            if (wayPointLocation == null)
                return;
            if (ghostLocationRefTerrain == null)
                return;

            /************************* Début du calcul préliminaire des infos utilisées ensuite ****************************/
            
            //Calcul du cap du Waypoint dans les référentiel terrain et robot
            double CapWayPointRefTerrain;
            if (wayPointLocation.X - ghostLocationRefTerrain.X != 0)
                CapWayPointRefTerrain = Math.Atan2(wayPointLocation.Y - ghostLocationRefTerrain.Y, wayPointLocation.X - ghostLocationRefTerrain.X);
            else
                CapWayPointRefTerrain = Math.Atan2(wayPointLocation.Y - ghostLocationRefTerrain.Y, 0.0001);

            //double CapWayPointRefRobot = CapWayPointRefTerrain - ghostLocation.Theta;
            CapWayPointRefTerrain = Toolbox.ModuloByAngle(capVitesseRefTerrain, CapWayPointRefTerrain);

            //Calcul de l'écart de cap
            double ecartCapVitesse = CapWayPointRefTerrain - capVitesseRefTerrain;

            ghostLocationRefTerrain.Theta = Toolbox.ModuloByAngle(wayPointLocation.Theta, ghostLocationRefTerrain.Theta);
            double ecartOrientationRobot = wayPointLocation.Theta - ghostLocationRefTerrain.Theta;
            
            //Calcul de la distance au WayPoint
            double distanceWayPoint = Math.Sqrt(Math.Pow(wayPointLocation.Y - ghostLocationRefTerrain.Y, 2) + Math.Pow(wayPointLocation.X - ghostLocationRefTerrain.X, 2));
            if (distanceWayPoint < 0.05)
                distanceWayPoint = 0;

            //Calcul de la vitesse linéaire du robot
            double vitesseLineaireRobot = Math.Sqrt(Math.Pow(ghostLocationRefTerrain.Vx, 2) + Math.Pow(ghostLocationRefTerrain.Vy, 2));

            //Calacul de la distance de freinage 
            double distanceFreinageLineaire = Math.Pow(vitesseLineaireRobot, 2) / (2 * accelLineaireMax);


            /* Fin du calcul des variéables intermédiaires */

            /************************ Ajustement de la vitesse linéaire du robot *******************************/
            // Si le robot a un cap vitesse à peu près aligné sur son Waypoint ou une vitesse presque nulle 
            // et que la distance au Waypoint est supérieure à la distance de freinage : on accélère en linéaire
            // sinon on freine
            double nouvelleVitesseLineaire;
            if (Math.Abs(ecartCapVitesse) < Math.PI / 2) //Le WayPoint est devant
            {
                if (distanceWayPoint > distanceFreinageLineaire)
                    nouvelleVitesseLineaire = Math.Min(vitesseLineaireMax, vitesseLineaireRobot + accelLineaireMax * ElapsedTimeBetweenCalculation); //On accélère
                else
                    //On détermine la valeur du freinage en fonction des conditions
                    nouvelleVitesseLineaire = Math.Max(0, vitesseLineaireRobot - accelLineaireMax * ElapsedTimeBetweenCalculation); //On freine
            }
            else //Le WayPoint est derrière
            {
                if (distanceWayPoint > distanceFreinageLineaire)
                    nouvelleVitesseLineaire = Math.Max(-vitesseLineaireMax, vitesseLineaireRobot - accelLineaireMax * ElapsedTimeBetweenCalculation); //On accélère
                else
                    //On détermine la valeur du freinage en fonction des conditions
                    nouvelleVitesseLineaire = Math.Min(0, vitesseLineaireRobot + accelLineaireMax * ElapsedTimeBetweenCalculation); //On freine
            }

            double ecartCapModuloPi = Toolbox.ModuloPiAngleRadian(ecartCapVitesse);

            /************************ Rotation du vecteur vitesse linéaire du robot *******************************/
            //Si le robot a un écart de cap vitesse supérieur à l'angle de freinage en rotation de cap vitesse, on accélère la rotation, sinon on freine
            double angleArretRotationCapVitesse = Math.Pow(vitesseRotationCapVitesse, 2) / (2 * accelRotationCapVitesseMax);
            if (ecartCapModuloPi > 0)
            {
                if (ecartCapModuloPi > angleArretRotationCapVitesse)
                    vitesseRotationCapVitesse = Math.Min(vitesseRotationCapVitesseMax, vitesseRotationCapVitesse + accelRotationCapVitesseMax * ElapsedTimeBetweenCalculation); //on accélère
                else
                    vitesseRotationCapVitesse = Math.Max(0, vitesseRotationCapVitesse - accelRotationCapVitesseMax * ElapsedTimeBetweenCalculation); //On freine
            }
            else
            {
                if (ecartCapModuloPi < -angleArretRotationCapVitesse)
                    vitesseRotationCapVitesse = Math.Max(-vitesseRotationCapVitesseMax, vitesseRotationCapVitesse - accelRotationCapVitesseMax * ElapsedTimeBetweenCalculation); //On accélère en négatif
                else
                    vitesseRotationCapVitesse = Math.Min(0, vitesseRotationCapVitesse + accelRotationCapVitesseMax * ElapsedTimeBetweenCalculation); //On freine en négatif
            }

            //On regarde si la vitesse linéaire est élevée ou pas. 
            //Si c'est le cas, on update le cap vitesse normalement en rampe
            //Sinon, on set le capvitesse à la valeur du cap WayPoint directement
            if (vitesseLineaireRobot > 0.5)
                capVitesseRefTerrain += vitesseRotationCapVitesse * ElapsedTimeBetweenCalculation;
            else
            {
                capVitesseRefTerrain = CapWayPointRefTerrain; //Si la vitesse linéaire est faible, on tourne instantanément
                vitesseRotationCapVitesse = 0;
            }

            //On regarde si la vitesse linéaire est négative, on la repasse en positif en ajoutant PI au cap Vitesse
            if (nouvelleVitesseLineaire < 0)
            {
                nouvelleVitesseLineaire = -nouvelleVitesseLineaire;
                capVitesseRefTerrain += Math.PI;
                capVitesseRefTerrain = Toolbox.Modulo2PiAngleRad(capVitesseRefTerrain);
            }

            /************************ Orientation angulaire du robot *******************************/
            double angleArretRotationOrientationRobot = Math.Pow(ghostLocationRefTerrain.Vtheta, 2) / (2 * accelRotationOrientationRobotMax);
            double nouvelleVitesseRotationOrientationRobot = 0;
            if (ecartOrientationRobot > 0)
            {
                if (ecartOrientationRobot > angleArretRotationOrientationRobot)
                    nouvelleVitesseRotationOrientationRobot = Math.Min(vitesseRotationOrientationRobotMax, ghostLocationRefTerrain.Vtheta + accelRotationOrientationRobotMax * ElapsedTimeBetweenCalculation); //on accélère
                else
                    nouvelleVitesseRotationOrientationRobot = Math.Max(0, ghostLocationRefTerrain.Vtheta - accelRotationOrientationRobotMax * ElapsedTimeBetweenCalculation); //On freine
            }
            else
            {
                if (ecartOrientationRobot < -angleArretRotationOrientationRobot)
                    nouvelleVitesseRotationOrientationRobot = Math.Max(-vitesseRotationOrientationRobotMax, ghostLocationRefTerrain.Vtheta - accelRotationOrientationRobotMax * ElapsedTimeBetweenCalculation); //On accélère en négatif
                else
                    nouvelleVitesseRotationOrientationRobot = Math.Min(0, ghostLocationRefTerrain.Vtheta + accelRotationOrientationRobotMax * ElapsedTimeBetweenCalculation); //On freine en négatif
            }

            /************************ Gestion des ordres d'arrêt global des robots *******************************/
            if (currentGameState != GameState.STOPPED)
            {
                //On génère les vitesses dans le référentiel du robot.
                ghostLocationRefTerrain.Vx = nouvelleVitesseLineaire * Math.Cos(capVitesseRefTerrain);
                ghostLocationRefTerrain.Vy = nouvelleVitesseLineaire * Math.Sin(capVitesseRefTerrain);
                ghostLocationRefTerrain.Vtheta = nouvelleVitesseRotationOrientationRobot;

                ////Test : à supprimer ensuite
                //ghostLocationRefTerrain.Vx = 1.0;
                //ghostLocationRefTerrain.Vy = 0.0;
                //ghostLocationRefTerrain.Vtheta = 1.0;

                //Nouvelle orientation du robot
                ghostLocationRefTerrain.X += ghostLocationRefTerrain.Vx * ElapsedTimeBetweenCalculation;
                ghostLocationRefTerrain.Y += ghostLocationRefTerrain.Vy * ElapsedTimeBetweenCalculation;
                ghostLocationRefTerrain.Theta += ghostLocationRefTerrain.Vtheta * ElapsedTimeBetweenCalculation;
            }
            else
            {
                //Si on est à l'arrêt, on ne change rien
            }

            OnGhostLocation(robotId, ghostLocationRefTerrain);
        }
        
        //Output events
        public event EventHandler<PolarSpeedArgs> OnSpeedConsigneEvent;
        public virtual void OnSpeedConsigneToRobot(int id, float vx, float vy, float vtheta)
        {
            var handler = OnSpeedConsigneEvent;
            if (handler != null)
            {
                handler(this, new PolarSpeedArgs { RobotId = id, Vx = vx, Vy = vy, Vtheta = vtheta });
            }
        }

        public event EventHandler<CollisionEventArgs> OnCollisionEvent;
        public virtual void OnCollision(int id, Location robotLocationRefTerrain)
        {
            var handler = OnCollisionEvent;
            if (handler != null)
            {
                handler(this, new CollisionEventArgs { RobotId = id, RobotRealPositionRefTerrain = robotLocationRefTerrain });
            }
        }


        public event EventHandler<RobotIdEventArgs> OnPidSpeedResetEvent;
        public virtual void OnPidSpeedReset(int id)
        {
            var handler = OnPidSpeedResetEvent;
            if (handler != null)
            {
                handler(this, new RobotIdEventArgs {RobotId = id});
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

        public event EventHandler<PolarPIDSetupArgs> OnMessageToDisplayPositionPidSetupEvent;
        public virtual void OnMessageToDisplayPositionPidSetup(PolarPIDSetupArgs setup)
        {
            OnMessageToDisplayPositionPidSetupEvent?.Invoke(this, setup);
        }

        public event EventHandler<PolarPidCorrectionArgs> OnMessageToDisplayPositionPidCorrectionEvent;
        public virtual void OnMessageToDisplayPositionPidCorrection(PolarPidCorrectionArgs corr)
        {
            OnMessageToDisplayPositionPidCorrectionEvent?.Invoke(this, corr);
        }
    }
}
