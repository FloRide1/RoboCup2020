using System;
using SciChart.Charting.Visuals;
using System.Collections.Generic;
using WorldMapManager;
using System.Threading;
using PerceptionManagement;
using Constants;
using TrajectoryGenerator;
using UDPMulticast;
using UdpMulticastInterpreter;
using WpfTeamInterfaceNS;
using Utilities;

namespace TeamSimulator
{
    static class TeamSimulator
    {
        static System.Timers.Timer timerStrategie;
        static PhysicalSimulator.PhysicalSimulator physicalSimulator;
        static GlobalWorldMapManager globalWorldMapManagerTeam1;
        static GlobalWorldMapManager globalWorldMapManagerTeam2;

        static Dictionary<int, StrategyManagerNS.StrategyManager> strategyManagerDictionary;
        //static List<WaypointGenerator> waypointGeneratorList;
        static List<TrajectoryPlanner> trajectoryPlannerList;
        static List<SensorSimulator.SensorSimulator> sensorSimulatorList;
        static List<KalmanPositioning.KalmanPositioning> kalmanPositioningList;
        static List<LocalWorldMapManager> localWorldMapManagerList;
        static List<PerceptionManager> perceptionManagerList;
        static List<UDPMulticastSender> robotUdpMulticastSenderList;
        static List<UDPMulticastReceiver> robotUdpMulticastReceiverList;
        static List<UDPMulticastInterpreter> robotUdpMulticastInterpreterList;

        static RefereeBoxAdapter.RefereeBoxAdapter refBoxAdapter;

        static UDPMulticastSender BaseStationUdpMulticastSenderTeam1;
        static UDPMulticastSender BaseStationUdpMulticastSenderTeam2;
        static UDPMulticastReceiver BaseStationUdpMulticastReceiverTeam1;
        static UDPMulticastReceiver BaseStationUdpMulticastReceiverTeam2;
        static UDPMulticastInterpreter BaseStationUdpMulticastInterpreterTeam1;
        static UDPMulticastInterpreter BaseStationUdpMulticastInterpreterTeam2;

        static object ExitLock = new object();

        static int nbPlayersTeam1 = 5;
        static int nbPlayersTeam2 = 5;

        static string[] team1PlayerNames = new string[5] { "Fabien", "Lilian", "Zinedine", "Kylian", "N'Golo" };
        static string[] team2PlayerNames = new string[5] { "VB", "JM", "SM", "VG", "QR" };

        [STAThread] //à ajouter au projet initial

        static void Main(string[] args)
        {
            // Set this code once in App.xaml.cs or application startup
            SciChartSurface.SetRuntimeLicenseKey("wsCOsvBlAs2dax4o8qBefxMi4Qe5BVWax7TGOMLcwzWFYRNCa/f1rA5VA1ITvLHSULvhDMKVTc+niao6URAUXmGZ9W8jv/4jtziBzFZ6Z15ek6SLU49eIqJxGoQEFWvjANJqzp0asw+zvLV0HMirjannvDRj4i/WoELfYDubEGO1O+oAToiJlgD/e2lVqg3F8JREvC0iqBbNrmfeUCQdhHt6SKS2QpdmOoGbvtCossAezGNxv92oUbog6YIhtpSyGikCEwwKSDrlKlAab6302LLyFsITqogZychLYrVXJTFvFVnDfnkQ9cDi7017vT5flesZwIzeH497lzGp3B8fKWFQyZemD2RzlQkvj5GUWBwxiKAHrYMnQjJ/PsfojF1idPEEconVsh1LoYofNk2v/Up8AzXEAvxWUEcgzANeQggaUNy+OFet8b/yACa/bgYG7QYzFQZzgdng8IK4vCPdtg4/x7g5EdovN2PI9vB76coMuKnNVPnZN60kSjtd/24N8A==");

            //waypointGeneratorList = new List<WaypointGenerator>();
            trajectoryPlannerList = new List<TrajectoryPlanner>();
            sensorSimulatorList = new List<SensorSimulator.SensorSimulator>();
            kalmanPositioningList = new List<KalmanPositioning.KalmanPositioning>();
            strategyManagerDictionary = new Dictionary<int, StrategyManagerNS.StrategyManager>();
            localWorldMapManagerList = new List<LocalWorldMapManager>();
            perceptionManagerList = new List<PerceptionManager>();
            robotUdpMulticastSenderList = new List<UDPMulticastSender>();
            robotUdpMulticastReceiverList = new List<UDPMulticastReceiver>();
            robotUdpMulticastInterpreterList = new List<UDPMulticastInterpreter>();

            physicalSimulator = new PhysicalSimulator.PhysicalSimulator("RoboCup");
            globalWorldMapManagerTeam1 = new GlobalWorldMapManager((int)TeamId.Team1, "224.16.32.79", bypassMulticast: false);
            globalWorldMapManagerTeam2 = new GlobalWorldMapManager((int)TeamId.Team2, "224.16.32.63", bypassMulticast: false);

            //BaseStation RCT
            BaseStationUdpMulticastSenderTeam1 = new UDPMulticastSender(0, "224.16.32.79");
            BaseStationUdpMulticastReceiverTeam1 = new UDPMulticastReceiver(0, "224.16.32.79");
            BaseStationUdpMulticastInterpreterTeam1 = new UDPMulticastInterpreter(0);

            //BaseStation TuE
            BaseStationUdpMulticastSenderTeam2 = new UDPMulticastSender(0, "224.16.32.63");
            BaseStationUdpMulticastReceiverTeam2 = new UDPMulticastReceiver(0, "224.16.32.63");
            BaseStationUdpMulticastInterpreterTeam2 = new UDPMulticastInterpreter(0);

            for (int i = 0; i < nbPlayersTeam1; i++)
            {
                CreatePlayer((int)TeamId.Team1, i, team1PlayerNames[i]);
            }

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                CreatePlayer((int)TeamId.Team2, i, team2PlayerNames[i]);
            }

