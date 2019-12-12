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
        public List<Location> obstaclesLocation { get; set; }

        public GlobalWorldMap()
        {
            robotLocationDictionary = new Dictionary<string, Location>();
            destinationLocationDictionary = new Dictionary<string, Location>();
            waypointLocationDictionary = new Dictionary<string, Location>();

            obstaclesLocation = new List<Location>();
        }

        public void AddOrUpdateRobotLocation(string name, Location loc)
        {
            if (robotLocationDictionary.ContainsKey(name))
                robotLocationDictionary[name] = loc;
            else
                robotLocationDictionary.Add(name, loc);
        }
        public void AddOrUpdateRobotDestination(string name, Location loc)
        {
            if (destinationLocationDictionary.ContainsKey(name))
                destinationLocationDictionary[name] = loc;
            else
                destinationLocationDictionary.Add(name, loc);
        }
        public void AddOrUpdateRobotWayPoint(string name, Location loc)
        {
            if (waypointLocationDictionary.ContainsKey(name))
                waypointLocationDictionary[name] = loc;
            else
                waypointLocationDictionary.Add(name, loc);
        }
    }
}
