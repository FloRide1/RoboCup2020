using BaseSLAM;
using CoreSLAM;
using EventArgsLibrary;
using System;
using Utilities;

namespace SLAM_NS
{
    public class SLAM
    {
        int robotId;

        double currentGpsXRefTerrain = 0;                       //position dans le référentiel terrain
        double currentGpsYRefTerrain = 0;
        double currentGpsTheta = 0;

        double currentOdoxRefRobot = 0;                         // positions amenées par l'odomètre
        double currentOdoyRefRobot = 0;
        double currentOdothetaRefRobot = 0;

        private readonly BufferedLogger bufferedLogger;
        private readonly Field field = new Field();
        private readonly ScaleTransform fieldScale;
        private readonly DispatcherTimer drawTimer;
        private Vector3 startPose;
        private Vector3 lidarPose;
        private readonly CoreSLAMProcessor coreSlam;
        //private readonly HectorSLAMProcessor hectorSlam;
        private readonly Thread lidarThread;
        private bool doReset;
        private bool isRunning;

        Location SLAMLocation = new Location(0, 0, 0, 0, 0, 0);    //valeur de sortie de mon programme
        public void InitSLAM(double GPS_X_Ref_Terrain, double GPS_Y_Ref_Terrain, double GPS_Theta)
        {
            startPose = Vector3(GPS_X_Ref_Terrain, GPS_Y_Ref_Terrain, GPS_Theta); //On initialise slam avec les positions données
            lidarPose = startPose;
        }

        public InialiseSLAM(int id)
        {
            robotId = id;

            //les arguments de coreslam sont : 
            // float physicalMapSize
            // int holeMapSize
            // int obstacleMapSize
            // Vector3 startPose
            //  float sigmaXY
            // float sigmaTheta
            //, int iterationsPerThread
            // int numSearchThreads

            coreSlam = new CoreSLAM.CoreSLAMProcessor(40.0f, 256, 64, startPose, 0.1f, MathEx.DegToRad(10), 1000, 4)
            {
                HoleWidth = 2.0f
            };
            //Create Field
            Field.CreateDefaultField(30.0f, new Vector2(5.0f, 5.0f));

            // Start periodic draw function
            drawTimer = new DispatcherTimer();
            drawTimer.Tick += (s, e) => Draw();
            drawTimer.Interval = TimeSpan.FromMilliseconds(20); // 50 fps
            drawTimer.Start()

            // Start scan timer in another thread
            lidarThread = new Thread(new ThreadStart(Scan))
            {
                Name = "Lidar"
            };

            isRunning = true;
            lidarThread.Start();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            isRunning = false;
            drawTimer.Stop();
            lidarThread.Join();
            coreSlam.Dispose();
            //hectorSlam.Dispose();
        }


        public void ScanSLAM()
        {
            int loops = 0;
            bool hsWrong = false; 

            while (isRunning)
            {
                if (doReset)
                {
                    coreSlam.Reset();
                    //hectorSlam.Reset();
                    lidarPose = startPose;
                    loops = 0;

                    doReset = false;
                    hsWrong = false;
                }

                var sw = Stopwatch.StartNew();

                var snapPose = lidarPose;
                bufferedLogger.LogInformation($"Real pose {snapPose.ToPoseString()}");

                ScanSegments(snapPose, coreSlam.Pose, out List<ScanSegment> scanSegments);                 //Ici simulation d'un lidar, je dois donc remplacer ça 

                coreSlam.Update(scanSegments);

                ScanCloud scanCloud = new ScanCloud()
                {
                    Pose = Vector3.Zero
                };

                foreach (ScanSegment seg in scanSegments)
                {
                    foreach (Ray ray in seg.Rays)
                    {
                        scanCloud.Points.Add(new Vector2()
                        {
                            X = ray.Radius * MathF.Cos(ray.Angle),
                            Y = ray.Radius * MathF.Sin(ray.Angle),
                        });
                    }
                }

                // Clear log buffer once in a while
                if (bufferedLogger.Items.Count > 130)
                {
                    bufferedLogger.Items.RemoveRange(0, 100);
                }

                // Ensure periodicity
                Thread.Sleep((int)Math.Max(0, (long)scanPeriod - sw.ElapsedMilliseconds));

                // Count loops
                loops++;

            }
        }

        /********************************** INPUT events ************************/
        public void OnOdometryRobotSpeedReceived(object sender, PolarSpeedArgs e)
        {

        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            
        }

        /********************************* OUTPUT events ************************/
        //Output events
        public event EventHandler<LocationArgs> OnSLAMLocationEvent;
        public virtual void OnSLAMLocation(int id, Location locationRefTerrain)
        {
            var handler = OnSLAMLocationEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = SLAMLocation });
            }
        }
    }
}
