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
                //localWorldMap.opponentLocationList = e.Perception.opponentLocationList;
                //localWorldMap.obstacleLocationList = e.Perception.obstacleLocationList;
                localWorldMap.ballLocation = e.Perception.ballLocation;

                if (localWorldMap.robotLocation != null)
                {
                    string json = JsonConvert.SerializeObject(localWorldMap, decimalJsonConverter);
                    OnMulticastSendLocalWorldMapCommand(json.GetBytes());
                    //OnLocalWorldMap(localWorldMap);
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

        public void OnDestinationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.destinationLocation = e.Location;
            }
        }

        public void OnHeatMapReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.heatMap = e.HeatMap;
            }
        }

        public void OnRawLidarDataReceived(object sender, EventArgsLibrary.RawLidarArgs e)
        {
            if (localWorldMap == null || localWorldMap.robotLocation == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                List<PointD> listPtLidar = new List<PointD>();
                for (int i=0; i< e.PtList.Count; i++)
                {
                    listPtLidar.Add(new PointD(localWorldMap.robotLocation.X + e.PtList[i].Distance * Math.Cos(e.PtList[i].Angle),
                                               localWorldMap.robotLocation.Y + e.PtList[i].Distance * Math.Sin(e.PtList[i].Angle)));
                }
                localWorldMap.lidarMap = listPtLidar;
            }
        }

        public void OnLidarObjectsReceived(object sender, EventArgsLibrary.PolarPointListExtendedListArgs e)
        {
            if (localWorldMap == null || localWorldMap.robotLocation == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.lidarObjectList = e.ObjectList;
            }
        }

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
    }
}
