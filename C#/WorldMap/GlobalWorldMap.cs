using PerceptionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldMap
{
    public class GlobalWorldMap
    {
        public Dictionary<string, Location> robotLocationDictionary { get; set; }
        public Dictionary<string, Location> destinationLocationDictionary { get; set; }
        public Dictionary<string, Location> waypointLocationDictionary { get; set; }
        public Dictionary<string,List<Location>> opponentsLocationListDictionary { get; set; }

        public GlobalWorldMap()
        {
            robotLocationDictionary = new Dictionary<string, Location>();
            destinationLocationDictionary = new Dictionary<string, Location>();
            waypointLocationDictionary = new Dictionary<string, Location>();
            opponentsLocationListDictionary = new Dictionary<string, List<Location>>();
        }

        public void AddOrUpdateRobotLocation(string name, Location loc)
        {
            lock (robotLocationDictionary)
            {
                if (robotLocationDictionary.ContainsKey(name))
                    robotLocationDictionary[name] = loc;
                else
                    robotLocationDictionary.Add(name, loc);
            }
        }
        public void AddOrUpdateRobotDestination(string name, Location loc)
        {
            lock (destinationLocationDictionary)
            {
                if (destinationLocationDictionary.ContainsKey(name))
                    destinationLocationDictionary[name] = loc;
                else
                    destinationLocationDictionary.Add(name, loc);
            }
        }
        public void AddOrUpdateRobotWayPoint(string name, Location loc)
        {
            lock (waypointLocationDictionary)
            {
                if (waypointLocationDictionary.ContainsKey(name))
                    waypointLocationDictionary[name] = loc;
                else
                    waypointLocationDictionary.Add(name, loc);
            }
        }
        public void AddOrUpdateOpponentsList(string name, List<Location> locList)
        {
            lock (opponentsLocationListDictionary)
            {
                if (opponentsLocationListDictionary.ContainsKey(name))
                    opponentsLocationListDictionary[name] = locList;
                else
                    opponentsLocationListDictionary.Add(name, locList);
            }
        }
    }
}
