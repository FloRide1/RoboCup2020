using System;
using SciChart.Charting.Visuals;
using WayPointGenerator;
using System.Collections.Generic;
using TeamInterface;
using WorldMapManager;
using System.Threading;
using PerceptionManagement;
using Constants;
using TrajectoryGenerator;
using PhysicalSimulator;
using UDPMulticast;
using System.Text;
using TCPAdapter;
using RefereeBoxAdapter;
using UdpMulticastInterpreter;
using SensorSimulator;
using KalmanPositioning;

namespace TeamSimulator
{
    static class TeamSimulator
    {
        static System.Timers.Timer timerStrategie;
        static PhysicalSimulator.PhysicalSimulator physicalSimulator;
        static GlobalWorldMapManager globalWorldMapManagerTeam1;
        static GlobalWorldMapManager globalWorldMapManagerTeam2;

        static Dictionary<int, StrategyManager.StrategyManager> strategyManagerDictionary;
        static List<WaypointGenerator> waypointGeneratorList;
        static List<TrajectoryPlanner> trajectoryPlannerList;
        static List<SensorSimulator.SensorSimulator> sensorSimulatorList;
        static List<KalmanPositioning.KalmanPositioning> kalmanPositioningList;
        static List<LocalWorldMapManager> localWorldMapManagerList;
        static List<PerceptionSimulator> perceptionSimulatorList;
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

        static int nbPlayersTeam1 = 1;
        static int nbPlayersTeam2 = 0;

        [STAThread] //à ajouter au projet initial

