using AdvancedTimers;
using Constants;
using MessageDecoder;
using MessageEncoder;
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
using WpfReplayNavigator;
using System.Runtime.InteropServices;
using StrategyManager;
using HerkulexManagerNS;
using ReliableSerialPortNS;
using Staudt.Engineering.LidaRx;
using Staudt.Engineering.LidaRx.Drivers.R2000;
using LidaRxR2000NS;

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

        static bool usingPhysicalSimulator;
        static bool usingXBoxController;
        static bool usingLidar;
        static bool usingLogging;
        static bool usingLogReplay;
        
        static bool usingRobotInterface = true;
        static bool usingReplayNavigator = true;

        //static HighFreqTimer highFrequencyTimer;
        static HighFreqTimer timerStrategie;
        //static ReliableSerialPort serialPort1;
        static USBVendor.USBVendor usbDriver;
        static MsgDecoder msgDecoder;
        static MsgEncoder msgEncoder;
        static MsgGenerator robotMsgGenerator;
        static MsgProcessor robotMsgProcessor;
        static WaypointGenerator waypointGenerator;
        static TrajectoryPlanner trajectoryPlanner;
        static KalmanPositioning.KalmanPositioning kalmanPositioning;

        static LocalWorldMapManager localWorldMapManager;
        //Lien de transmission par socket
        //static UDPMulticastSender baseStationUdpMulticastSender = null;
        //static UDPMulticastReceiver baseStationUdpMulticastReceiver = null;
        //static UDPMulticastInterpreter baseStationUdpMulticastInterpreter = null;
        //static UDPMulticastSender robotUdpMulticastSender = null;
        //static UDPMulticastReceiver robotUdpMulticastReceiver = null;
        //static UDPMulticastInterpreter robotUdpMulticastInterpreter = null;

        static GlobalWorldMapManager globalWorldMapManager;
                
        static ImuProcessor.ImuProcessor imuProcessor;
        static StrategyManager_Eurobot strategyManager;
        static PerceptionManager perceptionManager;        
        static LidaRxR2000 lidar_OMD60M_TCP;
        static XBoxController.XBoxController xBoxManette;

        static HerkulexManager herkulexManager;

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
            // Set this code once in App.xaml.cs or application startup
            SciChartSurface.SetRuntimeLicenseKey("RJWA77RbaJDdCRJpg4Iunl5Or6/FPX1xT+Gzu495Eaa0ZahxWi3jkNFDjUb/w70cHXyv7viRTjiNRrYqnqGA+Dc/yzIIzTJlf1s4DJvmQc8TCSrH7MBeQ2ON5lMs/vO0p6rBlkaG+wwnJk7cp4PbOKCfEQ4NsMb8cT9nckfdcWmaKdOQNhHsrw+y1oMR7rIH+rGes0jGGttRDhTOBxwUJK2rBA9Z9PDz2pGOkPjy9fwQ4YY2V4WPeeqM+6eYxnDZ068mnSCPbEnBxpwAldwXTyeWdXv8sn3Dikkwt3yqphQxvs0h6a8Dd6K/9UYni3o8pRkTed6SWodQwICcewfHTyGKQowz3afARj07et2h+becxowq3cRHL+76RyukbIXMfAqLYoT2UzDJNsZqcPPq/kxeXujuhT4SrNF3444MU1GaZZ205KYEMFlz7x/aEnjM6p3BuM6ZuO3Fjf0A0Ki/NBfS6n20E07CTGRtI6AsM2m59orPpI8+24GFlJ9xGTjoRA==");

            //On ajoute un gestionnaire d'évènement pour détecter la fermeture de l'application
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);


            //To use configuration file, must be declare variable no static
            //ConfigRobotEurobot cfgRobot = FileManager.JsonSerialize<ConfigRobotEurobot>.DeserializeObjectFromFile(@"Configs", "Robot");
            //robotMode = cfgRobot.RobotMode;
            //usingPhysicalSimulator = cfgRobot.UsingPhysicalSimulator;
            //usingXBoxController = cfgRobot.UsingXBoxController;
            //usingLidar = cfgRobot.UsingLidar;
            //usingLogging = cfgRobot.UsingLogging;
            //usingLogReplay = cfgRobot.UsingLogReplay;
            //usingRobotInterface = cfgRobot.usingRobotInterface;
            //usingReplayNavigator = cfgRobot.usingReplayNavigator;

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
            //ConfigSerialPort cfgSerialPort = FileManager.JsonSerialize<ConfigSerialPort>.DeserializeObjectFromFile(@"Configs", "SerialPort");
            //serialPort1 = new ReliableSerialPort(cfgSerialPort.CommName, cfgSerialPort.ComBaudrate, cfgSerialPort.Parity, cfgSerialPort.DataByte, cfgSerialPort.StopByte);
            //serialPort1 = new ReliableSerialPort("COM1", 115200, Parity.None, 8, StopBits.One);
            usbDriver = new USBVendor.USBVendor();
            msgDecoder = new MsgDecoder();
            msgEncoder = new MsgEncoder();
            robotMsgGenerator = new MsgGenerator();
            robotMsgProcessor = new MsgProcessor(Competition.Eurobot);
            
            int robotId = (int)TeamId.Team1 + (int)RobotId.Robot1;
            int teamId = (int)TeamId.Team1;

            perceptionManager = new PerceptionManager(robotId);
            imuProcessor = new ImuProcessor.ImuProcessor(robotId);
            kalmanPositioning = new KalmanPositioning.KalmanPositioning(robotId, 50, 0.2, 0.2, 0.2, 0.1, 0.1, 0.1, 0.02);

            localWorldMapManager = new LocalWorldMapManager(robotId, teamId);

            //On simule une base station en local
            //baseStationUdpMulticastSender = new UDPMulticastSender(0, "224.16.32.79");
            //baseStationUdpMulticastReceiver = new UDPMulticastReceiver(0, "224.16.32.79");
            //baseStationUdpMulticastInterpreter = new UDPMulticastInterpreter(0);

            //robotUdpMulticastSender = new UDPMulticastSender(robotId, "224.16.32.79");
            //robotUdpMulticastReceiver = new UDPMulticastReceiver(robotId, "224.16.32.79");
            //robotUdpMulticastInterpreter = new UDPMulticastInterpreter(robotId);

            globalWorldMapManager = new GlobalWorldMapManager(robotId, "0.0.0.0");
            strategyManager = new StrategyManager_Eurobot(robotId, teamId);
            waypointGenerator = new WaypointGenerator(robotId, "Eurobot");
            trajectoryPlanner = new TrajectoryPlanner(robotId);

            herkulexManager = new HerkulexManager();

            herkulexManager.AddServo(ServoId.BrasCentral, HerkulexDescription.JOG_MODE.positionControlJOG);
            herkulexManager.AddServo(ServoId.BrasDroit, HerkulexDescription.JOG_MODE.positionControlJOG);
            herkulexManager.AddServo(ServoId.BrasGauche, HerkulexDescription.JOG_MODE.positionControlJOG);
            herkulexManager.AddServo(ServoId.PorteDrapeau, HerkulexDescription.JOG_MODE.positionControlJOG);

            if (usingLidar)
            {
                lidar_OMD60M_TCP = new LidaRxR2000(50, R2000SamplingRate._72kHz);
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
            strategyManager.OnRefereeBoxCommandEvent += globalWorldMapManager.OnRefereeBoxCommandReceived;
            strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            strategyManager.OnHeatMapEvent += waypointGenerator.OnStrategyHeatMapReceived;
            if(usingLidar)
                strategyManager.OnMessageEvent += lidar_OMD60M_TCP.OnMessageReceivedEvent;
            strategyManager.OnSetRobotSpeedPolarPIDEvent += robotMsgGenerator.GenerateMessageSetupSpeedPolarPIDToRobot;
            strategyManager.OnSetRobotSpeedIndependantPIDEvent += robotMsgGenerator.GenerateMessageSetupSpeedIndependantPIDToRobot;
            strategyManager.OnSetAsservissementModeEvent += robotMsgGenerator.GenerateMessageSetAsservissementMode;
            strategyManager.OnHerkulexPositionRequestEvent += herkulexManager.OnHerkulexPositionRequestEvent;
            strategyManager.OnSetSpeedConsigneToMotor += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
            strategyManager.OnEnableDisableMotorCurrentDataEvent += robotMsgGenerator.GenerateMessageEnableMotorCurrentData;
            herkulexManager.OnHerkulexSendToSerialEvent += robotMsgGenerator.GenerateMessageForwardHerkulex;

            waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;
            
            if (usingLidar)
            {
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += perceptionManager.OnRawLidarDataReceived;
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += localWorldMapManager.OnRawLidarDataReceived;                
            }

            //Filtre de Kalman
            perceptionManager.OnAbsolutePositionEvent += kalmanPositioning.OnAbsolutePositionCalculatedEvent;
            robotMsgProcessor.OnSpeedPolarOdometryFromRobotEvent += kalmanPositioning.OnOdometryRobotSpeedReceived;
            imuProcessor.OnGyroSpeedEvent += kalmanPositioning.OnGyroRobotSpeedReceived;
            kalmanPositioning.OnKalmanLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            //trajectoryPlanner.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot; //Configuré dans le gestionnaire de manette
            kalmanPositioning.OnKalmanLocationEvent += perceptionManager.OnPhysicalRobotPositionReceived;
            kalmanPositioning.OnKalmanLocationEvent += strategyManager.OnPositionRobotReceived;

            //L'envoi des commandes dépend du fait qu'on soit en mode manette ou pas. 
            //Il faut donc enregistrer les évènement ou pas en fonction de l'activation
            //C'est fait plus bas dans le code avec la fonction que l'on appelle
            ConfigControlEvents(usingXBoxController);
            
            //Gestion des messages envoyé par le robot
            robotMsgGenerator.OnMessageToRobotGeneratedEvent += msgEncoder.EncodeMessageToRobot;
            //msgEncoder.OnMessageEncodedEvent += serialPort1.SendMessage;
            msgEncoder.OnMessageEncodedEvent += usbDriver.SendUSBMessage;

            //Gestion des messages reçu par le robot
            //serialPort1.OnDataReceivedEvent += msgDecoder.DecodeMsgReceived;
            usbDriver.OnUSBDataReceivedEvent += msgDecoder.DecodeMsgReceived;
            msgDecoder.OnMessageDecodedEvent += robotMsgProcessor.ProcessRobotDecodedMessage;
            robotMsgProcessor.OnIMURawDataFromRobotGeneratedEvent += imuProcessor.OnIMURawDataReceived;
            robotMsgProcessor.OnIOValuesFromRobotGeneratedEvent += strategyManager.OnIOValuesFromRobotEvent;
            robotMsgProcessor.OnIOValuesFromRobotGeneratedEvent += perceptionManager.OnIOValuesFromRobotEvent;
            robotMsgProcessor.OnMotorsCurrentsFromRobotGeneratedEvent += strategyManager.OnMotorCurrentReceive;

            //physicalSimulator.OnPhysicalRobotLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            //physicalSimulator.OnPhysicicalObjectListLocationEvent += perceptionSimulator.OnPhysicalObjectListLocationReceived;
            //physicalSimulator.OnPhysicalRobotLocationEvent += perceptionSimulator.OnPhysicalRobotPositionReceived;
            //physicalSimulator.OnPhysicalBallPositionEvent += perceptionSimulator.OnPhysicalBallPositionReceived;

            //Le local Manager n'est là que pour assurer le stockage de ma local world map avant affichage et transmission des infos, il ne doit pas calculer quoique ce soit, 
            //c'est le perception manager qui le fait.
            perceptionManager.OnPerceptionEvent += localWorldMapManager.OnPerceptionReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            strategyManager.OnHeatMapEvent += localWorldMapManager.OnHeatMapStrategyReceived;
            strategyManager.OnGameStateChangedEvent += trajectoryPlanner.OnGameStateChangeReceived;
            strategyManager.OnMirrorModeForwardEvent += perceptionManager.OnMirrorModeReceived;
            strategyManager.OnEnableMotorsEvent += robotMsgGenerator.GenerateMessageEnableDisableMotors;
            waypointGenerator.OnHeatMapEvent += localWorldMapManager.OnHeatMapWaypointReceived;

            //Transfert de la local map vers la global world map via UPD en mode Multicast : 
            //inutile dans Eurobot mais permet une extension radio simple.
            //localWorldMapManager.OnMulticastSendLocalWorldMapEvent += robotUdpMulticastSender.OnMulticastMessageToSendReceived;
            //baseStationUdpMulticastReceiver.OnDataReceivedEvent += baseStationUdpMulticastInterpreter.OnMulticastDataReceived;
            //baseStationUdpMulticastInterpreter.OnLocalWorldMapEvent += globalWorldMapManager.OnLocalWorldMapReceived;
            //globalWorldMapManager.OnMulticastSendGlobalWorldMapEvent += baseStationUdpMulticastSender.OnMulticastMessageToSendReceived;
            //robotUdpMulticastReceiver.OnDataReceivedEvent += robotUdpMulticastInterpreter.OnMulticastDataReceived;
            //robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += strategyManager.OnGlobalWorldMapReceived;
            //robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += waypointGenerator.OnGlobalWorldMapReceived;
            //robotUdpMulticastInterpreter.OnGlobalWorldMapEvent += perceptionManager.OnGlobalWorldMapReceived;

            //On essaie d'enlever la communication UDP interne
            localWorldMapManager.OnLocalWorldMapBypassEvent += globalWorldMapManager.OnLocalWorldMapReceived;
            globalWorldMapManager.OnGlobalWorldMapBypassEvent += strategyManager.OnGlobalWorldMapReceived;
            globalWorldMapManager.OnGlobalWorldMapBypassEvent += waypointGenerator.OnGlobalWorldMapReceived;
            globalWorldMapManager.OnGlobalWorldMapBypassEvent += perceptionManager.OnGlobalWorldMapReceived;

            //Events de recording
            if (usingLogging)
            {
                //lidar_OMD60M_UDP.OnLidarDecodedFrameEvent += logRecorder.OnRawLidarDataReceived;
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += logRecorder.OnRawLidarDataReceived;
                imuProcessor.OnIMUProcessedDataGeneratedEvent += logRecorder.OnIMURawDataReceived;
                robotMsgProcessor.OnSpeedPolarOdometryFromRobotEvent += logRecorder.OnSpeedDataReceived;
                //omniCamera.OpenCvMatImageEvent += logRecorder.OnOpenCVMatImageReceived;
            }

            //Events de replay
            if (usingLogReplay)
            {
                logReplay.OnLidarEvent += perceptionManager.OnRawLidarDataReceived;
                //logReplay.OnCameraImageEvent += imageProcessingPositionFromOmniCamera.ProcessOpenCvMatImage;
                //logReplay.OnCameraImageEvent += absolutePositionEstimator.AbsolutePositionEvaluation;
                //lidarProcessor.OnLidarObjectProcessedEvent += localWorldMapManager.OnLidarObjectsReceived;
            }


            
            //Timer de stratégie
            timerStrategie = new HighFreqTimer(0.5);
            timerStrategie.Tick += TimerStrategie_Tick;
            timerStrategie.Start();

            while(!exitSystem)
            {
                Thread.Sleep(500);
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
                trajectoryPlanner.OnSpeedConsigneEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                //if (interfaceRobot != null)
                //{
                //    xBoxManette.OnSpeedConsigneEvent += interfaceRobot.UpdateSpeedConsigneOnGraph;
                //}
                xBoxManette.OnPriseBalleEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent += robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent += robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent += robotMsgGenerator.GenerateMessageTir;
                xBoxManette.OnStopEvent += robotMsgGenerator.GenerateMessageSTOP;

                //Gestion des events liés à une détection de collision soft
                trajectoryPlanner.OnCollisionEvent -= kalmanPositioning.OnCollisionReceived;
            }
            else
            {
                //On se desabonne aux evenements suivants:
                //xBoxManette.OnSpeedConsigneEvent -= physicalSimulator.SetRobotSpeed;
                trajectoryPlanner.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                xBoxManette.OnSpeedConsigneEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
                
                xBoxManette.OnPriseBalleEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;
                xBoxManette.OnMoveTirUpEvent -= robotMsgGenerator.GenerateMessageMoveTirUp;
                xBoxManette.OnMoveTirDownEvent -= robotMsgGenerator.GenerateMessageMoveTirDown;
                xBoxManette.OnTirEvent -= robotMsgGenerator.GenerateMessageTir;
                xBoxManette.OnStopEvent -= robotMsgGenerator.GenerateMessageSTOP;

                //Gestion des events liés à une détection de collision soft
                trajectoryPlanner.OnCollisionEvent += kalmanPositioning.OnCollisionReceived;
                strategyManager.OnCollisionEvent += kalmanPositioning.OnCollisionReceived;
            }
        }


        /******************************************* Trap app termination ***************************************/
        static bool exitSystem = false;
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        enum CtrlType
        {
            CTRL_C_EVENT=0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 3,
            CTRL_SHUTDOWN_EVENT = 4
        }

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;
        //Gestion de la terminaison de l'application de manière propre
        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("Existing on CTRL+C or process kill or shutdown...");

            //Nettoyage des process à faire ici
            //serialPort1.Close();

            Console.WriteLine("Nettoyage effectué");
            exitSystem = true;

            //Sortie
            Environment.Exit(-1);
            return true;
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
            if(usingLidar)
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += interfaceRobot.OnRawLidarDataReceived;
            perceptionManager.OnLidarBalisePointListForDebugEvent += interfaceRobot.OnRawLidarBalisePointsReceived;

            robotMsgGenerator.OnMessageToDisplaySpeedPolarPidSetupEvent += interfaceRobot.OnMessageToDisplayPolarSpeedPidSetupReceived;
            robotMsgGenerator.OnMessageToDisplaySpeedIndependantPidSetupEvent += interfaceRobot.OnMessageToDisplayIndependantSpeedPidSetupReceived;
            trajectoryPlanner.OnMessageToDisplayPositionPidSetupEvent += interfaceRobot.OnMessageToDisplayPositionPidSetupReceived;
            trajectoryPlanner.OnMessageToDisplayPositionPidCorrectionEvent += interfaceRobot.OnMessageToDisplayPositionPidCorrectionReceived;

            //herkulexManager.OnHerkulexServoInformationEvent += interfaceRobot.OnHerkulexServoInformationReceived;


            //On récupère les évènements de type refbox, qui sont ici des tests manuels dans le globalManager pour lancer à la main des actions ou stratégies
            interfaceRobot.OnRefereeBoxCommandEvent += globalWorldMapManager.OnRefereeBoxCommandReceived;

            if (!usingLogReplay)
            {
                imuProcessor.OnIMUProcessedDataGeneratedEvent += interfaceRobot.UpdateImuDataOnGraph;
                robotMsgProcessor.OnMotorsCurrentsFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsCurrentsOnGraph;
                robotMsgProcessor.OnEncoderRawDataFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsEncRawDataOnGraph;

                robotMsgProcessor.OnEnableDisableMotorsACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableMotorsButton;
                robotMsgProcessor.OnEnableDisableTirACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableTirButton;

                //robotMsgProcessor.OnMotorVitesseDataFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsSpeedsOnGraph;
                //robotMsgProcessor.OnAuxiliarySpeedConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateAuxiliarySpeedConsigneOnGraph;

                robotMsgProcessor.OnEnableAsservissementACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableAsservissementButton;
                robotMsgProcessor.OnSpeedPolarOdometryFromRobotEvent += interfaceRobot.UpdateSpeedPolarOdometryOnInterface;
                robotMsgProcessor.OnIndependantOdometrySpeedFromRobotEvent += interfaceRobot.UpdateSpeedIndependantOdometryOnInterface;
                robotMsgProcessor.OnSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedPolarPidErrorCorrectionConsigneDataOnGraph;
                robotMsgProcessor.OnSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedIndependantPidErrorCorrectionConsigneDataOnGraph;
                robotMsgProcessor.OnSpeedPolarPidCorrectionDataFromRobotEvent += interfaceRobot.UpdateSpeedPolarPidCorrectionData;
                robotMsgProcessor.OnSpeedIndependantPidCorrectionDataFromRobotEvent += interfaceRobot.UpdateSpeedIndependantPidCorrectionData;

                robotMsgProcessor.OnErrorTextFromRobotGeneratedEvent += interfaceRobot.AppendConsole;
                robotMsgProcessor.OnPowerMonitoringValuesFromRobotGeneratedEvent += interfaceRobot.UpdatePowerMonitoringValues;
                robotMsgProcessor.OnEnableMotorCurrentACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableMotorCurrentCheckBox;
                robotMsgProcessor.OnEnableEncoderRawDataACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableEncoderRawDataCheckBox;
                robotMsgProcessor.OnEnableAsservissementDebugDataACKFromRobotEvent += interfaceRobot.ActualizeEnableAsservissementDebugDataCheckBox;
                robotMsgProcessor.OnEnableMotorSpeedConsigneDataACKFromRobotGeneratedEvent += interfaceRobot.ActualizEnableMotorSpeedConsigneCheckBox;
                robotMsgProcessor.OnEnablePowerMonitoringDataACKFromRobotGeneratedEvent += interfaceRobot.ActualizEnablePowerMonitoringCheckBox;

                robotMsgProcessor.OnMessageCounterEvent += interfaceRobot.MessageCounterReceived;
            }

            robotMsgGenerator.OnSetSpeedConsigneToRobotReceivedEvent += interfaceRobot.UpdatePolarSpeedConsigneOnGraph; //Valable quelque soit la source des consignes vitesse
            interfaceRobot.OnEnableDisableMotorsFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableDisableMotors;
            interfaceRobot.OnEnableDisableServosFromInterfaceGeneratedEvent += herkulexManager.OnEnableDisableServosRequestEvent;
            interfaceRobot.OnEnableDisableTirFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableDisableTir;
            interfaceRobot.OnEnableDisableControlManetteFromInterfaceGeneratedEvent += ChangeUseOfXBoxController;
            interfaceRobot.OnSetAsservissementModeFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageSetAsservissementMode;
            interfaceRobot.OnEnableEncodersRawDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableEncoderRawData;
            interfaceRobot.OnEnableMotorCurrentDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableMotorCurrentData;            
            interfaceRobot.OnEnableMotorsSpeedConsigneDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableMotorSpeedConsigne;
            interfaceRobot.OnSetRobotPIDFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageSetupSpeedPolarPIDToRobot;
            interfaceRobot.OnEnableAsservissementDebugDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableAsservissementDebugData;
            interfaceRobot.OnEnableSpeedPidCorrectionDataFromInterfaceEvent += robotMsgGenerator.GenerateMessageEnableSpeedPidCorrectionData;
            interfaceRobot.OnCalibrateGyroFromInterfaceGeneratedEvent += imuProcessor.OnCalibrateGyroFromInterfaceGeneratedEvent;
            interfaceRobot.OnEnablePowerMonitoringDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnablePowerMonitoring;

            localWorldMapManager.OnLocalWorldMapEventForDisplayOnly += interfaceRobot.OnLocalWorldMapStrategyEvent;
            localWorldMapManager.OnLocalWorldMapEventForDisplayOnly += interfaceRobot.OnLocalWorldMapWayPointEvent;

            if (usingLogReplay)
            {
                logReplay.OnIMUEvent += interfaceRobot.UpdateImuDataOnGraph;
                logReplay.OnSpeedDataEvent += interfaceRobot.UpdateSpeedPolarOdometryOnInterface;
            }

            strategyManager.Init();
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
    }

}
