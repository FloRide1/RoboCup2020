using Constants;
using MessageDecoder;
using MessageEncoder;
using MessageGeneratorNS;
using MessageProcessorNS;
using RobotInterface;
using SciChart.Charting.Visuals;
using USBVendorNS;
using XBoxControllerNS;

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
        static WpfRobotInterface interfaceRobot;

        static void Main(string[] args)
        {
            /// Enregistrement de la license SciChart en début de code
            SciChartSurface.SetRuntimeLicenseKey("RJWA77RbaJDdCRJpg4Iunl5Or6/FPX1xT+Gzu495Eaa0ZahxWi3jkNFDjUb/w70cHXyv7viRTjiNRrYqnqGA+Dc/yzIIzTJlf1s4DJvmQc8TCSrH7MBeQ2ON5lMs/vO0p6rBlkaG+wwnJk7cp4PbOKCfEQ4NsMb8cT9nckfdcWmaKdOQNhHsrw+y1oMR7rIH+rGes0jGGttRDhTOBxwUJK2rBA9Z9PDz2pGOkPjy9fwQ4YY2V4WPeeqM+6eYxnDZ068mnSCPbEnBxpwAldwXTyeWdXv8sn3Dikkwt3yqphQxvs0h6a8Dd6K/9UYni3o8pRkTed6SWodQwICcewfHTyGKQowz3afARj07et2h+becxowq3cRHL+76RyukbIXMfAqLYoT2UzDJNsZqcPPq/kxeXujuhT4SrNF3444MU1GaZZ205KYEMFlz7x/aEnjM6p3BuM6ZuO3Fjf0A0Ki/NBfS6n20E07CTGRtI6AsM2m59orPpI8+24GFlJ9xGTjoRA==");

            /// Initialisation des modules utilisés dans le robot
            int robotId = 0;
            usbDriver = new USBVendor();
            msgDecoder = new MsgDecoder();
            msgEncoder = new MsgEncoder();
            robotMsgGenerator = new MsgGenerator();
            robotMsgProcessor = new MsgProcessor(robotId, GameMode.Eurobot);
            xBoxManette = new XBoxControllerNS.XBoxController(robotId);

            /// Création des liens entre module, sauf depuis et vers l'interface graphique           
            msgEncoder.OnMessageEncodedEvent += usbDriver.SendUSBMessage;                                       // Envoi des messages en USB depuis le message encoder
            usbDriver.OnUSBDataReceivedEvent += msgDecoder.DecodeMsgReceived;                                   // Transmission des messages reçus par l'USB au Message Decoder
            msgDecoder.OnMessageDecodedEvent += robotMsgProcessor.ProcessRobotDecodedMessage;                   // Transmission les messages décodés par le Message Decoder au Message Processor
        }
    }
}
