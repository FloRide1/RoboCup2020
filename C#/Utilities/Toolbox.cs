using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace Utilities
{
    /// <summary>
    /// Contient plusieurs fonctions mathématiques utiles.
    /// </summary>
    public static class Toolbox
    {
        /// <summary>
        /// Renvoie la valeur max d'une liste de valeurs
        /// </summary>
        public static double Max(params double[] values)
            => values.Max();


        public static double[,] Multiply(double[,] matrix1, double[,] matrix2)
        {
            // cahing matrix lengths for better performance  
            var matrix1Rows = matrix1.GetLength(0);
            var matrix1Cols = matrix1.GetLength(1);
            var matrix2Rows = matrix2.GetLength(0);
            var matrix2Cols = matrix2.GetLength(1);

            // checking if product is defined  
            if (matrix1Cols != matrix2Rows)
                throw new InvalidOperationException
                  ("Product is undefined. n columns of first matrix must equal to n rows of second matrix");

            // creating the final product matrix  
            double[,] product = new double[matrix1Rows, matrix2Cols];

            // looping through matrix 1 rows  
            for (int matrix1_row = 0; matrix1_row < matrix1Rows; matrix1_row++)
            {
                // for each matrix 1 row, loop through matrix 2 columns  
                for (int matrix2_col = 0; matrix2_col < matrix2Cols; matrix2_col++)
                {
                    // loop through matrix 1 columns to calculate the dot product  
                    for (int matrix1_col = 0; matrix1_col < matrix1Cols; matrix1_col++)
                    {
                        product[matrix1_row, matrix2_col] +=
                          matrix1[matrix1_row, matrix1_col] *
                          matrix2[matrix1_col, matrix2_col];
                    }
                }
            }

            return product;
        }

        public static double[,] Inverse(double[,] matrix)
        {
            double a = matrix[0, 0];
            double b = matrix[0, 1];
            double c = matrix[1, 0];
            double d = matrix[1, 1];

            double frac = 1 / (a * d - b * c);

            double[,] result = new double[2, 2];
            result[0, 0] = frac * d;
            result[0, 1] = -frac * b;
            result[1, 0] = -frac * c;
            result[1, 1] = frac * a;

            return result;
        }

        public static double[,] Addition_Matrices(double[,] matrix1, double[,] matrix2)
        {
            double[,] resultat = new double[matrix1.GetLength(0), matrix1.GetLength(1)];
            for (int lignes = 0; lignes < matrix1.GetLength(0); lignes++)
            {
                for (int colonnes = 0; colonnes < matrix1.GetLength(1); colonnes++)
                {
                    resultat[lignes, colonnes] = matrix1[lignes, colonnes] + matrix2[lignes, colonnes];
                }
            }

            return resultat;
        }

        public static double[,] Transpose(double[,] matrix)
        {
            int w = matrix.GetLength(0);
            int h = matrix.GetLength(1);

            double[,] result = new double[h, w];

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    result[j, i] = matrix[i, j];
                }
            }

            return result;
        }

        /// <summary>Converti un angle en degrés en un angle en radians.</summary>
        public static float DegToRad(float angleDeg)
            => angleDeg * (float)Math.PI / 180f;

        /// <summary>Converti un angle en degrés en un angle en radians.</summary>
        public static double DegToRad(double angleDeg)
            => angleDeg * Math.PI / 180;

        /// <summary>Converti un angle en degrés en un angle en radians.</summary>
        public static float RadToDeg(float angleRad)
            => angleRad / (float)Math.PI * 180f;

        /// <summary>Converti un angle en radians en un angle en degrés.</summary>
        public static double RadToDeg(double angleRad)
            => angleRad / Math.PI * 180;

        /// <summary>Renvoie l'angle modulo 2*pi entre -pi et pi.</summary>
        public static double Modulo2PiAngleRad(double angleRad)
        {
            double angleTemp = (angleRad - Math.PI) % (2 * Math.PI) + Math.PI;
            return (angleTemp + Math.PI) % (2 * Math.PI) - Math.PI;
        }

        /// <summary>Renvoie l'angle modulo pi entre -pi/2 et pi/2.</summary>
        public static double ModuloPiAngleRadian(double angleRad)
        {
            double angleTemp = (angleRad - Math.PI / 2.0) % Math.PI + Math.PI / 2.0;
            return (angleTemp + Math.PI / 2.0) % Math.PI - Math.PI / 2.0;
        }


        /// <summary>Renvoie l'angle modulo pi entre -pi et pi.</summary>
        public static double ModuloPiDivTwoAngleRadian(double angleRad)
        {
            double angleTemp = (angleRad - Math.PI / 4.0) % (Math.PI / 2) + Math.PI / 4.0;
            return (angleTemp + Math.PI / 4.0) % (Math.PI / 2) - Math.PI / 4.0;
        }

        /// <summary>Borne la valeur entre les deux valeurs limites données.</summary>
        public static double LimitToInterval(double value, double lowLimit, double highLimit)
        {
            if (value > highLimit)
                return highLimit;
            else if (value < lowLimit)
                return lowLimit;
            else
                return value;
        }

        /// <summary>Décale un angle dans un intervale de [-PI, PI] autour d'un autre.</summary>
        public static double ModuloByAngle(double angleToCenterAround, double angleToCorrect)
        {
            // On corrige l'angle obtenu pour le moduloter autour de l'angle Kalman
            int decalageNbTours = (int)Math.Round((angleToCorrect - angleToCenterAround) / (2 * Math.PI));
            double thetaDest = angleToCorrect - decalageNbTours * 2 * Math.PI;

            return thetaDest;
        }

        public static double Distance(PointD pt1, PointD pt2)
        {
            return Math.Sqrt((pt2.X - pt1.X) * (pt2.X - pt1.X) + (pt2.Y - pt1.Y) * (pt2.Y - pt1.Y));
            //return Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2));
        }
        public static double Distance(PolarPointRssi pt1, PolarPointRssi pt2)
        {
            return Math.Sqrt(pt1.Distance * pt1.Distance + pt2.Distance * pt2.Distance - 2 * pt1.Distance * pt2.Distance * Math.Cos(pt1.Angle - pt2.Angle));
        }

        public static double Distance(PolarPointRssiExtended pt1, PolarPointRssiExtended pt2)
        {
            return Math.Sqrt(pt1.Pt.Distance * pt1.Pt.Distance + pt2.Pt.Distance * pt2.Pt.Distance - 2 * pt1.Pt.Distance * pt2.Pt.Distance * Math.Cos(pt1.Pt.Angle - pt2.Pt.Angle));
        }


        public static double DistanceL1(PointD pt1, PointD pt2)
        {
            return Math.Abs(pt2.X - pt1.X) + Math.Abs(pt2.Y - pt1.Y);
            //return Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2));
        }

        public static double Distance(double xPt1, double yPt1, double xPt2, double yPt2)
        {
            return Math.Sqrt(Math.Pow(xPt2 - xPt1, 2) + Math.Pow(yPt2 - yPt1, 2));
        }

        public static double DistancePointToLine(PointD pt, PointD LinePt, double LineAngle)
        {
            var xLineVect = Math.Cos(LineAngle);
            var yLineVect = Math.Sin(LineAngle);
            var dot = (pt.X - LinePt.X) * (yLineVect) - (pt.Y - LinePt.Y) * (xLineVect);
            return Math.Abs(dot);
        }
        public static double DistancePointToLine(PointD pt, PointD LinePt1, PointD LinePt2)
        {
            var lineAngle = Math.Atan2(LinePt2.Y - LinePt1.Y, LinePt2.X - LinePt1.X);
            var xLineVect = Math.Cos(lineAngle);
            var yLineVect = Math.Sin(lineAngle);
            var dot = (pt.X - LinePt1.X) * (yLineVect) - (pt.Y - LinePt1.Y) * (xLineVect);
            return Math.Abs(dot);
        }

        public static double DistancePointToSegment(PointD pt, PointD ptSeg1, PointD ptSeg2)
        {
            var A = pt.X - ptSeg1.X;
            var B = pt.Y - ptSeg1.Y;
            var C = ptSeg2.X - ptSeg1.X;
            var D = ptSeg2.Y - ptSeg1.Y;

            double dot = A * C + B * D;
            double len_sq = C * C + D * D;
            double param = -1;
            if (len_sq != 0) //in case of 0 length line
                param = dot / len_sq;

            double xx, yy;

            if (param < 0)
            {
                xx = ptSeg1.X;
                yy = ptSeg1.Y;
            }
            else if (param > 1)
            {
                xx = ptSeg2.X;
                yy = ptSeg2.Y;
            }
            else
            {
                xx = ptSeg1.X + param * C;
                yy = ptSeg1.Y + param * D;
            }

            var dx = pt.X - xx;
            var dy = pt.Y - yy;

            double distance = Math.Sqrt(dx * dx + dy * dy);
            return distance;
        }

        public static PointD GetInterceptionLocation(Location target, Location hunter, double huntingSpeed)
        {
            //D'après Al-Kashi, si d est la distance entre le pt target et le pt chasseur, que les vitesses sont constantes 
            //et égales à Vtarget et Vhunter
            //Rappel Al Kashi : A² = B²+C²-2BCcos(alpha) , alpha angle opposé au segment A
            //On a au moment de l'interception à l'instant Tinter: 
            //A = Vh * Tinter
            //B = VT * Tinter
            //C = initialDistance;
            //alpha = Pi - capCible - angleCible

            double targetSpeed = Math.Sqrt(Math.Pow(target.Vx, 2) + Math.Pow(target.Vy, 2));
            double initialDistance = Toolbox.Distance(new PointD(hunter.X, hunter.Y), new PointD(target.X, target.Y));
            double capCible = Math.Atan2(target.Vy, target.Vx);
            double angleCible = Math.Atan2(target.Y - hunter.Y, target.X - hunter.X);
            double angleCapCibleDirectionCibleChasseur = Math.PI - capCible + angleCible;

            //Résolution de ax²+bx+c=0 pour trouver Tinter
            double a = Math.Pow(huntingSpeed, 2) - Math.Pow(targetSpeed, 2);
            double b = 2 * initialDistance * targetSpeed * Math.Cos(angleCapCibleDirectionCibleChasseur);
            double c = -Math.Pow(initialDistance, 2);

            double delta = b * b - 4 * a * c;
            double t1 = (-b - Math.Sqrt(delta)) / (2 * a);
            double t2 = (-b + Math.Sqrt(delta)) / (2 * a);

            if (delta > 0 && t2 < 10)
            {
                double xInterception = target.X + targetSpeed * Math.Cos(capCible) * t2;
                double yInterception = target.Y + targetSpeed * Math.Sin(capCible) * t2;
                return new PointD(xInterception, yInterception);
            }
            else
                return null;
        }

        [DllImport("shlwapi.dll")]
        public static extern int ColorHLSToRGB(int H, int L, int S);

        static public System.Drawing.Color HLSToColor(int H, int L, int S)
        {
            //
            // Convert Hue, Luminance, and Saturation values to System.Drawing.Color structure.
            // H, L, and S are in the range of 0-240.
            // ColorHLSToRGB returns a Win32 RGB value (0x00BBGGRR).  To convert to System.Drawing.Color
            // structure, use ColorTranslator.FromWin32.
            //
            return ColorTranslator.FromWin32(ColorHLSToRGB(H, L, S));

        }

        static public PointDExtended ConvertPolarToPointD(PolarPointRssiExtended point)
        {
            return new PointDExtended(new PointD(point.Pt.Distance * Math.Cos(point.Pt.Angle), point.Pt.Distance * Math.Sin(point.Pt.Angle)), point.Color, point.Width);
        }

        static public PolarPointRssiExtended ConvertPointDToPolar(PointDExtended point)
        {
            return new PolarPointRssiExtended(new PolarPointRssi(Math.Atan2(point.Pt.Y, point.Pt.X), Math.Sqrt(Math.Pow(point.Pt.X, 2) + Math.Pow(point.Pt.Y, 2)), 0), point.Width, point.Color);
        }
        static public PointD ConvertPolarToPointD(PolarPointRssi point)
        {
            return new PointD(point.Distance * Math.Cos(point.Angle), point.Distance * Math.Sin(point.Angle));
        }

        static public PolarPointRssi ConvertPointDToPolar(PointD point)
        {
            return new PolarPointRssi(Math.Atan2(point.Y, point.X), Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2)), 0);
        }

        static public PointDExtended GetCrossingPointBetweenSegment(SegmentExtended segment_a, SegmentExtended segment_b)
        {
            PointDExtended crossing_point = new PointDExtended(new PointD(0, 0), segment_a.Color, segment_a.Width);

            if (segment_a.Segment.X1 == segment_a.Segment.X2 || segment_b.Segment.X1 == segment_b.Segment.X2)
            {
                return crossing_point;
            }
            double slope_a = (segment_a.Segment.Y2 - segment_a.Segment.Y1) / (segment_a.Segment.X2 - segment_a.Segment.X1);
            double y_intercept_a = segment_a.Segment.Y1 - (segment_a.Segment.X1) * slope_a;

            double slope_b = (segment_b.Segment.Y2 - segment_b.Segment.Y1) / (segment_b.Segment.X2 - segment_b.Segment.X1);
            double y_intercept_b = segment_b.Segment.Y1 - (segment_b.Segment.X1) * slope_b;

            if (slope_a == slope_b)
            {
                return crossing_point;
            }

            double x = (y_intercept_b - y_intercept_a) / (slope_a - slope_b);
            double y = slope_a * x + y_intercept_a;

            crossing_point.Pt = new PointD(x, y);

            return crossing_point;

        }

        /// <summary>
        /// Get all Combination of list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetKCombs(list, length - 1).SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0), (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public static Tuple<PointD, PointD, PointD, PointD> GetCornerOfAnOrientedRectangle(RectangleOriented rectangle)
        {
            double radius_of_the_circle = Math.Sqrt(Math.Pow(rectangle.Lenght, 2) + Math.Pow(rectangle.Width, 2)) / 2;
            double a_1_angle = Modulo2PiAngleRad(Math.Atan2(  rectangle.Width,   rectangle.Lenght) + rectangle.Angle);
            double a_3_angle = Modulo2PiAngleRad(Math.Atan2(  rectangle.Width, - rectangle.Lenght) + rectangle.Angle);
            double a_2_angle = Modulo2PiAngleRad(Math.Atan2(- rectangle.Width,   rectangle.Lenght) + rectangle.Angle);
            double a_4_angle = Modulo2PiAngleRad(Math.Atan2(- rectangle.Width, - rectangle.Lenght) + rectangle.Angle);

            PointD polar_a_1 = ConvertPolarToPointD(new PolarPointRssi(a_1_angle, radius_of_the_circle, 0));
            PointD polar_a_2 = ConvertPolarToPointD(new PolarPointRssi(a_2_angle, radius_of_the_circle, 0));
            PointD polar_a_3 = ConvertPolarToPointD(new PolarPointRssi(a_3_angle, radius_of_the_circle, 0));
            PointD polar_a_4 = ConvertPolarToPointD(new PolarPointRssi(a_4_angle, radius_of_the_circle, 0));

            PointD a1 = new PointD(polar_a_1.X + rectangle.Center.X, polar_a_1.Y + rectangle.Center.Y);
            PointD a2 = new PointD(polar_a_2.X + rectangle.Center.X, polar_a_2.Y + rectangle.Center.Y);
            PointD a3 = new PointD(polar_a_3.X + rectangle.Center.X, polar_a_3.Y + rectangle.Center.Y);
            PointD a4 = new PointD(polar_a_4.X + rectangle.Center.X, polar_a_4.Y + rectangle.Center.Y);

            return new Tuple<PointD, PointD, PointD, PointD>(a1, a2, a3, a4);
        }

        public static bool TestIfPointInsideAnOrientedRectangle(RectangleOriented rectangle, PointD point)
        {
            /// Whe simply make the dot product with each angle
            Tuple<PointD, PointD, PointD, PointD> corners = GetCornerOfAnOrientedRectangle(rectangle);

            PointD a = corners.Item1;
            PointD b = corners.Item2;
            PointD c = corners.Item3;

            PointD vector_a_b = new PointD(b.X - a.X, b.Y - a.Y);
            PointD vector_a_c = new PointD(c.X - a.X, c.Y - a.Y);
            PointD vector_a_point = new PointD(point.X - a.X, point.Y - a.Y);

            double dot_product_point_b = (vector_a_b.X * vector_a_point.X) + (vector_a_b.Y * vector_a_point.Y);
            double dot_product_point_c = (vector_a_c.X * vector_a_point.X) + (vector_a_c.Y * vector_a_point.Y);

            double dot_product_b_b = Math.Pow(vector_a_b.X, 2) + Math.Pow(vector_a_b.Y, 2);
            double dot_product_c_c = Math.Pow(vector_a_c.X, 2) + Math.Pow(vector_a_c.Y, 2);
            return dot_product_point_b >= 0 && dot_product_point_c >= 0 && dot_product_point_b <= dot_product_b_b && dot_product_point_c <= dot_product_c_c;
        }

        public static double DotProduct(PointD vector_a, PointD vector_b)
        {
            return (vector_a.X * vector_b.X) + (vector_a.Y * vector_b.Y);
        }

        public static double Distance(SegmentExtended segment)
        {
            return Math.Sqrt(Math.Pow(segment.Segment.X2 - segment.Segment.X1, 2) + Math.Pow(segment.Segment.Y2 - segment.Segment.Y1, 2));
        }

        public static double Angle(SegmentExtended segment)
        {
            return Math.Atan2(segment.Segment.Y2 - segment.Segment.Y1, segment.Segment.X2 - segment.Segment.X1);
        }

        public static double Angle(PointD pt1, PointD pt2)
        {
            return Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X);
        }

        public static void SwapNum(ref double x, ref double y)
        {
            x += y;
            y = x - y;
            x -= y;
        }

        public static void SwapNum(ref SegmentExtended s1, ref SegmentExtended s2)
        {
            SegmentExtended temporary_segment = s1;
            s1 = s2;
            s2 = temporary_segment;
        }
    }
}

