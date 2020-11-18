using Constants;
using EventArgsLibrary;
using HeatMap;
using HerkulexManagerNS;
using RefereeBoxAdapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using WorldMap;
using static HerkulexManagerNS.HerkulexEventArgs;

namespace StrategyManager.StrategyEurobotNS
{
    public class StrategyEurobot : StrategyInterface
    {
        public int robotId = 0;
        int teamId = 0;

        public PointD robotDestination = new PointD(0, 0);
        public double robotOrientation = 0;
        public Location robotCurentLocation = new Location(0, 0, 0, 0, 0, 0);
        Stopwatch sw = new Stopwatch();
        Heatmap heatMap;
        System.Timers.Timer configTimer;


        public TaskBrasGauche taskBrasGauche;
        public TaskBrasDroit taskBrasDroit;
        public TaskBrasCentre taskBrasCentre;
        public TaskBrasDrapeau taskBrasDrapeau;
        public TaskBalade taskBalade;
        public TaskDepose taskDepose;
        public TaskWindFlag taskWindFlag;
        public TaskFinDeMatch taskFinDeMatch;
        public TaskPhare taskPhare;
        public TaskDistributeur taskDistributeur;
        TaskStrategy taskStrategy;


        public Equipe Team
        {
            get
            {
                return taskStrategy.playingTeam;
            }
        }
        
        
        public StrategyEurobot(int robotId, int teamId)
        {
            this.teamId = teamId;
            this.robotId = robotId;
            heatMap = new Heatmap(3, 2, (int)Math.Pow(2, 5), 1); //Init HeatMap

            //Initialisation des taches de la stratégie

            //Taches de bas niveau
            taskBrasGauche = new TaskBrasGauche();
            taskBrasGauche.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasGauche.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasGauche.OnMotorCurrentReceive;

            taskBrasDroit = new TaskBrasDroit();
            taskBrasDroit.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasDroit.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasDroit.OnMotorCurrentReceive;

            taskBrasCentre = new TaskBrasCentre();
            taskBrasCentre.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasCentre.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasCentre.OnMotorCurrentReceive;

            taskBrasDrapeau = new TaskBrasDrapeau();
            taskBrasDrapeau.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasDrapeau.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasDrapeau.OnMotorCurrentReceive;

            taskBalade = new TaskBalade(this);
            OnMotorCurrentReceiveForwardEvent += taskBalade.OnMotorCurrentReceive;
            taskBalade.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;

            taskDepose = new TaskDepose(this);
            OnMotorCurrentReceiveForwardEvent += taskDepose.OnMotorCurrentReceive;
            taskDepose.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;

            taskWindFlag = new TaskWindFlag(this);
            taskWindFlag.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskWindFlag.OnMotorCurrentReceive;
            taskWindFlag.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;

            taskStrategy = new TaskStrategy(this);
            OnIOValuesEvent += taskStrategy.OnIOValuesFromRobotEvent;
            taskStrategy.OnMirrorModeEvent += OnMirrorMode;

            taskFinDeMatch = new TaskFinDeMatch(this);
            taskFinDeMatch.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskFinDeMatch.OnMotorCurrentReceive;
            taskFinDeMatch.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;

            taskPhare = new TaskPhare(this);
            taskPhare.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskPhare.OnMotorCurrentReceive;
            taskPhare.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;

            taskDistributeur = new TaskDistributeur(this);
            taskDistributeur.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskDistributeur.OnPilotageVentouseEvent += OnPilotageVentouseForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskDistributeur.OnMotorCurrentReceive;

            //On initialisae le timer de réglage récurrent 
            //Il permet de modifier facilement les paramètre des asservissement durant l'exécution
            configTimer = new System.Timers.Timer(1000);
            configTimer.Elapsed += ConfigTimer_Elapsed;
            configTimer.Start();

            OnEnableDisableMotorCurrentData(true);
        }

