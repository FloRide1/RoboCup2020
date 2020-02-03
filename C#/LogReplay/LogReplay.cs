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
            sr = new StreamReader(@"C:\Github\RoboCup2020\C#\_Logs\logFilePath_2020-02-03_15-47-29.rbt");
            string s = sr.ReadLine();

            var currentLidarLog = JsonConvert.DeserializeObject<RawLidarArgsWithTimeStamp>(s);
            var currentIMULog= JsonConvert.DeserializeObject<IMUDataEventArgs>(s);
            var currentSpeedDataLog = JsonConvert.DeserializeObject<SpeedDataEventArgs>(s);
            while (true)
            {
                double elapsedMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
                //currentIMULog = JsonConvert.DeserializeObject<IMUDataEventArgs>(s);
                currentSpeedDataLog = JsonConvert.DeserializeObject<SpeedDataEventArgs>(s);
                //OnIMU(currentIMULog);
                OnSpeedData(currentSpeedDataLog);
                while (elapsedMs >= currentLidarLog.InstantInMs)
                {
                    //On génère un évènement et on va chercher le log suivant
                    //Console.WriteLine(currentLog.PtList.Count);
                   // OnLidar(currentLidarLog.RobotId, currentLidarLog.PtList);

                    s = sr.ReadLine();
                    try
                    {
                        if (s != null)
                        {
                            currentLidarLog = JsonConvert.DeserializeObject<RawLidarArgsWithTimeStamp>(s);

                            elapsedMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
                        }
                    }
                    catch { }

                }
                //while (logQueue.Count > 0)
                //{
                //    string s = "";
                //    lock (logLock) // get a lock on the queue
                //    {
                //        s = logQueue.Dequeue();
                //    }
                //    sw.WriteLine(s);
                //}
                Thread.Sleep(50);
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
                handler(this, new IMUDataEventArgs { accelX = dat.accelX, accelY = dat.accelY, accelZ = dat.accelZ, gyrX = dat.gyrX, gyrY = dat.gyrY, gyrZ = dat.gyrZ,timeStampMS=dat.timeStampMS});
            }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<SpeedDataEventArgs> OnSpeedDataEvent;
        public virtual void OnSpeedData( SpeedDataEventArgs dat)
        {
            var handler = OnSpeedDataEvent;
            if (handler != null)
            {
                handler(this, new SpeedDataEventArgs { Vx = dat.Vx, Vy = dat.Vy, Vtheta = dat.Vtheta, RobotId = dat.RobotId, timeStampMS=dat.timeStampMS});
            }
        }
    }
}
