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
            if (robotId == e.RobotId)
            {
                List<PointD> listPtLidar = new List<PointD>();
                for (int i = 0; i < e.AngleList.Count; i++)
                {
                    listPtLidar.Add(new PointD(e.DistanceList[i] * Math.Cos(e.AngleList[i]),
                                               e.DistanceList[i] * Math.Sin(e.AngleList[i])));
                }
            }
        }
    }
}
