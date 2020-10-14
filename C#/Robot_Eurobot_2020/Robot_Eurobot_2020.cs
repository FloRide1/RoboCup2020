﻿using AdvancedTimers;
using Constants;
using ExtendedSerialPort;
using LidarOMD60M;
using MessageDecoder;
using MessageEncoder;
using PhysicalSimulator;
using RobotInterface;
using RobotMessageGenerator;
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
using LogRecorder;
using LogReplay;
using LidarProcessor;
using WpfReplayNavigator;
using System.Runtime.InteropServices;
using PositionEstimator;
using Staudt.Engineering.LidaRx.Drivers.R2000;
using System.Net;
using Staudt.Engineering.LidaRx;
using System.Linq;
using ImuProcessor;
using KalmanPositioning;
using StrategyManager;

namespace Robot
{

    //public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
    //// A delegate type to be used as the handler routine
    //// for SetConsoleCtrlHandler.
    //public delegate bool HandlerRoutine(CtrlTypes CtrlType);

    //// An enumerated type for the control messages
    //// sent to the handler routine.
    //public enum CtrlTypes
    //{
    //    CTRL_C_EVENT = 0,
    //    CTRL_BREAK_EVENT,
    //    CTRL_CLOSE_EVENT,
    //    CTRL_LOGOFF_EVENT = 5,
    //    CTRL_SHUTDOWN_EVENT
    //}

    //private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
    //{
    //    // Put your own handler here
    //    return true;
    //}

    //SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

    enum RobotMode
    {
        Acquisition,
        Replay,
        Standard,
        Nolidar,
        NoCamera
    }
    class Robot_Eurobot_2020
    {
        #region Gestion Arret Console (Do not Modify)
        // Declare the SetConsoleCtrlHandler function 
        // as external and receiving a delegate.
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            // Put your own handler here            
            t1.Abort();
            return true;
        }
        #endregion

        static RobotMode robotMode = RobotMode.Standard;

        static bool usingPhysicalSimulator = true;
        static bool usingXBoxController = false;
        static bool usingLidar = true;
        static bool usingLogging = false;
        static bool usingLogReplay = false;
        
        static bool usingRobotInterface = true;
        static bool usingReplayNavigator = true;

        //static HighFreqTimer highFrequencyTimer;
        static HighFreqTimer timerStrategie;
        static ReliableSerialPort serialPort1;
        static MsgDecoder msgDecoder;
        static MsgEncoder msgEncoder;
        static RobotMsgGenerator robotMsgGenerator;
        static RobotMsgProcessor robotMsgProcessor;
        static RobotPilot.RobotPilot robotPilot;
        static AbsolutePositionEstimator absolutePositionEstimator;
        //static PhysicalSimulator.PhysicalSimulator physicalSimulator;
        static WaypointGenerator waypointGenerator;
        static TrajectoryPlanner trajectoryPlanner;
        static KalmanPositioning.KalmanPositioning kalmanPositioning;

        static LocalWorldMapManager localWorldMapManager;
        //static LidarSimulator.LidarSimulator lidarSimulator;
        static ImuProcessor.ImuProcessor imuProcessor;
        static StrategyManager_Eurobot strategyManager;
        static PerceptionSimulator perceptionSimulator;
        //static Lidar_OMD60M_UDP lidar_OMD60M_UDP;
        static Lidar_OMD60M_TCP lidar_OMD60M_TCP;
        static LidarProcessor.LidarProcessor lidarProcessor;
        static XBoxController.XBoxController xBoxManette;

        static object ExitLock = new object();

        static WpfRobotInterface interfaceRobot;
        static LogRecorder.LogRecorder logRecorder;
        static LogReplay.LogReplay logReplay;
        static ReplayNavigator replayNavigator;


        [STAThread] //à ajouter au projet initial

