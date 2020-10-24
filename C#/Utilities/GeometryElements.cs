using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Shapes;

namespace Utilities
{
    public class PointD
    {
        public double X;// { get; set; }
        public double Y;// { get; set; }
        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    
    public class Point3D
    {
        public double X;// { get; set; }
        public double Y;// { get; set; }
        public double Z;// { get; set; }
        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class PolarPoint
    {
        public double Distance;
        public double Angle;

        public PolarPoint(double angle, double distance)
        {
            Distance = distance;
            Angle = angle;
        }
    }
    public class PolarPointRssi
    {
        public double Distance;
        public double Angle;
        public double Rssi;

        public PolarPointRssi(double angle, double distance, double rssi)
        {
            Distance = distance;
            Angle = angle;
            Rssi = rssi;
        }

    }

    public class PointDExtended
    {
        public double X;
        public double Y;
        public ObjectType type;
    }

    public class Location
    {
        public double X;
        public double Y;
        public double Theta;
        public double Vx;
        public double Vy;
        public double Vtheta;

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

    public class LocationExtended
    {
        public double X;
        public double Y;
        public double Theta;
        public double Vx;
        public double Vy;
        public double Vtheta;
        public ObjectType Type;

        public LocationExtended(double x, double y, double theta, double vx, double vy, double vtheta, ObjectType type)
        {
            X = x;
            Y = y;
            Theta = theta;
            Vx = vx;
            Vy = vy;
            Vtheta = vtheta;
            Type = type;
        }
    }

    public class PolygonExtended
    {
        public Polygon polygon = new Polygon();
        public float borderWidth = 1;
        public System.Drawing.Color borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        public double borderOpacity = 1;
        public double[] borderDashPattern = new double[] { 1.0 };
        public System.Drawing.Color backgroundColor = System.Drawing.Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF);
    }

    public class PolarPointListExtended
    {
        public List<PolarPointRssi> polarPointList;
        public ObjectType type;
        //public System.Drawing.Color displayColor;
        //public double displayWidth=1;
    }

    public enum ObjectType
    {
        Balle,
        Obstacle,
        Robot,
        Poteau,
        Balise,
        LimiteHorizontaleHaute,
        LimiteHorizontaleBasse,
        LimiteVerticaleGauche,
        LimiteVerticaleDroite,

    }
}