        static void Main(string[] args)
        {
            SciChartSurface.SetRuntimeLicenseKey(@"<LicenseContract>
            <Customer>Universite De Toulon</Customer>
            <OrderId>EDUCATIONAL-USE-0128</OrderId>
            <LicenseCount>1</LicenseCount>
            <IsTrialLicense>false</IsTrialLicense>
            <SupportExpires>02/17/2020 00:00:00</SupportExpires>
            <ProductCode>SC-WPF-2D-PRO-SITE</ProductCode>
            <KeyCode>lwAAAQEAAACS9FAFUqnVAXkAQ3VzdG9tZXI9VW5pdmVyc2l0ZSBEZSBUb3Vsb247T3JkZXJJZD1FRFVDQVRJT05BTC1VU0UtMDEyODtTdWJzY3JpcHRpb25WYWxpZFRvPTE3LUZlYi0yMDIwO1Byb2R1Y3RDb2RlPVNDLVdQRi0yRC1QUk8tU0lURYcbnXYui4rna7TqbkEmUz1V7oD1EwrO3FhU179M9GNhkL/nkD/SUjwJ/46hJZ31CQ==</KeyCode>
            </LicenseContract>");

            waypointGeneratorList = new List<WaypointGenerator>();
            trajectoryPlannerList = new List<TrajectoryPlanner>();
            sensorSimulatorList = new List<SensorSimulator.SensorSimulator>();
            kalmanPositioningList = new List<KalmanPositioning.KalmanPositioning>();
            strategyManagerDictionary = new Dictionary<int, StrategyManager.StrategyManager>();
            localWorldMapManagerList = new List<LocalWorldMapManager>();
            perceptionSimulatorList = new List<PerceptionSimulator>();
            robotUdpMulticastSenderList = new List<UDPMulticastSender>();
            robotUdpMulticastReceiverList = new List<UDPMulticastReceiver>();
            robotUdpMulticastInterpreterList = new List<UDPMulticastInterpreter>();

            physicalSimulator = new PhysicalSimulator.PhysicalSimulator(22, 14);
            globalWorldMapManagerTeam1 = new GlobalWorldMapManager((int)TeamId.Team1, "224.16.32.79");
            globalWorldMapManagerTeam2 = new GlobalWorldMapManager((int)TeamId.Team2, "224.16.32.63");

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
                CreatePlayer((int)TeamId.Team1, i);
            }

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                CreatePlayer((int)TeamId.Team2, i);
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
        private static void CreatePlayer(int TeamNumber, int RobotNumber)
        {
            int robotId = TeamNumber + RobotNumber;
            var strategyManager = new StrategyManager.StrategyManager(robotId, TeamNumber);
            var waypointGenerator = new WaypointGenerator(robotId);
            var trajectoryPlanner = new TrajectoryPlanner(robotId);
            var sensorSimulator = new SensorSimulator.SensorSimulator(robotId);
            var kalmanPositioning = new KalmanPositioning.KalmanPositioning(robotId, 50, 0.2, 0.2, 0.2, 0.1, 0.1, 0.1, 0.02);
            var localWorldMapManager = new LocalWorldMapManager(robotId, TeamNumber);
            //var lidarSimulator = new LidarSimulator.LidarSimulator(robotId);
            var perceptionSimulator = new PerceptionSimulator(robotId);
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
            strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            strategyManager.OnHeatMapEvent += waypointGenerator.OnStrategyHeatMapReceived;
            strategyManager.OnGameStateChangedEvent += trajectoryPlanner.OnGameStateChangeReceived;
            waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;
            trajectoryPlanner.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;

            //Gestion des events liés à une détection de collision soft
            trajectoryPlanner.OnCollisionEvent += kalmanPositioning.OnCollisionReceived;
            trajectoryPlanner.OnCollisionEvent += physicalSimulator.OnCollisionReceived;
            
            //trajectoryPlanner.InitRobotPosition(xInit, yInit, thetaInit);
            //physicalSimulator.SetRobotPosition(robotId, xInit, yInit, thetaInit);
            //kalmanPositioning.InitFilter(xInit, 0, 0, yInit, 0, 0, thetaInit, 0, 0);

            ////physicalSimulator.OnPhysicalRobotLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived; //replacé par les 5 lignes suivantes
            physicalSimulator.OnPhysicalRobotLocationEvent += sensorSimulator.OnPhysicalRobotPositionReceived;
            sensorSimulator.OnCamLidarSimulatedRobotPositionEvent += kalmanPositioning.OnCamLidarSimulatedRobotPositionReceived;
            sensorSimulator.OnGyroSimulatedRobotSpeedEvent += kalmanPositioning.OnGyroRobotSpeedReceived;
            sensorSimulator.OnOdometrySimulatedRobotSpeedEvent += kalmanPositioning.OnOdometryRobotSpeedReceived;
            
            kalmanPositioning.OnKalmanLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            //physicalSimulator.OnPhysicalRobotLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived; //ajout

            kalmanPositioning.OnKalmanLocationEvent += perceptionSimulator.OnPhysicalRobotPositionReceived;            
            //physicalSimulator.OnPhysicalRobotLocationEvent += perceptionSimulator.OnPhysicalRobotPositionReceived; //ajout


            physicalSimulator.OnPhysicicalObjectListLocationEvent += perceptionSimulator.OnPhysicalObjectListLocationReceived;
            physicalSimulator.OnPhysicalBallPositionListEvent += perceptionSimulator.OnPhysicalBallPositionListReceived;

            //Update des données de la localWorldMap
            perceptionSimulator.OnPerceptionEvent += localWorldMapManager.OnPerceptionReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            trajectoryPlanner.OnGhostLocationEvent += localWorldMapManager.OnGhostLocationReceived;

            //Event de Réception de data Multicast sur sur le robot
            robotUdpMulticastReceiver.OnDataReceivedEvent += robotUdpMulticastInterpreter.OnMulticastDataReceived;
            //Event d'interprétation d'une globalWorldMap à sa réception dans le robot
            robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += strategyManager.OnGlobalWorldMapReceived;
            robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;
            robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += perceptionSimulator.OnGlobalWorldMapReceived;
            //Event de Transmission des Local World Map du robot vers le multicast
            localWorldMapManager.OnMulticastSendLocalWorldMapEvent += robotUdpMulticastSender.OnMulticastMessageToSendReceived;

            strategyManagerDictionary.Add(robotId, strategyManager);
            waypointGeneratorList.Add(waypointGenerator);
            trajectoryPlannerList.Add(trajectoryPlanner);
            sensorSimulatorList.Add(sensorSimulator);
            kalmanPositioningList.Add(kalmanPositioning);
            localWorldMapManagerList.Add(localWorldMapManager);
            //lidarSimulatorList.Add(lidarSimulator);
            perceptionSimulatorList.Add(perceptionSimulator);
            robotUdpMulticastReceiverList.Add(robotUdpMulticastReceiver);
            robotUdpMulticastSenderList.Add(robotUdpMulticastSender);
            robotUdpMulticastInterpreterList.Add(robotUdpMulticastInterpreter);

            double xInit, yInit, thetaInit;
            if (TeamNumber == (int)TeamId.Team1)
            {
                xInit = 2 * RobotNumber + 2;
                yInit = -7;
                thetaInit = Math.PI/2;
            }
            else
            {
                xInit = - (2 * RobotNumber + 2);
                yInit = -7;
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
                strategyManagerDictionary[(int)TeamId.Team1 + i].SetRole((StrategyManager.PlayerRole)roleList[i]);
                //strategyManagerDictionary[(int)TeamId.Team1 + i].ProcessStrategy();
            }
            
            roleList = new List<int>();

            for (int i = 0; i < nbPlayersTeam2; i++)
                roleList.Add(i + 1);

            Shuffle(roleList);

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                strategyManagerDictionary[(int)TeamId.Team2 + i].SetRole((StrategyManager.PlayerRole)roleList[i]);
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
                TeamConsole = new WpfTeamInterface();

                for (int i = 0; i < nbPlayersTeam1; i++)
                {
                    localWorldMapManagerList[i].OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived; //-> version simulation                    
                }
                BaseStationUdpMulticastInterpreterTeam1.OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived; //->version base station
                BaseStationUdpMulticastInterpreterTeam2.OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived; //->version base station
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += TeamConsole.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam2.OnGlobalWorldMapEvent += TeamConsole.OnGlobalWorldMapReceived;

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

