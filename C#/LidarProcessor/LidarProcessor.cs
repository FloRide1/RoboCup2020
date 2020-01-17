using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace LidarProcessor
{
    public class LidarProcessor
    {        
        int robotId;
        public LidarProcessor(int id)
        {
            robotId = id;
        }
        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            //Segmentation en objets
            if (robotId == e.RobotId)
            {
                ProcessLidarData(e.AngleList, e.DistanceList);
            }
        }

        void ProcessLidarData(List<double> angleList, List<double> distanceList)
        {
            List<double> AngleListProcessed = new List<double>();
            List<double> DistanceListProcessed = new List<double>();

            for (int i = 0; i < angleList.Count; i++)
            {
                if (distanceList[i] < 3)
                {
                    AngleListProcessed.Add(angleList[i]);
                    DistanceListProcessed.Add(distanceList[i]);
                }
            }
            OnLidarProcessed(robotId, AngleListProcessed, DistanceListProcessed);
        }

        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarProcessedEvent;
        public virtual void OnLidarProcessed(int id, List<double> angleList, List<double> distanceList)
        {
            var handler = OnLidarProcessedEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, AngleList = angleList, DistanceList = distanceList });
            }
        }
    }
}
