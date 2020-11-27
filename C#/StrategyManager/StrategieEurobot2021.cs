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

namespace StrategyManagerNS
{
    /****************************************************************************/
    /// <summary>
    /// Il y a un Strategy Manager par robot, qui partage la même Global World Map -> les stratégies collaboratives sont possibles
    /// Le Strategy Manager a pour rôle de déterminer les déplacements et les actions du robot auquel il appartient
    /// 
    /// Il implante implante à minima le schéma de fonctionnement suivant
    /// - Récupération asynchrone de la Global World Map décrivant l'état du monde autour du robot
    ///     La Global World Map inclus en particulier l'état du jeu (à voir pour changer cela)
    /// - Sur Timer Strategy : détermination si besoin du rôle du robot :
    ///         - simple si Eurobot car les rôles sont figés
    ///         - complexe dans le cas de la RoboCup car les rôles sont changeant en fonction des positions et du contexte.
    /// - Sur Timer Strategy : Itération des machines à état de jeu définissant les déplacements et actions
    ///         - implante les machines à état de jeu à Eurobot, ainsi que les règles spécifiques 
    ///         de jeu (déplacement max en controlant le ballon par exemple à la RoboCup).
    ///         - implante les règles de mise à jour 
    ///             des zones préférentielles de destination (par exemple la balle pour le joueur qui la conteste à la RoboCup), 
    ///             des zones interdites (par exemple les zones de départ à Eurobot), d
    ///             es zones à éviter (par exemple pour se démarquer à la RoboCup)...
    /// - DONE - Sur Timer Strategy : génération de la HeatMap de positionnement X Y donnant l'indication d'intérêt de chacun des points du terrain
    ///     et détermination de la destination théorique (avant inclusion des masquages waypoint)
    /// - DONE - Sur Timer Strategy : prise en compte de la osition des obstacles pour générer la HeatMap de WayPoint 
    ///     et trouver le WayPoint courant.
    /// - Sur Timer Strategy : gestion des actions du robot en fonction du contexte
    ///     Il est à noter que la gestion de l'orientation du robot (différente du cap en déplacement de celui-ci)
    ///     est considérée comme une action, et non comme un déplacement car celle-ci dépend avant tout du contexte du jeu
    ///     et non pas de la manière d'aller à un point.
    /// </summary>

    /****************************************************************************/
    public class StrategyEurobot2021 : StrategyGenerique
    {
        public string DisplayName;

        public PointD robotDestination = new PointD(0, 0);

        public GameState gameState = GameState.STOPPED;
        public StoppedGameAction stoppedGameAction = StoppedGameAction.NONE;
        public Location externalRefBoxPosition = new Location();

        public double robotOrientation;
        public Location robotCurentLocation = new Location(0, 0, 0, 0, 0, 0);
        RobotRole role = RobotRole.Stopped;
        System.Timers.Timer configTimer;


        Stopwatch sw = new Stopwatch();
        //Timer timerStrategy;

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

        public StrategyEurobot2021(int robotId, int teamId, string teamIpAddress) : base(robotId, teamId, teamIpAddress)
        {
            //this.teamId = teamId;
            //this.robotId = robotId;

            globalWorldMap = new GlobalWorldMap();

            InitHeatMap();
            //timerStrategy = new Timer();
            //timerStrategy.Interval = 50;
            //timerStrategy.Elapsed += TimerStrategy_Elapsed;
            //timerStrategy.Start();

        }
        public void  InitStrategy(int robotId, int teamId)
        {
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
        public void DefinePlayerZones(RobotRole role)
        {

        }


        //************************ Events reçus ************************************************/

        //Event de récupération d'une GlobalWorldMap mise à jour
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            //On récupère la nouvelle worldMap
            lock (globalWorldMap)
            {
                globalWorldMap = e.GlobalWorldMap;
            }

            ////On regarde si le gamestate a changé
            //if (globalWorldMap.gameState != gameState_1)
            //{
            //    //Le gameState a changé, on envoie un event
            //    OnGameStateChanged(robotId, globalWorldMap.gameState);
            //}
        }

