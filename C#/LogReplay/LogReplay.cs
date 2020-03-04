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
        ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        ManualResetEvent _pauseEvent = new ManualResetEvent(true);        //The true parameter tells the event to start out in the signaled state.
        private StreamReader sr;
        private Queue<string> replayQueue = new Queue<string>();
        public string logLock = "";
        private bool filePathChanged = false;
        
        string folderPath= @"C:\Github\RoboCup2020\C#\_Logs\";              //Emplacement du dossier logs (par defaut)
        string fileName= "logFilePath_2020-02-04_20-30-38.rbt";
        string filePath = "";
        List<string> filesNamesList = new List<string>();
        int fileIndexInList = 0;

        DateTime initialDateTime;
        double? LogDateTimeOffsetInMs = null;
        double speedFactor = 1.0;

        bool loopReplayFile = false;
        bool RepeatReplayFile = false;
        public LogReplay()
        {
            filePath = folderPath + fileName;
            filesNamesList = Directory.GetFiles(folderPath).ToList();
            if (filesNamesList.Contains(filePath))
            {
                fileIndexInList = filesNamesList.IndexOf(filePath);
            }

            replayThread = new Thread(ReplayLoop);
            replayThread.SetApartmentState(ApartmentState.STA);
            replayThread.IsBackground = true;
            replayThread.Name = "Replay Thread";
            replayThread.Start();
            initialDateTime = DateTime.Now;
        }

        public void ReplaySpeedChanged(object sender, DoubleArgs args)
        {
            speedFactor = args.Value;
        }

        public void LoopReplayChanged(object sender, BoolEventArgs args)
        {
            loopReplayFile = args.value;
        }

        public void RepeatReplayChanged(object sender, BoolEventArgs args)
        {
            RepeatReplayFile = args.value;
        }

        public void PauseReplay(object sender, EventArgs arg)
        {
            _pauseEvent.Reset();          //Pause the Thread
        }

        public void StartReplay(object sender, EventArgs arg)
        {
            if (replayThread.IsAlive)
            {
                if (replayThread.ThreadState.HasFlag(ThreadState.WaitSleepJoin))
                {
                    //replayThread = new Thread(ReplayLoop);
                    //replayThread.Start();
                    //_shutdownEvent.Reset();
                }
                _pauseEvent.Set();            //Resume the Thread
            }
        }


        public void StopReplay(object sender, EventArgs arg)
        {
            // Signal the shutdown event
            _shutdownEvent.Set();

            // Make sure to resume any paused threads
            _pauseEvent.Set();

            // Wait for the thread to exit
            replayThread.Join();
            
        }

        public void PreviousReplay(object sender, EventArgs arg)
        {
            filePathChanged = true;
            if (fileIndexInList >0)
            {
                fileIndexInList--;
                filePath = filesNamesList[fileIndexInList];
            }
            else
            {
                fileIndexInList = filesNamesList.Count - 1;
                filePath = filesNamesList[fileIndexInList];
            }
        }

        public void NextReplay(object sender, EventArgs arg)
        {
            if(fileIndexInList+1<filesNamesList.Count)
            {
                fileIndexInList++;
                filePathChanged = true;
                filePath = filesNamesList[fileIndexInList];
            }
            else
            {
                fileIndexInList = 0;
                filePath = filesNamesList[fileIndexInList];
            }
        }

        public void OpenReplayFile(object sender,StringEventArgs e)
        {
            filePath = e.value;
            filesNamesList.Clear();
        }

        public void OpenReplayFolder(object sender, StringEventArgs e)
        {
            filesNamesList.Clear();
            if(Directory.Exists(e.value))
            {
                var lst=Directory.GetFiles(e.value).ToList();
                foreach (string str in lst)
                {
                    if (str.Contains(".rbt"))
                    {
                        filesNamesList.Add(str);
                    }
                }
            }
           

            if(filesNamesList.Count>0)
            {
                filePath = filesNamesList[0];
            }
            else
            {

                filePath = folderPath + fileName;
            }
        }


        private void ReplayLoop()
        {
            while (true)
            {
                _pauseEvent.WaitOne(Timeout.Infinite);

                //if (_shutdownEvent.WaitOne(0))
                //  break;
                sr = new StreamReader(filePath);
                OnFileNameChange(filePath);

                using (JsonTextReader txtRdr = new JsonTextReader(sr))
                {
                    txtRdr.SupportMultipleContent = true;

                    while (txtRdr.Read())
                    {
                        _pauseEvent.WaitOne(Timeout.Infinite);
                        if (txtRdr.TokenType == JsonToken.StartObject)
                        {
                            //SpeedDataEventArgs speed=serializer.Deserialize<SpeedDataEventArgs>(txtRdr);
                            // Load each object from the stream and do something with it
                            JObject obj = JObject.Load(txtRdr);
                            string objType = (string)obj["Type"];
                            double newReplayInstant = (double)obj["InstantInMs"];
                            if (LogDateTimeOffsetInMs == null)
                                LogDateTimeOffsetInMs = newReplayInstant;

                            //Tant que l'on a pas un nouvel echantillon, on attends
                            while (DateTime.Now.Subtract(initialDateTime).TotalMilliseconds + LogDateTimeOffsetInMs < ((newReplayInstant) * (1/speedFactor) ))
                            {
                                Thread.Sleep(10); //On bloque
                            }

                            switch (objType)
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

                        if(filePathChanged)
                        {
                            break;
                        }
                    }

                    if(RepeatReplayFile)
                    {
                        if (loopReplayFile)
                        {
                            if (fileIndexInList + 1 < filesNamesList.Count)
                            {
                                fileIndexInList++;
                                filePath = filesNamesList[fileIndexInList];
                            }
                            else
                            {
                                fileIndexInList = 0;
                                if(filesNamesList.Count>0)
                                    filePath = filesNamesList[fileIndexInList];
                            }
                        }
                        else
                        {
                            
                        }
                    }
                    else
                    {
                        if (loopReplayFile && !filePathChanged)
                        {
                            if (fileIndexInList + 1 < filesNamesList.Count)
                            {
                                fileIndexInList++;
                                filePath = filesNamesList[fileIndexInList];
                            }
                        }
                        else
                        {
                            if (filePathChanged)
                            {
                                filePathChanged = false;
                            }
                            else
                            {
                                _pauseEvent.Reset();          //Pause the Thread
                            }
                        }
                    }
                }
                sr.Close();
                sr.Dispose();
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
                handler(this, new IMUDataEventArgs { accelX = dat.accelX, accelY = dat.accelY, accelZ = dat.accelZ, gyroX = dat.gyroX, gyroY = dat.gyroY, gyroZ = dat.gyroZ,EmbeddedTimeStampInMs=dat.EmbeddedTimeStampInMs});
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
                handler(this, new OpenCvMatImageArgsLog { Mat=dat.Mat, Descriptor= "ImageFromCamera"});
                }
        }

        //public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
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
