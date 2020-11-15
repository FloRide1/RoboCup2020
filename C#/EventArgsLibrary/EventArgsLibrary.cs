using Emgu.CV;
using HeatMap;
using PerceptionManagement;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using WorldMap;

namespace EventArgsLibrary
{
    public class DataReceivedArgs : EventArgs
    {
        public byte[] Data { get; set; }
    }

    public class StringArgs : EventArgs
    {
        public string Value { get; set; }
    }

    public class LidarMessageArgs : EventArgs
    {
        public string Value { get; set; }
        public int Line { get; set; }
    }

    public class DoubleArgs : EventArgs
    {
        public double Value { get; set; }
    }
    public class CameraImageArgs : EventArgs
    {
        public Bitmap ImageBmp { get; set; }
    }

    public class OpenCvMatImageArgs : EventArgs
    {
        public Mat Mat { get; set; }
        public string Descriptor { get; set; }
        public void Dispose()
        {
            Mat.Dispose();
        }
    }

    public class MessageDecodedArgs : EventArgs
    {
        public int MsgFunction { get; set; }
        public int MsgPayloadLength { get; set; }
        public byte[] MsgPayload { get; set; }
    }

    public class MessageEncodedArgs : EventArgs
    {
        public byte[] Msg { get; set; }
    }

    public class MessageToRobotArgs : EventArgs
    {
        public Int16 MsgFunction { get; set; }
        public Int16 MsgPayloadLength { get; set; }
        public byte[] MsgPayload { get; set; }
    }
    public class PolarSpeedArgs : EventArgs
    {
        public int RobotId { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Vtheta { get; set; }
    }
    public class IndependantSpeedArgs : EventArgs
    {
        public int RobotId { get; set; }
        public double VitesseMoteur1 { get; set; }
        public double VitesseMoteur2 { get; set; }
        public double VitesseMoteur3 { get; set; }
        public double VitesseMoteur4 { get; set; }
    }
    public class AuxiliarySpeedArgs : EventArgs
    {
        public int RobotId { get; set; }
        public double VitesseMoteur5 { get; set; }
        public double VitesseMoteur6 { get; set; }
        public double VitesseMoteur7 { get; set; }
    }
    public class GyroArgs : EventArgs
    {
        public int RobotId { get; set; }
        public double Vtheta { get; set; }
    }
    public class PolarSpeedEventArgs : PolarSpeedArgs
    {
        public uint timeStampMs;
    }
    public class IndependantSpeedEventArgs : IndependantSpeedArgs
    {
        public uint timeStampMs;
    }
    public class AuxiliarySpeedEventArgs : AuxiliarySpeedArgs
    {
        public uint timeStampMs;
    }
    public class TirEventArgs : EventArgs
    {
        public int RobotId { get; set; }
        public float Puissance { get; set; }
    }

    public class IMUDataEventArgs : EventArgs
    {
        public uint EmbeddedTimeStampInMs;
        public double accelX;
        public double accelY;
        public double accelZ;
        public double gyroX;
        public double gyroY;
        public double gyroZ;
        public double magX;
        public double magY;
        public double magZ;
    }
    public class MotorsCurrentsEventArgs : EventArgs
    {
        public uint timeStampMS;
        public double motor1;
        public double motor2;
        public double motor3;
        public double motor4;
        public double motor5;
        public double motor6;
        public double motor7;
    }
    public class EncodersRawDataEventArgs : EventArgs
    {
        public uint timeStampMS;
        public int motor1;
        public int motor2;
        public int motor3;
        public int motor4;
        public int motor5;
        public int motor6;
        public int motor7;
    }
    public class IOValuesEventArgs : EventArgs
    {
        public uint timeStampMS;
        public int ioValues;
    }
    public class PowerMonitoringValuesEventArgs : EventArgs
    {
        public uint timeStampMS;
        public double battCMDVoltage;
        public double battCMDCurrent;
        public double battPWRVoltage;
        public double battPWRCurrent;
    }
    public class MotorsPositionDataEventArgs : MotorsCurrentsEventArgs
    {

    }

    //public class MotorsVitesseDataEventArgs : EventArgs
    //{
    //    public uint timeStampMS;
    //    public double vitesseMotor1;
    //    public double vitesseMotor2;
    //    public double vitesseMotor3;
    //    public double vitesseMotor4;
    //    public double vitesseMotor5;
    //    public double vitesseMotor6;
    //    public double vitesseMotor7;
    //}

    public class AuxiliaryMotorsVitesseDataEventArgs : EventArgs
    {
        public uint timeStampMS;
        public double vitesseMotor5;
        public double vitesseMotor6;
        public double vitesseMotor7;
    }
    public class PolarPidErrorCorrectionConsigneDataArgs : EventArgs
    {
        public uint timeStampMS;
        public double xErreur;
        public double yErreur;
        public double thetaErreur;

        public double xCorrection;
        public double yCorrection;
        public double thetaCorrection;

