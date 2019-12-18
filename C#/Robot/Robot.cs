using AdvancedTimers;
using CameraAdapter;
using Constants;
using EthernetTeamNetwork;
using ExtendedSerialPort;
using ImageProcessingOmniCamera;
using LidarOMD60M;
using MessageDecoder;
using MessageEncoder;
using PhysicalSimulator;
using RobotInterface;
using RobotMessageGenerator;
using RobotMonitor;
using SciChart.Charting.Visuals;
using System;
using System.IO.Ports;
using System.Threading;
using TrajectoryGenerator;
using WayPointGenerator;
using WorldMapManager;

namespace Robot
{
    class Robot
    {
        static bool usingSimulatedCamera = true;
        static bool usingLidar = false;
        static bool usingPhysicalSimulator = true;
        static bool usingXBoxController = true;

        //static HighFreqTimer highFrequencyTimer;
        static HighFreqTimer timerStrategie;

        static ReliableSerialPort serialPort1;
        static RefereeBoxAdapter.RefereeBoxAdapter refBoxAdapter;
        static EthernetTeamNetworkAdapter ethernetTeamNetworkAdapter;
        static MsgDecoder msgDecoder;
        static MsgEncoder msgEncoder;
        static RobotMsgGenerator robotMsgGenerator;
        static RobotPilot.RobotPilot robotPilot;
        static BaslerCameraAdapter omniCamera;
        static SimulatedCamera.SimulatedCamera omniCameraSimulator;
        static ImageProcessingPositionFromOmniCamera imageProcessingPositionFromOmniCamera;
        static PhysicalSimulator.PhysicalSimulator physicalSimulator;
        static TrajectoryPlanner trajectoryPlanner;
        static WaypointGenerator waypointGenerator;
        static LocalWorldMapManager localWorldMapManager;
        static LidarSimulator.LidarSimulator lidarSimulator;
        static StrategyManager.StrategyManager strategyManager;
        static Lidar_OMD60M lidar_OMD60M;

        static XBoxController.XBoxController xBoxManette;

        static object ExitLock = new object();

        static WpfRobotInterface Console1;
        static WpfCameraMonitor ConsoleCamera;


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


            //TODO : Créer un projet World...

            ethernetTeamNetworkAdapter = new EthernetTeamNetworkAdapter();
            serialPort1 = new ReliableSerialPort("FTDI", 115200, Parity.None, 8, StopBits.One);                    
            msgDecoder = new MsgDecoder();
            msgEncoder = new MsgEncoder();
            robotMsgGenerator = new RobotMsgGenerator();

            physicalSimulator = new PhysicalSimulator.PhysicalSimulator();
            physicalSimulator.RegisterRobot((int)TeamId.Team1+1,0,0);

            robotPilot = new RobotPilot.RobotPilot((int)TeamId.Team1 + 1);
            refBoxAdapter = new RefereeBoxAdapter.RefereeBoxAdapter();
            trajectoryPlanner = new TrajectoryPlanner((int)TeamId.Team1 + 1);
            waypointGenerator = new WaypointGenerator((int)TeamId.Team1 + 1);
            strategyManager = new StrategyManager.StrategyManager((int)TeamId.Team1 + 1);
            localWorldMapManager = new LocalWorldMapManager((int)TeamId.Team1 + 1);
            lidarSimulator = new LidarSimulator.LidarSimulator((int)TeamId.Team1 + 1);

            if (usingLidar)
                lidar_OMD60M = new Lidar_OMD60M((int)TeamId.Team1 + 1);

            xBoxManette = new XBoxController.XBoxController((int)TeamId.Team1 + 1);

            if (!usingSimulatedCamera)
                omniCamera = new BaslerCameraAdapter();
            else
                omniCameraSimulator = new SimulatedCamera.SimulatedCamera();

            imageProcessingPositionFromOmniCamera = new ImageProcessingPositionFromOmniCamera();
                        
            StartInterfaces();

            //Liens entre modules

            strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            strategyManager.OnHeatMapEvent += waypointGenerator.OnStrategyHeatMapReceived;
            waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;
            if (!usingXBoxController)
            {
                trajectoryPlanner.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;
                robotPilot.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
            }
            else
            {
                xBoxManette.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;
                xBoxManette.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnPriseBalleEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent += robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent += robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent += robotMsgGenerator.GenerateMessageTir;
            }

