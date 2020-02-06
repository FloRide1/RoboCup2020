using EventArgsLibrary;
using LogRecorder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace LogReplay
{
    public class LogReplay
    {
        private Thread replayThread;
        private StreamReader sr;
        private Queue<string> replayQueue = new Queue<string>();
        public string logLock = "";

        DateTime initialDateTime;
        double? LogDateTimeOffsetInMs = null;

        public LogReplay()
        {
            replayThread = new Thread(ReplayLoop);
            replayThread.SetApartmentState(ApartmentState.STA);
            replayThread.IsBackground = true;
            replayThread.Name = "Replay Thread";
            replayThread.Start();
            initialDateTime = DateTime.Now;
        }

        private void ReplayLoop()
        {
            //sr = new StreamReader(@"C:\Github\RoboCup2020\C#\_Logs\logFilePath_Static_Passage.rbt");
            //sr = new StreamReader(@"C:\Github\RoboCup2020\C#\_Logs\logFilePath-Mvt1.rbt");
            //sr = new StreamReader(@"C:\Github\RoboCup2020\C#\_Logs\logFilePath_2020-02-03_15-47-29.rbt");
            sr = new StreamReader(@"C:\Github\RoboCup2020\C#\_Logs\logFilePath_2020-02-04_20-30-38.rbt");
            //string s = sr.ReadLine();

            using (JsonTextReader txtRdr = new JsonTextReader(sr))
            {
                txtRdr.SupportMultipleContent = true;                

                while (txtRdr.Read())
                {
                    if (txtRdr.TokenType == JsonToken.StartObject)
                    {
                        //SpeedDataEventArgs speed=serializer.Deserialize<SpeedDataEventArgs>(txtRdr);
                        // Load each object from the stream and do something with it
                        JObject obj = JObject.Load(txtRdr);
                        string objType = (string)obj["Type"];
                        double newReplayInstant = (double)obj["InstantInMs"];
                        if(LogDateTimeOffsetInMs==null)
                            LogDateTimeOffsetInMs = newReplayInstant;

                        while (DateTime.Now.Subtract(initialDateTime).TotalMilliseconds + LogDateTimeOffsetInMs < newReplayInstant)
                        {
                            Thread.Sleep(10); //On bloque
                        }
                        
                        switch(objType)
                        {
                            case "RawLidar":
                                var currentLidarLog = obj.ToObject<RawLidarArgsLog>(); 
                                OnLidar(currentLidarLog.RobotId, currentLidarLog.PtList);
                                break;
                            case "SpeedFromOdometry":
                                var robotSpeedData = obj.ToObject<SpeedDataEventArgsLog>();
                                OnSpeedData(robotSpeedData);
                                break;
                            case "ImuData":
                                var ImuData = obj.ToObject<IMUDataEventArgsLog>();
                                OnIMU(ImuData);
                                break;
                            case "CameraOmni":
                                var cameraImage = obj.ToObject<OpenCvMatImageArgsLog>();
                                OnCameraImage(cameraImage);
                                break;
                            default:
                                Console.WriteLine("Log Replay : wrong type");
                                break;
                        }
                    }
                }
            }
        }

        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarEvent;
        public virtual void OnLidar(int id, List<PolarPoint> ptList)
        {
            var handler = OnLidarEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, PtList = ptList});
            }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<IMUDataEventArgs> OnIMUEvent;
        public virtual void OnIMU(IMUDataEventArgs dat)
        {
            var handler = OnIMUEvent;
            if (handler != null)
            {
                handler(this, new IMUDataEventArgs { accelX = dat.accelX, accelY = dat.accelY, accelZ = dat.accelZ, gyrX = dat.gyrX, gyrY = dat.gyrY, gyrZ = dat.gyrZ,EmbeddedTimeStampInMs=dat.EmbeddedTimeStampInMs});
            }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<SpeedDataEventArgs> OnSpeedDataEvent;
        public virtual void OnSpeedData( SpeedDataEventArgs dat)
        {
            var handler = OnSpeedDataEvent;
            if (handler != null)
            {
                handler(this, new SpeedDataEventArgs { Vx = dat.Vx, Vy = dat.Vy, Vtheta = dat.Vtheta, RobotId = dat.RobotId, EmbeddedTimeStampInMs=dat.EmbeddedTimeStampInMs});
            }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<OpenCvMatImageArgsLog> OnCameraImageEvent;
        public virtual void OnCameraImage(OpenCvMatImageArgsLog dat)
        {
            var handler = OnCameraImageEvent;
            if (handler != null)
            {
                handler(this, new OpenCvMatImageArgsLog { Mat=dat.Mat, Descriptor=dat.Descriptor });
            }
        }
    }
}
