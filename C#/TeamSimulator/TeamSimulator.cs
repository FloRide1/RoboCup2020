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

namespace TeamSimulator
{
    static class TeamSimulator
    {
        //static bool usingPhysicalSimulator = true;

        static System.Timers.Timer timerStrategie;
        static PhysicalSimulator.PhysicalSimulator physicalSimulator;
        static GlobalWorldMapManager globalWorldMapManagerTeam1;
        static GlobalWorldMapManager globalWorldMapManagerTeam2;

        static List<RobotPilot.RobotPilot> robotPilotList;
        static List<TrajectoryPlanner> trajectoryPlannerList;
        static List<WaypointGenerator> waypointGeneratorList;
        static Dictionary<int, StrategyManager.StrategyManager> strategyManagerDictionary;
        static List<LocalWorldMapManager> localWorldMapManagerList;
        static List<LidarSimulator.LidarSimulator> lidarSimulatorList;
        static List<PerceptionSimulator> perceptionSimulatorList;

        static RefereeBoxAdapter.RefereeBoxAdapter refBoxAdapter;
        //static RefereeBoxAdapter.RefereeBoxAdapter refBoxAdapter2;

        static System.Timers.Timer timerTest;
        static UDPMulticastSender sender1;
        //static UDPMulticastSender sender2;

        static object ExitLock = new object();

        static int nbPlayersTeam1 = 5;
        static int nbPlayersTeam2 = 5;

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

            robotPilotList = new List<RobotPilot.RobotPilot>();
            trajectoryPlannerList = new List<TrajectoryPlanner>();
            waypointGeneratorList = new List<WaypointGenerator>();
            lidarSimulatorList = new List<LidarSimulator.LidarSimulator>();
            strategyManagerDictionary = new Dictionary<int, StrategyManager.StrategyManager>();
            localWorldMapManagerList = new List<LocalWorldMapManager>();
            perceptionSimulatorList = new List<PerceptionSimulator>();

            physicalSimulator = new PhysicalSimulator.PhysicalSimulator();
            globalWorldMapManagerTeam1 = new GlobalWorldMapManager((int)TeamId.Team1);
            globalWorldMapManagerTeam2 = new GlobalWorldMapManager((int)TeamId.Team2);
            
            for (int i = 0; i < nbPlayersTeam1; i++)
            {
                //ethernetTeamNetworkAdapter = new EthernetTeamNetworkAdapter();
                //var LocalWorldMapManager = new  ("Robot" + (i + 1).ToString());
                CreatePlayer((int)TeamId.Team1, i);
            }

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                //ethernetTeamNetworkAdapter = new EthernetTeamNetworkAdapter();
                //var LocalWorldMapManager = new  ("Robot" + (i + 1).ToString());
                CreatePlayer((int)TeamId.Team2, i);
            }

            DefineRoles();

            StartInterfaces();

            refBoxAdapter = new RefereeBoxAdapter.RefereeBoxAdapter();
            //refBoxAdapter2 = new RefereeBoxAdapter.RefereeBoxAdapter();

            //Timer de stratégie
            timerStrategie = new System.Timers.Timer(20000);
            timerStrategie.Elapsed += TimerStrategie_Tick;
            timerStrategie.Start();

            //Tests à supprimer plus tard
            timerTest = new System.Timers.Timer(100);
            timerTest.Elapsed += TimerTest_Elapsed;

            sender1 = new UDPMulticastSender();
            //sender2 = new UDPMulticastSender();
            UDPMulticastReceiver receiver1 = new UDPMulticastReceiver(0);
            //UDPMulticastReceiver receiver2 = new UDPMulticastReceiver(0);
            //UDPMulticastReceiver receiver3 = new UDPMulticastReceiver(0);

            receiver1.OnDataReceivedEvent += Receiver1_OnDataReceivedEvent;
            //receiver2.OnDataReceivedEvent += Receiver2_OnDataReceivedEvent;
            //receiver3.OnDataReceivedEvent += Receiver3_OnDataReceivedEvent;
            timerTest.Start();

