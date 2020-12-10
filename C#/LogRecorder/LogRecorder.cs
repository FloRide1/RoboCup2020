using EventArgsLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        bool isLogging = false;

        public LogRecorder()
        {
            logThread = new Thread(LogLoop);
            logThread.IsBackground = true;
            logThread.Name = "Logging Thread";
            logThread.Start();
        }

        int subFileIndex = 0;
        void StartLogging()
        {
            initialDateTime = DateTime.Now;
            subFileIndex = 0;
            isLogging = true;
        }
        void StopLogging()
        {
            isLogging = false;
        }

        bool isRecordingFileOpened = false;
        private void LogLoop()
        {
            while (true)
            {
                if (isLogging)
                {
                    /// On est en mode logging

                    if (isRecordingFileOpened == false)
                    {
                        /// Le fichier de log n'est pas créé
                        /// On le crée, mais pour cela on commence par remonter au directory RoboCup2020

                        /// On récupère le répertoire courant de l'application pour débuter
                        var currentDir = Directory.GetCurrentDirectory();
                        
                        string pattern = @"(.*(?'name'RoboCup2020))"; // Regex pour la recherche des FTDI 232
                        Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        Match m = r.Match(currentDir);
                        if (m.Success)
                        {
                            string path = m.Groups[1].ToString();                            
                            var logPath = path + "\\LogFiles\\";
                            string currentFileName = "";
                            if(subFileIndex==0)
                                currentFileName = logPath+"Log_" + initialDateTime.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + subFileIndex + "_Init.rbt";
                            else
                                currentFileName = logPath + "Log_" + initialDateTime.ToString("yyyy-MM-dd_HH-mm-ss") + "_"+subFileIndex+".rbt";
                            subFileIndex++;
                            sw = new StreamWriter(currentFileName, true);
                            sw.AutoFlush = true;
                            isRecordingFileOpened = true;
                        }

                        
                    }

                    while (logQueue.Count > 0)
                    {
                        string s = "";
                        lock (logLock) // get a lock on the queue
                        {
                            s = logQueue.Dequeue();
                        }
                        sw.WriteLine(s);

                        //Vérification de la taille du fichier
                        if (sw.BaseStream.Length > 90 * 1000000)
                        {
                            //On ferme le fichier, ce qui a pour conséquence de le splitter
                            sw.Close();
                            isRecordingFileOpened = false;
                            break; //On sort de la boucle de logging puisque le fichier est fermé
                        }
                    }
                }
                else
                {
                    if (isRecordingFileOpened == true)
                    {
                        //On ferme le fichier, ce qui a pour conséquence de le splitter
                        sw.Close();
                        isRecordingFileOpened = false;
                    }
                    
                    lock (logLock)
                    {
                        logQueue.Clear();
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

        public void OnIMURawDataReceived(object sender, IMUDataEventArgs e)
        {
            IMUDataEventArgsLog data = new IMUDataEventArgsLog();
            data.accelX = e.accelX;
            data.accelY = e.accelY;
            data.accelZ = e.accelZ;
            data.gyroX = e.gyroX;
            data.gyroY = e.gyroY;
            data.gyroZ = e.gyroZ;
            data.magX = e.magX;
            data.magY = e.magY;
            data.magZ = e.magZ;
            data.EmbeddedTimeStampInMs = e.EmbeddedTimeStampInMs;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }

        public void OnSpeedDataReceived(object sender, PolarSpeedEventArgs e)
        {
            SpeedDataEventArgsLog data = new SpeedDataEventArgsLog();
            data.Vx = e.Vx;
            data.Vy = e.Vy;
            data.Vtheta = e.Vtheta;
            data.RobotId = e.RobotId;
            data.timeStampMs = e.timeStampMs;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }

        //public void OnOpenCVMatImageReceived(object sender, OpenCvMatImageArgs e)
        //{
        //    OpenCvMatImageArgsLog data = new OpenCvMatImageArgsLog();
        //    data.Mat = e.Mat;
        //    data.Descriptor = e.Descriptor;
        //    data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
        //    string json = JsonConvert.SerializeObject(data);
        //    Log(json);
        //}

        public void OnBitmapImageReceived(object sender, BitmapImageArgs e)
        {
            BitmapDataPanoramaArgsLog data = new BitmapDataPanoramaArgsLog();

            Bitmap originalImage = e.Bitmap;
            BitmapData bmpDataOriginal = originalImage.LockBits(new Rectangle(0, 0, originalImage.Width, originalImage.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int bytesPerPixel = 0;
            if (bmpDataOriginal.PixelFormat == PixelFormat.Format24bppRgb)
                bytesPerPixel = 3; //TODO modif si canal alpha
            else if (bmpDataOriginal.PixelFormat == PixelFormat.Format32bppArgb)
                bytesPerPixel = 4; //TODO modif si canal alpha
            else
                Console.WriteLine("Pb de log dans Log Recorder : PixelFormat anormal");
            byte[] bmpData = new byte[bmpDataOriginal.Width * bmpDataOriginal.Height * bytesPerPixel];
            System.Runtime.InteropServices.Marshal.Copy(bmpDataOriginal.Scan0, bmpData, 0, bmpDataOriginal.Width * bmpDataOriginal.Height * bytesPerPixel);
            originalImage.UnlockBits(bmpDataOriginal);

            data.Descriptor = e.Descriptor;
            data.BitmapData = bmpDataOriginal;
            data.Data = bmpData;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            string json = JsonConvert.SerializeObject(data);
            Log(json);
        }
        public void OnEnableDisableLoggingReceived(object sender, BoolEventArgs e)
        {
            if (e.value == true)
            {
                StartLogging();
            }
            else
            {
                StopLogging();
            }
        }
    }
    

    public class RawLidarArgsLog : RawLidarArgs
    {
        public string Type = "RawLidar";
        public double InstantInMs;
    }

    public class SpeedDataEventArgsLog : PolarSpeedEventArgs
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
    public class BitmapPanoramaArgsLog : BitmapImageArgs
    {
        public string Type = "BitmapPanorama";
        public double InstantInMs;
    }
    public class BitmapDataPanoramaArgsLog : EventArgs
    {
        public string Type = "BitmapDataPanorama";
        public double InstantInMs { get; set; }     
        public string Descriptor { get; set; }
        public BitmapData BitmapData { get; set; }
        public byte[] Data { get; set; }
    }
}
