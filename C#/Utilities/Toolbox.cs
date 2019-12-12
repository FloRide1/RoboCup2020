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
            return Math.Sqrt(Math.Pow(pt2.X - pt1.X, 2) + Math.Pow(pt2.Y - pt1.Y, 2));
        }
        public static double Distance(double xPt1, double yPt1, double xPt2, double yPt2)
        {
            return Math.Sqrt(Math.Pow(xPt2 - xPt1, 2) + Math.Pow(yPt2 - yPt1, 2));
        }

        /// <summary>Calcul le modulo autour d'un angle d'un autre angle donné.</summary>
        //public static double ModuloAroundAngle(double angleToCenterAround, double angleToCorrect)
        //{
        //    // On corrige l'angle obtenu pour le moduloter autour de l'angle Kalman
        //    int decalageNbTours = (int)Math.Round(angleToCenterAround / (2 * Math.PI));
        //    double thetaCorrected = angleToCorrect + decalageNbTours * 2 * Math.PI;

        //    return thetaCorrected;
        //}
    }
}
