using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Utilities;
using WorldMap;
using HeatMap;
using System.Diagnostics;
using PerceptionManagement;
using System.Timers;
using Constants;
using System.Threading;
using static HerkulexManagerNS.HerkulexEventArgs;
using RefereeBoxAdapter;
using HerkulexManagerNS;

namespace StrategyManager
{
    public class StrategyManager_Eurobot
    {
        public int robotId = 0;
        int teamId = 0;
        public Equipe Team
        {
            get
            {
                return taskStrategy.playingTeam;
            }
        }
        
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();
        Heatmap heatMap;
        Stopwatch sw = new Stopwatch();

        PlayerRole robotRole = PlayerRole.Stop;

        public PointD robotDestination = new PointD(0, 0);
        public double robotOrientation = 0;
        public Location robotCurentLocation = new Location(0,0,0,0,0,0);
        
        //System.Timers.Timer timerStrategy;

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


        System.Timers.Timer configTimer;

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
        enum TaskStrategyState
        {
            Init,
            Attente,
            ChasseAuxGobelets,
            Finished
        }

        public StrategyManager_Eurobot(int robotId, int teamId)
        {
            this.teamId = teamId;
            this.robotId = robotId;
            //heatMap = new Heatmap(22.0, 14.0, 22.0/Math.Pow(2,8), 2); //Init HeatMap
            heatMap = new Heatmap(3, 2, (int)Math.Pow(2, 5), 1); //Init HeatMap

            //OnSetRobotVitessePID(50, 100, 0, 50, 100, 0, 50, 100, 0,
            //2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 5.0, 5.0, 5.0);

            //OnSetRobotVitessePID(
            //    200, 200, 0,
            //    200, 200, 0,
            //    80, 100, 0,
            //2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0);



            OnGameStateChanged(robotId, globalWorldMap.gameState);


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

            //Thread GameManagementThread = new Thread(ThreadManagementTask);
            //GameManagementThread.IsBackground = true;
            //GameManagementThread.Start();

        }


        public void Init()
        {
            //taskStrategy.Init();
            //taskBrasGauche.Init();
            //taskBrasDroit.Init();
            //taskBrasCentre.Init();
            //taskBrasDrapeau.Init();
            //taskWindFlag.Init();
            //taskFinDeMatch.Init();

            configTimer = new System.Timers.Timer(1000);
            configTimer.Elapsed += ConfigTimer_Elapsed;
            configTimer.Start();


            OnEnableDisableMotorCurrentData(true);

        }

        private void ConfigTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //On envoie périodiquement les réglages du PID de vitesse embarqué
            //OnSetRobotVitessePID(5.0, 0, 0, 5.0, 0, 0, 5.0, 0, 0, 100.0, 0, 0, 100.0, 0, 0, 100.0, 0, 0);
            //OnSetRobotVitessePID(Kpx, Kix, Kdx, Kpy, Kiy, Kdy, KpTheta, KiTheta, KdTheta);
            OnSetRobotVitessePID(1.0, 0, 0.0, 1.0, 0, 0, ptheta:6, itheta:500, 0, 4.0, 0, 0, 4.0, 0, 0, pthetaLimit:4.0, ithetaLimit:4.0, 0);
        }


        //Events
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
        
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            //On récupère le gameState avant arrivée de la nouvelle worldMap
            GameState gameState_1 = globalWorldMap.gameState;

            //On écupère la nouvelle worldMap
            globalWorldMap = e.GlobalWorldMap;

            //On regarde si le gamestate a changé
            if (globalWorldMap.gameState != gameState_1)
            {
                //Le gameState a changé, on envoie un event
                OnGameStateChanged(robotId, globalWorldMap.gameState);
            }
            
