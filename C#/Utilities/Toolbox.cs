using System;
using System.Drawing;
using System.Linq;


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
            return Math.Sqrt((pt2.X - pt1.X)* (pt2.X - pt1.X) + (pt2.Y - pt1.Y)* (pt2.Y - pt1.Y));
            //return Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2));
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
            //var A = x - x1;
            //var B = y - y1;
            //var C = x2 - x1;
            //var D = y2 - y1;

            //var dot = A * C + B * D;
            //var len_sq = C * C + D * D;
            //var param = -1;
            //if (len_sq != 0) //in case of 0 length line
            //    param = dot / len_sq;

            //var xx, yy;

            //if (param < 0)
            //{
            //    xx = x1;
            //    yy = y1;
            //}
            //else if (param > 1)
            //{
            //    xx = x2;
            //    yy = y2;
            //}
            //else
            //{
            //    xx = x1 + param * C;
            //    yy = y1 + param * D;
            //}

            //var dx = x - xx;
            //var dy = y - yy;
            //return Math.sqrt(dx * dx + dy * dy);
        }
    }
}

