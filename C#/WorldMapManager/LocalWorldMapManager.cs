using EventArgsLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using WorldMap;

namespace WorldMapManager
{
    public class LocalWorldMapManager
    {
        LocalWorldMap localWorldMap;

        public LocalWorldMapManager(int robotId, int teamId)
        {
            localWorldMap = new LocalWorldMap();
            localWorldMap.RobotId = robotId;
            localWorldMap.TeamId = teamId;
        }

        //public void OnPhysicalPositionReceived(object sender, EventArgsLibrary.LocationArgs e)
        //{
        //    if (localWorldMap == null)
        //        return;
        //    if (robotId == e.RobotId)
        //    {
        //        localWorldMap.robotLocation = e.Location;
        //        OnLocalWorldMap(robotId, localWorldMap);
        //    }
        //}

        DecimalJsonConverter decimalJsonConverter = new DecimalJsonConverter();
        public void OnPerceptionReceived(object sender, EventArgsLibrary.PerceptionArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.robotLocation = e.Perception.robotLocation;
                localWorldMap.obstaclesLocationList = e.Perception.obstaclesLocationList;
                localWorldMap.ballLocationList = e.Perception.ballLocationList;

                if (localWorldMap.robotLocation != null)
                {
                    string json = JsonConvert.SerializeObject(localWorldMap, decimalJsonConverter);
                    OnMulticastSendLocalWorldMapCommand(json.GetBytes());

                    OnLocalWorldMap(localWorldMap); //For debug only !!!
                }
            }
        }

        public void OnWaypointReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.waypointLocation = e.Location;
            }
        }

        public void OnGhostLocationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.robotGhostLocation = e.Location;
            }
        }

        public void OnDestinationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.destinationLocation = e.Location;
            }
        }

        public void OnHeatMapStrategyReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.heatMapStrategy = e.HeatMap;
            }
        }

        public void OnHeatMapWaypointReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.heatMapWaypoint = e.HeatMap;
            }
        }

        //int i = 0;
        public void OnRawLidarDataReceived(object sender, EventArgsLibrary.RawLidarArgs e)
        {
            if (localWorldMap == null || localWorldMap.robotLocation == null)
                return;
            if (localWorldMap.RobotId == e.RobotId && e.PtList.Count!=0)
            {
                List<PointD> listPtLidar = new List<PointD>();

                try
                {
                    //for (int i = 0; i < 500; i++) //Stress test
                    {
                        listPtLidar = e.PtList.Select(
                        pt => new PointD(localWorldMap.robotLocation.X + pt.Distance * Math.Cos(pt.Angle + localWorldMap.robotLocation.Theta),
                                         localWorldMap.robotLocation.Y + pt.Distance * Math.Sin(pt.Angle + localWorldMap.robotLocation.Theta))).ToList();
                    }
                }
                catch { };

                localWorldMap.lidarMap = listPtLidar;
            }
        }

        //public void OnLidarObjectsReceived(object sender, EventArgsLibrary.PolarPointListExtendedListArgs e)
        //{
        //    if (localWorldMap == null || localWorldMap.robotLocation == null)
        //        return;
        //    if (localWorldMap.RobotId == e.RobotId)
        //    {
        //        localWorldMap.lidarObjectList = e.ObjectList;
        //    }
        //}

        //Output events
        public event EventHandler<DataReceivedArgs> OnMulticastSendLocalWorldMapEvent;
        public virtual void OnMulticastSendLocalWorldMapCommand(byte[] data)
        {
            var handler = OnMulticastSendLocalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }

        //Output event for debug only
        public event EventHandler<LocalWorldMapArgs> OnLocalWorldMapEvent;
        public virtual void OnLocalWorldMap(LocalWorldMap map)
        {
            var handler = OnLocalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new LocalWorldMapArgs {  LocalWorldMap = map });
            }
        }
    }
}
