using PerceptionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldMap
{
    public class GlobalWorldMapStorage
    {
        public Dictionary<int, Location> robotLocationDictionary { get; set; }
        public Dictionary<int, Location> ballLocationDictionary { get; set; }
        public Dictionary<int, Location> destinationLocationDictionary { get; set; }
        public Dictionary<int, Location> waypointLocationDictionary { get; set; }
        public Dictionary<int, List<Location>> opponentsLocationListDictionary { get; set; }

        public GlobalWorldMapStorage()
        {
            robotLocationDictionary = new Dictionary<int, Location>();
            ballLocationDictionary = new Dictionary<int, Location>();
            destinationLocationDictionary = new Dictionary<int, Location>();
            waypointLocationDictionary = new Dictionary<int, Location>();
            opponentsLocationListDictionary = new Dictionary<int, List<Location>>();
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

        public void AddOrUpdateBallLocation(int id, Location loc)
        {
            lock (ballLocationDictionary)
            {
                if (ballLocationDictionary.ContainsKey(id))
                    ballLocationDictionary[id] = loc;
                else
                    ballLocationDictionary.Add(id, loc);
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
        public void AddOrUpdateOpponentsList(int id, List<Location> locList)
        {
            lock (opponentsLocationListDictionary)
            {
                if (opponentsLocationListDictionary.ContainsKey(id))
                    opponentsLocationListDictionary[id] = locList;
                else
                    opponentsLocationListDictionary.Add(id, locList);
            }
        }
    }
}
