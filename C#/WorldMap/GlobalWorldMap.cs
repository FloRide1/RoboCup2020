using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace WorldMap
{
    public class GlobalWorldMapStorage
    {
        public Dictionary<int, Location> robotLocationDictionary { get; set; }
        public Dictionary<int, Location> destinationLocationDictionary { get; set; }
        public Dictionary<int, Location> waypointLocationDictionary { get; set; }
        public Dictionary<int, List<Location>> ballLocationListDictionary { get; set; }
        public Dictionary<int, List<Location>> ObstaclesLocationListDictionary { get; set; }

        public GlobalWorldMapStorage()
        {
            robotLocationDictionary = new Dictionary<int, Location>();
            destinationLocationDictionary = new Dictionary<int, Location>();
            waypointLocationDictionary = new Dictionary<int, Location>();
            ballLocationListDictionary = new Dictionary<int, List<Location>>();
            ObstaclesLocationListDictionary = new Dictionary<int, List<Location>>();
        }

        public void AddOrUpdateRobotLocation(int id, Location loc)
        {
            lock (robotLocationDictionary)
            {
                if (robotLocationDictionary.ContainsKey(id))
                    robotLocationDictionary[id] = loc;
                else
                    robotLocationDictionary.Add(id, loc);
            }
        }

        public void AddOrUpdateRobotDestination(int id, Location loc)
        {
            lock (destinationLocationDictionary)
            {
                if (destinationLocationDictionary.ContainsKey(id))
                    destinationLocationDictionary[id] = loc;
                else
                    destinationLocationDictionary.Add(id, loc);
            }
        }

        public void AddOrUpdateRobotWayPoint(int id, Location loc)
        {
            lock (waypointLocationDictionary)
            {
                if (waypointLocationDictionary.ContainsKey(id))
                    waypointLocationDictionary[id] = loc;
                else
                    waypointLocationDictionary.Add(id, loc);
            }
        }

        public void AddOrUpdateBallLocationList(int id, List<Location> ballLocationList)
        {
            lock (ballLocationListDictionary)
            {
                if (ballLocationListDictionary.ContainsKey(id))
                    ballLocationListDictionary[id] = ballLocationList;
                else
                    ballLocationListDictionary.Add(id, ballLocationList);
            }
        }

        public void AddOrUpdateObstaclesList(int id, List<Location> locList)
        {
            lock (ObstaclesLocationListDictionary)
            {
                if (ObstaclesLocationListDictionary.ContainsKey(id))
                    ObstaclesLocationListDictionary[id] = locList;
                else
                    ObstaclesLocationListDictionary.Add(id, locList);
            }
        }
    }
}