            lock (ExitLock)
            {
                // Do whatever setup code you need here
                // once we are done wait
                Monitor.Wait(ExitLock);
            }
        }

        private static void Receiver1_OnDataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            Console.WriteLine("Received on UDP Receiver 1 : " + Encoding.ASCII.GetString(e.Data));
        }

        //private static void Receiver2_OnDataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        //{
        //    Console.WriteLine("Received on UDP Receiver 2 : " + Encoding.ASCII.GetString(e.Data));
        //}

        //private static void Receiver3_OnDataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        //{
        //    Console.WriteLine("Received on UDP Receiver 3 : " + Encoding.ASCII.GetString(e.Data));
        //}

        static int index = 0;
        private static void TimerTest_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string msg = "Toto " + index.ToString();
            index++;
            sender1.Send(Encoding.ASCII.GetBytes(msg + " X"));
            //sender2.Send(Encoding.ASCII.GetBytes(msg + "  X"));
        }

        static Random randomGenerator = new Random();
        private static void CreatePlayer(int TeamNumber, int RobotNumber)
        {   
            int robotId = TeamNumber + RobotNumber;
            var strategyManager = new StrategyManager.StrategyManager(robotId);
            var waypointGenerator = new WaypointGenerator(robotId);
            var trajectoryPlanner = new TrajectoryPlanner(robotId);
            var robotPilot = new RobotPilot.RobotPilot(robotId);
            var localWorldMapManager = new LocalWorldMapManager(robotId, TeamNumber);
            var lidarSimulator = new LidarSimulator.LidarSimulator(robotId);
            var perceptionSimulator = new PerceptionSimulator(robotId);

            //Liens entre modules
            
            strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            strategyManager.OnHeatMapEvent += waypointGenerator.OnStrategyHeatMapReceived;
            waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;

            trajectoryPlanner.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;

            physicalSimulator.OnPhysicalRobotPositionEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            physicalSimulator.OnPhysicicalObjectListLocationEvent += perceptionSimulator.OnPhysicalObjectListLocationReceived;
            physicalSimulator.OnPhysicalRobotPositionEvent += perceptionSimulator.OnPhysicalRobotPositionReceived;
            physicalSimulator.OnPhysicalBallPositionEvent += perceptionSimulator.OnPhysicalBallPositionReceived;

            perceptionSimulator.OnPerceptionEvent += localWorldMapManager.OnPerceptionReceived;
            lidarSimulator.OnSimulatedLidarEvent += localWorldMapManager.OnRawLidarDataReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            //strategyManager.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;

            if (TeamNumber == (int)TeamId.Team1)
            {
                localWorldMapManager.OnLocalWorldMapEvent += globalWorldMapManagerTeam1.OnLocalWorldMapReceived;
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += strategyManager.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += perceptionSimulator.OnGlobalWorldMapReceived;
            }
            else if (TeamNumber == (int)TeamId.Team2)
            {
                localWorldMapManager.OnLocalWorldMapEvent += globalWorldMapManagerTeam2.OnLocalWorldMapReceived;
                globalWorldMapManagerTeam2.OnGlobalWorldMapEvent += strategyManager.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam2.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam2.OnGlobalWorldMapEvent += perceptionSimulator.OnGlobalWorldMapReceived;
            }

            strategyManagerDictionary.Add(robotId, strategyManager);
            waypointGeneratorList.Add(waypointGenerator);
            trajectoryPlannerList.Add(trajectoryPlanner);
            robotPilotList.Add(robotPilot);
            localWorldMapManagerList.Add(localWorldMapManager);
            lidarSimulatorList.Add(lidarSimulator);
            perceptionSimulatorList.Add(perceptionSimulator);

            physicalSimulator.RegisterRobot(robotId, randomGenerator.Next(-10,10), randomGenerator.Next(-6, 6));
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
                //strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team1"].SetRole((StrategyManager.PlayerRole)roleList[i]);
                //strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team1"].ProcessStrategy();
                strategyManagerDictionary[(int)TeamId.Team1 + i].SetRole((StrategyManager.PlayerRole)roleList[i]);
                strategyManagerDictionary[(int)TeamId.Team1 + i].ProcessStrategy();
            }
            
            roleList = new List<int>();

            for (int i = 0; i < nbPlayersTeam2; i++)
                roleList.Add(i + 1);

            Shuffle(roleList);

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                strategyManagerDictionary[(int)TeamId.Team2 + i].SetRole((StrategyManager.PlayerRole)roleList[i]);
                strategyManagerDictionary[(int)TeamId.Team2 + i].ProcessStrategy();
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
                    localWorldMapManagerList[i].OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived;
                }
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += TeamConsole.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam2.OnGlobalWorldMapEvent += TeamConsole.OnGlobalWorldMapReceived;
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

