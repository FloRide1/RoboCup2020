using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Shapes;
using ZeroFormatter;

namespace Utilities
{
    public class PointD
    {
        public double X; // { get; set; }
        public double Y; // { get; set; }
        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class PointDExtended
    {
        public PointD Pt;
        public Color Color;
        public double Width;

        public PointDExtended(PointD pt, Color c, double size)
        {
            Pt = pt;
            Color = c;
            Width = size;
        }
    }

    public class LineD
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public LineD(PointD ptDebut, PointD ptFin)
        {
            X1 = ptDebut.X;
            Y1 = ptDebut.Y;
            X2 = ptFin.X;
            Y2 = ptFin.Y;
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

    public class RectangleD
    {
        public double Xmin;
        public double Xmax;// { get; set; }
        public double Ymin;
        public double Ymax;// { get; set; }
        public RectangleD(double xMin, double xMax, double yMin, double yMax)
        {
            Xmin = xMin;
            Xmax = xMax;
            Ymin = yMin;
            Ymax = yMax;
        }
    }

    public class RectangleOriented
    {
        public PointD Center { get; set; }
        public double Lenght { get; set; }
        public double Width { get; set; }
        public double Angle { get; set; }

        public RectangleOriented()
        {
            Center = new PointD(0, 0);
            Lenght = 0;
            Width = 0;
            Angle = 0;
        }

        public RectangleOriented(PointD center, double lenght, double width, double angle)
        {
            Center = center;
            Lenght = lenght;
            Width = width;
            Angle = angle;
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
    [ZeroFormattable]
    public class PolarPointRssi
    {
        [Index(0)]
        public virtual double Distance { get; set; }
        [Index(1)]
        public virtual double Angle { get; set; }
        [Index(2)]
        public virtual double Rssi { get; set; }

        public PolarPointRssi(double angle, double distance, double rssi)
        {
            Distance = distance;
            Angle = angle;
            Rssi = rssi;
        }
        public PolarPointRssi()
        {

        }
    }

    public class Landmark
    {

        public PointD Position; //landmarks (x,y) position relative to map
        public int id; //the landmarks unique ID
        public int life; //a life counter used to determine whether to discard a landmark
        public int totalTimesObserved; //the number of times we have seen landmark
        public double range; //last observed range to landmark
        public double bearing; //last observed bearing to landmark
                               //RANSAC: Now store equation of a line
        public double slope;
        public double y_intercept;

        public double rangeError; //distance from robot position to the wall we are using as a landmark (to calculate error)
        public double bearingError; //bearing from robot position to the wall we are using as a landmark (to calculate error)

        public Landmark(int LIFE)
        {
            totalTimesObserved = 0;
            id = -1;
            life = LIFE;
            Position = new PointD(0, 0);
            slope = -1;
            y_intercept = -1;
        }
    }

    public class ClusterObjects
    {
        public List<PolarPointRssiExtended> points { get; set; }

        public ClusterObjects()
        {
            points = new List<PolarPointRssiExtended>();
        }
        public ClusterObjects(List<PolarPointRssiExtended> polarPointRssis)
        {
            points = polarPointRssis;
        }
    }



    public class PolarPointRssiExtended
    {
        public PolarPointRssi Pt { get; set; }
        public double Width { get; set; }
        public Color Color { get; set; }

        public PolarPointRssiExtended(PolarPointRssi pt, double width, Color c)
        {
            Pt = pt;
            Width = width;
            Color = c;
        }
    }

    public class PolarCourbure
    {
        public virtual double Courbure { get; set; }
        public virtual double Angle { get; set; }
        public virtual bool Discontinuity { get; set; }
        public PolarCourbure(double angle, double courbure, bool discontinuity)
        {
            Angle = angle;
            Courbure = courbure;
            Discontinuity = discontinuity;
        }
    }

    [ZeroFormattable]
    public class Location
    {
        [Index(0)]
        public virtual double X { get; set; }
        [Index(1)]
        public virtual double Y { get; set; }
        [Index(2)]
        public virtual double Theta { get; set; }
        [Index(3)]
        public virtual double Vx { get; set; }
        [Index(4)]
        public virtual double Vy { get; set; }
        [Index(5)]
        public virtual double Vtheta { get; set; }

        public Location()
        {

        }
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

    //Pose probleme
    [ZeroFormattable]
    public class LocationExtended
    {
        [Index(0)]
        public virtual double X { get; set; }
        [Index(1)]
        public virtual double Y { get; set; }
        [Index(2)]
        public virtual double Theta { get; set; }
        [Index(3)]
        public virtual double Vx { get; set; }
        [Index(4)]
        public virtual double Vy { get; set; }
        [Index(5)]
        public virtual double Vtheta { get; set; }
        [Index(6)]
        public virtual ObjectType Type { get; set; }

        public LocationExtended()
        {

        }
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

    public class SegmentExtended
    {
        public LineD Segment;
        public double Width = 10;
        public System.Drawing.Color Color = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        public double Opacity = 1;
        public double[] DashPattern = new double[] { 1.0 };

        public SegmentExtended(PointD ptDebut, PointD ptFin, System.Drawing.Color color, double width = 1)
        {
            Segment = new LineD(ptDebut, ptFin);
            Color = color;
            Width = width;
        }
    }

    public class PolarPointListExtended
    {
        public List<PolarPointRssi> polarPointList;
        public ObjectType type;
        //public System.Drawing.Color displayColor;
        //public double displayWidth=1;
    }

    public class Zone
    {
        public PointD center;
        public double radius; //Le rayon correspond à la taille la zone - à noter que l'intensité diminuera avec le rayon
        public double strength; //La force correspond à l'intensité du point central de la zone
        public Zone(PointD center, double radius, double strength)
        {
            this.radius = radius;
            this.center = center;
            this.strength = strength;
        }
    }
    public class RectangleZone
    {
        public RectangleD rectangularZone;
        public double strength; //La force correspond à l'intensité du point central de la zone
        public RectangleZone(RectangleD rect, double strength = 1)
        {
            this.rectangularZone = rect;
            this.strength = strength;
        }
    }

    public class ConicalZone
    {
        public PointD InitPoint;
        public PointD Cible;
        public double Radius;
        public ConicalZone(PointD initPt, PointD ciblePt, double radius)
        {
            InitPoint = initPt;
            Cible = ciblePt;
            Radius = radius;
        }
    }
    public class SegmentZone
    {
        public PointD PointA;
        public PointD PointB;
        public double Radius;
        public double Strength;
        public SegmentZone(PointD ptA, PointD ptB, double radius, double strength)
        {
            PointA = ptA;
            PointB = ptB;
            Radius = radius;
            Strength = strength;
        }
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
