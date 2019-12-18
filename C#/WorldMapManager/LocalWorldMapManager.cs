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
        int robotId = 0;
        LocalWorldMap localWorldMap;

        public LocalWorldMapManager(int id)
        {
            robotId = id;
            localWorldMap = new LocalWorldMap();
        }

        public void OnPhysicalPositionReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotId == e.RobotId)
            {
                localWorldMap.robotLocation = e.Location;
                OnLocalWorldMap(robotId, localWorldMap);
            }
        }

        public void OnPerceptionReceived(object sender, EventArgsLibrary.PerceptionArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotId == e.RobotId)
            {
                localWorldMap.robotLocation = e.Perception.robotLocation;
                localWorldMap.teamLocationList = e.Perception.teamLocationList;
                localWorldMap.opponentLocationList = e.Perception.opponentLocationList;
                localWorldMap.obstacleLocationList = e.Perception.obstacleLocationList;
                localWorldMap.ballLocation = e.Perception.ballLocation;

                if (localWorldMap.robotLocation !=null)
                    OnLocalWorldMap(robotId, localWorldMap);
            }
        }

        public void OnWaypointReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotId == e.RobotId)
            {
                localWorldMap.waypointLocation = e.Location;
            }
        }

        public void OnDestinationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotId == e.RobotId)
            {
                localWorldMap.destinationLocation = e.Location;
            }
        }

        public void OnHeatMapReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {
            if (localWorldMap == null)
                return;
            if (robotId == e.RobotId)
            {
                localWorldMap.heatMap = e.HeatMap;
            }
        }

        public void OnRawLidarDataReceived(object sender, EventArgsLibrary.RawLidarArgs e)
        {
            if (localWorldMap == null || localWorldMap.robotLocation == null)
                return;
            if (robotId == e.RobotId)
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
        public virtual void OnLocalWorldMap(int id, LocalWorldMap localWorldMap)
        {
            var handler = OnLocalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new LocalWorldMapArgs { RobotId = id, LocalWorldMap = this.localWorldMap });
            }
        }
    }
}
