using EventArgsLibrary;
using LogRecorder;
using Newtonsoft.Json;
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
            sr = new StreamReader(@"C:\Github\RoboCup2020\C#\_Logs\logFilePath-X_0-Y_0-Theta_0 310cm face but.rbt");
            //sr = new StreamReader(@"C:\Github\RoboCup2020\C#\_Logs\testLog.rbt");
            string s = sr.ReadLine();
            var currentLog = JsonConvert.DeserializeObject<RawLidarArgsWithTimeStamp>(s);

            while (true)
            {
                double elapsedMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
                while(elapsedMs >= currentLog.InstantInMs)
                {
                    //On génère un évènement et on va chercher le log suivant
                    OnLidar(currentLog.RobotId, currentLog.PtList);
                    s = sr.ReadLine();
                    try
                    {
                        if (s != null)
                        {
                            currentLog = JsonConvert.DeserializeObject<RawLidarArgsWithTimeStamp>(s);
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
    }
}