            DefineRoles();
            StartInterfaces();

            refBoxAdapter = new RefereeBoxAdapter.RefereeBoxAdapter();

            //Event de réception d'une commande de la réferee box
            refBoxAdapter.OnRefereeBoxCommandEvent += globalWorldMapManagerTeam1.OnRefereeBoxCommandReceived;
            refBoxAdapter.OnRefereeBoxCommandEvent += globalWorldMapManagerTeam2.OnRefereeBoxCommandReceived;

            //Event de réception de data Multicast sur la base Station Team X
            BaseStationUdpMulticastReceiverTeam1.OnDataReceivedEvent += BaseStationUdpMulticastInterpreterTeam1.OnMulticastDataReceived;
            BaseStationUdpMulticastReceiverTeam2.OnDataReceivedEvent += BaseStationUdpMulticastInterpreterTeam2.OnMulticastDataReceived;
            
            //Event d'interprétation d'une localWorldMap à sa réception dans la base station
            BaseStationUdpMulticastInterpreterTeam1.OnLocalWorldMapEvent += globalWorldMapManagerTeam1.OnLocalWorldMapReceived;
            BaseStationUdpMulticastInterpreterTeam2.OnLocalWorldMapEvent += globalWorldMapManagerTeam2.OnLocalWorldMapReceived;
            
            //Event d'envoi de la global world map sur le Multicast
            globalWorldMapManagerTeam1.OnMulticastSendGlobalWorldMapEvent += BaseStationUdpMulticastSenderTeam1.OnMulticastMessageToSendReceived;
            globalWorldMapManagerTeam2.OnMulticastSendGlobalWorldMapEvent += BaseStationUdpMulticastSenderTeam2.OnMulticastMessageToSendReceived;
            
            //Timer de stratégie
            timerStrategie = new System.Timers.Timer(20000);
            timerStrategie.Elapsed += TimerStrategie_Tick;
            timerStrategie.Start();
            

            lock (ExitLock)
            {
                // Do whatever setup code you need here
                // once we are done wait
                Monitor.Wait(ExitLock);
            }
        }
        
