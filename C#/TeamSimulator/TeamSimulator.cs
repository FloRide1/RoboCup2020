using System;
using System.Threading;
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

namespace TeamSimulator
{
    static class TeamSimulator
    {
        //static bool usingPhysicalSimulator = true;

        static HighFreqTimer timerStrategie;
        static PhysicalSimulator physicalSimulator;
        static GlobalWorldMapManager globalWorldMapManager;

        static List<RobotPilot.RobotPilot> robotPilotList;
        static List<TrajectoryPlanner> trajectoryPlannerList;
        static List<WaypointGenerator> waypointGeneratorList;
        static List<StrategyManager.StrategyManager> strategyManagerList;
        static List<LocalWorldMapManager> localWorldMapManagerList;
        static List<LidarSimulator.LidarSimulator> lidarSimulatorList;


        static object ExitLock = new object();


        static int nbPlayers = 3;


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
            strategyManagerList = new List<StrategyManager.StrategyManager>();
            localWorldMapManagerList = new List<LocalWorldMapManager>();
            physicalSimulator = new PhysicalSimulator();
            globalWorldMapManager = new GlobalWorldMapManager();

            for (int i = 0; i < nbPlayers; i++)
            {
                //ethernetTeamNetworkAdapter = new EthernetTeamNetworkAdapter();
                //var LocalWorldMapManager = new  ("Robot" + (i + 1).ToString());
                var robotPilot = new RobotPilot.RobotPilot("Robot" + (i + 1).ToString());
                var trajectoryPlanner = new TrajectoryPlanner("Robot" + (i + 1).ToString());
                var waypointGenerator = new WaypointGenerator("Robot" + (i + 1).ToString());
                var strategyManager = new StrategyManager.StrategyManager("Robot" + (i + 1).ToString());
                var localWorldMapManager = new LocalWorldMapManager("Robot" + (i + 1).ToString());
                var lidarSimulator = new LidarSimulator.LidarSimulator("Robot" + (i + 1).ToString());

                //Liens entre modules
                trajectoryPlanner.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;
                physicalSimulator.OnPhysicalPositionEvent += trajectoryPlanner.OnPhysicalPositionReceived;
                waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;
                strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;

                physicalSimulator.OnPhysicalPositionEvent += localWorldMapManager.OnPhysicalPositionReceived;
                lidarSimulator.OnSimulatedLidarEvent += localWorldMapManager.OnRawLidarDataReceived;
                strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
                waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
                //strategyManager.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
                waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;                
                localWorldMapManager.OnLocalWorldMapEvent += globalWorldMapManager.OnLocalWorldMapReceived;

                globalWorldMapManager.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;

                robotPilotList.Add(robotPilot);
                trajectoryPlannerList.Add(trajectoryPlanner);
                waypointGeneratorList.Add(waypointGenerator);
                strategyManagerList.Add(strategyManager);
                localWorldMapManagerList.Add(localWorldMapManager);
                lidarSimulatorList.Add(lidarSimulator);

                physicalSimulator.RegisterRobot("Robot" + (i + 1).ToString(), i*4-10 , i*1.5-4);
            }

            DefineRoles();


            StartInterfaces();
            
            //Timer de stratégie
            timerStrategie = new HighFreqTimer(0.2);
            timerStrategie.Tick += TimerStrategie_Tick;
            timerStrategie.Start();

            lock (ExitLock)
            {
                // Do whatever setup code you need here
                // once we are done wait
                Monitor.Wait(ExitLock);
            }
        }

        static Random rand = new Random();
        private static void TimerStrategie_Tick(object sender, EventArgs e)
        {
            DefineRoles();

            //foreach (var strategyManager in strategyManagerList)
            //{
            //    var role = (StrategyManager.PlayerRole)rand.Next(1, 5);
            //    strategyManager.SetRole(role);
            //    strategyManager.ProcessStrategy();
            //}
        }

        private static void DefineRoles()
        {
            List<int> roleList = new List<int>();

            for (int i = 0; i < nbPlayers; i++)
            {
                roleList.Add(i + 1);
            }

            Shuffle(roleList);

            for (int i = 0; i < nbPlayers; i++)
            {
                strategyManagerList[i].SetRole((StrategyManager.PlayerRole)roleList[i]);
                strategyManagerList[i].ProcessStrategy();
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        { 
            Random rng = new Random(); 
            int n = list.Count; 
            while (n > 1) 
            { 
                n--; 
                int k = rng.Next(n + 1); 
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

                for (int i = 0; i < nbPlayers; i++)
                {
                    localWorldMapManagerList[i].OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived;
                }
                globalWorldMapManager.OnGlobalWorldMapEvent += TeamConsole.OnGlobalWorldMapReceived;
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

