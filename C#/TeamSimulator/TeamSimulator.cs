using System;
using AdvancedTimers;
using SciChart.Charting.Visuals;
using PhysicalGameSimulator;
using TrajectoryGeneration;
using WayPointGenerator;
using System.Collections.Generic;
using RobotInterface;
using TeamInterface;
using WorldMapManager;
using LidarSimulator;
using System.Timers;
using System.Threading;

namespace TeamSimulator
{
    static class TeamSimulator
    {
        //static bool usingPhysicalSimulator = true;

        static System.Timers.Timer timerStrategie;
        static PhysicalSimulator physicalSimulator;
        static GlobalWorldMapManager globalWorldMapManagerTeam1;
        static GlobalWorldMapManager globalWorldMapManagerTeam2;

        static List<RobotPilot.RobotPilot> robotPilotList;
        static List<TrajectoryPlanner> trajectoryPlannerList;
        static List<WaypointGenerator> waypointGeneratorList;
        static Dictionary<string, StrategyManager.StrategyManager> strategyManagerDictionary;
        static List<LocalWorldMapManager> localWorldMapManagerList;
        static List<LidarSimulator.LidarSimulator> lidarSimulatorList;


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
            strategyManagerDictionary = new Dictionary<string, StrategyManager.StrategyManager>();
            localWorldMapManagerList = new List<LocalWorldMapManager>();
            physicalSimulator = new PhysicalSimulator();

            globalWorldMapManagerTeam1 = new GlobalWorldMapManager();
            globalWorldMapManagerTeam2 = new GlobalWorldMapManager();

            for (int i = 0; i < nbPlayersTeam1; i++)
            {
                //ethernetTeamNetworkAdapter = new EthernetTeamNetworkAdapter();
                //var LocalWorldMapManager = new  ("Robot" + (i + 1).ToString());
                CreatePlayer(i+1, 1);
            }

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                //ethernetTeamNetworkAdapter = new EthernetTeamNetworkAdapter();
                //var LocalWorldMapManager = new  ("Robot" + (i + 1).ToString());
                CreatePlayer(i + 1, 2);
            }

            DefineRoles();


            StartInterfaces();
            
            //Timer de stratégie
            timerStrategie = new System.Timers.Timer(5000);
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
        private static void CreatePlayer(int RobotNumber, int TeamNumber)
        {
            string robotName = "Robot" + RobotNumber.ToString() + "Team" + TeamNumber.ToString();
            var strategyManager = new StrategyManager.StrategyManager(robotName);
            var waypointGenerator = new WaypointGenerator(robotName);
            var trajectoryPlanner = new TrajectoryPlanner(robotName);
            var robotPilot = new RobotPilot.RobotPilot(robotName);
            var localWorldMapManager = new LocalWorldMapManager(robotName);
            var lidarSimulator = new LidarSimulator.LidarSimulator(robotName);

            //Liens entre modules
            if (TeamNumber == 1)
            {
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += strategyManager.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;
                localWorldMapManager.OnLocalWorldMapEvent += globalWorldMapManagerTeam1.OnLocalWorldMapReceived;
            }
            else if (TeamNumber == 2)
            {
                globalWorldMapManagerTeam2.OnGlobalWorldMapEvent += strategyManager.OnGlobalWorldMapReceived;
                globalWorldMapManagerTeam2.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;
                localWorldMapManager.OnLocalWorldMapEvent += globalWorldMapManagerTeam2.OnLocalWorldMapReceived;
            }
            strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;
            trajectoryPlanner.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;
            physicalSimulator.OnPhysicalPositionEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            
            physicalSimulator.OnPhysicalPositionEvent += localWorldMapManager.OnPhysicalPositionReceived;
            lidarSimulator.OnSimulatedLidarEvent += localWorldMapManager.OnRawLidarDataReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            strategyManager.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            //waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            strategyManager.OnHeatMapEvent += waypointGenerator.OnStrategyHeatMapReceived;

            strategyManagerDictionary.Add(robotName, strategyManager);
            waypointGeneratorList.Add(waypointGenerator);
            trajectoryPlannerList.Add(trajectoryPlanner);
            robotPilotList.Add(robotPilot);
            localWorldMapManagerList.Add(localWorldMapManager);
            lidarSimulatorList.Add(lidarSimulator);

            physicalSimulator.RegisterRobot(robotName, randomGenerator.Next(-10,10), randomGenerator.Next(-6, 6));
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
                strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team1"].SetRole((StrategyManager.PlayerRole)roleList[i]);
                strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team1"].ProcessStrategy();
            }
            
            roleList = new List<int>();

            for (int i = 0; i < nbPlayersTeam2; i++)
                roleList.Add(i + 1);

            Shuffle(roleList);

            for (int i = 0; i < nbPlayersTeam2; i++)
            {
                strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team2"].SetRole((StrategyManager.PlayerRole)roleList[i]);
                strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team2"].ProcessStrategy();
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


        static WpfRobotInterface Console1;
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