        static Random randomGenerator = new Random();
        private static void CreatePlayer(int TeamNumber, int RobotNumber, string Name)
        {
            int robotId = TeamNumber + RobotNumber;
            var strategyManager = new StrategyManagerNS.StrategyManager(robotId, TeamNumber, GameMode.RoboCup);
            //var waypointGenerator = new WaypointGenerator(robotId, GameMode.RoboCup);
            var trajectoryPlanner = new TrajectoryPlanner(robotId);
            var sensorSimulator = new SensorSimulator.SensorSimulator(robotId);
            var kalmanPositioning = new KalmanPositioning.KalmanPositioning(robotId, 50, 0.2, 0.2, 0.2, 0.1, 0.1, 0.1, 0.02);
            var localWorldMapManager = new LocalWorldMapManager(robotId, TeamNumber, bypassMulticast: false);
            //var lidarSimulator = new LidarSimulator.LidarSimulator(robotId);
            var perceptionSimulator = new PerceptionManager(robotId);
            UDPMulticastSender robotUdpMulticastSender = null;
            UDPMulticastReceiver robotUdpMulticastReceiver = null;
            UDPMulticastInterpreter robotUdpMulticastInterpreter = null;

            if (TeamNumber == (int)TeamId.Team1)
            {
                robotUdpMulticastSender = new UDPMulticastSender(robotId, "224.16.32.79");
                robotUdpMulticastReceiver = new UDPMulticastReceiver(robotId, "224.16.32.79");
                robotUdpMulticastInterpreter = new UDPMulticastInterpreter(robotId);
            }
            else if (TeamNumber == (int)TeamId.Team2)
            {
                robotUdpMulticastSender = new UDPMulticastSender(robotId, "224.16.32.63");
                robotUdpMulticastReceiver = new UDPMulticastReceiver(robotId, "224.16.32.63");
                robotUdpMulticastInterpreter = new UDPMulticastInterpreter(robotId);
            }

            //Liens entre modules
            //strategyManager.strategy.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            //strategyManager.strategy.OnHeatMapStrategyEvent += waypointGenerator.OnStrategyHeatMapReceived;
            strategyManager.strategy.OnGameStateChangedEvent += trajectoryPlanner.OnGameStateChangeReceived;
            strategyManager.strategy.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;
            strategyManager.strategy.OnShootRequestEvent += physicalSimulator.OnShootOrderReceived;
            //waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;
            trajectoryPlanner.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;

            //Gestion des events liés à une détection de collision soft
            trajectoryPlanner.OnCollisionEvent += kalmanPositioning.OnCollisionReceived;
            trajectoryPlanner.OnCollisionEvent += physicalSimulator.OnCollisionReceived;
            
            //trajectoryPlanner.InitRobotPosition(xInit, yInit, thetaInit);
            //physicalSimulator.SetRobotPosition(robotId, xInit, yInit, thetaInit);
            //kalmanPositioning.InitFilter(xInit, 0, 0, yInit, 0, 0, thetaInit, 0, 0);

            ////physicalSimulator.OnPhysicalRobotLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived; //replacé par les 5 lignes suivantes
            physicalSimulator.OnPhysicalRobotLocationEvent += sensorSimulator.OnPhysicalRobotPositionReceived;
            physicalSimulator.OnPhysicalBallHandlingEvent += sensorSimulator.OnPhysicalBallHandlingReceived;
            sensorSimulator.OnCamLidarSimulatedRobotPositionEvent += kalmanPositioning.OnCamLidarSimulatedRobotPositionReceived;
            sensorSimulator.OnGyroSimulatedRobotSpeedEvent += kalmanPositioning.OnGyroRobotSpeedReceived;
            sensorSimulator.OnOdometrySimulatedRobotSpeedEvent += kalmanPositioning.OnOdometryRobotSpeedReceived;
            sensorSimulator.OnBallHandlingSimulatedEvent += strategyManager.strategy.OnBallHandlingReceived;


            kalmanPositioning.OnKalmanLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            //physicalSimulator.OnPhysicalRobotLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived; //ajout

            kalmanPositioning.OnKalmanLocationEvent += perceptionSimulator.OnPhysicalRobotPositionReceived;
            kalmanPositioning.OnKalmanLocationEvent += strategyManager.strategy.OnPositionRobotReceived;
            //physicalSimulator.OnPhysicalRobotLocationEvent += perceptionSimulator.OnPhysicalRobotPositionReceived; //ajout

            physicalSimulator.OnPhysicicalObjectListLocationEvent += perceptionSimulator.OnPhysicalObjectListLocationReceived;
            physicalSimulator.OnPhysicalBallPositionListEvent += perceptionSimulator.OnPhysicalBallPositionListReceived;

            //Update des données de la localWorldMap
            perceptionSimulator.OnPerceptionEvent += localWorldMapManager.OnPerceptionReceived;
            strategyManager.strategy.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            strategyManager.strategy.OnRoleEvent += localWorldMapManager.OnRoleReceived; //Utile pour l'affichage
            strategyManager.strategy.OnMessageDisplayEvent += localWorldMapManager.OnMessageDisplayReceived; //Utile pour l'affichage
            //strategyManager.strategy.OnPlayingSideEvent += localWorldMapManager.OnPlayingSideReceived;  //inutile
            strategyManager.strategy.OnHeatMapStrategyEvent += localWorldMapManager.OnHeatMapStrategyReceived;
            strategyManager.strategy.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            strategyManager.strategy.OnHeatMapWayPointEvent += localWorldMapManager.OnHeatMapWaypointReceived;
            //waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            //waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapWaypointReceived;
            trajectoryPlanner.OnGhostLocationEvent += localWorldMapManager.OnGhostLocationReceived;

            //Event de Réception de data Multicast sur le robot
            robotUdpMulticastReceiver.OnDataReceivedEvent += robotUdpMulticastInterpreter.OnMulticastDataReceived;

            //Event d'interprétation d'une globalWorldMap à sa réception dans le robot
            robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += strategyManager.strategy.OnGlobalWorldMapReceived;
            //robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;
            robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += perceptionSimulator.OnGlobalWorldMapReceived;

            //Event de Transmission des Local World Map du robot vers le multicast
            localWorldMapManager.OnMulticastSendLocalWorldMapEvent += robotUdpMulticastSender.OnMulticastMessageToSendReceived;

            strategyManagerDictionary.Add(robotId, strategyManager);
            //waypointGeneratorList.Add(waypointGenerator);
            trajectoryPlannerList.Add(trajectoryPlanner);
            sensorSimulatorList.Add(sensorSimulator);
            kalmanPositioningList.Add(kalmanPositioning);
            localWorldMapManagerList.Add(localWorldMapManager);
            //lidarSimulatorList.Add(lidarSimulator);
            perceptionManagerList.Add(perceptionSimulator);
            robotUdpMulticastReceiverList.Add(robotUdpMulticastReceiver);
            robotUdpMulticastSenderList.Add(robotUdpMulticastSender);
            robotUdpMulticastInterpreterList.Add(robotUdpMulticastInterpreter);

            double xInit, yInit, thetaInit;
            if (TeamNumber == (int)TeamId.Team1)
            {
                xInit = 2 * RobotNumber + 2;
                yInit = -6.5;
                thetaInit = Math.PI/2;
            }
            else
            {
                xInit = - (2 * RobotNumber + 2);
                yInit = +6.5;
                thetaInit = 0;
            }
            physicalSimulator.RegisterRobot(robotId, xInit, yInit);
            trajectoryPlanner.InitRobotPosition(xInit, yInit, thetaInit);
        }


