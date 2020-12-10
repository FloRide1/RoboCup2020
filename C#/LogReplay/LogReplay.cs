using EventArgsLibrary;
using LogRecorder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;
using ZeroFormatter;

namespace LogReplay
{
    public class LogReplay
    {
        private Thread replayThread;
        ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        ManualResetEvent _pauseEvent = new ManualResetEvent(true);        //The true parameter tells the event to start out in the signaled state.
        private StreamReader sr;
        private Queue<string> replayQueue = new Queue<string>();
        public string logLock = "";
        private bool filePathChanged = false;
        
        string folderPath= @"C:\Github\RoboCup2020\C#\_Logs\";              //Emplacement du dossier logs (par defaut)
        string fileName= "logFilePath_2020-03-05_17-47-11.rbt";
        string filePath = "";
        List<string> filesNamesList = new List<string>();
        int fileIndexInList = 0;

        DateTime initialDateTime;
        double? LogDateTimeOffsetInMs = null;
        double speedFactor = 1.0;

        bool loopReplayFile = false;
        bool RepeatReplayFile = false;

        bool replayModeActivated = false;

        public LogReplay()
        {
            replayThread = new Thread(ReplayLoop);
            replayThread.SetApartmentState(ApartmentState.STA);
            replayThread.IsBackground = true;
            replayThread.Name = "Replay Thread";
            replayThread.Start();
            initialDateTime = DateTime.Now;
        }

        public void OnEnableDisableLogReplayEvent(object sender, BoolEventArgs e)
        {
            replayModeActivated = e.value;
        }

        bool isReplayingFileSerieDefined = false;
        string replayFileSerieName = "";
        bool isReplayingFileOpened = false;


