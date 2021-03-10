using EventArgsLibrary;
using MessageDecoder;
using MessageEncoder;
using MessageGeneratorNS;
using MessageProcessorNS;
using RobotInterface;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using USBVendorNS;
using XBoxControllerNS;
using Constants;
using StrategyManagerProjetEtudiantNS;
using SciChart.Charting.Visuals;
using WorldMapManager;
using TrajectoryGeneratorNonHolonomeNS;
using Positioning2WheelsNS;

namespace RobotEurobot2Roues
{
    class RobotEurobot2Roues
    {
        static USBVendor usbDriver;
        static MsgDecoder msgDecoder;
        static MsgEncoder msgEncoder;
        static MsgGenerator robotMsgGenerator;
        static MsgProcessor robotMsgProcessor;
        static XBoxController xBoxManette;
        static StrategyGenerique strategyManager;
        static LocalWorldMapManager localWorldMapManager;

        static Positioning2Wheels positioning2Wheels;
        static TrajectoryGeneratorNonHolonome trajectoryGenerator;

        static WpfRobot2RouesInterface interfaceRobot;
        static GameMode competition = GameMode.Eurobot;

        static bool usingXBoxController;

        static object ExitLock = new object();

        static void Main(string[] args)
        {
            /// Enregistrement de la license SciChart en début de code
            SciChartSurface.SetRuntimeLicenseKey("RJWA77RbaJDdCRJpg4Iunl5Or6/FPX1xT+Gzu495Eaa0ZahxWi3jkNFDjUb/w70cHXyv7viRTjiNRrYqnqGA+Dc/yzIIzTJlf1s4DJvmQc8TCSrH7MBeQ2ON5lMs/vO0p6rBlkaG+wwnJk7cp4PbOKCfEQ4NsMb8cT9nckfdcWmaKdOQNhHsrw+y1oMR7rIH+rGes0jGGttRDhTOBxwUJK2rBA9Z9PDz2pGOkPjy9fwQ4YY2V4WPeeqM+6eYxnDZ068mnSCPbEnBxpwAldwXTyeWdXv8sn3Dikkwt3yqphQxvs0h6a8Dd6K/9UYni3o8pRkTed6SWodQwICcewfHTyGKQowz3afARj07et2h+becxowq3cRHL+76RyukbIXMfAqLYoT2UzDJNsZqcPPq/kxeXujuhT4SrNF3444MU1GaZZ205KYEMFlz7x/aEnjM6p3BuM6ZuO3Fjf0A0Ki/NBfS6n20E07CTGRtI6AsM2m59orPpI8+24GFlJ9xGTjoRA==");

            /// Initialisation des modules utilisés dans le robot
            int robotId = 0;
            int teamId = 0;

            usbDriver = new USBVendor();
            msgDecoder = new MsgDecoder();
            msgEncoder = new MsgEncoder();
            robotMsgGenerator = new MsgGenerator();
            robotMsgProcessor = new MsgProcessor(robotId, competition);
            xBoxManette = new XBoxControllerNS.XBoxController(robotId);
            strategyManager = new StrategyEurobot(robotId, teamId, "224.16.32.79");
            localWorldMapManager = new LocalWorldMapManager(robotId, teamId, bypassMulticast: false);
            positioning2Wheels = new Positioning2Wheels();
            trajectoryGenerator = new TrajectoryGeneratorNonHolonome(robotId);

            /// Création des liens entre module, sauf depuis et vers l'interface graphique           
            usbDriver.OnUSBDataReceivedEvent += msgDecoder.DecodeMsgReceived;                                   // Transmission des messages reçus par l'USB au Message Decoder
            msgDecoder.OnMessageDecodedEvent += robotMsgProcessor.ProcessRobotDecodedMessage;                   // Transmission les messages décodés par le Message Decoder au Message Processor

            strategyManager.On2WheelsToPolarMatrixSetupEvent += robotMsgGenerator.GenerateMessage2WheelsToPolarMatrixSet;   //Transmission des messages de set-up de la matrice de transformation moteurindepeandt -> polaire en embarqué
            strategyManager.On2WheelsAngleSetupEvent += robotMsgGenerator.GenerateMessage2WheelsAngleSet;                   //Transmission des messages de set-up de la config angulaire des roues en embarqué
            strategyManager.OnOdometryPointToMeterSetupEvent += robotMsgGenerator.GenerateMessageOdometryPointToMeter;      //Transmission des messages de set-up du coeff pointToMeter en embarqué
            strategyManager.On2WheelsPolarSpeedPIDSetupEvent += robotMsgGenerator.GenerateMessage2WheelsPolarSpeedPIDSetup;                 //Setup du PID Polaire
            strategyManager.On2WheelsIndependantSpeedPIDSetupEvent += robotMsgGenerator.GenerateMessage2WheelsIndependantSpeedPIDSetup;     //Setup du PID independant
            strategyManager.OnSetSpeedConsigneToMotor += robotMsgGenerator.GenerateMessageSetSpeedConsigneToMotor;                          //Transmission des commande de vitesse (en polaire)

            robotMsgGenerator.OnMessageToRobotGeneratedEvent += msgEncoder.EncodeMessageToRobot;                // Envoi des messages du générateur de message à l'encoder
            msgEncoder.OnMessageEncodedEvent += usbDriver.SendUSBMessage;                                       // Envoi des messages en USB depuis le message encoder

            robotMsgProcessor.OnPolarOdometrySpeedFromRobotEvent += positioning2Wheels.OnOdometryRobotSpeedReceived;        //Envoi des vitesses reçues de l'embarqué au module de calcul de positionnement
            positioning2Wheels.OnCalculatedLocationEvent += trajectoryGenerator.OnPhysicalPositionReceived;                 //Envoi du positionnement calculé au module de génération de trajectoire
            trajectoryGenerator.OnGhostLocationEvent += localWorldMapManager.OnGhostLocationReceived;             // Envoi de la position du ghost au localWorldMapManager

            strategyManager.InitStrategy(); //à faire après avoir abonné les events !

            StartRobotInterface();


            while (!exitSystem)
            {
                Thread.Sleep(500);
            }
        }

