using CameraAdapter;
using Constants;
using ExtendedSerialPort;
using ImageProcessingOmniCamera;
using MessageDecoder;
using MessageEncoder;
using RobotMonitor;
using SciChart.Charting.Visuals;
using System;
using System.IO.Ports;
using System.Threading;
using TrajectoryGenerator;
using WayPointGenerator;
using WorldMapManager;
using PerceptionManagement;
using EventArgsLibrary;
using WpfReplayNavigator;
using System.Runtime.InteropServices;
using AbsolutePositionEstimatorNS;
using Staudt.Engineering.LidaRx;
using MessageProcessorNS;
using MessageGeneratorNS;
using LidaRxR2000NS;
using HerkulexManagerNS;
using StrategyManagerProjetEtudiantNS;
using RobotInterface;
using StrategyManagerProjetEtudiantNS.StrategyRoboCupNS;

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
    class Robot
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
            if (omniCamera != null)
                omniCamera.DestroyCamera();
            if(yoloDetector!=null)
                yoloDetector.Dispose();
            t1.Abort();
            t2.Abort();
            return true;
        }
        #endregion

        static RobotMode robotMode = RobotMode.Standard;

        static bool usingSimulatedCamera = true;
        static bool usingPhysicalSimulator = true;
        static bool usingXBoxController = false;
        static bool usingLidar = true;
        static bool usingCamera = true;
        static bool usingLogging = false;
        static bool usingLogReplay = false;
        static bool usingImageExtractor = true;     //Utilisé pour extraire des images du flux camera et les enregistrer en tant que JPG
        static bool usingYolo = false;               //Permet de ne pas utiliser Yolo


        static bool usingRobotInterface = true;
        static bool usingCameraInterface = true;
        static bool usingReplayNavigator = true;

        static ImageSaver.ImageSaver imgSaver;
        static ReliableSerialPort serialPort1;
        static MsgDecoder msgDecoder;
        static MsgEncoder msgEncoder;
        static MsgGenerator robotMsgGenerator;
        static MsgProcessor robotMsgProcessor;
        static RobotPilot.RobotPilot robotPilot;
        static BaslerCameraAdapter omniCamera;
        static ImageProcessingPositionFromOmniCamera imageProcessingPositionFromOmniCamera;
        static AbsolutePositionEstimator absolutePositionEstimator;
        static WaypointGenerator waypointGenerator;
        static TrajectoryPlanner trajectoryPlanner;
        static KalmanPositioning.KalmanPositioning kalmanPositioning;

        static HerkulexManager herkulexManager;

        static LocalWorldMapManager localWorldMapManager;
        static ImuProcessor.ImuProcessor imuProcessor;
        static StrategyGenerique strategyManager;
        static PerceptionManager perceptionManager;
        static LidaRxR2000 lidar_OMD60M_TCP;
        static LidarProcessor.LidarProcessor lidarProcessor;
        static XBoxControllerNS.XBoxController xBoxManette;
        static YoloObjectDetector.YoloObjectDetector yoloDetector;

        static object ExitLock = new object();

        static WpfRobot4RouesHolonomeInterface interfaceRobot;
        static WpfCameraMonitor ConsoleCamera;
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
            SciChartSurface.SetRuntimeLicenseKey("wsCOsvBlAs2dax4o8qBefxMi4Qe5BVWax7TGOMLcwzWFYRNCa/f1rA5VA1ITvLHSULvhDMKVTc+niao6URAUXmGZ9W8jv/4jtziBzFZ6Z15ek6SLU49eIqJxGoQEFWvjANJqzp0asw+zvLV0HMirjannvDRj4i/WoELfYDubEGO1O+oAToiJlgD/e2lVqg3F8JREvC0iqBbNrmfeUCQdhHt6SKS2QpdmOoGbvtCossAezGNxv92oUbog6YIhtpSyGikCEwwKSDrlKlAab6302LLyFsITqogZychLYrVXJTFvFVnDfnkQ9cDi7017vT5flesZwIzeH497lzGp3B8fKWFQyZemD2RzlQkvj5GUWBwxiKAHrYMnQjJ/PsfojF1idPEEconVsh1LoYofNk2v/Up8AzXEAvxWUEcgzANeQggaUNy+OFet8b/yACa/bgYG7QYzFQZzgdng8IK4vCPdtg4/x7g5EdovN2PI9vB76coMuKnNVPnZN60kSjtd/24N8A==");

            switch (robotMode)
            {
                case RobotMode.Standard:
                    usingLidar = true;
                    usingCamera = true;
                    usingLogging = false;
                    usingLogReplay = false;
                    break;
                case RobotMode.Acquisition:
                    usingLidar = true;
                    usingCamera = true;
                    usingLogging = true;
                    usingLogReplay = false;
                    break;
                case RobotMode.Replay:
                    usingLidar = false;
                    usingCamera = false;
                    usingLogging = false;
                    usingLogReplay = true;
                    break;
                case RobotMode.Nolidar:
                    usingLidar = false;
                    usingCamera = true;
                    usingLogging = false;
                    usingLogReplay = false;
                    break;
                case RobotMode.NoCamera:
                    usingLidar = true;
                    usingCamera = false;
                    usingLogging = false;
                    usingLogReplay = false;
                    break;
            }


            int robotId = (int)TeamId.Team1 + (int)RobotId.Robot1;
            int teamId = (int)TeamId.Team1;

            serialPort1 = new ReliableSerialPort("COM1", 115200, Parity.None, 8, StopBits.One);
            msgDecoder = new MsgDecoder();
            msgEncoder = new MsgEncoder();
            robotMsgGenerator = new MsgGenerator();
            robotMsgProcessor = new MsgProcessor(robotId, GameMode.RoboCup);

            robotPilot = new RobotPilot.RobotPilot(robotId);
            strategyManager = new StrategyRoboCup(robotId, teamId, "224.16.32.79");
            waypointGenerator = new WaypointGenerator(robotId, GameMode.RoboCup);
            trajectoryPlanner = new TrajectoryPlanner(robotId, GameMode.RoboCup);
            kalmanPositioning = new KalmanPositioning.KalmanPositioning(robotId, 50, 0.2, 0.2, 0.2, 0.1, 0.1, 0.1, 0.02);

            localWorldMapManager = new LocalWorldMapManager(robotId, teamId, bypassMulticast:false);
            perceptionManager = new PerceptionManager(robotId, GameMode.RoboCup);
            imuProcessor = new ImuProcessor.ImuProcessor(robotId);

            if (usingYolo)
            {
                yoloDetector = new YoloObjectDetector.YoloObjectDetector(false);            //Instancie un detecteur avec un Wrappeur Yolo utilisant le GPU
            }

            if (usingLidar)
            {
                lidar_OMD60M_TCP = new LidaRxR2000();
            }

            if (usingLidar || usingLogReplay)
            {
                lidarProcessor = new LidarProcessor.LidarProcessor(robotId, GameMode.RoboCup);
            }

            xBoxManette = new XBoxControllerNS.XBoxController(robotId);

            if (usingCamera || usingLogReplay)
            {
                imageProcessingPositionFromOmniCamera = new ImageProcessingPositionFromOmniCamera();
                absolutePositionEstimator = new AbsolutePositionEstimator(robotId);
            }

            if (usingCamera)
            {
                omniCamera = new BaslerCameraAdapter();
                omniCamera.CameraInit();
                //omniCamera.BitmapPanoramaImageEvent += absolutePositionEstimator.AbsolutePositionEvaluation;
            }

            if (usingImageExtractor && usingCamera)
            {
                imgSaver = new ImageSaver.ImageSaver();
                omniCamera.BitmapPanoramaImageEvent += imgSaver.OnSaveBitmapImage;
            }

           

            //Démarrage des interface de visualisation
            if (usingRobotInterface)
                StartRobotInterface();
            if (usingCameraInterface)
                StartCameraInterface();

            //Démarrage du logger si besoin
            if (usingLogging)
                logRecorder = new LogRecorder.LogRecorder();

            //Démarrage du log replay si l'interface est utilisée et existe ou si elle n'est pas utilisée, sinon on bloque
            if (usingLogReplay)
                logReplay = new LogReplay.LogReplay();
             
            //Liens entre modules
            strategyManager.OnDestinationEvent += waypointGenerator.OnDestinationReceived;
            strategyManager.OnHeatMapStrategyEvent += waypointGenerator.OnStrategyHeatMapReceived;
            waypointGenerator.OnWaypointEvent += trajectoryPlanner.OnWaypointReceived;


            //Filtre de Kalman perceptionManager.OnAbsolutePositionEvent += kalmanPositioning.OnAbsolutePositionCalculatedEvent;
            robotMsgProcessor.OnSpeedPolarOdometryFromRobotEvent += kalmanPositioning.OnOdometryRobotSpeedReceived;
            imuProcessor.OnGyroSpeedEvent += kalmanPositioning.OnGyroRobotSpeedReceived;
            kalmanPositioning.OnKalmanLocationEvent += trajectoryPlanner.OnPhysicalPositionReceived;
            kalmanPositioning.OnKalmanLocationEvent += perceptionManager.OnPhysicalRobotPositionReceived;
            //kalmanPositioning.OnKalmanLocationEvent += strategyManager.OnPositionRobotReceived;

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

            perceptionManager.OnPerceptionEvent += localWorldMapManager.OnPerceptionReceived;
            strategyManager.OnDestinationEvent += localWorldMapManager.OnDestinationReceived;
            waypointGenerator.OnWaypointEvent += localWorldMapManager.OnWaypointReceived;
            strategyManager.OnHeatMapStrategyEvent += localWorldMapManager.OnHeatMapStrategyReceived;
            
            //if (usingLidar)
            //{
            //    lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += lidarProcessor.OnRawLidarDataReceived;
            //    //lidar_OMD60M.OnLidarDecodedFrameEvent += absolutePositionEstimator.OnRawLidarDataReceived;
            //    lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += localWorldMapManager.OnRawLidarDataReceived;
            //    lidarProcessor.OnLidarObjectProcessedEvent += localWorldMapManager.OnLidarObjectsReceived;
            //}
            if (usingLidar)
            {
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += perceptionManager.OnRawLidarDataReceived;
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += localWorldMapManager.OnRawLidarDataReceived;
                lidarProcessor.OnLidarProcessedEvent += localWorldMapManager.OnProcessedLidarDataReceived;
            }

            //Events de recording
            if (usingLogging)
            {
                //lidar_OMD60M_UDP.OnLidarDecodedFrameEvent += logRecorder.OnRawLidarDataReceived;
                lidar_OMD60M_TCP.OnLidarDecodedFrameEvent += logRecorder.OnRawLidarDataReceived;
                omniCamera.BitmapFishEyeImageEvent += logRecorder.OnBitmapImageReceived;
                imuProcessor.OnIMUProcessedDataGeneratedEvent += logRecorder.OnIMURawDataReceived;
                robotMsgProcessor.OnSpeedPolarOdometryFromRobotEvent += logRecorder.OnPolarSpeedDataReceived;
                //omniCamera.OpenCvMatImageEvent += logRecorder.OnOpenCVMatImageReceived;
            }

            if (usingLogReplay)
            {
                logReplay.OnLidarEvent += perceptionManager.OnRawLidarDataReceived;
                //logReplay.OnCameraImageEvent += imageProcessingPositionFromOmniCamera.ProcessOpenCvMatImage;
                //logReplay.OnCameraImageEvent += absolutePositionEstimator.AbsolutePositionEvaluation;
                //lidarProcessor.OnLidarObjectProcessedEvent += localWorldMapManager.OnLidarObjectsReceived;
            }

            lock (ExitLock)
            {
                // Do whatever setup code you need here
                // once we are done wait
                Monitor.Wait(ExitLock);
            }
        }

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
                //strategyManager.OnCollisionEvent -= kalmanPositioning.OnCollisionReceived;
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
                //strategyManager.OnCollisionEvent += kalmanPositioning.OnCollisionReceived;
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
                interfaceRobot = new RobotInterface.WpfRobot4RouesHolonomeInterface( GameMode.RoboCup);
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

            if (!usingLogReplay)
            {
                imuProcessor.OnIMUProcessedDataGeneratedEvent += interfaceRobot.UpdateImuDataOnGraph;
                robotMsgProcessor.OnMotorsCurrentsFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsCurrentsOnGraph;
                robotMsgProcessor.OnEncoderRawDataFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsEncRawDataOnGraph;

                robotMsgProcessor.OnEnableDisableMotorsACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableMotorsButton;
                robotMsgProcessor.OnEnableDisableTirACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableTirButton;

                robotMsgProcessor.OnAsservissementModeStatusFromRobotGeneratedEvent += interfaceRobot.UpdateAsservissementMode;
                robotMsgProcessor.OnSpeedPolarOdometryFromRobotEvent += interfaceRobot.UpdateSpeedPolarOdometryOnInterface;
                robotMsgProcessor.OnIndependantOdometrySpeedFromRobotEvent += interfaceRobot.UpdateSpeedIndependantOdometryOnInterface;
                robotMsgProcessor.On4WheelsSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedPolarPidErrorCorrectionConsigneDataOnGraph;
                robotMsgProcessor.On4WheelsSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedIndependantPidErrorCorrectionConsigneDataOnGraph;
                robotMsgProcessor.OnSpeedPolarPidCorrectionDataFromRobotEvent += interfaceRobot.UpdateSpeedPolarPidCorrectionData;
                robotMsgProcessor.OnSpeedIndependantPidCorrectionDataFromRobotEvent += interfaceRobot.UpdateSpeedIndependantPidCorrectionData;

                robotMsgProcessor.OnErrorTextFromRobotGeneratedEvent += interfaceRobot.AppendConsole;
                robotMsgProcessor.OnPowerMonitoringValuesFromRobotGeneratedEvent += interfaceRobot.UpdatePowerMonitoringValues;
                robotMsgProcessor.OnEnableMotorCurrentACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableMotorCurrentCheckBox;
                //robotMsgProcessor.OnEnableEncoderRawDataACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableEncoderRawDataCheckBox;
                robotMsgProcessor.OnEnableAsservissementDebugDataACKFromRobotEvent += interfaceRobot.ActualizeEnableAsservissementDebugDataCheckBox;
                //robotMsgProcessor.OnEnableMotorSpeedConsigneDataACKFromRobotGeneratedEvent += interfaceRobot.ActualizEnableMotorSpeedConsigneCheckBox;
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
            interfaceRobot.OnSetRobotPIDFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessage4WheelsPolarSpeedPIDSetup;
            interfaceRobot.OnEnableSpeedPIDEnableDebugInternalFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageSpeedPIDEnableDebugInternal;
            interfaceRobot.OnEnableSpeedPIDEnableDebugErrorCorrectionConsigneFromInterfaceEvent += robotMsgGenerator.GenerateMessageSpeedPIDEnableDebugErrorCorrectionConsigne;
            interfaceRobot.OnCalibrateGyroFromInterfaceGeneratedEvent += imuProcessor.OnCalibrateGyroFromInterfaceGeneratedEvent;
            interfaceRobot.OnEnablePowerMonitoringDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnablePowerMonitoring;

            localWorldMapManager.OnLocalWorldMapForDisplayOnlyEvent += interfaceRobot.OnLocalWorldMapStrategyEvent;
            localWorldMapManager.OnLocalWorldMapForDisplayOnlyEvent += interfaceRobot.OnLocalWorldMapWayPointEvent;

            if (usingLogReplay)
            {
                logReplay.OnIMURawDataFromReplayGeneratedEvent += interfaceRobot.UpdateImuDataOnGraph;
                logReplay.OnSpeedPolarOdometryFromReplayEvent += interfaceRobot.UpdateSpeedPolarOdometryOnInterface;
            }
        }


        static Thread t2;
        static void StartCameraInterface()
        {
            t2 = new Thread(() =>
            {
                //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.
                ConsoleCamera = new RobotMonitor.WpfCameraMonitor();
                ConsoleCamera.Loaded += RegisterCameraInterfaceEvents;
                ConsoleCamera.ShowDialog();
            });
            t2.SetApartmentState(ApartmentState.STA);
            t2.Start();
        }

        static void RegisterCameraInterfaceEvents(object sender, EventArgs e)
        {
            if (usingCamera || usingLogging)
            {
                omniCamera.BitmapFishEyeImageEvent += ConsoleCamera.DisplayBitmapImage;
                ////absolutePositionEstimator.OnBitmapImageProcessedEvent += ConsoleCamera.DisplayBitmapImage;
                omniCamera.BitmapPanoramaImageEvent += ConsoleCamera.DisplayBitmapImage;
                ConsoleCamera.CalibrateCameraEvent += omniCamera.CalibrateFishEye;
                ConsoleCamera.ResetCalibrationCameraEvent += omniCamera.ResetFishEyeCalibration;
                ConsoleCamera.StartCameraEvent += omniCamera.StartAcquisition;
                ConsoleCamera.StopCameraEvent += omniCamera.StopAcquisition;
            }

            if (usingLogReplay)
            {
                //logReplay.OnCameraImageEvent += ConsoleCamera.DisplayOpenCvMatImage;                
            }
            
            if (usingYolo)
            {
                omniCamera.BitmapPanoramaImageEvent += yoloDetector.SetNewYoloImageToProcess;        //On envoie l'image dewrappée dans le detecteur Yolo, et on effectue la detection avec les poids UTLN
                yoloDetector.OnYoloBitmapImageProcessedAndLabelledEvent += ConsoleCamera.DisplayBitmapImage;       //Event d'image processée et labelisée
                //yoloDetector.OnYoloImageProcessedAndLabelled_LabelEvent += ConsoleCamera.DisplayMessageInConsole;       //Permet d'afficher du txt dans la console camera
            }

        }


        private static void RefBoxAdapter_DataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            throw new NotImplementedException();
        }
    }

}
