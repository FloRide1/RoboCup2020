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

        double _obstacleAvoidanceDistance = 0.2;
        public override double ObstacleAvoidanceDistance
        {
            get { return _obstacleAvoidanceDistance; }
            set { _obstacleAvoidanceDistance = value; }
        }
        public GameState gameState = GameState.STOPPED;
        public StoppedGameAction stoppedGameAction = StoppedGameAction.NONE;
        public Location externalRefBoxPosition = new Location();

        PlayingSide playingSide = PlayingSide.Left;

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

        public override void InitStrategy(int robotId, int teamId)
        {
            //Initialisation des taches de la stratégie

            //Taches de bas niveau
            taskBrasGauche = new TaskBrasGauche();
            taskBrasGauche.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasGauche.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasGauche.OnMotorCurrentReceive;

            taskBrasDroit = new TaskBrasDroit();
            taskBrasDroit.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasDroit.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasDroit.OnMotorCurrentReceive;

            taskBrasCentre = new TaskBrasCentre();
            taskBrasCentre.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasCentre.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasCentre.OnMotorCurrentReceive;

            taskBrasDrapeau = new TaskBrasDrapeau();
            taskBrasDrapeau.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskBrasDrapeau.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;
            OnMotorCurrentReceiveForwardEvent += taskBrasDrapeau.OnMotorCurrentReceive;

            taskBalade = new TaskBalade(this);
            OnMotorCurrentReceiveForwardEvent += taskBalade.OnMotorCurrentReceive;
            taskBalade.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;

            taskDepose = new TaskDepose(this);
            OnMotorCurrentReceiveForwardEvent += taskDepose.OnMotorCurrentReceive;
            taskDepose.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;

            taskWindFlag = new TaskWindFlag(this);
            taskWindFlag.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskWindFlag.OnMotorCurrentReceive;
            taskWindFlag.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;

            taskStrategy = new TaskStrategy(this);
            OnIOValuesEvent += taskStrategy.OnIOValuesFromRobotEvent;
            taskStrategy.OnMirrorModeEvent += OnMirrorMode;

            taskFinDeMatch = new TaskFinDeMatch(this);
            taskFinDeMatch.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskFinDeMatch.OnMotorCurrentReceive;
            taskFinDeMatch.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;

            taskPhare = new TaskPhare(this);
            taskPhare.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            OnMotorCurrentReceiveForwardEvent += taskPhare.OnMotorCurrentReceive;
            taskPhare.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;

            taskDistributeur = new TaskDistributeur(this);
            taskDistributeur.OnHerkulexPositionRequestEvent += OnHerkulexPositionRequestForwardEvent;
            taskDistributeur.OnPilotageVentouseEvent += OnSetSpeedConsigneToMotorEvent;
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
                if (robotOrientation - robotCurrentLocation.Theta < Toolbox.DegToRad(1.0) &&
                    Toolbox.Distance(new PointD(robotCurrentLocation.X, robotCurrentLocation.Y), robotDestination) < 0.05)
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
            
            OnSetRobotSpeedIndependantPID(pM1: 4.0, iM1: 300, 0.0, pM2: 4.0, iM2: 300, 0, pM3: 4.0, iM3: 300, 0, pM4: 4.0, iM4: 300, 0.0,
                pM1Limit: 4.0, iM1Limit: 4.0, 0, pM2Limit: 4.0, iM2Limit: 4.0, 0, pM3Limit: 4.0, iM3Limit: 4.0, 0, pM4Limit: 4.0, iM4Limit: 4.0, 0);
            
            OnSetAsservissementMode((byte)AsservissementMode.Independant);
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
        }

        public override void OnRefBoxMsgReceived(object sender, WorldMap.RefBoxMessageArgs e)
        {
            var command = e.refBoxMsg.command;
            var targetTeam = e.refBoxMsg.targetTeam;

            switch (command)
            {
                case RefBoxCommand.START:
                    gameState = GameState.PLAYING;
                    stoppedGameAction = StoppedGameAction.NONE;
                    break;
                case RefBoxCommand.STOP:
                    gameState = GameState.STOPPED;
                    break;                
                case RefBoxCommand.GOTO:
                    if (e.refBoxMsg.robotID == robotId)
                    {
                        gameState = GameState.STOPPED_GAME_POSITIONING;
                        externalRefBoxPosition = new Location(e.refBoxMsg.posX, e.refBoxMsg.posY, e.refBoxMsg.posTheta, 0, 0, 0);
                        if (targetTeam == teamIpAddress)
                            stoppedGameAction = StoppedGameAction.GOTO;
                        else
                            stoppedGameAction = StoppedGameAction.GOTO_OPPONENT;
                    }
                    else
                    {

                    }
                    break;
                case RefBoxCommand.PLAYLEFT:
                    //currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        playingSide = PlayingSide.Left;
                    else
                        playingSide = PlayingSide.Right;
                    break;
                case RefBoxCommand.PLAYRIGHT:
                    //currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        playingSide = PlayingSide.Right;
                    else
                        playingSide = PlayingSide.Left;
                    break;
            }
        }

        public override void DetermineRobotRole() //A définir dans les classes héritées
        {
            switch (gameState)
            {
                case GameState.STOPPED:
                    role = RobotRole.Stopped;
                    break;
                case GameState.PLAYING:
                    {
                    }
                    break;
                case GameState.STOPPED_GAME_POSITIONING:
                    role = RobotRole.Positioning;
                    break;
            }
            DefinePlayerZones(role);
        }


        public void DefinePlayerZones(RobotRole role)
        {
            switch (role)
            {
                case RobotRole.Positioning:
                    AddPreferedZone(new PointD(externalRefBoxPosition.X, externalRefBoxPosition.Y), 0.3);
                    robotOrientation = 0;
                    break;
            }
        }

        public override void IterateStateMachines() //A définir dans les classes héritées
        {
            ;
        }
        public override void InitHeatMap()
        {
            strategyHeatMap = new Heatmap(3.0, 2.0, (int)Math.Pow(2, 9)); //Init HeatMap Strategy Eurobot
            WayPointHeatMap = new Heatmap(3.0, 2.0, (int)Math.Pow(2, 9)); //Init HeatMap WayPoint Eurobot
        }       
        

        /****************************************** Events envoyés ***********************************************/
        //On fait juste un forward d'event sans le récupérer localement
        public event EventHandler<HerkulexPositionsArgs> OnHerkulexPositionRequestEvent;
        public void OnHerkulexPositionRequestForwardEvent(object sender, HerkulexPositionsArgs e)
        {
            OnHerkulexPositionRequestEvent?.Invoke(sender, e);
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
        

        

        public event EventHandler<MotorsCurrentsEventArgs> OnMotorCurrentReceiveForwardEvent;
        public void OnMotorCurrentReceive(object sender, MotorsCurrentsEventArgs e)
        {
            //Forward event to task on low level
            OnMotorCurrentReceiveForwardEvent?.Invoke(sender, e);
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