            //Le joueur détermine sa stratégie
            SetRobotRole();
            SetRobotDestination(robotRole);
            ProcessStrategy();
        }

        void SetRobotRole()
        {
            //On détermine les distances des joueurs à la balle
            Dictionary<int, double> DictDistancePlayerBall = new Dictionary<int, double>();
            var ballLocationList = globalWorldMap.ballLocationList;
            foreach (var player in globalWorldMap.teammateLocationList)
            {
                //On exclut le gardien
                if (player.Key != (int)TeamId.Team1 + (int)Constants.RobotId.Robot1 && player.Key != (int)TeamId.Team2 + (int)Constants.RobotId.Robot1)
                {
                    DictDistancePlayerBall.Add(player.Key, Toolbox.Distance(new PointD(player.Value.X, player.Value.Y), new PointD(ballLocationList[0].X, ballLocationList[0].Y)));
                }
            }

            var OrderedDictDistancePlayerBall = DictDistancePlayerBall.OrderBy(p => p.Value);
            for (int i = 0; i < OrderedDictDistancePlayerBall.Count(); i++)
            {
                if (OrderedDictDistancePlayerBall.ElementAt(i).Key == robotId)
                {
                    switch (i)
                    {
                        case 0:
                            robotRole = PlayerRole.AttaquantAvecBalle;
                            break;
                        case 1:
                            robotRole = PlayerRole.AttaquantPlace;
                            break;
                        case 2:
                            robotRole = PlayerRole.Centre;
                            break;
                        default:
                            robotRole = PlayerRole.Centre;
                            break;
                    }
                }                
            }

            if (robotId == (int)TeamId.Team1 + (int)Constants.RobotId.Robot1 || robotId == (int)TeamId.Team2 + (int)Constants.RobotId.Robot1)
            {
                //Cas du gardien
                robotRole = PlayerRole.Gardien;
            }
        }
        void SetRobotDestination(PlayerRole role)
        {
            switch (globalWorldMap.gameState)
            {
                case GameState.STOPPED:
                    if(globalWorldMap.teammateLocationList.ContainsKey(robotId))
                        robotDestination = new PointD(globalWorldMap.teammateLocationList[robotId].X, globalWorldMap.teammateLocationList[robotId].Y);
                    break;
                case GameState.PLAYING:
                    //C'est ici qu'il faut calculer les fonctions de cout pour chacun des roles.
                    //switch (role)
                    //{
                    //    case PlayerRole.Stop:
                    //        robotDestination = new PointD(-8, 3);
                    //        break;
                    //    case PlayerRole.Gardien:
                    //        if(teamId == (int)TeamId.Team1)
                    //            robotDestination = new PointD(10.5, 0);
                    //        else
                    //            robotDestination = new PointD(-10.5, 0);
                    //        break;
                    //    case PlayerRole.DefenseurPlace:
                    //        robotDestination = new PointD(-8, 3);
                    //        break;
                    //    case PlayerRole.DefenseurActif:
                    //        robotDestination = new PointD(-8, -3);
                    //        break;
                    //    case PlayerRole.AttaquantPlace:
                    //        robotDestination = new PointD(6, -3);
                    //        break;
                    //    case PlayerRole.AttaquantAvecBalle:
                    //        //if (globalWorldMap.ballLocation != null)
                    //        //    robotDestination = new PointD(globalWorldMap.ballLocation.X, globalWorldMap.ballLocation.Y);
                    //        //else
                    //        //    robotDestination = new PointD(6, 0);
                    //        {
                    //            if (globalWorldMap.ballLocationList.Count > 0)
                    //            {
                    //                var ptInterception = GetInterceptionLocation(new Location(globalWorldMap.ballLocationList[0].X, globalWorldMap.ballLocationList[0].Y, 0, globalWorldMap.ballLocationList[0].Vx, globalWorldMap.ballLocationList[0].Vy, 0), new Location(globalWorldMap.teammateLocationList[robotId].X, globalWorldMap.teammateLocationList[robotId].Y, 0, 0, 0, 0), 3);

                    //                if (ptInterception != null)
                    //                    robotDestination = ptInterception;
                    //                else
                    //                    robotDestination = new PointD(globalWorldMap.ballLocationList[0].X, globalWorldMap.ballLocationList[0].Y);
                    //            }
                    //            else
                    //                robotDestination = new PointD(6, -3);
                    //        }
                    //        break;
                    //    case PlayerRole.Centre:
                    //        robotDestination = new PointD(0, 0);
                    //        break;
                    //    default:
                    //        break;
                    //}
                    break;
                case GameState.STOPPED_GAME_POSITIONING:
                    switch(globalWorldMap.stoppedGameAction)
                    {
                        case StoppedGameAction.KICKOFF:        
                            switch(robotId)
                            {
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(10, 0);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot2:
                                    robotDestination = new PointD(-1, 2);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot3:
                                    robotDestination = new PointD(1, -2);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot4:
                                    robotDestination = new PointD(6, -3);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot5:
                                    robotDestination = new PointD(6, 3);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(-10, 0);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot2:
                                    robotDestination = new PointD(1, 2);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot3:
                                    robotDestination = new PointD(-1, -2);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot4:
                                    robotDestination = new PointD(-6, -3);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot5:
                                    robotDestination = new PointD(-6, 3);
                                    break;
                            }
                            break;

                        case StoppedGameAction.GOTO_0_1:
                            switch (robotId)
                            {
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(0, 1);
                                    robotOrientation = Math.PI / 2;
                                    break;
                            }
                            break;

                        case StoppedGameAction.GOTO_1_0:
                            switch (robotId)
                            {
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(1, 0);
                                    robotOrientation = 0;
                                    break;
                            }
                            break;

                        case StoppedGameAction.GOTO_0_M1:
                            switch (robotId)
                            {
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(0, -1); 
                                    robotOrientation = Math.PI;
                                    break;
                            }
                            break;

                        case StoppedGameAction.GOTO_M1_0:
                            switch (robotId)
                            {
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(-1, 0);
                                    robotOrientation = 3 * Math.PI / 2;
                                    break;
                            }
                            break;

                        case StoppedGameAction.GOTO_0_0:
                            switch (robotId)
                            {
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(0, 0);
                                    robotOrientation = 0;
                                    break;
                            }
                            break;

                        case StoppedGameAction.KICKOFF_OPPONENT:
                            switch (robotId)
                            {
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(10, 0);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot2:
                                    robotDestination = new PointD(1, 2);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot3:
                                    robotDestination = new PointD(1, -2);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot4:
                                    robotDestination = new PointD(6, -3);
                                    break;
                                case (int)TeamId.Team1 + (int)Constants.RobotId.Robot5:
                                    robotDestination = new PointD(6, 3);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot1:
                                    robotDestination = new PointD(-10, 0);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot2:
                                    robotDestination = new PointD(-1, 2);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot3:
                                    robotDestination = new PointD(-1, -2);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot4:
                                    robotDestination = new PointD(-6, -3);
                                    break;
                                case (int)TeamId.Team2 + (int)Constants.RobotId.Robot5:
                                    robotDestination = new PointD(-6, 3);
                                    break;
                            }
                            break;
                    }
                    break;                
            }            
        }



        public void ProcessStrategy()
        {
            //TestGPU.ActionWithClosure();
            sw.Reset();
            sw.Start(); // début de la mesure
                        
            //Génération de la HeatMap
            heatMap.ReInitHeatMapData();
            
            //On construit le heatMap en mode multi-résolution :
            //On commence par une heatmap très peu précise, puis on construit une heat map de taille réduite plus précise autour du point chaud,
            //Puis on construit une heatmap très précise au cm autour du point chaud.
            double optimizedAreaSize;
            PointD OptimalPosition = new PointD(0, 0);
            PointD OptimalPosInBaseHeatMapCoordinates = heatMap.GetBaseHeatMapPosFromFieldCoordinates(0, 0);
            
            ParallelCalculateHeatMap(heatMap.BaseHeatMapData, heatMap.nbCellInBaseHeatMapWidth, heatMap.nbCellInBaseHeatMapHeight, (float)heatMap.FieldLength, (float)heatMap.FieldHeight, (float)robotDestination.X, (float)robotDestination.Y);
            //gpuDll.GpuGenerateHeatMap("GPU_DLL_CUDA.dll", heatMap.BaseHeatMapData, heatMap.nbCellInBaseHeatMapWidth, heatMap.nbCellInBaseHeatMapHeight, (float)heatMap.FieldLength, (float)heatMap.FieldHeight, (float)robotDestination.X, (float)robotDestination.Y);
            //ParallelCalculateHeatMap(heatMap.BaseHeatMapData, heatMap.nbCellInBaseHeatMapWidth, heatMap.nbCellInBaseHeatMapHeight, (float)heatMap.FieldLength, (float)heatMap.FieldHeight, (float)robotDestination.X, (float)robotDestination.Y);

            double[] tabMax = new double[heatMap.nbCellInBaseHeatMapHeight];
            int[] tabIndexMax = new int[heatMap.nbCellInBaseHeatMapHeight];
            Parallel.For(0, heatMap.nbCellInBaseHeatMapHeight, i =>
            //for (int i =0; i< heatMap.nbCellInBaseHeatMapHeight;i++)
            {
                tabMax[i] = 0;
                tabIndexMax[i] = 0;
                for (int j = 0; j < heatMap.nbCellInBaseHeatMapWidth; j++)
                {
                    if (heatMap.BaseHeatMapData[i, j] > tabMax[i])
                    {
                        tabMax[i] = heatMap.BaseHeatMapData[i, j];
                        tabIndexMax[i] = j;
                    }
                }
            });

            //Recherche du maximum
            double max = 0;
            int indexMax = 0;
            for (int i = 0; i < heatMap.nbCellInBaseHeatMapHeight; i++)
            {
                if (tabMax[i] > max)
                {
                    max = tabMax[i];
                    indexMax = i;
                }
            }

            int maxYpos = indexMax;// indexMax % heatMap.nbCellInBaseHeatMapWidth;
            int maxXpos = tabIndexMax[indexMax];// indexMax / heatMap.nbCellInBaseHeatMapWidth;

            OptimalPosInBaseHeatMapCoordinates = new PointD(maxXpos, maxYpos);

            OptimalPosition = heatMap.GetFieldPosFromBaseHeatMapCoordinates(OptimalPosInBaseHeatMapCoordinates.X, OptimalPosInBaseHeatMapCoordinates.Y);
            
            //Si la position optimale est très de la cible théorique, on prend la cible théorique
            double seuilPositionnementFinal = 0.1;
            if (Toolbox.Distance(new PointD(robotDestination.X, robotDestination.Y), new PointD(OptimalPosition.X, OptimalPosition.Y)) < seuilPositionnementFinal)
            {
                OptimalPosition = robotDestination;
            }


            OnHeatMap(robotId, heatMap);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, (float)robotOrientation, 0, 0, 0));

            //heatMap.Dispose();
            sw.Stop(); // Fin de la mesure
            //for (int n = 0; n < nbComputationsList.Length; n++)
            //{
            //    Console.WriteLine("Calcul Strategy - Nb Calculs Etape " + n + " : " + nbComputationsList[n]);
            //}
            //Console.WriteLine("Temps de calcul de la heatMap de stratégie : " + sw.Elapsed.TotalMilliseconds.ToString("N4")+" ms"); // Affichage de la mesure
        }

        //public void ProcessStrategy()
        //{
        //    //TestGPU.ActionWithClosure();
        //    sw.Reset();
        //    sw.Start(); // début de la mesure

        //    //Génération de la HeatMap
        //    heatMap.ReInitHeatMapData();

        //    int[] nbComputationsList = new int[heatMap.nbIterations];

        //    //On construit le heatMap en mode multi-résolution :
        //    //On commence par une heatmap très peu précise, puis on construit une heat map de taille réduite plus précise autour du point chaud,
        //    //Puis on construit une heatmap très précise au cm autour du point chaud.
        //    double optimizedAreaSize;
        //    PointD OptimalPosition = new PointD(0, 0);
        //    PointD OptimalPosInBaseHeatMapCoordinates = heatMap.GetBaseHeatMapPosFromFieldCoordinates(0, 0);

        //    for (int n = 0; n < heatMap.nbIterations; n++)
        //    {
        //        double subSamplingRate = heatMap.SubSamplingRateList[n];
        //        if (n >= 1)
        //            optimizedAreaSize = heatMap.nbCellInSubSampledHeatMapWidthList[n] / heatMap.nbCellInSubSampledHeatMapWidthList[n - 1];
        //        else
        //            optimizedAreaSize = heatMap.nbCellInSubSampledHeatMapWidthList[n];

        //        optimizedAreaSize /= 2;

        //        double minY = Math.Max(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate - optimizedAreaSize, 0);
        //        double maxY = Math.Min(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate + optimizedAreaSize, (int)heatMap.nbCellInSubSampledHeatMapHeightList[n]);
        //        double minX = Math.Max(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate - optimizedAreaSize, 0);
        //        double maxX = Math.Min(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate + optimizedAreaSize, (int)heatMap.nbCellInSubSampledHeatMapWidthList[n]);

        //        double max = double.NegativeInfinity;
        //        int maxXpos = 0;
        //        int maxYpos = 0;

        //        Parallel.For((int)minY, (int)maxY, (y) =>
        //        //for (double y = minY; y < maxY; y += 1)
        //        {
        //            Parallel.For((int)minX, (int)maxX, (x) =>
        //            //for (double x = minX; x < maxX; x += 1)
        //            {
        //                //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
        //                //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
        //                var heatMapPos = heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y, n);
        //                double value = EvaluateStrategyCostFunction(robotDestination, heatMapPos);
        //                //heatMap.SubSampledHeatMapData1[y, x] = value;
        //                int yBase = (int)(y * subSamplingRate);
        //                int xBase = (int)(x * subSamplingRate);
        //                heatMap.BaseHeatMapData[yBase, xBase] = value;
        //                nbComputationsList[n]++;

        //                if (value > max)
        //                {
        //                    max = value;
        //                    maxXpos = xBase;
        //                    maxYpos = yBase;
        //                }

        //                ////Code ci-dessous utile si on veut afficher la heatmap complete(video), mais consommateur en temps
        //                //for (int i = 0; i < heatMap.SubSamplingRateList[n]; i += 1)
        //                //{
        //                //    for (int j = 0; j < heatMap.SubSamplingRateList[n]; j += 1)
        //                //    {
        //                //        if ((xBase + j < heatMap.nbCellInBaseHeatMapWidth) && (yBase + i < heatMap.nbCellInBaseHeatMapHeight))
        //                //            heatMap.BaseHeatMapData[yBase + i, xBase + j] = value;
        //                //    }
        //                //}
        //            });
        //        });
        //        //OptimalPosInBaseHeatMapCoordinates = heatMap.GetMaxPositionInBaseHeatMapCoordinates();
        //        OptimalPosInBaseHeatMapCoordinates = new PointD(maxXpos, maxYpos);
        //    }
        //    //OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();
        //    OptimalPosition = heatMap.GetFieldPosFromBaseHeatMapCoordinates(OptimalPosInBaseHeatMapCoordinates.X, OptimalPosInBaseHeatMapCoordinates.Y);

        //    OnHeatMap(robotId, heatMap);
        //    SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, (float)robotOrientation, 0, 0, 0));

        //    //heatMap.Dispose();
        //    sw.Stop(); // Fin de la mesure
        //    //for (int n = 0; n < nbComputationsList.Length; n++)
        //    //{
        //    //    Console.WriteLine("Calcul Strategy - Nb Calculs Etape " + n + " : " + nbComputationsList[n]);
        //    //}
        //    //Console.WriteLine("Temps de calcul de la heatMap de stratégie : " + sw.Elapsed.TotalMilliseconds.ToString("N4")+" ms"); // Affichage de la mesure
        //}

        public void ParallelCalculateHeatMap(double[,] heatMap, int width, int height, float widthTerrain, float heightTerrain, float destinationX, float destinationY)
        {
            float destXInHeatmap = (float)((float)destinationX / widthTerrain + 0.5) * (width-1);  //-1 car on a augmenté la taille de 1 pour avoir une figure symétrique
            float destYInHeatmap = (float)((float)destinationY / heightTerrain + 0.5) * (height-1);  //-1 car on a augmenté la taille de 1 pour avoir une figure symétrique

            float normalizer = height;

            Parallel.For(0, height, y =>
            //for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //Calcul de la fonction de cout de stratégie
                    heatMap[y, x] = Math.Max(0, 1 - Math.Sqrt((destXInHeatmap - x) * (destXInHeatmap - x) + (destYInHeatmap - y) * (destYInHeatmap - y)) / normalizer);
                }
            });
        }

        double EvaluateStrategyCostFunction(PointD destination, PointD fieldPos)
        {
            return Math.Max(0, 1 - Toolbox.Distance(destination, fieldPos) / 20.0);
        }        

        public PointD GetInterceptionLocation(Location target, Location hunter, double huntingSpeed)
        {
            //D'après Al-Kashi, si d est la distance entre le pt target et le pt chasseur, que les vitesses sont constantes 
            //et égales à Vtarget et Vhunter
            //Rappel Al Kashi : A² = B²+C²-2BCcos(alpha) , alpha angle opposé au segment A
            //On a au moment de l'interception à l'instant Tinter: 
            //A = Vh * Tinter
            //B = VT * Tinter
            //C = initialDistance;
            //alpha = Pi - capCible - angleCible

            double targetSpeed = Math.Sqrt(Math.Pow(target.Vx, 2) + Math.Pow(target.Vy, 2));
            double initialDistance = Toolbox.Distance(new PointD(hunter.X, hunter.Y), new PointD(target.X, target.Y));
            double capCible = Math.Atan2(target.Vy, target.Vx);
            double angleCible = Math.Atan2(target.Y- hunter.Y, target.X- hunter.X);
            double angleCapCibleDirectionCibleChasseur = Math.PI - capCible + angleCible;

            //Résolution de ax²+bx+c=0 pour trouver Tinter
            double a = Math.Pow(huntingSpeed, 2) - Math.Pow(targetSpeed, 2);
            double b = 2 * initialDistance * targetSpeed * Math.Cos(angleCapCibleDirectionCibleChasseur);
            double c = -Math.Pow(initialDistance, 2);

            double delta = b * b - 4 * a * c;
            double t1 = (-b - Math.Sqrt(delta)) / (2 * a);
            double t2 = (-b + Math.Sqrt(delta)) / (2 * a);

            if (delta > 0 && t2<10)
            {
                double xInterception = target.X + targetSpeed * Math.Cos(capCible) * t2;
                double yInterception = target.Y + targetSpeed * Math.Sin(capCible) * t2;
                return new PointD(xInterception, yInterception);
            }
            else
                return null;
        }
        public void SetRole(PlayerRole role)
        {
            robotRole = role;
        }
                     
        public void SetDestination(Location location)
        {
            OnDestination(robotId, location);
        }

        public delegate void DestinationEventHandler(object sender, LocationArgs e);
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

        public delegate void HeatMapEventHandler(object sender, HeatMapArgs e);
        public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        public virtual void OnHeatMap(int id, Heatmap heatMap)
        {
            var handler = OnHeatMapEvent;
            if (handler != null)
            {
                handler(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
            }
        }

        public event EventHandler<GameStateArgs> OnGameStateChangedEvent;
        public virtual void OnGameStateChanged(int robotId, GameState state)
        {
            var handler = OnGameStateChangedEvent;
            if (handler != null)
            {
                handler(this, new GameStateArgs { RobotId = robotId, gameState = state });
            }
        }


        public event EventHandler<LidarMessageArgs> OnMessageEvent;
        public virtual void OnLidarMessage(string message, int line)
        {
            var handler = OnMessageEvent;
            if (handler != null)
            {
                handler(this, new LidarMessageArgs { Value = message, Line = line });
            }
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<PIDSetupArgs> OnSetRobotVitessePIDEvent;
        public virtual void OnSetRobotVitessePID(double px, double ix, double dx, double py, double iy, double dy, double ptheta, double itheta, double dtheta,
            double pxLimit, double ixLimit, double dxLimit, double pyLimit, double iyLimit, double dyLimit, double pthetaLimit, double ithetaLimit, double dthetaLimit
            )
        {
            OnSetRobotVitessePIDEvent?.Invoke(this, new PIDSetupArgs {
                P_x = px, I_x = ix, D_x = dx, P_y = py, I_y = iy, D_y = dy, P_theta = ptheta, I_theta = itheta, D_theta = dtheta,
                P_x_Limit = pxLimit, I_x_Limit = ixLimit, D_x_Limit = dxLimit, P_y_Limit = pyLimit, I_y_Limit = iyLimit, D_y_Limit = dyLimit, P_theta_Limit = pthetaLimit, I_theta_Limit = ithetaLimit, D_theta_Limit = dthetaLimit
            });
        }

        public event EventHandler<BoolEventArgs> OnEnableAsservissementEvent;
        public virtual void OnEnableAsservissement(bool val)
        {
            OnEnableAsservissementEvent?.Invoke(this, new BoolEventArgs { value = val });
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

    public enum PlayerRole
    {
        Stop,
        Gardien,
        DefenseurPlace,
        DefenseurActif,
        AttaquantAvecBalle,
        AttaquantPlace,
        Centre,
    }

    
}