        public override void OnRefBoxMsgReceived(object sender, WorldMap.RefBoxMessageArgs e)
        {
            var command = e.refBoxMsg.command;
            var robotId = e.refBoxMsg.robotID;
            var targetTeam = e.refBoxMsg.targetTeam;

            switch (command)
            {
                case RefBoxCommand.GOTO:
                    externalRefBoxPosition = new Location(e.refBoxMsg.posX, e.refBoxMsg.posY, e.refBoxMsg.posTheta, 0, 0, 0);
                    break;
            }
        }

        public override void DetermineRobotRole() //A définir dans les classes héritées
        {
            DefinePlayerZones(RobotRole.Eurobot_gros_robot);
        }

        public override void IterateStateMachines() //A définir dans les classes héritées
        {
            ;
        }
        public override void InitHeatMap()
        {
            positioningHeatMap = new Heatmap(3.0, 2.0, (int)Math.Pow(2, 9)); //Init HeatMap Eurobot
        }       
        

        /****************************************** Events envoyés ***********************************************/
        //On fait juste un forward d'event sans le récupérer localement
        public event EventHandler<HerkulexPositionsArgs> OnHerkulexPositionRequestEvent;
        public void OnHerkulexPositionRequestForwardEvent(object sender, HerkulexPositionsArgs e)
        {
            OnHerkulexPositionRequestEvent?.Invoke(sender, e);
        }

        public event EventHandler<ByteEventArgs> OnSetAsservissementModeEvent;
        public virtual void OnSetAsservissementMode(byte val)
        {
            OnSetAsservissementModeEvent?.Invoke(this, new ByteEventArgs { Value = val });
        }
        public event EventHandler<BoolEventArgs> OnEnableDisableMotorCurrentDataEvent;
        public virtual void OnEnableDisableMotorCurrentData(bool val)
        {
            OnEnableDisableMotorCurrentDataEvent?.Invoke(this, new BoolEventArgs { value = val });
        }
        public event EventHandler<IOValuesEventArgs> OnIOValuesEvent;
        public void OnIOValuesFromRobotEvent(object sender, IOValuesEventArgs e)
        {
            OnIOValuesEvent?.Invoke(sender, e);
        }
        public event EventHandler<BoolEventArgs> OnEnableMotorsEvent;
        public virtual void OnEnableMotors(bool val)
        {
            OnEnableMotorsEvent?.Invoke(this, new BoolEventArgs { value = val });
        }

        public event EventHandler<SpeedConsigneToMotorArgs> OnSetSpeedConsigneToMotor;
        public virtual void OnPilotageVentouseForwardEvent(object sender, SpeedConsigneToMotorArgs e)
        {
            OnSetSpeedConsigneToMotor?.Invoke(sender, e);
        }
        public event EventHandler<BoolEventArgs> OnMirrorModeForwardEvent;
        public virtual void OnMirrorMode(object sender, BoolEventArgs val)
        {
            OnMirrorModeForwardEvent?.Invoke(sender, val);
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

        public event EventHandler<MotorsCurrentsEventArgs> OnMotorCurrentReceiveForwardEvent;
        public void OnMotorCurrentReceive(object sender, MotorsCurrentsEventArgs e)
        {
            //Forward event to task on low level
            OnMotorCurrentReceiveForwardEvent?.Invoke(sender, e);
        }
        public event EventHandler<CollisionEventArgs> OnCollisionEvent;
        public virtual void OnCollision(int id, Location robotLocation)
        {
            OnCollisionEvent?.Invoke(this, new CollisionEventArgs { RobotId = id, RobotRealPositionRefTerrain = robotLocation });
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

        //Output events
        public event EventHandler<RefBoxMessageArgs> OnRefereeBoxCommandEvent;
        public virtual void OnRefereeBoxReceivedCommand(RefBoxMessage msg)
        {
            OnRefereeBoxCommandEvent?.Invoke(this, new RefBoxMessageArgs { refBoxMsg = msg });
        }

    }
}