        JsonTextReader txtRdr;
        double newReplayInstant = 0;
        int subFileIndex = 0;
        private void ReplayLoop()
        {
            while (true)
            {
                if (replayModeActivated)
                {
                    if (isReplayingFileSerieDefined == false)
                    {
                        /// Défini le path des fichiers de logReplay
                        var currentDir = Directory.GetCurrentDirectory();

                        string pattern = @"(.*(?'name'RoboCup2020))"; // Regex pour la recherche des FTDI 232
                        Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        Match m = r.Match(currentDir);
                        if (m.Success)
                        {
                            //On a trouvé le path
                            string path = m.Groups[1].ToString();
                            var logPath = path + "\\LogFiles\\";

                            //Ouvre une boite de dialog pour demander le fichier à ouvrir
                            OpenFileDialog openFileDialog = new OpenFileDialog();
                            openFileDialog.InitialDirectory = logPath;
                            openFileDialog.Filter = "Log files |*_0_Init.rbt";
                            if (openFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                filePath = openFileDialog.FileName;
                                replayFileSerieName = filePath.Substring(0, filePath.Length - 10);
                                isReplayingFileSerieDefined = true;
                                subFileIndex = 0; //On réinit le compteur de sous-fichier pour les multi files chainés
                            }
                        }
                    }

                    if (isReplayingFileSerieDefined)
                    {
                        //Si le nom de la série de fichier à traiter est défini
                        if (!isReplayingFileOpened)
                        {
                            //Si aucun fichier n'est ouvert, on ouvre le courant
                            if (subFileIndex == 0)
                                filePath = replayFileSerieName + "0_Init.rbt";
                            else
                                filePath = replayFileSerieName + subFileIndex + ".rbt";
                            if (File.Exists(filePath))
                            {
                                sr = new StreamReader(filePath);
                                isReplayingFileOpened = true;
                                OnFileNameChange(filePath);
                                initialDateTime = DateTime.Now;
                            }
                            else
                            {
                                //On n'a plus de fichier à traiter
                                isReplayingFileOpened = false;
                                isReplayingFileSerieDefined = false;
                                replayModeActivated = false;
                            }
                        }

                        if (isReplayingFileOpened)
                        {
                            //Si un fichier est déjà ouvert
                            do
                            {
                                var s = sr.ReadLine();
                                if (s != null)
                                {
                                    byte[] bytes = Convert.FromBase64String(s);
                                    var deserialization = ZeroFormatterSerializer.Deserialize<ZeroFormatterLogging>(bytes);

                                    switch (deserialization.Type)
                                    {
                                        case ZeroFormatterLoggingType.IMUDataEventArgs:
                                            IMUDataEventArgsLog ImuData = (IMUDataEventArgsLog)deserialization;
                                            newReplayInstant = ImuData.InstantInMs;
                                            var e = new IMUDataEventArgs();
                                            e.accelX = ImuData.accelX;
                                            e.accelY = ImuData.accelY;
                                            e.accelZ = ImuData.accelZ;
                                            e.gyroX = ImuData.gyroX;
                                            e.gyroY = ImuData.gyroY;
                                            e.gyroZ = ImuData.gyroZ;
                                            e.magX = ImuData.magX;
                                            e.magY = ImuData.magY;
                                            e.magZ = ImuData.magZ;
                                            e.EmbeddedTimeStampInMs = ImuData.EmbeddedTimeStampInMs;
                                            OnIMU(e);
                                            break;
                                        case ZeroFormatterLoggingType.PolarSpeedEventArgs:
                                            PolarSpeedEventArgsLog robotSpeedData = (PolarSpeedEventArgsLog)deserialization;
                                            newReplayInstant = robotSpeedData.InstantInMs;
                                            var eSpeed = new PolarSpeedEventArgs();
                                            eSpeed.RobotId = robotSpeedData.RobotId;
                                            eSpeed.Vtheta = robotSpeedData.Vtheta;
                                            eSpeed.Vx = robotSpeedData.Vx;
                                            eSpeed.Vy = robotSpeedData.Vy;
                                            eSpeed.timeStampMs = robotSpeedData.timeStampMs;
                                            OnSpeedData(eSpeed);
                                            break;
                                        case ZeroFormatterLoggingType.RawLidarArgs:
                                            RawLidarArgsLog currentLidarLog = (RawLidarArgsLog)deserialization;
                                            newReplayInstant = currentLidarLog.InstantInMs;
                                            OnLidar(currentLidarLog.RobotId, currentLidarLog.PtList);
                                            break;
                                        default:
                                            break;
                                    }

                                    if (LogDateTimeOffsetInMs == null)
                                        LogDateTimeOffsetInMs = newReplayInstant;
                                }
                                else
                                {
                                    //Lecture échouée, on a une fin de fichier
                                    isReplayingFileOpened = false;
                                    subFileIndex++;
                                }
                            }

                            //On le fait tant que l'instant courant des data de Replay est inférieur à l'instant théorique de simulation, on traite les datas
                            while (newReplayInstant / speedFactor < DateTime.Now.Subtract(initialDateTime).TotalMilliseconds + LogDateTimeOffsetInMs && isReplayingFileOpened);
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }
               

        public event EventHandler<RawLidarArgs> OnLidarEvent;
        public virtual void OnLidar(int id, List<PolarPointRssi> ptList)
        {
            var handler = OnLidarEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, PtList = ptList});
            }
        }

        public event EventHandler<IMUDataEventArgs> OnIMURawDataFromReplayGeneratedEvent;
        public virtual void OnIMU(IMUDataEventArgs dat)
        {
            var handler = OnIMURawDataFromReplayGeneratedEvent;
            if (handler != null)
            {
                handler(this, new IMUDataEventArgs { accelX = dat.accelX, accelY = dat.accelY, accelZ = dat.accelZ, gyroX = dat.gyroX, gyroY = dat.gyroY, gyroZ = dat.gyroZ,EmbeddedTimeStampInMs=dat.EmbeddedTimeStampInMs});
            }
        }

        public event EventHandler<PolarSpeedEventArgs> OnSpeedPolarOdometryFromReplayEvent;
        public virtual void OnSpeedData( PolarSpeedEventArgs dat)
        {
            var handler = OnSpeedPolarOdometryFromReplayEvent;
            if (handler != null)
            {
                handler(this, new PolarSpeedEventArgs { Vx = dat.Vx, Vy = dat.Vy, Vtheta = dat.Vtheta, RobotId = dat.RobotId, timeStampMs=dat.timeStampMs});
            }
        }

        //public event EventHandler<OpenCvMatImageArgsLog> OnCameraImageEvent;
        //public virtual void OnCameraImage(OpenCvMatImageArgsLog dat)
        //{
        //    var handler = OnCameraImageEvent;
        //    if (handler != null)
        //    {
        //        handler(this, new OpenCvMatImageArgsLog { Mat = dat.Mat, Descriptor = "ImageFromCamera" });
        //    }
        //}

        public event EventHandler<BitmapDataPanoramaArgsLog> OnBitmapDataEvent;
        public virtual void OnBitmapDataPanorama(BitmapDataPanoramaArgsLog dat)
        {
            var handler = OnBitmapDataEvent;
            if (handler != null)
            {
                handler(this, new BitmapDataPanoramaArgsLog { Descriptor = "BitmapFromCamera", BitmapData = dat.BitmapData, Data = dat.Data, InstantInMs = dat.InstantInMs });
            }
        }

        public event EventHandler<StringEventArgs> OnUpdateFileNameEvent;
        public virtual void OnFileNameChange(string name)
        {
            var handler = OnUpdateFileNameEvent;
            if (handler != null)
            {
                handler(this, new StringEventArgs { value=name});
            }
        }
    }
}
