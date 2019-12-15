using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Z { get; set; } = 0;

        public Point3D() { }
        public Point3D(double x, double y, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Crée un point selon des coordonnées polaires. Angle en degrés.</summary>
        public static Point3D FromPolar(double distance, double angle_z, double z = 0)
        {
            return new Point3D
            {
                X = distance * Math.Cos(angle_z * Math.PI / 180),
                Y = distance * Math.Sin(angle_z * Math.PI / 180),
                Z = z
            };
        }

        #region Operators

        /// <summary>Renvoie la distance entre le point et l'origine du repère (0;0;0) en prenant en compte Z.</summary>
        public double D => Math.Sqrt(X * X + Y * Y + Z * Z);

        /// <summary>Renvoie l'angle en la projection du point sur le plan XY et l'axe X en degrés.</summary>
        public double Angle => Math.Atan2(Y, X) * 180 / Math.PI;

        public static Point3D operator +(Point3D pt, double nb)
            => new Point3D(pt.X + nb, pt.Y + nb, pt.Z + nb);

        public static Point3D operator -(Point3D pt, double nb)
            => new Point3D(pt.X - nb, pt.Y - nb, pt.Z - nb);

        public static Point3D operator /(Point3D pt, double nb)
            => new Point3D(pt.X / nb, pt.Y / nb, pt.Z / nb);

        public static Point3D operator *(Point3D pt, double nb)
            => new Point3D(pt.X * nb, pt.Y * nb, pt.Z * nb);

        public static Point3D operator +(Point3D p1, Point3D p2)
            => new Point3D(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);

        public static Point3D operator -(Point3D p1, Point3D p2)
            => new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

        public override bool Equals(object obj)
        {
            var d = obj as Point3D;
            double TOLERANCE = 0.000001;
            return !(d is null) &&
                   Math.Abs(X - d.X) < TOLERANCE &&
                   Math.Abs(Y - d.Y) < TOLERANCE &&
                   Math.Abs(Z - d.Z) < TOLERANCE;
        }

        public override int GetHashCode()
        {
            var hashCode = -307843816;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }

        #endregion
        #region Methods

        public Point3D Copy() => new Point3D(X, Y, Z);

        /// <summary>Déplace le point relativement à sa position actuelle.</summary>
        public void Move(Point3D dpt) => Move(dpt.X, dpt.Y, dpt.Z);
        /// <summary>Déplace le point relativement à sa position actuelle.</summary>
        public void Move(double dx, double dy, double dz = 0)
        {
            X += dx;
            Y += dy;
            Z += dz;
        }

        /// <summary>Tourne le point selon center en 2D X et Y (angle en degrés).</summary>
        public void Rotate2D(double angle) => Rotate(0, 0, angle);
        /// <summary>Tourne le point selon le centre du repère 2D X et Y (angle en degrés).</summary>
        public void Rotate2D(Point3D center, double angle) => Rotate(0, 0, angle, center);

        /// <summary>Tourne le point selon le centre du repère (angles en degrés).</summary>
        public void Rotate(Point3D angles) => Rotate(angles.X, angles.Y, angles.Z);
        /// <summary>Tourne le point selon center (angles en degrés).</summary>
        public void Rotate(Point3D angles, Point3D center) => Rotate(angles.X, angles.Y, angles.Z, center);
        /// <summary>Tourne le point selon le centre du repère (angles en degrés).</summary>
        public void Rotate(double anglex, double angley, double anglez) => Rotate(anglex, angley, anglez, new Point3D(0, 0));
        /// <summary>Tourne le point selon center (angles en degrés).</summary>
        public void Rotate(double anglex, double angley, double anglez, Point3D center)
        {
            anglex *= Math.PI / 180;
            angley *= Math.PI / 180;
            anglez *= Math.PI / 180;

            double x, y, z;

            // Rotation selon l'axe Z
            x = center.X + (X - center.X) * Math.Cos(anglez) - (Y - center.Y) * Math.Sin(anglez);
            y = center.Y + (X - center.X) * Math.Sin(anglez) + (Y - center.Y) * Math.Cos(anglez);
            z = Z;

            X = x;
            Y = y;
            Z = z;

            // Rotation selon l'axe X
            y = center.Y + (Y - center.Y) * Math.Cos(anglex) - (Z - center.Z) * Math.Sin(anglex);
            z = center.Z + (Y - center.Y) * Math.Sin(anglex) + (Z - center.Z) * Math.Cos(anglex);
            x = X;

            X = x;
            Y = y;
            Z = z;

            // Rotation selon l'axe Y
            z = center.Z + (Z - center.Z) * Math.Cos(angley) - (X - center.X) * Math.Sin(angley);
            x = center.X + (Z - center.Z) * Math.Sin(angley) + (X - center.X) * Math.Cos(angley);
            y = Y;

            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Copie puis déplace le nouveau point relativement à sa position actuelle.</summary>
        public Point3D MoveCopy(Point3D dpt) => MoveCopy(dpt.X, dpt.Y, dpt.Z);
        /// <summary>Copie puis déplace le nouveau point relativement à sa position actuelle.</summary>
        public Point3D MoveCopy(double dx, double dy, double dz = 0)
        {
            Point3D newPoint = Copy();
            newPoint.Move(dx, dy, dz);

            return newPoint;
        }

        /// <summary>Met à zéro les coordonnées.</summary>
        public void Reset()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public override string ToString() => $"({X}; {Y}; {Z})";

        #endregion
    }

    #region Abstract Classes

    public abstract class Element
    {
        /// <summary>Position de l'élément géométrique.</summary>
        public Point3D Position { get; set; } = new Point3D();

        /// <summary>Angle de l'objet selon les axes X, Y et Z (en degrés).</summary>
        public Point3D Orientation { get; set; } = new Point3D();

        /// <summary>Couleur de l'objet.</summary>
        public Color Color { get; set; } = Color.Black;

        /// <summary>Nom de l'objet.</summary>
        public string Name { get; set; } = "";

        /// <summary>Indique si l'<see cref="Element"/> doit être transformé avant obtention de ses points/composantes.</summary>
        public bool NeedsTransform { get; set; } = true;

        public Element() { }
        public Element(Point3D position, Color color, string name)
        {
            Position = position ?? new Point3D();
            Color = color;
            Name = name;
        }

        /// <summary>Attribue un nouveau position.</summary>
        public void SetPos(Point3D newPosition) => SetPos(newPosition.X, newPosition.Y, newPosition.Z);
        /// <summary>Attribue un nouveau position.</summary>
        public void SetPos(double x, double y, double z = 0)
        {
            Position.X = x;
            Position.Y = y;
            Position.Z = z;
        }

        /// <summary>Déplace la position relativement à sa position actuelle.</summary>
        public void Move(double dx, double dy, double dz = 0) => Position.Move(dx, dy, dz);
        /// <summary>Déplace la position relativement à sa position actuelle.</summary>
        public void Move(Point3D dpt) => Position.Move(dpt);

        /// <summary>Attribue une nouvelle orientation (en degrés).</summary>
        public void SetRotation(Point3D newOrientation) => SetRotation(newOrientation.X, newOrientation.Y, newOrientation.Z);
        /// <summary>Attribue une nouvelle orientation (en degrés).</summary>
        public void SetRotation(double anglex, double angley, double anglez)
        {
            Orientation.X = anglex;
            Orientation.Y = angley;
            Orientation.Z = anglez;
        }

        /// <summary>Modifie l'orientation relativement à l'actuelle (en degrés).</summary>
        public void Rotate(double dtheta_x, double dtheta_y, double dtheta_z) => Orientation.Move(dtheta_x, dtheta_y, dtheta_z);
        /// <summary>Modifie l'orientation relativement à l'actuelle (en degrés).</summary>
        public void Rotate(Point3D dpt) => Orientation.Move(dpt);

        public override string ToString() => $"Position: {Position}";
        public abstract Element Copy();

        /// <summary>Copie puis déplace le nouvel élément relativement à sa position actuelle.</summary>
        public Element MoveCopy(double dx, double dy, double dz = 0)
        {
            Element element = Copy();
            element.Move(dx, dy, dz);

            return element;
        }

        /// <summary>Copie puis tourne le nouvel élément relativement à sa position actuelle.</summary>
        public Element RotateCopy(double ax, double ay, double az)
        {
            Element element = Copy();
            element.Rotate(ax, ay, az);

            return element;
        }

        /// <summary>Copie puis déplace et tourne le nouvel élément relativement à sa position actuelle.</summary>
        public Element MoveRotateCopy(Point3D dpt, Point3D angles)
        {
            Element element = Copy();
            element.Move(dpt.X, dpt.Y, dpt.Z);
            element.Rotate(angles.X, angles.Y, angles.Z);

            return element;
        }
    }

    #endregion
    //#region Base Classes

    ////public class Polygon : Element
    ////{
    ////    /// <summary>Liste de points définissant la forme du polygone.</summary>
    ////    public List<Point3D> Shape { get; set; } = new List<Point3D>();
    ////    /// <summary>Indique si le polygone est rempli.</summary>
    ////    public bool IsFilled { get; set; } = true;

    ////    public Polygon() { }
    ////    public Polygon(List<Point3D> shape) => Shape = shape;
    ////    public Polygon(List<Point3D> shape, Point3D position, Color color, string name) : base(position, color, name) => Shape = shape;

    ////    /// <summary>Liste de points placés et orientés selon Position et Orientation.</summary>
    ////    public IEnumerable<Point3D> Points => NeedsTransform ?
    ////        Shape.MoveRotateCopy(Position, Orientation) :
    ////        Shape.Copy();

    ////    public override string ToString() => $"Position {Position} | {Shape.Count} points | IsFilled: {IsFilled}";
    ////    public override Element Copy()
    ////        => new Polygon(new List<Point3D>(Shape.Copy()), Position.Copy(), Color, Name)
    ////        {
    ////            IsFilled = IsFilled,
    ////            NeedsTransform = NeedsTransform
    ////        };

    ////    /// <summary>Retourne la liste de points définissant le polygone en tout instant.</summary>
    ////    public PointList GetBorderPointList(Color color, PointList.PointListType type = PointList.PointListType.Loop)
    ////        => new PointList(Shape, Position, color, Name + "_borderList", type) { Orientation = Orientation.Copy() };
    ////}

    //public class ElementList : Element
    //{
    //    /// <summary>Liste d'éléments représentant <see cref="ElementList"/>.</summary>
    //    public List<Element> Shape { get; set; } = new List<Element>();

    //    public ElementList() { }
    //    public ElementList(List<Element> shape) => Shape = shape;
    //    public ElementList(List<Element> shape, Point3D position, Color color, string name) : base(position, color, name) => Shape = shape;

    //    public void SetElementsColor(Color color)
    //    {
    //        foreach (Element element in Shape)
    //        {
    //            element.Color = color;
    //            if (element is ElementList elementList)
    //                elementList.SetElementsColor(color);
    //        }
    //    }

    //    /// <summary>Liste d'éléments placés et orientés selon Position et Orientation.</summary>
    //    public IEnumerable<Element> Elements
    //    {
    //        get
    //        {
    //            if (NeedsTransform)
    //            {
    //                List<Element> elements = new List<Element>();
    //                foreach (Element element in Shape)
    //                {
    //                    Element newElement = element.Copy();
    //                    newElement.Rotate(Orientation);
    //                    newElement.Move(Position);
    //                    newElement.Position.Rotate(Orientation, Position);

    //                    elements.Add(newElement);
    //                }

    //                return elements;
    //            }
    //            else
    //            {
    //                return Shape.Copy();
    //            }
    //        }
    //    }

    //    public override string ToString() => $"Position {Position} | {Shape.Count} elements";
    //    public override Element Copy() =>
    //        new ElementList(new List<Element>(Shape.Copy()), Position.Copy(), Color, Name)
    //        {
    //            Orientation = Orientation.Copy(),
    //            NeedsTransform = NeedsTransform
    //        };
    //}

    //public class Circle : Element
    //{
    //    /// <summary>Rayon du cercle.</summary>
    //    public double Radius { get; set; } = 0;
    //    /// <summary>Angle à partir duquel le cercle commence (en degrés).</summary>
    //    public double AngleStart { get; set; } = 0;
    //    /// <summary>Angle pour lequel le cercle s'arrête (en degrés).</summary>
    //    public double AngleStop { get; set; } = 360;

    //    public Circle() { }
    //    public Circle(double radius) => Radius = radius;
    //    public Circle(double radius, Point3D position, Color color, string name) : base(position, color, name) => Radius = radius;

    //    public override string ToString() => $"Position: {Position} | Radius: {Radius}";
    //    public override Element Copy()
    //        => new Circle(Radius, Position.Copy(), Color, Name)
    //        {
    //            Orientation = Orientation.Copy(),
    //            AngleStop = AngleStop,
    //            AngleStart = AngleStart,
    //            NeedsTransform = NeedsTransform
    //        };
    //}

    public class Segment : Element
    {
        /// <summary>Point de départ du segment.</summary>
        public Point3D A { get; set; } = new Point3D();
        /// <summary>Point d'arrivée du segment.</summary>
        public Point3D B { get; set; } = new Point3D();

        public Segment() { }

        public Segment(Point3D a, Point3D b)
        {
            A = a;
            B = b;
        }

        public Segment(Point3D a, Point3D b, Point3D position, Color color, string name) : base(position, color, name)
        {
            A = a;
            B = b;
        }

        /// <summary>Liste de points placés et orientés selon Position et Orientation.</summary>
        public virtual IEnumerable<Point3D> Points => NeedsTransform ? new[] { A, B }.MoveRotateCopy(Position, Orientation) : new[] { A, B }.Copy();

        public override string ToString() => $"Position {Position} | A: {A} -> B: {B}";
        public override Element Copy() => new Segment(A.Copy(), B.Copy(), Position.Copy(), Color, Name)
        {
            Orientation = Orientation.Copy(),
            NeedsTransform = NeedsTransform
        };
    }

    //public class Triangle : Element
    //{
    //    /// <summary>Premier point du triangle.</summary>
    //    public Point3D A { get; set; } = new Point3D();
    //    /// <summary>Deuxième point du triangle.</summary>
    //    public Point3D B { get; set; } = new Point3D();
    //    /// <summary>Troisième point du triangle.</summary>
    //    public Point3D C { get; set; } = new Point3D();

    //    public Triangle() { }
    //    public Triangle(Point3D a, Point3D b, Point3D c)
    //    {
    //        A = a;
    //        B = b;
    //        C = c;
    //    }

    //    public Triangle(Point3D a, Point3D b, Point3D c, Point3D position, Color color, string name) : base(position, color, name)
    //    {
    //        A = a;
    //        B = b;
    //        C = c;
    //    }

    //    /// <summary>Liste de points placés et orientés selon Position et Orientation.</summary>
    //    public virtual IEnumerable<Point3D> Points => NeedsTransform ? new[] { A, B, C }.MoveRotateCopy(Position, Orientation) : new[] { A, B, C }.Copy();

    //    public override string ToString() => $"Position {Position} | A: {A} -> B: {B} -> C: {C}";
    //    public override Element Copy() => new Triangle(A.Copy(), B.Copy(), C.Copy(), Position.Copy(), Color, Name)
    //    {
    //        Orientation = Orientation.Copy(),
    //        NeedsTransform = NeedsTransform
    //    };
    //}

    //public class PointList : Element
    //{
    //    /// <summary>Liste des points représentant <see cref="PointList"/>.</summary>
    //    public List<Point3D> SourcePoints { get; set; } = new List<Point3D>();

    //    /// <summary>Type de liaison entre les points d'une <see cref="PointList"/>.</summary>
    //    public enum PointListType
    //    {
    //        NotLinked,
    //        Linked,
    //        Loop
    //    }

    //    /// <summary>Indique si les points sont liés entre eux (non bouclée).</summary>
    //    public PointListType Type { get; set; } = PointListType.NotLinked;

    //    public PointList() { }
    //    public PointList(List<Point3D> points, PointListType type = PointListType.NotLinked)
    //    {
    //        SourcePoints = points;
    //        Type = type;
    //    }

    //    public PointList(List<Point3D> points, Point3D position, Color color, string name, PointListType type = PointListType.NotLinked) : base(position, color, name)
    //    {
    //        SourcePoints = points;
    //        Type = type;
    //    }

    //    /// <summary>Liste de points placés et orientés selon Position et Orientation.</summary>
    //    public virtual IEnumerable<Point3D> Points => NeedsTransform ? SourcePoints.MoveRotateCopy(Position, Orientation) : SourcePoints.Copy();

    //    public override string ToString() => $"Position: {Position} | {SourcePoints.Count} points | Type: {Type}";
    //    public override Element Copy() =>
    //        new PointList(new List<Point3D>(SourcePoints.Copy()), Position.Copy(), Color, Name)
    //        {
    //            Orientation = Orientation.Copy(),
    //            Type = Type,
    //            NeedsTransform = NeedsTransform
    //        };
    //}

    //#endregion
    //#region Derivated Classes

    //public class Rectangle : Polygon
    //{
    //    /// <summary>Largeur du rectangle.</summary>
    //    public double Width { get; } = 0;
    //    /// <summary>Hauteur du rectangle.</summary>
    //    public double Height { get; } = 0;

    //    /// <summary>Crée une nouvelle instance de <see cref="Rectangle"/> centré en Position.</summary>
    //    public Rectangle(double width, double height, Point3D position, Color color, string name) : base(null, position, color, name)
    //    {
    //        Width = width;
    //        Height = height;

    //        Shape = new List<Point3D>
    //        {
    //            new Point3D(0, 0),
    //            new Point3D(0, height),
    //            new Point3D(width, height),
    //            new Point3D(width, 0)
    //        };

    //        foreach (Point3D point in Shape)
    //            point.Move(-width / 2, -height / 2);
    //    }

    //    public override string ToString() => $"Position: {Position} | Width: {Width} | Height: {Height}";
    //    public override Element Copy()
    //        => new Rectangle(Width, Height, Position.Copy(), Color, Name)
    //        {
    //            Orientation = Orientation.Copy(),
    //            IsFilled = IsFilled,
    //            NeedsTransform = NeedsTransform
    //        };
    //}

    //public class OutlinedRectangle : ElementList
    //{
    //    /// <summary>Largeur du rectangle.</summary>
    //    public double Width { get; } = 0;
    //    /// <summary>Hauteur du rectangle.</summary>
    //    public double Height { get; } = 0;
    //    /// <summary>Épaisseur du rectangle (vers l'intérieur).</summary>
    //    public double Thickness { get; } = 0;

    //    /// <summary>Crée une nouvelle instance de <see cref="OutlinedRectangle"/> centré en Position.</summary>
    //    public OutlinedRectangle(double width, double height, double thickness, Point3D position, Color color, string name) : base(null, position, color, name)
    //    {
    //        Width = width;
    //        Height = height;
    //        Thickness = thickness;

    //        Shape = new List<Element>
    //        {
    //            new Rectangle(width, thickness, new Point3D(width / 2, thickness / 2), color, ""), // Bas
    //            new Rectangle(thickness, height, new Point3D(thickness / 2, height / 2), color, ""), // Gauche
    //            new Rectangle(width, thickness, new Point3D(width / 2, height - thickness / 2), color, ""), // Haut
    //            new Rectangle(thickness, height, new Point3D(width - thickness / 2, height / 2), color, "") // Droite
    //        };

    //        foreach (Element element in Shape)
    //            element.Move(-width / 2, -height / 2);
    //    }

    //    public override string ToString() => $"Position: {Position} | Width: {Width} | Height: {Height} | Thickness: {Thickness}";
    //    public override Element Copy() => new OutlinedRectangle(Width, Height, Thickness, Position.Copy(), Color, Name)
    //    {
    //        Orientation = Orientation.Copy(),
    //        NeedsTransform = NeedsTransform
    //    };
    //}

    //public class OutlinedCircle : Circle
    //{
    //    /// <summary>Épaisseur du cercle (vers l'intérieur).</summary>
    //    public double Thickness { get; } = 0;

    //    public OutlinedCircle() { }
    //    public OutlinedCircle(double radius, double thickness) : base(radius) => Thickness = thickness;
    //    public OutlinedCircle(double radius, double thickness, Point3D position, Color color, string name) : base(radius, position, color, name) => Thickness = thickness;

    //    public override string ToString() => $"Position: {Position} | Radius: {Radius} | Thickness: {Thickness}";
    //    public override Element Copy()
    //        => new OutlinedCircle(Radius, Thickness, Position.Copy(), Color, Name)
    //        {
    //            Orientation = Orientation.Copy(),
    //            AngleStart = AngleStart,
    //            AngleStop = AngleStop,
    //            NeedsTransform = NeedsTransform
    //        };
    //}

    //public class Arrow : ElementList
    //{
    //    /// <summary>Longueur de la flèche.</summary>
    //    public double Length { get; } = 0;
    //    /// <summary>Hauteur de la flèche.</summary>
    //    public double Height { get; } = 0;
    //    /// <summary>Longueur interne de la flèche.</summary>
    //    public double InnerLength { get; } = 0;

    //    public Arrow(double length, double height, double innerLength, Point3D position, Point3D orientation, Color color, string name) : base(null, position, color, name)
    //    {
    //        Length = length;
    //        Height = height;
    //        InnerLength = Math.Min(innerLength, Length);
    //        Orientation = orientation;

    //        Shape = new List<Element>
    //        {
    //            new Rectangle(Length - InnerLength, Height * 0.3,
    //                new Point3D((Length - InnerLength) / 2, 0),
    //                color, name + "_base"),

    //            new Triangle(new Point3D(0, 0),
    //                         new Point3D(0, Height),
    //                         new Point3D(InnerLength, Height / 2),
    //                         new Point3D(Length - InnerLength, -Height / 2),
    //                         color, name + "_tip")
    //        };
    //    }

    //    public Arrow(Point3D tip, double height, double innerLength, Point3D position, Point3D orientation, Color color, string name) : base(null, position, color, name)
    //    {
    //        Length = tip.D;
    //        Height = height;
    //        InnerLength = Math.Min(innerLength, Length);

    //        orientation.Z = tip.Angle;
    //        Orientation = orientation;

    //        Shape = new List<Element>
    //        {
    //            new Rectangle(Length - InnerLength, Height * 0.3,
    //                new Point3D((Length - InnerLength) / 2, 0),
    //                color, name + "_base"),

    //            new Triangle(new Point3D(0, 0),
    //                         new Point3D(0, Height),
    //                         new Point3D(InnerLength, Height / 2),
    //                         new Point3D(Length - InnerLength, -Height / 2),
    //                         color, name + "_tip")
    //        };
    //    }

    //    public override string ToString() => $"Position: {Position} | Length: {Length} | Height: {Height} | InnerLength: {InnerLength}";
    //    public override Element Copy() => new Arrow(Length, Height, InnerLength, Position.Copy(), Orientation.Copy(), Color, Name)
    //    {
    //        NeedsTransform = NeedsTransform
    //    };
    //}

    //public class BigArrow : ElementList
    //{
    //    /// <summary>Longueur de la flèche.</summary>
    //    public double Length { get; } = 0;
    //    /// <summary>Hauteur de la flèche.</summary>
    //    public double Height { get; } = 0;
    //    /// <summary>Longueur interne de la flèche.</summary>
    //    public double InnerLength { get; } = 0;

    //    public BigArrow(double length, double height, double innerLength, Point3D position, Color color, string name) : base(null, position, color, name)
    //    {
    //        Length = length;
    //        Height = height;
    //        InnerLength = Math.Min(innerLength, 0.6 * length);

    //        Shape = new List<Element>
    //        {
    //            new Triangle(new Point3D(0, Height), new Point3D(Length, Height / 2), new Point3D(InnerLength, Height / 2), new Point3D(), color, name + "_tri1"),
    //            new Triangle(new Point3D(0, 0), new Point3D(Length, Height / 2), new Point3D(InnerLength, Height / 2), new Point3D(), color, name + "_tri1"),
    //            new Mark(Length / 5, Height / 5, Length / 35, new Point3D(Length / 2, Height / 2), Color.White, name + "_middleMark")
    //        };

    //        foreach (Element element in Shape)
    //            element.Move(-Length / 2, -Height / 2);
    //    }

    //    public new void SetElementsColor(Color color)
    //    {
    //        Shape[0].Color = color;
    //        Shape[1].Color = color;
    //    }

    //    public override string ToString() => $"Position: {Position} | Length: {Length} | Height: {Height} | InnerLength: {InnerLength}";
    //    public override Element Copy() => new BigArrow(Length, Height, InnerLength, Position.Copy(), Color, Name)
    //    {
    //        Orientation = Orientation.Copy(),
    //        NeedsTransform = NeedsTransform
    //    };
    //}

    //public class Mark : ElementList
    //{
    //    public double Width { get; }
    //    public double Height { get; }
    //    public double Thickness { get; }

    //    public bool IsStraight { get; } = true;

    //    public Mark(double width, double height, double thickness, Point3D position, Color color, string name) : base(null, position, color, name)
    //    {
    //        Width = width;
    //        Height = height;
    //        Thickness = thickness;

    //        Shape = new List<Element>
    //        {
    //            new Rectangle(Width, Thickness, new Point3D(0, 0), Color, Name),
    //            new Rectangle(Thickness, Height, new Point3D(0, 0), Color, Name)
    //        };

    //        if (!IsStraight)
    //            foreach (Element element in Shape)
    //                element.Rotate(0, 0, 45);
    //    }

    //    public override string ToString() => $"Position: {Position} | IsStraight: {IsStraight}";
    //    public override Element Copy() => new Mark(Width, Height, Thickness, Position.Copy(), Color, Name)
    //    {
    //        Orientation = Orientation.Copy(),
    //        NeedsTransform = NeedsTransform
    //    };
    //}

    //#endregion
    //#region Field Classes

    //public class VectorField : ElementList
    //{
    //    public VectorField(Color color, string name)
    //    {
    //        Color = color;
    //        Name = name;
    //        NeedsTransform = false;

    //        Shape = new List<Element>();
    //    }

    //    public void AddVector(Point3D pos, double angle, Point3D vector)
    //    {
    //        Shape.Add(new Arrow(vector.D, 0.05, 0.05,
    //                            pos, new Point3D(0, 0, angle),
    //                            Color, Name));
    //    }

    //    public void ClearVectors()
    //        => Shape.Clear();

    //    public override Element Copy() => new VectorField(Color, Name);
    //}
    //#endregion
}
