using EventArgsLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace LogRecorder
{
    public class LogRecorder
    {
        private Thread logThread;
        private StreamWriter sw;
        private Queue<string> logQueue = new Queue<string>();
        public string logLock = "";
        JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
        DateTime initialDateTime;

        public LogRecorder()
        {
            logThread = new Thread(LogLoop);
            logThread.IsBackground = true;
            logThread.Name = "Logging Thread";
            logThread.Start();
            initialDateTime = DateTime.Now;
        }

        private void LogLoop()
        {
            string currentFileName = "logFilePath_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".rbt";
            sw = new StreamWriter(currentFileName, true);
            sw.AutoFlush = true;
            while (true)
            {
                while (logQueue.Count > 0)
                {
                    string s = "";
                    lock (logLock) // get a lock on the queue
                    {
                        s = logQueue.Dequeue();
                    }
                    sw.WriteLine(s);

                    //Vérification de la taille du fichier
                    if(sw.BaseStream.Length > 90*1000000)
                    {
                        //On split le fichier
                        sw.Close();
                        currentFileName = "logFilePath_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".rbt";
                        sw = new StreamWriter(currentFileName, true);
                    }
                }
                Thread.Sleep(10);
            }
        }
        public void Log(string contents)
        {            
            lock (logLock) // get a lock on the queue
            {
                logQueue.Enqueue(contents);
            }
        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            RawLidarArgsLog data = new RawLidarArgsLog();
            data.PtList = e.PtList;
            data.RobotId = e.RobotId;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }

        public void OnIMUDataReceived(object sender, IMUDataEventArgs e)
        {
            IMUDataEventArgsLog data = new IMUDataEventArgsLog();
            data.accelX = e.accelX;
            data.accelY = e.accelY;
            data.accelZ = e.accelZ;
            data.gyrX = e.gyrX;
            data.gyrY = e.gyrY;
            data.gyrZ = e.gyrZ;
            data.magX = e.magX;
            data.magY = e.magY;
            data.magZ = e.magZ;
            data.EmbeddedTimeStampInMs = e.EmbeddedTimeStampInMs;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }

        public void OnSpeedDataReceived(object sender, SpeedDataEventArgs e)
        {
            SpeedDataEventArgsLog data = new SpeedDataEventArgsLog();
            data.Vx = e.Vx;
            data.Vy = e.Vy;
            data.Vtheta = e.Vtheta;
            data.RobotId = e.RobotId;
            data.EmbeddedTimeStampInMs = e.EmbeddedTimeStampInMs;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }

        public void OnOpenCVMatImageReceived(object sender, OpenCvMatImageArgs e)
        {
            OpenCvMatImageArgsLog data = new OpenCvMatImageArgsLog();
            data.Mat = e.Mat;
            data.Descriptor = e.Descriptor;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }
    }

    public class RawLidarArgsLog : RawLidarArgs
    {
        public string Type = "RawLidar";
        public double InstantInMs;
    }

    public class SpeedDataEventArgsLog : SpeedDataEventArgs
    {
        public string Type = "SpeedFromOdometry";
        public double InstantInMs;
    }

    public class IMUDataEventArgsLog : IMUDataEventArgs
    {
        public string Type = "ImuData";
        public double InstantInMs;
    }
    public class OpenCvMatImageArgsLog : OpenCvMatImageArgs
    {
        public string Type = "CameraOmni";
        public double InstantInMs;
    }
}
