using EventArgsLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using WorldMap;
using ZeroFormatter;

namespace WorldMapManager
{
    public class LocalWorldMapManager
    {
        LocalWorldMap localWorldMap;
        bool bypassMulticastUdp = false;

        public LocalWorldMapManager(int robotId, int teamId, bool bypassMulticast)
        {
            localWorldMap = new LocalWorldMap();
            localWorldMap.RobotId = robotId;
            localWorldMap.TeamId = teamId;
            bypassMulticastUdp = bypassMulticast;
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
                //On ajoute les infos à la Local World Map
                localWorldMap.robotLocation = e.Perception.robotKalmanLocation;
                localWorldMap.obstaclesLocationList = e.Perception.obstaclesLocationList;
                localWorldMap.ballLocationList = e.Perception.ballLocationList;
                
                //On recopie les infos de la local World Map dans la structure de transfert (sans ce qui coute cher : heatmaps, lidarpoints...)
                LocalWorldMap transferLocalWorldMap = new LocalWorldMap();
                transferLocalWorldMap.RobotId = localWorldMap.RobotId;
                transferLocalWorldMap.TeamId = localWorldMap.TeamId;
                transferLocalWorldMap.robotLocation = localWorldMap.robotLocation;
                transferLocalWorldMap.destinationLocation = localWorldMap.destinationLocation;
                transferLocalWorldMap.waypointLocation = localWorldMap.waypointLocation;
                transferLocalWorldMap.robotGhostLocation = localWorldMap.robotGhostLocation;
                transferLocalWorldMap.robotRole = localWorldMap.robotRole;
                transferLocalWorldMap.messageDisplay = localWorldMap.messageDisplay;
                transferLocalWorldMap.playingSide = localWorldMap.playingSide;
                transferLocalWorldMap.obstaclesLocationList = localWorldMap.obstaclesLocationList;
                transferLocalWorldMap.ballLocationList = localWorldMap.ballLocationList;

                if (transferLocalWorldMap.robotLocation != null)
                {
                    if (bypassMulticastUdp)
                    {
                        OnLocalWorldMapForDisplayOnly(localWorldMap); //Pour affichage uniquement, sinon transmission radio en, multicast
                    }
                    else
                    {
                        var s = ZeroFormatterSerializer.Serialize<WorldMap.WorldMap>(transferLocalWorldMap);


                        var deserialzation = ZeroFormatterSerializer.Deserialize<WorldMap.WorldMap>(s);

                        switch (deserialzation.Type)
                        {
                            case WorldMapType.LocalWM:
                                transferLocalWorldMap = (LocalWorldMap)deserialzation;
                                break;
                            default:
                                break;
                        }
                        //string json = JsonConvert.SerializeObject(transferLocalWorldMap, decimalJsonConverter);


                        //OnMulticastSendLocalWorldMapCommand(json.GetBytes()); //Retiré pour test de robustesse, mais nécessaire à la RoboCup

                        //for (int i = 0; i < s.Length; i++)
                        //    s[i] = (byte)i;
                        OnMulticastSendLocalWorldMapCommand(s); //Retiré pour test de robustesse, mais nécessaire à la RoboCup

                        //ATTENTION : appel douteux...
                        OnLocalWorldMapForDisplayOnly(localWorldMap); //Pour affichage uniquement, sinon transmission radio en, multicast
                    }
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

        public void OnRoleReceived(object sender, EventArgsLibrary.RoleArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.robotRole = e.Role;
            }
        }

        public void OnMessageDisplayReceived(object sender, EventArgsLibrary.MessageDisplayArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.messageDisplay = e.Message;
            }
        }

        public void OnPlayingSideReceived(object sender, EventArgsLibrary.PlayingSideArgs e)
        {
            if (localWorldMap == null)
                return;
            if (localWorldMap.RobotId == e.RobotId)
            {
                localWorldMap.playingSide = e.PlaySide;
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

        ////Output event for display only : NO USE for transmitting data !
        public event EventHandler<LocalWorldMapArgs> OnLocalWorldMapForDisplayOnlyEvent;
        public virtual void OnLocalWorldMapForDisplayOnly(LocalWorldMap map)
        {
            var handler = OnLocalWorldMapForDisplayOnlyEvent;
            if (handler != null)
            {
                handler(this, new LocalWorldMapArgs { LocalWorldMap = map });
            }
        }

        ////Output event for Multicast Bypass : NO USE at RoboCup !
        public event EventHandler<LocalWorldMapArgs> OnLocalWorldMapBypassEvent;
        public virtual void OnLocalWorldMapBypass(LocalWorldMap map)
        {
            var handler = OnLocalWorldMapBypassEvent;
            if (handler != null)
            {
                handler(this, new LocalWorldMapArgs { LocalWorldMap = map });
            }
        }
    }
}
