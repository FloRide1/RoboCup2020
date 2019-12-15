using AdvancedTimers;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarSimulator
{
    public class LidarSimulator
    {
        int robotId = 0;

        HighFreqTimer timerSensor;

        double resolution = 0.3*Math.PI/180.0;

        public LidarSimulator(int id)
        {
            robotId = id;

            //Timer
            timerSensor = new HighFreqTimer(50);
            timerSensor.Tick += TimerSensor_Tick; ;
            timerSensor.Start();
        }

        private void TimerSensor_Tick(object sender, EventArgs e)
        {
            GenerateLidarData();
        }

        Random rand = new Random();
        public void GenerateLidarData()
        {
            List<double> angleList = new List<double>();
            List<double> distanceList = new List<double>();

            for (double angle = 0; angle < Math.PI*2; angle+=resolution)
            {
                angleList.Add(angle);
                distanceList.Add(4.0f + 2 * rand.Next(-50, 50) / 100.0);
            }

            OnSimulatedLidar(robotId, angleList, distanceList);
        }


        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnSimulatedLidarEvent;
        public virtual void OnSimulatedLidar(int id, List<double> angleList, List<double> distanceList)
        {
            var handler = OnSimulatedLidarEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, AngleList = angleList, DistanceList = distanceList});
            }
        }
    }
}
