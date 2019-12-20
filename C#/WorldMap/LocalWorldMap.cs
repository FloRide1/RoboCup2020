using HeatMap;
using PerceptionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace WorldMap
{
    public class GlobalWorldMap
    {
        public int TeamId;
        public Location ballLocation { get; set; }
        public Dictionary<int, Location> teammateLocationList { get; set; }
        public List<Location> opponentLocationList { get; set; }
        public List<Location> obstacleLocationList { get; set; }

        public GlobalWorldMap()
        {
        }
        public GlobalWorldMap(int teamId)
        {
            TeamId = teamId;
        }

    }
    public class LocalWorldMap
    {
        public Location robotLocation { get; set; }
        public Location ballLocation { get; set; }
        public Location destinationLocation { get; set; }
        public Location waypointLocation { get; set; }
        public List<Location> obstaclesLocationList { get; set; }
        public List<PointD> lidarMap { get; set; }
        public Heatmap heatMap { get; set; }

        public LocalWorldMap()
        {
        }
    }

}
