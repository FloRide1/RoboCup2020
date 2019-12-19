using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerceptionManagement
{
    public class Perception
    {
        public Location robotLocation;
        public Location ballLocation;
        //public Dictionary<int, Location> teamLocationList;
        public List<Location> obstaclesLocationList;
        //public List<Location> opponentLocationList;
        //public List<Location> obstacleLocationList;
    }
    public class Location
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Theta { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Vtheta { get; set; }

        public Location(double x, double y, double theta, double vx, double vy, double vtheta)
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
