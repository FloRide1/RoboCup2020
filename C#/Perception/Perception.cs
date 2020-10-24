using System;
using System.Collections.Generic;
using Utilities;

namespace PerceptionManagement
{
    public class Perception
    {
        public Location robotKalmanLocation;
        public Location robotAbsoluteLocation;
        public List<Location> ballLocationList;
        //public Dictionary<int, Location> teamLocationList;
        public List<LocationExtended> obstaclesLocationList;
        //public List<Location> opponentLocationList;
        //public List<Location> obstacleLocationList;

        public Perception()
        {
            ballLocationList = new List<Location>();
            obstaclesLocationList = new List<LocationExtended>();
        }
    }
}
