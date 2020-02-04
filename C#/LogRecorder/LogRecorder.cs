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
            RawLidarArgsWithTimeStamp data = new RawLidarArgsWithTimeStamp();
            data.PtList = e.PtList;
            data.RobotId = e.RobotId;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            //On serialize l'objet sur une ligne (pas d'indentation), et on y inclut le nom de l'objet
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }

        public void OnIMUDataReceived(object sender, IMUDataEventArgs e)
        {
            string json = JsonConvert.SerializeObject(e, Formatting.None, new JsonSerializerSettings {  TypeNameHandling=TypeNameHandling.Objects } );
            Log(json);
        }

        public void OnSpeedDataReceived(object sender, SpeedDataEventArgs e)
        {
            string json = JsonConvert.SerializeObject(e);
            Log(json);
        }
    }
    public class RawLidarArgsWithTimeStamp : RawLidarArgs
    {
        public double InstantInMs;
    }
}
