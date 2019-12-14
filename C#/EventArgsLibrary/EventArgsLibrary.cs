using Emgu.CV;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class SpeedConsigneArgs : EventArgs
    {
        public string RobotName { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }
        public float Vtheta { get; set; }
    }
    public class TirEventArgs : EventArgs
    {
        public string RobotName { get; set; }
        public float Puissance { get; set; }
    }
    public class SpeedConsigneToMotorArgs : EventArgs
    {
        public float V { get; set; }
        public byte MotorNumber { get; set; }
    }
    public class PositionArgs : EventArgs
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Angle { get; set; }
        public float Reliability { get; set; }
    }

    public class LocationArgs : EventArgs
    {
        public string RobotName { get; set; }

        public Location Location { get; set; }
    }
    public class HeatMapArgs : EventArgs
    {
        public string RobotName { get; set; }
        public Heatmap HeatMap { get; set; }
    }

    public class LocalWorldMapArgs : EventArgs
    {
        public string RobotName { get; set; }
        public LocalWorldMap LocalWorldMap { get; set; }
    }

    public class GlobalWorldMapArgs : EventArgs
    {
        public GlobalWorldMap GlobalWorldMap { get; set; }
    }
    public class RawLidarArgs : EventArgs
    {
        public string RobotName { get; set; }
        public List<double> AngleList { get; set; }
        public List<double> DistanceList { get; set; }
    }
}