        private static void TimerStrategie_Tick(object sender, EventArgs e)
        {
            DefineRoles();
        }

        private static void DefineRoles()
        {
            List<int> roleList = new List<int>();

            for (int i = 0; i < nbPlayersTeam1; i++)
                roleList.Add(i + 1);

            Shuffle(roleList);

            for (int i = 0; i < nbPlayersTeam1; i++)
            {
                strategyManagerDictionary[(int)TeamId.Team1 + i].SetRole((StrategyManagerNS.PlayerRole)roleList[i]);
                //strategyManagerDictionary[(int)TeamId.Team1 + i].ProcessStrategy();
            }
            
            roleList = new List<int>();

            for (int i = 0; i < nbPlayersTeam2; i++)
                roleList.Add(i + 1);

            Shuffle(roleList);

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                strategyManagerDictionary[(int)TeamId.Team2 + i].SetRole((StrategyManagerNS.PlayerRole)roleList[i]);
                //strategyManagerDictionary[(int)TeamId.Team2 + i].ProcessStrategy();
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        { 
            int n = list.Count; 
            while (n > 1) 
            { 
                n--; 
                int k = randomGenerator.Next(n + 1); 
                T value = list[k]; 
                list[k] = list[n]; 
                list[n] = value; 
            } 
        }        
        
        static void ExitProgram()
        {
            lock (ExitLock)
            {
                Monitor.Pulse(ExitLock);
            }
        }
        
        static WpfTeamInterface TeamConsole;

        static void StartInterfaces()
       {            
            Thread t1 = new Thread(() =>
            {
                //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.
                TeamConsole = new WpfTeamInterface("RoboCup", team1PlayerNames, team2PlayerNames);  //RoboCup

                //On s'abonne aux évènements permettant de visualiser les localWorldMap à leur génération : attention, event réservé à la visualisation car il passe les heat maps et pts lidar
                for (int i = 0; i < nbPlayersTeam1; i++)
                {
                    localWorldMapManagerList[i].OnLocalWorldMapForDisplayOnlyEvent += TeamConsole.OnLocalWorldMapReceived;
                }

                //On s'abonne aux évènements permettant de visualiser les localWorldMap à leur réception par la BaseStation
                //BaseStationUdpMulticastInterpreterTeam1.OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived; //->version base station
                //BaseStationUdpMulticastInterpreterTeam2.OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived; //->version base station

                //On s'abonne aux évènements permettant de visualiser la globalWorldMap à leur réception par les robots
                foreach (var udpInterpreter in robotUdpMulticastInterpreterList)
                {
                    udpInterpreter.OnGlobalWorldMapEvent += TeamConsole.OnGlobalWorldMapReceived;
                }

                //Event de simulation de ref box sur le simulateur
                TeamConsole.OnRefereeBoxCommandEvent += globalWorldMapManagerTeam1.OnRefereeBoxCommandReceived;
                TeamConsole.OnRefereeBoxCommandEvent += globalWorldMapManagerTeam2.OnRefereeBoxCommandReceived;



                TeamConsole.ShowDialog();
            });
            t1.SetApartmentState(ApartmentState.STA);
            t1.Start();
        }

        private static void RefBoxAdapter_DataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

