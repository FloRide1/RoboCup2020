using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Utilities;
using ZeroFormatter;

namespace LogRecorder
{
    public class LogRecorder
    {
        private Thread logThread;
        private StreamWriter sw;
        //private Queue<string> logQueue = new Queue<string>();
        private Queue<byte[]> logQueue = new Queue<byte[]>();
        public string logLock = "";
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
                        byte[] s;
                        lock (logLock) // get a lock on the queue
                        {
                            s = logQueue.Dequeue();
                        }

                        sw.WriteLine(Convert.ToBase64String(s));

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
        public void Log(byte[] content)
        {
            lock (logLock) // get a lock on the queue
            {
                logQueue.Enqueue(content);
            }
        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            RawLidarArgsLog data = new RawLidarArgsLog();
            data.PtList = e.PtList;
            data.RobotId = e.RobotId;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            var msg = ZeroFormatterSerializer.Serialize<ZeroFormatterLogging>(data);
            Log(msg);
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
            var msg = ZeroFormatterSerializer.Serialize<ZeroFormatterLogging>(data);
            Log(msg);
        }

        public void OnPolarSpeedDataReceived(object sender, PolarSpeedEventArgs e)
        {
            PolarSpeedEventArgsLog data = new PolarSpeedEventArgsLog();
            data.Vx = e.Vx;
            data.Vy = e.Vy;
            data.Vtheta = e.Vtheta;
            data.RobotId = e.RobotId;
            data.timeStampMs = e.timeStampMs;
            data.InstantInMs = DateTime.Now.Subtract(initialDateTime).TotalMilliseconds;
            var msg = ZeroFormatterSerializer.Serialize<ZeroFormatterLogging>(data);
            Log(msg);
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
            //string json = JsonConvert.SerializeObject(data);
            //Log(json);
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

    [ZeroFormattable]
    public class RawLidarArgsLog : ZeroFormatterLogging
    {
        public override ZeroFormatterLoggingType Type
        {
            get
            {
                return ZeroFormatterLoggingType.RawLidarArgs;
            }
        }
        //public string Type = "RawLidar";
        [Index(0)]
        public virtual double InstantInMs { get; set; }
        [Index(1)]
        public virtual int RobotId { get; set; }
        [Index(2)]
        public virtual List<PolarPointRssi> PtList { get; set; }
        [Index(3)]
        public virtual int LidarFrameNumber { get; set; }
    }

    [ZeroFormattable]
    public class PolarSpeedEventArgsLog : ZeroFormatterLogging
    {
        public override ZeroFormatterLoggingType Type
        {
            get
            {
                return ZeroFormatterLoggingType.PolarSpeedEventArgs;
            }
        }
        //public string Type = "SpeedFromOdometry";
        [Index(0)]
        public virtual double InstantInMs { get; set; }
        [Index(1)]
        public virtual uint timeStampMs { get; set; }
        [Index(2)]
        public virtual int RobotId { get; set; }
        [Index(3)]
        public virtual double Vx { get; set; }
        [Index(4)]
        public virtual double Vy { get; set; }
        [Index(5)]
        public virtual double Vtheta { get; set; }
    }

    [ZeroFormattable]
    public class IMUDataEventArgsLog :  ZeroFormatterLogging
    {
        public override ZeroFormatterLoggingType Type
        {
            get
            {
                return ZeroFormatterLoggingType.IMUDataEventArgs;
            }
        }
        //public string Type = "ImuData";
        [Index(0)]
        public virtual double InstantInMs { get; set; }
        [Index(1)]
        public virtual uint EmbeddedTimeStampInMs { get; set; }
        [Index(2)]
        public virtual double accelX { get; set; }
        [Index(3)]
        public virtual double accelY { get; set; }
        [Index(4)]
        public virtual double accelZ { get; set; }
        [Index(5)]
        public virtual double gyroX { get; set; }
        [Index(6)]
        public virtual double gyroY { get; set; }
        [Index(7)]
        public virtual double gyroZ { get; set; }
        [Index(8)]
        public virtual double magX { get; set; }
        [Index(9)]
        public virtual double magY { get; set; }
        [Index(10)]
        public virtual double magZ { get; set; }
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
