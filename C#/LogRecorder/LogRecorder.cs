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
            sw = new StreamWriter("logFilePath_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log", true);
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
            RawLidarArgsWithTimeStamp data = new RawLidarArgsWithTimeStamp();
            data.AngleList = e.AngleList;
            data.DistanceList = e.DistanceList;
            data.RobotId = e.RobotId;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }
    }
    public class RawLidarArgsWithTimeStamp : RawLidarArgs
    {
        public double InstantInMs;
    }
}
