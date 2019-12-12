using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public static class GeometryOperations
    {
        #region Move & Rotate

        public static Point3D RotatePointAroundCenter(Point3D pt, Point3D center, double angle)
        {
            double x = center.X + (pt.X - center.X) * Math.Cos(angle) - (pt.Y - center.Y) * Math.Sin(angle);
            double y = center.Y + (pt.X - center.X) * Math.Sin(angle) + (pt.Y - center.Y) * Math.Cos(angle);

            return new Point3D(x, y, pt.Z);
        }

        public static void RotatePoint(Point3D pt, Point3D center, double angle)
        {
            double x = center.X + (pt.X - center.X) * Math.Cos(angle) - (pt.Y - center.Y) * Math.Sin(angle);
            double y = center.Y + (pt.X - center.X) * Math.Sin(angle) + (pt.Y - center.Y) * Math.Cos(angle);

            pt.X = x;
            pt.Y = y;
        }

        public static Point3D PointMove(Point3D pt, double x, double y, double angle)
        {
            Point3D rotatedPoint = RotatePointAroundCenter(pt, new Point3D(0, 0), angle);
            rotatedPoint.Move(x, y, 0);

            return rotatedPoint;
        }

        public static void PtMove(Point3D pt, double x, double y, double angle)
        {
            RotatePoint(pt, new Point3D(0, 0), angle);
            pt.Move(x, y, 0);
        }

        public static Point3D PointMove(Point3D pt, double x, double y, double z, double angle)
        {
            Point3D ptOutput = new Point3D(pt.X * Math.Cos(angle) - pt.Y * Math.Sin(angle) + x,
                pt.X * Math.Sin(angle) + pt.Y * Math.Cos(angle) + y, pt.Z + z);
            return ptOutput;
        }

        #endregion
        #region Calculation

        public static double Distance(Point3D p1, Point3D p2)
            => Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));

        /// <summary>Renvoie l'angle entre 2 points en radians.</summary>
        public static double Angle(Point3D p1, Point3D p2)
            => Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);

        public static double Angle(Segment segmentA, Segment segmentB)
        {
            double angleA = Math.Atan2(segmentA.B.Y - segmentA.A.Y, segmentA.B.X - segmentA.A.X);
            double angleB = Math.Atan2(segmentB.B.Y - segmentB.A.Y, segmentB.B.X - segmentB.A.X);
            return angleB - angleA;
        }

        public static double ScalarProduct(Segment s1, Segment s2)
            => (s1.B.X - s1.A.X) * (s2.B.X - s2.A.X) + (s1.B.Y - s1.A.Y) * (s2.B.Y - s2.A.Y);

        /// <summary>Modifie les coordonnées du <see cref="Point3D"/> actuellement dans un
        /// référentiel d'origine dont on connait le centre dans un autre référentiel de destination (angles en degrés).</summary>
        /// <param name="pt">Point dont changer le référentiel.</param>
        /// <param name="ref_x">Position x du centre du référentiel d'origine dans le référentiel de destination.</param>
        /// <param name="ref_y">Position y du centre du référentiel d'origine dans le référentiel de destination.</param>
        /// <param name="ref_theta">Angle theta en degrés du centre du référentiel d'origine dans le référentiel de destination.</param>
        public static void ToParentRef(this Point3D pt, double ref_x, double ref_y, double ref_theta)
        {
            pt.Rotate2D(ref_theta);
            pt.X += ref_x;
            pt.Y += ref_y;
        }

        /// <summary>Modifie les coordonnées du <see cref="Point3D"/> actuellement dans un
        /// référentiel d'origine dont on connait le centre dans un autre référentiel de destination (angles en degrés).</summary>
        /// <param name="pt">Point dont changer le référentiel.</param>
        /// <param name="ref_x">Position x du centre du référentiel d'origine dans le référentiel de destination.</param>
        /// <param name="ref_y">Position y du centre du référentiel d'origine dans le référentiel de destination.</param>
        /// <param name="ref_theta">Angle theta en degrés du centre du référentiel d'origine dans le référentiel de destination.</param>
        public static void ToChildRef(this Point3D pt, double ref_x, double ref_y, double ref_theta)
        {
            // Il faut prendre l'opposé des valeurs dans le cas parent -> enfant
            ref_x = -ref_x;
            ref_y = -ref_y;
            ref_theta = -ref_theta;

            pt.X += ref_x;
            pt.Y += ref_y;

            // Pour un passage parent -> enfant, il faut faire la rotation après la translation
            pt.Rotate2D(ref_theta);
        }

        #endregion
        #region Segment Operations

        public static double GetDistancePointToSegment(Point3D pt, Segment S)
        {
            Point3D p1 = S.A;
            Point3D p2 = S.B;
            Point3D closest;

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) /
                       (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Point3D(p1.X, p1.Y);
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
            }
            else if (t > 1)
            {
                closest = new Point3D(p2.X, p2.Y);
                dx = pt.X - p2.X;
                dy = pt.Y - p2.Y;
            }
            else
            {
                closest = new Point3D(p1.X + t * dx, p1.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static Point3D GetIntersectionPoint(Point3D A, Point3D B, Point3D C, Point3D D)
        {
            var I = new Point3D(B.X - A.X, B.Y - A.Y);
            var J = new Point3D(D.X - C.X, D.Y - C.Y);

            double m = 0;//, k = 0;
            double diviseur = I.X * J.Y - I.Y * J.X;

            if (diviseur != 0)
            {
                m = (I.X * A.Y
                     - I.X * C.Y
                     - I.Y * A.X
                     + I.Y * C.X
                    ) / diviseur;
                //k = (J.X * A.Y
                //     - J.X * C.Y
                //     - J.Y * A.X
                //     + J.Y * C.X
                //    ) / diviseur;
            }

            // ou bien en utilisant k
            // return Point(_A + k * I);
            return new Point3D(C.X + m * J.X, C.Y + m * J.Y);
        }

        public static Point3D GetIntersectionOfSegments(Segment S1, Segment S2)
        {
            Point3D ptInter = GetIntersectionPoint(S1.A, S1.B, S2.A, S2.B);

            if (IsPointInSegment(ptInter, S1.A, S1.B) && IsPointInSegment(ptInter, S2.A, S2.B))
                return ptInter;
            else return null;
        }

        public static bool IsPointInSegment(Point3D pt, Segment seg) => IsPointInSegment(pt, seg.A, seg.B);

        public static bool IsPointInSegment(Point3D pt, Point3D segA, Point3D segB)
        {
            //Les + ou - correction sont là pour éviter les erreurs de calcul en virgule flottante !
            double correction = 0.001;
            if (((pt.X <= (segA.X + correction) && pt.X >= (segB.X - correction)) ||
                 (pt.X >= (segA.X - correction) && pt.X <= (segB.X + correction))) &&
                ((pt.Y <= (segA.Y + correction) && pt.Y >= (segB.Y - correction)) ||
                 (pt.Y >= (segA.Y - correction) && pt.Y <= (segB.Y + correction))))
                return true;
            else return false;
        }

        /// <summary>Retourne une liste de point définissant un segment en utilisant l'algorithme de Bresenham.</summary>
        public static List<Point3D> GetPointListFromSegment(int x0, int y0, int x1, int y1)
        {
            // Explication de l'algo de Bresenham : https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
            List<Point3D> points = new List<Point3D>();

            int w = x1 - x0;
            int h = y1 - y0;

            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;

            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (longest <= shortest)
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++)
            {
                points.Add(new Point3D(x0, y0));
                numerator += shortest;
                if (numerator >= longest)
                {
                    numerator -= longest;
                    x0 += dx1;
                    y0 += dy1;
                }
                else
                {
                    x0 += dx2;
                    y0 += dy2;
                }
            }

            return points;
        }

        #endregion
    }

    #region Extensions

    public static class ConvertersExtensions
    {
        ///// <summary>Convertit un <see cref="System.Windows.Point"/> en <see cref="Point3D"/>.</summary>
        //public static Point3D To_Point3D(this System.Windows.Point point, double z = 0) => new Point3D(point.X, point.Y, z);
        /// <summary>Convertit un <see cref="System.Drawing.Point"/> en <see cref="Point3D"/>.</summary>
        public static Point3D To_Point3D(this System.Drawing.Point point, double z = 0) => new Point3D(point.X, point.Y, z);
    }

    public static class OtherExtensions
    {
        public static IEnumerable<Point3D> Copy(this IEnumerable<Point3D> list)
        {
            foreach (Point3D p in list)
                yield return p.Copy();
        }

        public static void Move(this IEnumerable<Point3D> list, Point3D dpt) => list.Move(dpt.X, dpt.Y, dpt.Z);
        public static void Move(this IEnumerable<Point3D> list, double dx, double dy, double dz)
        {
            foreach (Point3D pt in list)
                pt.Move(dx, dy, dz);
        }

        public static IEnumerable<Point3D> MoveRotateCopy(this IEnumerable<Point3D> points, Point3D dpt, Point3D angles)
        {
            List<Point3D> newPoints = new List<Point3D>();
            foreach (Point3D point in points)
            {
                Point3D newPoint = point.Copy();
                newPoint.Rotate(angles);
                newPoint.Move(dpt);

                newPoints.Add(newPoint);
            }

            return newPoints;
        }

        public static IEnumerable<Point3D> MoveRotateCopy(this IEnumerable<Point3D> points, Point3D dpt, Point3D angles, Point3D centerPosition)
        {
            List<Point3D> newPoints = new List<Point3D>();
            foreach (Point3D point in points)
            {
                Point3D newPoint = point.Copy();
                newPoint.Rotate(angles, centerPosition);
                newPoint.Move(dpt);

                newPoints.Add(newPoint);
            }

            return newPoints;
        }

        /// <summary>
        /// Ajoute <see cref="Element"/> au dictionnaire ou remplace l'<see cref="Element"/> ayant le même Name par l'<see cref="Element"/> spécifié.
        /// </summary>
        public static void AddOrUpdate(this Dictionary<string, Element> dic, Element elementToAdd)
        {
            if (dic.ContainsKey(elementToAdd.Name))
                dic[elementToAdd.Name] = elementToAdd;
            else
                dic.Add(elementToAdd.Name, elementToAdd);
        }

        public static void RemoveIfContains<T>(this Dictionary<string, T> dic, string stringToSearch)
        {
            List<string> keysToRemove = new List<string>();

            foreach (var item in dic)
                if (item.Key.Contains(stringToSearch))
                    keysToRemove.Add(item.Key);

            foreach (string key in keysToRemove)
                dic.Remove(key);
        }

        /// <summary>Copie les éléments (nouvelle référence) de la liste vers une nouvelle liste.</summary>
        public static IEnumerable<Element> Copy(this IEnumerable<Element> source)
        {
            foreach (Element element in source)
                yield return element;
        }

        /// <summary>Copie les éléments (nouvelle référence) de la liste dans une autre liste.</summary>
        public static void CopyTo(this IEnumerable<Element> source, List<Element> dest)
        {
            dest.Clear();
            foreach (Element element in source)
                dest.Add(element.Copy());
        }

        /// <summary>Obtient une liste de segments (bouclée) entre les points de la liste.</summary>
        public static List<Segment> GetSegmentList(this List<Point3D> pointList)
        {
            List<Segment> objectsSegmentList = new List<Segment>();
            if (pointList.Count >= 2)
            {
                for (int i = 0; i < pointList.Count - 1; i++)
                    objectsSegmentList.Add(new Segment(pointList[i], pointList[i + 1]));

                // Reboucle sur le premier et le dernier point
                objectsSegmentList.Add(new Segment(pointList[pointList.Count - 1], pointList[0]));
            }

            return objectsSegmentList;
        }

        /// <summary>Renvoie une liste de points définissant ce segment selon l'algorithme de Bresenham.</summary>
        /// <param name="s">Segment à traiter.</param>
        /// <param name="pixelSize">Taille d'un pixel de la grille dans laquelle trouver les points.</param>
        public static IEnumerable<Point3D> GetPoints(this Segment s, double pixelSize)
        {
            int x0 = (int)(s.A.X / pixelSize), y0 = (int)(s.A.Y / pixelSize);
            int x1 = (int)(s.B.X / pixelSize), y1 = (int)(s.B.Y / pixelSize);
            List<Point3D> pts = GeometryOperations.GetPointListFromSegment(x0, y0, x1, y1);

            foreach (Point3D p in pts)
            {
                p.X *= pixelSize;
                p.Y *= pixelSize;
            }

            return pts;
        }
    }

    #endregion
}
