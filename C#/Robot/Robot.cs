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
using RobotMessageProcessor;
using PerceptionManagement;
using EventArgsLibrary;

namespace Robot
{
    class Robot
    {
        static bool usingSimulatedCamera = true;
        static bool usingLidar = false;
        static bool usingPhysicalSimulator = true;
        static bool usingXBoxController = false;

        //static HighFreqTimer highFrequencyTimer;
        static HighFreqTimer timerStrategie;

        static ReliableSerialPort serialPort1;
        static RefereeBoxAdapter.RefereeBoxAdapter refBoxAdapter;
        static EthernetTeamNetworkAdapter ethernetTeamNetworkAdapter;
        static MsgDecoder msgDecoder;
        static MsgEncoder msgEncoder;
        static RobotMsgGenerator robotMsgGenerator;
        static RobotMsgProcessor robotMsgProcessor;
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
        static PerceptionSimulator perceptionSimulator;
        static Lidar_OMD60M lidar_OMD60M;

        static XBoxController.XBoxController xBoxManette;

        static object ExitLock = new object();

        static WpfRobotInterface interfaceRobot;
        static WpfCameraMonitor ConsoleCamera;


        [STAThread] //à ajouter au projet initial

        static void Main(string[] args)
        {
            SciChartSurface.SetRuntimeLicenseKey(@"<LicenseContract>
  <Customer>University of  Toulon</Customer>
  <OrderId>EDUCATIONAL-USE-0109</OrderId>
  <LicenseCount>1</LicenseCount>
  <IsTrialLicense>false</IsTrialLicense>
  <SupportExpires>11/04/2019 00:00:00</SupportExpires>
  <ProductCode>SC-WPF-SDK-PRO-SITE</ProductCode>
  <KeyCode>lwABAQEAAABZVzOfQ0zVAQEAewBDdXN0b21lcj1Vbml2ZXJzaXR5IG9mICBUb3Vsb247T3JkZXJJZD1FRFVDQVRJT05BTC1VU0UtMDEwOTtTdWJzY3JpcHRpb25WYWxpZFRvPTA0LU5vdi0yMDE5O1Byb2R1Y3RDb2RlPVNDLVdQRi1TREstUFJPLVNJVEWDf0QgB8GnCQXI6yAqNM2njjnGbUt2KsujTDzeE+k69K1XYVF1s1x1Hb/i/E3GHaU=</KeyCode>
</LicenseContract>");


            //TODO : Créer un projet World...

            ethernetTeamNetworkAdapter = new EthernetTeamNetworkAdapter();
            serialPort1 = new ReliableSerialPort("FTDI", 230400/*115200*/, Parity.None, 8, StopBits.One);                    
            msgDecoder = new MsgDecoder();
            msgEncoder = new MsgEncoder();
            robotMsgGenerator = new RobotMsgGenerator();
            robotMsgProcessor = new RobotMsgProcessor();

            physicalSimulator = new PhysicalSimulator.PhysicalSimulator();

            int robotId = (int)TeamId.Team1 + (int)RobotId.Robot1;
            int teamId = (int)TeamId.Team1;
            physicalSimulator.RegisterRobot(robotId, 0,0);

            robotPilot = new RobotPilot.RobotPilot(robotId);
            refBoxAdapter = new RefereeBoxAdapter.RefereeBoxAdapter();
            trajectoryPlanner = new TrajectoryPlanner(robotId);
            waypointGenerator = new WaypointGenerator(robotId);
            strategyManager = new StrategyManager.StrategyManager(robotId);
            localWorldMapManager = new LocalWorldMapManager(robotId, teamId);
            lidarSimulator = new LidarSimulator.LidarSimulator(robotId);
            perceptionSimulator = new PerceptionSimulator(robotId);

            if (usingLidar)
                lidar_OMD60M = new Lidar_OMD60M(robotId);

            xBoxManette = new XBoxController.XBoxController(robotId);

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
                //Sur evenement xx              -->>        Action a effectuer
                xBoxManette.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;
                xBoxManette.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnPriseBalleEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent += robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent += robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent += robotMsgGenerator.GenerateMessageTir;
            }
            robotMsgGenerator.OnMessageToRobotGeneratedEvent += msgEncoder.EncodeMessageToRobot;
            msgEncoder.OnMessageEncodedEvent += serialPort1.SendMessage;
            serialPort1.OnDataReceivedEvent += msgDecoder.DecodeMsgReceived;
            msgDecoder.OnMessageDecodedEvent += robotMsgProcessor.ProcessRobotDecodedMessage;

            physicalSimulator.OnPhysicalRobotPositionEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            physicalSimulator.OnPhysicicalObjectListLocationEvent += perceptionSimulator.OnPhysicalObjectListLocationReceived;
            physicalSimulator.OnPhysicalRobotPositionEvent += perceptionSimulator.OnPhysicalRobotPositionReceived;
            physicalSimulator.OnPhysicalBallPositionEvent += perceptionSimulator.OnPhysicalBallPositionReceived;

            perceptionSimulator.OnPerceptionEvent += localWorldMapManager.OnPerceptionReceived;
            //lidarSimulator.OnSimulatedLidarEvent += localWorldMapManager.OnRawLidarDataReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            strategyManager.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            //waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            
            if (usingLidar)
                lidar_OMD60M.OnLidarEvent += localWorldMapManager.OnRawLidarDataReceived;
            
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

        //static int nbMsgSent = 0;
        //static private void HighFrequencyTimer_Tick(object sender, EventArgs e)
        //{
        //    //Utilisé pour des tests de stress sur l'interface série.
        //    //robotPilot.SendSpeedConsigneToRobot();
        //    //nbMsgSent += 1;
        //    //robotPilot.SendSpeedConsigneToMotor();
        //    //nbMsgSent += 1;
        //    //robotPilot.SendPositionFromKalmanFilter();
        //}
        static void ChangeUseOfXBoxController(object sender, BoolEventArgs e)
        {
            if (e.value)
            {
                usingXBoxController = e.value;
                xBoxManette.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;
                xBoxManette.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnPriseBalleEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent += robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent += robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent += robotMsgGenerator.GenerateMessageTir;
            }
            else
            {
                //On se desabonne aux evenements suivants:
                xBoxManette.OnSpeedConsigneEvent -= physicalSimulator.SetRobotSpeed;
                xBoxManette.OnSpeedConsigneEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnPriseBalleEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent -= robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent -= robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent -= robotMsgGenerator.GenerateMessageTir;
            }

        }

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
                interfaceRobot = new RobotInterface.WpfRobotInterface();
                //Sur evenement xx              -->>        Action a effectuer
                msgDecoder.OnMessageDecodedEvent += interfaceRobot.DisplayMessageDecoded;
                msgDecoder.OnMessageDecodedErrorEvent += interfaceRobot.DisplayMessageDecodedError;
                
                robotMsgProcessor.OnIMUDataFromRobotGeneratedEvent += interfaceRobot.UpdateImuDataOnGraph;
                robotMsgProcessor.OnMotorsCurrentsFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsCurrentsOnGraph;
                robotMsgProcessor.OnEnableDisableMotorsACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableMotorsButton;
                robotMsgProcessor.OnEnableDisableTirACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableTirButton;
                xBoxManette.OnSpeedConsigneEvent += interfaceRobot.UpdateSpeedConsigneOnGraph;
                interfaceRobot.OnEnableDisableMotorsFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableDisableMotors;
                interfaceRobot.OnEnableDisableTirFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableDisableTir;
                interfaceRobot.OnEnableDisableControlManetteFromInterfaceGeneratedEvent += ChangeUseOfXBoxController;

                localWorldMapManager.OnLocalWorldMapEvent+= interfaceRobot.OnLocalWorldMapEvent;

                interfaceRobot.ShowDialog();
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