        public void EvaluateStrategy()
        {
            CalculateDestination();
        }

        public void CalculateDestination()
        {
            //TestGPU.ActionWithClosure();
            sw.Reset();
            sw.Start(); // début de la mesure

            //Génération de la HeatMap
            heatMap.ReInitHeatMapData();

            double optimizedAreaSize;
            PointD OptimalPosition = new PointD(0, 0);
            PointD OptimalPosInBaseHeatMapCoordinates = heatMap.GetBaseHeatMapPosFromFieldCoordinates(0, 0);

            //Réglage des inputs de la heatmap
            //On set la destination souhaitée
            heatMap.SetPreferedDestination((float)robotDestination.X, (float)robotDestination.Y);
            //Génération de la heatmap
            heatMap.GenerateHeatMap(heatMap.BaseHeatMapData, heatMap.nbCellInBaseHeatMapWidth, heatMap.nbCellInBaseHeatMapHeight, (float)heatMap.FieldLength, (float)heatMap.FieldHeight);
            OptimalPosition = heatMap.GetOptimalPosition();

            //Si la position optimale est très de la cible théorique, on prend la cible théorique
            double seuilPositionnementFinal = 0.1;
            if (Toolbox.Distance(new PointD(robotDestination.X, robotDestination.Y), new PointD(OptimalPosition.X, OptimalPosition.Y)) < seuilPositionnementFinal)
            {
                OptimalPosition = robotDestination;
            }

            OnHeatMap(robotId, heatMap);
            OnDestination(robotId, new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, (float)robotOrientation, 0, 0, 0));

            sw.Stop();
        }
        
        public bool isDeplacementFinished
        {
            get
            {
                if (robotOrientation - robotCurentLocation.Theta < Toolbox.DegToRad(1.0) &&
                    Toolbox.Distance(new PointD(robotCurentLocation.X, robotCurentLocation.Y), robotDestination) < 0.05)
                    return true;
                else
                    return false;
            }
            private set
            {

            }
        }


        private void ConfigTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //On envoie périodiquement les réglages du PID de vitesse embarqué
            //OnSetRobotVitessePID(5.0, 0, 0, 5.0, 0, 0, 5.0, 0, 0, 100.0, 0, 0, 100.0, 0, 0, 100.0, 0, 0);
            //OnSetRobotVitessePID(Kpx, Kix, Kdx, Kpy, Kiy, Kdy, KpTheta, KiTheta, KdTheta);

            //OnSetRobotSpeedPolarPID(px:4.0, ix:300, 0.0, py:4.0, iy:300, 0, ptheta:6, itheta:500, 0, 
            //    pxLimit:4.0, ixLimit:4.0, 0, pyLimit:4.0, iyLimit:4.0, 0, pthetaLimit:4.0, ithetaLimit:4.0, 0);

            OnSetRobotSpeedIndependantPID(pM1: 4.0, iM1: 300, 0.0, pM2: 4.0, iM2: 300, 0, pM3: 4.0, iM3: 300, 0, pM4: 4.0, iM4: 300, 0.0,
                pM1Limit: 4.0, iM1Limit: 4.0, 0, pM2Limit: 4.0, iM2Limit: 4.0, 0, pM3Limit: 4.0, iM3Limit: 4.0, 0, pM4Limit: 4.0, iM4Limit: 4.0, 0);
            //OnSetRobotSpeedIndependantPID(pM1: 4.1, iM1: 0, 0.0, pM2: 4.2, iM2: 0, 0, pM3: 4.3, iM3: 0, 0, pM4: 4.4, iM4: 0, 0.0,
            //    pM1Limit: 3.1, iM1Limit: 2.1, 0, pM2Limit: 3.2, iM2Limit: 2.2, 0, pM3Limit: 3.3, iM3Limit: 2.3, 0, pM4Limit: 3.4, iM4Limit: 2.4, 0);