        static Thread t1;
        static void StartRobotInterface()
        {
            t1 = new Thread(() =>
            {
                //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.
                interfaceRobot = new WpfRobot2RouesInterface(competition);
                interfaceRobot.Loaded += RegisterRobotInterfaceEvents;
                interfaceRobot.ShowDialog();
            });
            t1.SetApartmentState(ApartmentState.STA);
            t1.Start();
        }

        static void RegisterRobotInterfaceEvents(object sender, EventArgs e)
        {
            /// Principe général des events :
            /// Sur evenement xx        -->>        Action a effectuer
            /// 

            /// Affichage des évènements en provenance du uC
            robotMsgGenerator.OnMessageToDisplaySpeedPolarPidSetupEvent += interfaceRobot.OnMessageToDisplayPolarSpeedPidSetupReceived;
            robotMsgGenerator.OnMessageToDisplaySpeedIndependantPidSetupEvent += interfaceRobot.OnMessageToDisplayIndependantSpeedPidSetupReceived;

            robotMsgProcessor.OnMotorsCurrentsFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsCurrentsOnGraph;
            robotMsgProcessor.OnEncoderRawDataFromRobotGeneratedEvent += interfaceRobot.UpdateMotorsEncRawDataOnGraph;

            robotMsgProcessor.OnEnableDisableMotorsACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableDisableMotorsButton;

            robotMsgProcessor.OnAsservissementModeStatusFromRobotGeneratedEvent += interfaceRobot.UpdateAsservissementMode;
            robotMsgProcessor.OnPolarOdometrySpeedFromRobotEvent += interfaceRobot.UpdateSpeedPolarOdometryOnInterface;

            robotMsgProcessor.OnIndependantOdometrySpeedFromRobotEvent += interfaceRobot.UpdateSpeedIndependantOdometryOnInterface;
            robotMsgProcessor.On4WheelsSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedPolarPidErrorCorrectionConsigneDataOnGraph;
            robotMsgProcessor.On4WheelsSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedIndependantPidErrorCorrectionConsigneDataOnGraph;
            robotMsgProcessor.On2WheelsSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedPolarPidErrorCorrectionConsigneDataOnGraph;
            robotMsgProcessor.On2WheelsSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent += interfaceRobot.UpdateSpeedIndependantPidErrorCorrectionConsigneDataOnGraph;

            //robotMsgProcessor.On4WheelsSpeedPolarPidCorrectionsFromRobotEvent += interfaceRobot.Update4WheelsSpeedPolarPidCorrections;
            //robotMsgProcessor.On4WheelsSpeedIndependantPidCorrectionsFromRobotEvent += interfaceRobot.Update4WheelsSpeedIndependantPidCorrections;
            robotMsgProcessor.On2WheelsSpeedPolarPidCorrectionDataFromRobotEvent += interfaceRobot.Update2WheelsSpeedPolarPidCorrections;
            robotMsgProcessor.On2WheelsSpeedIndependantPidCorrectionDataFromRobotEvent += interfaceRobot.Update2WheelsSpeedIndependantPidCorrections;

            robotMsgProcessor.OnErrorTextFromRobotGeneratedEvent += interfaceRobot.AppendConsole;
            robotMsgProcessor.OnPowerMonitoringValuesFromRobotGeneratedEvent += interfaceRobot.UpdatePowerMonitoringValues;
            robotMsgProcessor.OnEnableMotorCurrentACKFromRobotGeneratedEvent += interfaceRobot.ActualizeEnableMotorCurrentCheckBox;
            robotMsgProcessor.OnEnableAsservissementDebugDataACKFromRobotEvent += interfaceRobot.ActualizeEnableAsservissementDebugDataCheckBox;
            robotMsgProcessor.OnEnablePowerMonitoringDataACKFromRobotGeneratedEvent += interfaceRobot.ActualizEnablePowerMonitoringCheckBox;

            robotMsgProcessor.OnMessageCounterEvent += interfaceRobot.MessageCounterReceived;
            robotMsgGenerator.OnSetSpeedConsigneToRobotReceivedEvent += interfaceRobot.UpdatePolarSpeedConsigneOnGraph; //Valable quelque soit la source des consignes vitesse

            /// Envoi des ordres en provenance de l'interface graphique
            interfaceRobot.OnEnableDisableMotorsFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableDisableMotors;
            interfaceRobot.OnEnableDisableControlManetteFromInterfaceGeneratedEvent += ChangeUseOfXBoxController;
            interfaceRobot.OnSetAsservissementModeFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageSetAsservissementMode;
            interfaceRobot.OnEnableEncodersRawDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableEncoderRawData;
            interfaceRobot.OnEnableMotorCurrentDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableMotorCurrentData;
            interfaceRobot.OnEnableMotorsSpeedConsigneDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnableMotorSpeedConsigne;
            interfaceRobot.OnSetRobotPIDFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessage4WheelsPolarSpeedPIDSetup;
            interfaceRobot.OnEnableSpeedPIDEnableDebugInternalFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageSpeedPIDEnableDebugInternal;
            interfaceRobot.OnEnableSpeedPIDEnableDebugErrorCorrectionConsigneFromInterfaceEvent += robotMsgGenerator.GenerateMessageSpeedPIDEnableDebugErrorCorrectionConsigne;
            interfaceRobot.OnEnablePowerMonitoringDataFromInterfaceGeneratedEvent += robotMsgGenerator.GenerateMessageEnablePowerMonitoring;

            /// Affichage des infos en provenance du décodeur de message
            msgDecoder.OnMessageDecodedEvent += interfaceRobot.DisplayMessageDecoded;
            msgDecoder.OnMessageDecodedErrorEvent += interfaceRobot.DisplayMessageDecodedError;

            /// Affichage des infos en provenance du strategyManager
            strategyManager.OnTextMessageEvent += interfaceRobot.AppendConsole;


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
                xBoxManette.OnSpeedConsigneEvent += robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
            }
            else
            {
                xBoxManette.OnSpeedConsigneEvent -= robotMsgGenerator.GenerateMessageSetSpeedConsigneToRobot;
            }
        }

        /******************************************* Trap app termination ***************************************/
        static bool exitSystem = false;
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        enum CtrlType
        {
            CTRL_C_EVENT = 0,
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
    }
}