        LidarStatusEvent methodeStatus()
        {
            LidarStatusEvent ev=null;

            return ev;
        }
        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);

            // Set this code once in App.xaml.cs or application startup
            SciChartSurface.SetRuntimeLicenseKey("RJWA77RbaJDdCRJpg4Iunl5Or6/FPX1xT+Gzu495Eaa0ZahxWi3jkNFDjUb/w70cHXyv7viRTjiNRrYqnqGA+Dc/yzIIzTJlf1s4DJvmQc8TCSrH7MBeQ2ON5lMs/vO0p6rBlkaG+wwnJk7cp4PbOKCfEQ4NsMb8cT9nckfdcWmaKdOQNhHsrw+y1oMR7rIH+rGes0jGGttRDhTOBxwUJK2rBA9Z9PDz2pGOkPjy9fwQ4YY2V4WPeeqM+6eYxnDZ068mnSCPbEnBxpwAldwXTyeWdXv8sn3Dikkwt3yqphQxvs0h6a8Dd6K/9UYni3o8pRkTed6SWodQwICcewfHTyGKQowz3afARj07et2h+becxowq3cRHL+76RyukbIXMfAqLYoT2UzDJNsZqcPPq/kxeXujuhT4SrNF3444MU1GaZZ205KYEMFlz7x/aEnjM6p3BuM6ZuO3Fjf0A0Ki/NBfS6n20E07CTGRtI6AsM2m59orPpI8+24GFlJ9xGTjoRA==");

            switch (robotMode)
            {
                case RobotMode.Standard:
                    usingLidar = true;
                    usingLogging = false;
                    usingLogReplay = false;
                    break;
                case RobotMode.Acquisition:
                    usingLidar = true;
                    usingLogging = true;
                    usingLogReplay = false;
                    break;
                case RobotMode.Replay:
                    usingLidar = false;
                    usingLogging = false;
                    usingLogReplay = true;
                    break;
                case RobotMode.Nolidar:
                    usingLidar = false;
                    usingLogging = false;
                    usingLogReplay = false;
                    break;
                case RobotMode.NoCamera:
                    usingLidar = true;
                    usingLogging = false;
                    usingLogReplay = false;
                    break;
            }

            serialPort1 = new ReliableSerialPort("COM4", 115200, Parity.None, 8, StopBits.One);
            msgDecoder = new MsgDecoder();
            msgEncoder = new MsgEncoder();
            robotMsgGenerator = new RobotMsgGenerator();
            robotMsgProcessor = new RobotMsgProcessor();

            //physicalSimulator = new PhysicalSimulator.PhysicalSimulator();

            int robotId = (int)TeamId.Team1 + (int)RobotId.Robot1;
            int teamId = (int)TeamId.Team1;
            //physicalSimulator.RegisterRobot(robotId, 0, 0);

            robotPilot = new RobotPilot.RobotPilot(robotId);
            strategyManager = new StrategyManager_Eurobot(robotId, teamId);
            waypointGenerator = new WaypointGenerator(robotId);
            trajectoryPlanner = new TrajectoryPlanner(robotId);
            kalmanPositioning = new KalmanPositioning.KalmanPositioning(robotId, 50, 0.2, 0.2, 0.2, 0.1, 0.1, 0.1, 0.02);

            localWorldMapManager = new LocalWorldMapManager(robotId, teamId);
            //lidarSimulator = new LidarSimulator.LidarSimulator(robotId);
            perceptionSimulator = new PerceptionSimulator(robotId);
            imuProcessor = new ImuProcessor.ImuProcessor(robotId);

            if (usingLidar)
            {
                lidar_OMD60M_TCP = new Lidar_OMD60M_TCP(50, R2000SamplingRate._40kHz);
            }

            if (usingLidar || usingLogReplay)
            {
                lidarProcessor = new LidarProcessor.LidarProcessor(robotId);
            }

            xBoxManette = new XBoxController.XBoxController(robotId);
                    
           

            //Démarrage des interface de visualisation
            if (usingRobotInterface)
                StartRobotInterface();
            if (usingLogReplay)
                StartReplayNavigatorInterface();

            //Démarrage du logger si besoin
            if (usingLogging)
                logRecorder = new LogRecorder.LogRecorder();

            //Démarrage du log replay si l'interface est utilisée et existe ou si elle n'est pas utilisée, sinon on bloque
            if (usingLogReplay)
                logReplay = new LogReplay.LogReplay();
             
            //Liens entre modules
            strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            strategyManager.OnHeatMapEvent += waypointGenerator.OnStrategyHeatMapReceived;
            waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;


            //Filtre de Kalman
            robotMsgProcessor.OnSpeedDataFromRobotGeneratedEvent += kalmanPositioning.OnOdometryRobotSpeedReceived;
            imuProcessor.OnGyroSpeedEvent += kalmanPositioning.OnGyroRobotSpeedReceived;
            kalmanPositioning.OnKalmanLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            kalmanPositioning.OnKalmanLocationEvent += perceptionSimulator.OnPhysicalRobotPositionReceived;

            //L'envoi des commandes dépend du fait qu'on soit en mode manette ou pas. 
            //Il faut donc enregistrer les évènement ou pas en fonction de l'activation
            //C'est fait plus bas dans le code avec la fonction que l'on appelle
            ConfigControlEvents(useXBoxController: true);
            
            //Gestion des messages envoyé par le robot
            robotMsgGenerator.OnMessageToRobotGeneratedEvent += msgEncoder.EncodeMessageToRobot;
            msgEncoder.OnMessageEncodedEvent += serialPort1.SendMessage;

            //Gestion des messages reçu par le robot
            serialPort1.OnDataReceivedEvent += msgDecoder.DecodeMsgReceived;
            msgDecoder.OnMessageDecodedEvent += robotMsgProcessor.ProcessRobotDecodedMessage;
            robotMsgProcessor.OnIMURawDataFromRobotGeneratedEvent += imuProcessor.OnIMURawDataReceived;            
            
            //physicalSimulator.OnPhysicalRobotLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            //physicalSimulator.OnPhysicicalObjectListLocationEvent += perceptionSimulator.OnPhysicalObjectListLocationReceived;
            //physicalSimulator.OnPhysicalRobotLocationEvent += perceptionSimulator.OnPhysicalRobotPositionReceived;
            //physicalSimulator.OnPhysicalBallPositionEvent += perceptionSimulator.OnPhysicalBallPositionReceived;

            perceptionSimulator.OnPerceptionEvent += localWorldMapManager.OnPerceptionReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            strategyManager.OnHeatMapEvent += localWorldMapManager.OnHeatMapReceived;
            
            if (usingLidar)
            {
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += lidarProcessor.OnRawLidarDataReceived;
                //lidar_OMD60M.OnLidarDecodedFrameEvent += absolutePositionEstimator.OnRawLidarDataReceived;
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += localWorldMapManager.OnRawLidarDataReceived;
                lidarProcessor.OnLidarObjectProcessedEvent += localWorldMapManager.OnLidarObjectsReceived;
            }

            //Events de recording
            if (usingLogging)
            {
                //lidar_OMD60M_UDP.OnLidarDecodedFrameEvent += logRecorder.OnRawLidarDataReceived;
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += logRecorder.OnRawLidarDataReceived;
                imuProcessor.OnIMUProcessedDataGeneratedEvent += logRecorder.OnIMURawDataReceived;
                robotMsgProcessor.OnSpeedDataFromRobotGeneratedEvent += logRecorder.OnSpeedDataReceived;
                //omniCamera.OpenCvMatImageEvent += logRecorder.OnOpenCVMatImageReceived;
            }

            //Events de replay
            if (usingLogReplay)
            {
                logReplay.OnLidarEvent += lidarProcessor.OnRawLidarDataReceived;
                //logReplay.OnCameraImageEvent += imageProcessingPositionFromOmniCamera.ProcessOpenCvMatImage;
                //logReplay.OnCameraImageEvent += absolutePositionEstimator.AbsolutePositionEvaluation;
                lidarProcessor.OnLidarProcessedEvent += localWorldMapManager.OnRawLidarDataReceived;
                lidarProcessor.OnLidarObjectProcessedEvent += localWorldMapManager.OnLidarObjectsReceived;
            }

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
            var role = (StrategyManager.PlayerRole)rand.Next((int)(int)StrategyManager.PlayerRole.Centre, (int)StrategyManager.PlayerRole.Centre);
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
            ConfigControlEvents(e.value);
        }

        private static void ConfigControlEvents(bool useXBoxController)
        {
            usingXBoxController = useXBoxController;
            if (usingXBoxController)
            {
                //xBoxManette.OnSpeedConsigneEvent += physicalSimulator.SetRobotSpeed;
                xBoxManette.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnPriseBalleEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent += robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent += robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent += robotMsgGenerator.GenerateMessageTir;
                xBoxManette.OnStopEvent += robotMsgGenerator.GenerateMessageSTOP;

                trajectoryPlanner.OnSpeedConsigneEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                //Gestion des events liés à une détection de collision soft
                trajectoryPlanner.OnCollisionEvent -= kalmanPositioning.OnCollisionReceived;
            }
            else
            {
                //On se desabonne aux evenements suivants:
                //xBoxManette.OnSpeedConsigneEvent -= physicalSimulator.SetRobotSpeed;
                xBoxManette.OnSpeedConsigneEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnPriseBalleEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent -= robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent -= robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent -= robotMsgGenerator.GenerateMessageTir;
                xBoxManette.OnStopEvent -= robotMsgGenerator.GenerateMessageSTOP;

                trajectoryPlanner.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                //Gestion des events liés à une détection de collision soft
                trajectoryPlanner.OnCollisionEvent += kalmanPositioning.OnCollisionReceived;
            }
        }

        static void ExitProgram()
        {
            lock (ExitLock)
            {
                Monitor.Pulse(ExitLock);
            }
        }

        static Thread t1;
        static void StartRobotInterface()
        {
            t1 = new Thread(() =>
            {
                //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.
                interfaceRobot = new RobotInterface.WpfRobotInterface();
                interfaceRobot.Loaded += RegisterRobotInterfaceEvents;
                interfaceRobot.ShowDialog();
            });
            t1.SetApartmentState(ApartmentState.STA);
            t1.Start();
        }

        static void RegisterRobotInterfaceEvents(object sender, EventArgs e)
        {
            //Sur evenement xx        -->>        Action a effectuer
            msgDecoder.OnMessageDecodedEvent += interfaceRobot.DisplayMessageDecoded;
            msgDecoder.OnMessageDecodedErrorEvent += interfaceRobot.DisplayMessageDecodedError;

            //lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += interfaceRobot.OnRawLidarDataReceived;
            lidarProcessor.OnLidarProcessedEvent += interfaceRobot.OnRawLidarDataReceived;
            //lidarProcessor.OnLidarObjectProcessedEvent +=  

            if (!usingLogReplay)
            {
                imuProcessor.OnIMUProcessedDataGeneratedEvent += interfaceRobot.UpdateImuDataOnGraph;
                robotMsgProcessor.OnMotorsCurrentsFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsCurrentsOnGraph;
                robotMsgProcessor.OnEncoderRawDataFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsEncRawDataOnGraph;


                robotMsgProcessor.OnEnableDisableMotorsACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableMotorsButton;
                robotMsgProcessor.OnEnableDisableTirACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableTirButton;
                robotMsgProcessor.OnMotorVitesseDataFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsSpeedsOnGraph;
                robotMsgProcessor.OnSpeedConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateMotorSpeedConsigneOnGraph;
                robotMsgProcessor.OnEnableAsservissementACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableAsservissementButton;
                robotMsgProcessor.OnSpeedDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedDataOnGraph;
                robotMsgProcessor.OnPIDDebugDataFromRobotGeneratedEvent += interfaceRobot.UpdatePIDDebugDataOnGraph;
                robotMsgProcessor.OnErrorTextFromRobotGeneratedEvent += interfaceRobot.AppendConsole;

                robotMsgProcessor.OnMessageCounterEvent += interfaceRobot.MessageCounterReceived;
            }
            xBoxManette.OnSpeedConsigneEvent += interfaceRobot.UpdateSpeedConsigneOnGraph;
            interfaceRobot.OnEnableDisableMotorsFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableDisableMotors;
            interfaceRobot.OnEnableDisableTirFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableDisableTir;
            interfaceRobot.OnEnableDisableControlManetteFromInterfaceGeneratedEvent += ChangeUseOfXBoxController;
            interfaceRobot.OnEnableAsservissementFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableAsservissement;
            interfaceRobot.OnEnableEncodersRawDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableEncoderRawData;
            interfaceRobot.OnEnableMotorCurrentDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableMotorCurrentData;
            interfaceRobot.OnEnableMotorsSpeedConsigneDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableMotorSpeedConsigne;
            interfaceRobot.OnSetRobotPIDFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageSetPIDValueToRobot;
            interfaceRobot.OnEnablePIDDebugDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnablePIDDebugData;
            interfaceRobot.OnCalibrateGyroFromInterfaceGeneratedEvent += imuProcessor.OnCalibrateGyroFromInterfaceGeneratedEvent;

            localWorldMapManager.OnLocalWorldMapEvent += interfaceRobot.OnLocalWorldMapEvent;
            if (usingLogReplay)
            {
                logReplay.OnIMUEvent += interfaceRobot.UpdateImuDataOnGraph;
                logReplay.OnSpeedDataEvent += interfaceRobot.UpdateSpeedDataOnGraph;
            }
        }
        
        
        static Thread t3;
        static void StartReplayNavigatorInterface()
        {
            t3 = new Thread(() =>
            {
                //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.

                replayNavigator = new ReplayNavigator();
                replayNavigator.Loaded += RegisterReplayInterfaceEvents;
                replayNavigator.ShowDialog();

            });
            t3.SetApartmentState(ApartmentState.STA);
            t3.Start();
        }
        
        static void RegisterReplayInterfaceEvents(object sender, EventArgs e)
        {
            if (usingLogReplay)
            {
                replayNavigator.OnPauseEvent += logReplay.PauseReplay;
                replayNavigator.OnPlayEvent += logReplay.StartReplay;
                replayNavigator.OnLoopEvent += logReplay.LoopReplayChanged;
                logReplay.OnUpdateFileNameEvent += replayNavigator.UpdateFileName;
                replayNavigator.OnNextEvent += logReplay.NextReplay;
                replayNavigator.OnPrevEvent += logReplay.PreviousReplay;
                replayNavigator.OnRepeatEvent += logReplay.RepeatReplayChanged;
                replayNavigator.OnOpenFileEvent += logReplay.OpenReplayFile;
                replayNavigator.OnOpenFolderEvent += logReplay.OpenReplayFolder;
                replayNavigator.OnSpeedChangeEvent += logReplay.ReplaySpeedChanged;
            }

            //imageProcessingPositionFromOmniCamera.OnOpenCvMatImageProcessedEvent += ConsoleCamera.DisplayOpenCvMatImage;
        }

        private static void RefBoxAdapter_DataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            throw new NotImplementedException();
        }
    }

}