using HeatMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace WorldMap
{
    public class LocalWorldMap
    {
        public Location robotLocation { get; set; }
        public Location destinationLocation { get; set; }
        public Location waypointLocation { get; set; }
        public List<Location> obstaclesLocation { get; set; }
        public List<PointD> lidarMap { get; set; }
        public Heatmap heatMap { get; set; }

        public LocalWorldMap()
        {
            obstaclesLocation = new List<Location>();
        }
    }

    public class Location
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Theta { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }
        public float Vtheta { get; set; }

        public Location(float x, float y, float theta, float vx, float vy, float vtheta)
        {
            X = x;
            Y = y;
            Theta = theta;
            Vx = vx;
            Vy = vy;
            Vtheta = vtheta;     
        }
    }
}
