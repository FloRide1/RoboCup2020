using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using WorldMap;

namespace WorldMapManager
{
    public class LocalWorldMapManager
    {
        string robotName = "";
        LocalWorldMap localWorldMap;

        public LocalWorldMapManager(string name)
        {
            robotName = name;
            localWorldMap = new LocalWorldMap();
        }
        
        public void OnPhysicalPositionReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotName == e.RobotName)
            {
                localWorldMap.robotLocation = e.Location;
                OnLocalWorldMap(robotName, localWorldMap);
            }
        }

        public void OnWaypointReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotName == e.RobotName)
            {
                localWorldMap.waypointLocation = e.Location;
            }
        }

        public void OnDestinationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotName == e.RobotName)
            {
                localWorldMap.destinationLocation = e.Location;
            }
        }

        public void OnHeatMapReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotName == e.RobotName)
            {
                localWorldMap.heatMap = e.HeatMap;
            }
        }

        public void OnRawLidarDataReceived(object sender, EventArgsLibrary.RawLidarArgs e)
        {
            if (localWorldMap == null || localWorldMap.robotLocation == null)
                return;
            if (robotName == e.RobotName)
            {
                List<PointD> listPtLidar = new List<PointD>();
                for (int i=0; i< e.AngleList.Count; i++)
                {
                    listPtLidar.Add(new PointD(localWorldMap.robotLocation.X + e.DistanceList[i] * Math.Cos(e.AngleList[i]),
                                               localWorldMap.robotLocation.Y + e.DistanceList[i] * Math.Sin(e.AngleList[i])));
                }
                localWorldMap.lidarMap = listPtLidar;
            }
        }

        public delegate void LocalWorldMapEventHandler(object sender, LocalWorldMapArgs e);
        public event EventHandler<LocalWorldMapArgs> OnLocalWorldMapEvent;
        public virtual void OnLocalWorldMap(string name, LocalWorldMap localWorldMap)
        {
            var handler = OnLocalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new LocalWorldMapArgs { RobotName = name, LocalWorldMap = this.localWorldMap });
            }
        }
    }
}