            OnSetAsservissementMode((byte)AsservissementMode.Independant);
        }


        /********************************** Events entrants ********************************************/
        public event EventHandler<BoolEventArgs> OnEnableDisableMotorCurrentDataEvent;
        public virtual void OnEnableDisableMotorCurrentData(bool val)
        {
            OnEnableDisableMotorCurrentDataEvent?.Invoke(this, new BoolEventArgs { value = val });
        }

        public event EventHandler<MotorsCurrentsEventArgs> OnMotorCurrentReceiveForwardEvent;
        public void OnMotorCurrentReceive(object sender, MotorsCurrentsEventArgs e)
        {
            //Forward event to task on low level
            OnMotorCurrentReceiveForwardEvent?.Invoke(sender, e);
        }

        //On fait juste un forward d'event sans le récupérer localement
        public event EventHandler<HerkulexPositionsArgs> OnHerkulexPositionRequestEvent;
        public void OnHerkulexPositionRequestForwardEvent(object sender, HerkulexPositionsArgs e)
        {
            OnHerkulexPositionRequestEvent?.Invoke(sender, e);
        }

        public event EventHandler<IOValuesEventArgs> OnIOValuesEvent;
        public void OnIOValuesFromRobotEvent(object sender, IOValuesEventArgs e)
        {
            OnIOValuesEvent?.Invoke(sender, e);
        }

        public event EventHandler<SpeedConsigneToMotorArgs> OnSetSpeedConsigneToMotor;
        public virtual void OnPilotageVentouseForwardEvent(object sender, SpeedConsigneToMotorArgs e)
        {
            OnSetSpeedConsigneToMotor?.Invoke(sender, e);
        }

        public void OnPositionRobotReceived(object sender, LocationArgs location)
        {
            robotCurentLocation.X = location.Location.X;
            robotCurentLocation.Y = location.Location.Y;
            robotCurentLocation.Theta = location.Location.Theta;

            robotCurentLocation.Vx = location.Location.Vx;
            robotCurentLocation.Vy = location.Location.Vy;
            robotCurentLocation.Vtheta = location.Location.Vtheta;
        }

        

        public event EventHandler<PolarPIDSetupArgs> OnSetRobotSpeedPolarPIDEvent;
        public virtual void OnSetRobotSpeedPolarPID(double px, double ix, double dx, double py, double iy, double dy, double ptheta, double itheta, double dtheta,
            double pxLimit, double ixLimit, double dxLimit, double pyLimit, double iyLimit, double dyLimit, double pthetaLimit, double ithetaLimit, double dthetaLimit
            )
        {
            OnSetRobotSpeedPolarPIDEvent?.Invoke(this, new PolarPIDSetupArgs
            {
                P_x = px,
                I_x = ix,
                D_x = dx,
                P_y = py,
                I_y = iy,
                D_y = dy,
                P_theta = ptheta,
                I_theta = itheta,
                D_theta = dtheta,
                P_x_Limit = pxLimit,
                I_x_Limit = ixLimit,
                D_x_Limit = dxLimit,
                P_y_Limit = pyLimit,
                I_y_Limit = iyLimit,
                D_y_Limit = dyLimit,
                P_theta_Limit = pthetaLimit,
                I_theta_Limit = ithetaLimit,
                D_theta_Limit = dthetaLimit
            });
        }

        public event EventHandler<IndependantPIDSetupArgs> OnSetRobotSpeedIndependantPIDEvent;
        public virtual void OnSetRobotSpeedIndependantPID(double pM1, double iM1, double dM1, double pM2, double iM2, double dM2, double pM3, double iM3, double dM3, double pM4, double iM4, double dM4,
            double pM1Limit, double iM1Limit, double dM1Limit, double pM2Limit, double iM2Limit, double dM2Limit, double pM3Limit, double iM3Limit, double dM3Limit, double pM4Limit, double iM4Limit, double dM4Limit
            )
        {
            OnSetRobotSpeedIndependantPIDEvent?.Invoke(this, new IndependantPIDSetupArgs
            {
                P_M1 = pM1,
                I_M1 = iM1,
                D_M1 = dM1,
                P_M2 = pM2,
                I_M2 = iM2,
                D_M2 = dM2,
                P_M3 = pM3,
                I_M3 = iM3,
                D_M3 = dM3,
                P_M4 = pM4,
                I_M4 = iM4,
                D_M4 = dM4,
                P_M1_Limit = pM1Limit,
                I_M1_Limit = iM1Limit,
                D_M1_Limit = dM1Limit,
                P_M2_Limit = pM2Limit,
                I_M2_Limit = iM2Limit,
                D_M2_Limit = dM2Limit,
                P_M3_Limit = pM3Limit,
                I_M3_Limit = iM3Limit,
                D_M3_Limit = dM3Limit,
                P_M4_Limit = pM4Limit,
                I_M4_Limit = iM4Limit,
                D_M4_Limit = dM4Limit
            });
        }


        public event EventHandler<ByteEventArgs> OnSetAsservissementModeEvent;
        public virtual void OnSetAsservissementMode(byte val)
        {
            OnSetAsservissementModeEvent?.Invoke(this, new ByteEventArgs { Value = val });
        }

        public event EventHandler<BoolEventArgs> OnEnableMotorsEvent;
        public virtual void OnEnableMotors(bool val)
        {
            OnEnableMotorsEvent?.Invoke(this, new BoolEventArgs { value = val });
        }

        public event EventHandler<BoolEventArgs> OnMirrorModeForwardEvent;
        public virtual void OnMirrorMode(object sender, BoolEventArgs val)
        {
            OnMirrorModeForwardEvent?.Invoke(sender, val);
        }


        public event EventHandler<LocationArgs> OnDestinationEvent;
        public virtual void OnDestination(int id, Location location)
        {
            var handler = OnDestinationEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = location });
            }
        }

        //Output events
        public event EventHandler<RefBoxMessageArgs> OnRefereeBoxCommandEvent;
        public virtual void OnRefereeBoxReceivedCommand(RefBoxMessage msg)
        {
            OnRefereeBoxCommandEvent?.Invoke(this, new RefBoxMessageArgs { refBoxMsg = msg });
        }

        public event EventHandler<CollisionEventArgs> OnCollisionEvent;
        public virtual void OnCollision(int id, Location robotLocation)
        {
            OnCollisionEvent?.Invoke(this, new CollisionEventArgs { RobotId = id, RobotRealPosition = robotLocation });
        }

        public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        public virtual void OnHeatMap(int id, Heatmap heatMap)
        {
            OnHeatMapEvent?.Invoke(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
        }

        public event EventHandler<GameStateArgs> OnGameStateChangedEvent;
        public virtual void OnGameStateChanged(int robotId, GameState state)
        {
            OnGameStateChangedEvent?.Invoke(this, new GameStateArgs { RobotId = robotId, gameState = state });
        }


        public event EventHandler<LidarMessageArgs> OnMessageEvent;
        public virtual void OnLidarMessage(string message, int line)
        {
            OnMessageEvent?.Invoke(this, new LidarMessageArgs { Value = message, Line = line });
        }

        public Dictionary<ServoId, Servo> HerkulexServos = new Dictionary<ServoId, Servo>();
        int counterServo = 0;
        public void OnHerkulexServoInformationReceived(object sender, HerkulexEventArgs.HerkulexServoInformationArgs e)
        {
            lock (HerkulexServos)
            {
                if (HerkulexServos.ContainsKey(e.Servo.GetID()))
                {
                    HerkulexServos[e.Servo.GetID()] = e.Servo;
                }
                else
                {
                    HerkulexServos.Add(e.Servo.GetID(), e.Servo);
                }
            }
        }
    }
}