        public double xConsigneFromRobot;
        public double yConsigneFromRobot;
        public double thetaConsigneFromRobot;
    }
    public class IndependantPidErrorCorrectionConsigneDataArgs : EventArgs
    {
        public uint timeStampMS;
        public double M1Erreur;
        public double M2Erreur;
        public double M3Erreur;
        public double M4Erreur;

        public double M1Correction;
        public double M2Correction;
        public double M3Correction;
        public double M4Correction;

        public double M1ConsigneFromRobot;
        public double M2ConsigneFromRobot;
        public double M3ConsigneFromRobot;
        public double M4ConsigneFromRobot;
    }
    public class PolarPIDSetupArgs : EventArgs
    {
        public double P_x;
        public double I_x;
        public double D_x;
        public double P_y;
        public double I_y;
        public double D_y;
        public double P_theta;
        public double I_theta;
        public double D_theta;
        public double P_x_Limit;
        public double I_x_Limit;
        public double D_x_Limit;
        public double P_y_Limit;
        public double I_y_Limit;
        public double D_y_Limit;
        public double P_theta_Limit;
        public double I_theta_Limit;
        public double D_theta_Limit;
    }

    public class IndependantPIDSetupArgs : EventArgs
    {
        public double P_M1;
        public double I_M1;
        public double D_M1;
        public double P_M2;
        public double I_M2;
        public double D_M2;
        public double P_M3;
        public double I_M3;
        public double D_M3;
        public double P_M4;
        public double I_M4;
        public double D_M4;
        public double P_M1_Limit;
        public double I_M1_Limit;
        public double D_M1_Limit;
        public double P_M2_Limit;
        public double I_M2_Limit;
        public double D_M2_Limit;
        public double P_M3_Limit;
        public double I_M3_Limit;
        public double D_M3_Limit;
        public double P_M4_Limit;
        public double I_M4_Limit;
        public double D_M4_Limit;
    }

    public class PolarPidCorrectionArgs : EventArgs
    {
        public double CorrPx;
        public double CorrIx;
        public double CorrDx;
        public double CorrPy;
        public double CorrIy;
        public double CorrDy;
        public double CorrPTheta;
        public double CorrITheta;
        public double CorrDTheta;
    }
    public class IndependantPidCorrectionArgs : EventArgs
    {
        public double CorrPM1;
        public double CorrIM1;
        public double CorrDM1;
        public double CorrPM2;
        public double CorrIM2;
        public double CorrDM2;
        public double CorrPM3;
        public double CorrIM3;
        public double CorrDM3;
        public double CorrPM4;
        public double CorrIM4;
        public double CorrDM4;
    }

    public class AccelEventArgs : EventArgs
    {
        public int timeStampMS;
        public double accelX;
        public double accelY;
        public double accelZ;
    }
    public class BoolEventArgs : EventArgs
    {
        public bool value { get; set; }
    }
    public class ByteEventArgs : EventArgs
    {
        public byte Value { get; set; }
    }
    public class CollisionEventArgs : EventArgs
    {
        public int RobotId { get; set; }
        public Location RobotRealPosition { get; set; }
    }
    public class StringEventArgs : EventArgs
    {
        public string value { get; set; }
    }
    public class SpeedConsigneToMotorArgs : EventArgs
    {
        public double V { get; set; }
        public byte MotorNumber { get; set; }
    }
    public class PositionArgs : EventArgs
    {
        public int RobotId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Theta { get; set; }
        public double Reliability { get; set; }
    }

    public class LocationArgs : EventArgs
    {
        public int RobotId { get; set; }

        public Location Location { get; set; }
    }
    public class LocationListArgs : EventArgs
    {
        public List<Location> LocationList { get; set; }
    }
    public class LocationExtendedListArgs : EventArgs
    {
        public List<LocationExtended> LocationExtendedList { get; set; }
    }
    public class PerceptionArgs : EventArgs
    {
        public int RobotId { get; set; }
        public Perception Perception { get; set; }
    }
    public class HeatMapArgs : EventArgs
    {
        public int RobotId { get; set; }
        public Heatmap HeatMap { get; set; }
    }

    public class LocalWorldMapArgs : EventArgs
    {
        //public int RobotId { get; set; }
        //public int TeamId { get; set; }
        public LocalWorldMap LocalWorldMap { get; set; }
    }

    public class GlobalWorldMapArgs : EventArgs
    {
        public GlobalWorldMap GlobalWorldMap { get; set; }
    }
    public class RawLidarArgs : EventArgs
    {
        public int RobotId { get; set; }
        public List<PolarPointRssi> PtList { get; set; }
        public int LidarFrameNumber { get; set; }
    }
    public class PolarPointListExtendedListArgs : EventArgs
    {
        public int RobotId { get; set; }
        public List<PolarPointListExtended> ObjectList { get; set; }
    }


    public class BitmapImageArgs : EventArgs
    {
        public Bitmap Bitmap { get; set; }
        public string Descriptor { get; set; }
    }
    public class MsgCounterArgs : EventArgs
    {
        public int nbMessageIMU { get; set; }
        public int nbMessageOdometry { get; set; }
    }
    public class GameStateArgs : EventArgs
    {
        public int RobotId { get; set; }
        public GameState gameState { get; set; }
    }
}