            physicalSimulator.OnPhysicalPositionEvent += trajectoryPlanner.OnPhysicalPositionReceived;

           
            robotPilot.OnSpeedConsigneToMotorEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
            robotMsgGenerator.OnMessageToRobotGeneratedEvent += msgEncoder.EncodeMessageToRobot;
            msgEncoder.OnMessageEncodedEvent += serialPort1.SendMessage;
            serialPort1.OnDataReceivedEvent += msgDecoder.DecodeMsgReceived;



            physicalSimulator.OnPhysicalPositionEvent += localWorldMapManager.OnPhysicalPositionReceived;
            //lidarSimulator.OnSimulatedLidarEvent += localWorldMapManager.OnRawLidarDataReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            strategyManager.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            //waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            //lidarSimulator.OnSimulatedLidarEvent += localWorldMapManager.OnRawLidarDataReceived;
            if(usingLidar)
                lidar_OMD60M.OnLidarEvent += localWorldMapManager.OnRawLidarDataReceived;

            //Timer de simulation
            //highFrequencyTimer = new HighFreqTimer(2000);
            //highFrequencyTimer.Tick += HighFrequencyTimer_Tick;
            //highFrequencyTimer.Start();

            //Timer de stratégie
            timerStrategie = new HighFreqTimer(0.5);
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
            var role = (StrategyManager.PlayerRole)rand.Next(1, 3);
            strategyManager.SetRole(role);
            strategyManager.ProcessStrategy();
        }

        static int nbMsgSent = 0;
        //static private void HighFrequencyTimer_Tick(object sender, EventArgs e)
        //{
        //    //Utilisé pour des tests de stress sur l'interface série.
        //    //robotPilot.SendSpeedConsigneToRobot();
        //    //nbMsgSent += 1;
        //    //robotPilot.SendSpeedConsigneToMotor();
        //    //nbMsgSent += 1;
        //    //robotPilot.SendPositionFromKalmanFilter();
        //}

        static void ExitProgram()
        {
            lock (ExitLock)
            {
                Monitor.Pulse(ExitLock);
            }
        }

        static void StartInterfaces()
        {
            Thread t1 = new Thread(() =>
            {
                //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.
                Console1 = new RobotInterface.WpfRobotInterface();
                msgDecoder.OnMessageDecodedEvent += Console1.DisplayMessageDecoded;
                msgDecoder.OnMessageDecodedErrorEvent += Console1.DisplayMessageDecodedError;                

                localWorldMapManager.OnLocalWorldMapEvent+= Console1.OnLocalWorldMapEvent;

                Console1.ShowDialog();
            });
            t1.SetApartmentState(ApartmentState.STA);
            t1.Start();

            //Thread t2 = new Thread(() =>
            //{
            //    //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.
                
            //    ConsoleCamera = new RobotMonitor.WpfCameraMonitor();
            //    if (!simulatedCamera)
            //    {
            //        omniCamera.CameraInit();
            //        //omniCamera.CameraImageEvent += ConsoleCamera.CameraImageEventCB;
            //        omniCamera.OpenCvMatImageEvent += ConsoleCamera.DisplayOpenCvMatImage;
            //    }
            //    else
            //    {
            //        //omniCameraSimulator.Start();
            //        //omniCameraSimulator.CameraImageEvent += ConsoleCamera.CameraImageEventCB;
            //        omniCameraSimulator.OnOpenCvMatImageReadyEvent += ConsoleCamera.DisplayOpenCvMatImage;
            //        omniCameraSimulator.OnOpenCvMatImageReadyEvent += imageProcessingPositionFromOmniCamera.ProcessOpenCvMatImage;
            //        imageProcessingPositionFromOmniCamera.OnOpenCvMatImageProcessedEvent += ConsoleCamera.DisplayOpenCvMatImage;                    
            //    }
            //    ConsoleCamera.ShowDialog();

            //    //Inutile mais debug pour l'instant
            //    refBoxAdapter.OnRefereeBoxReceivedCommandEvent += ConsoleCamera.DisplayRefBoxCommand;
            //    msgDecoder.OnMessageDecodedEvent += ConsoleCamera.DisplayMessageDecoded;
            //});
            //t2.SetApartmentState(ApartmentState.STA);

            //t2.Start();
        }

        private static void RefBoxAdapter_DataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
