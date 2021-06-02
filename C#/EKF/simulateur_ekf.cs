using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Utilities;
using System.Linq;
using System.Threading;
using RobotInterface;
using Constants;
using SciChart.Charting.Visuals;

namespace EKF
{
    public class Simulateur_ekf
    {
        //double LengthTerrain = 0;
        //double WidthTerrain = 0;
        //double vmax = 20;
        static List<List<double>> liste_landmarks_vus;
        //double anglePerceptionRobot = Math.PI;

        static Location PosRobot;
        static public List<List<double>> fabrication_landmarks()
        {
            List<List<double>> liste_total_landmarks = new List<List<double>> { };

            List<double> ld = new List<double> { -1.5, -1 };
            liste_total_landmarks.Add(ld);

            ld = new List<double> { -1.5, 0 };
            liste_total_landmarks.Add(ld);

            ld = new List<double> { -1.5, 1 };
            liste_total_landmarks.Add(ld);


            ld = new List<double> { 0, -1 };
            liste_total_landmarks.Add(ld);

            ld = new List<double> { 0, 1 };
            liste_total_landmarks.Add(ld);

            ld = new List<double> { 1.5, -1 };
            liste_total_landmarks.Add(ld);

            ld = new List<double> { 1.5, 0 };
            liste_total_landmarks.Add(ld);


            ld = new List<double> { 1.5, 1 };
            liste_total_landmarks.Add(ld);                                          // on crée des ld artificiels là ou on veut 


            return liste_total_landmarks; // Cette liste doit être triée
        }
        static public List<List<double>> Landmarks_vus(Location PosRobot, List<List<double>> liste_total_landmarks, double anglePerceptionRobot)
        {
            List<List<double>> MaListe = new List<List<double>> { };

            foreach (List<double> ld in liste_total_landmarks)
            {
                if (Math.Abs((PosRobot.Theta) - (Math.Atan2((ld[1] - PosRobot.Y), (ld[0] - PosRobot.X)))) < anglePerceptionRobot / 2)
                {
                    MaListe.Add(ld);
                }
            }
            return MaListe;
        }

        static public Location PosRobotQuandTuVeux(double date, Location PosRobot)
        {
            if (date <= 4.5) 
            {
                if (date < 0.5) //accélération 
                {
                    PosRobot.X = 0.5 * date * date - 1;
                    PosRobot.Vx = date;
                }
                else if (date < 4) // v =cst
                {
                    PosRobot.X = 0.5 * date + 0.125 - 1;
                    PosRobot.Vx = 0.5;
                }
                else //descélération
                {
                    date -= 4; 
                    PosRobot.X = -0.5 * date * date + 0.5 * date + 1.875 - 1;
                    PosRobot.Vx = 0.5;
                    date += 4;
                }
            } //1er trajet, REMARQUE : j'ai mis -1 dans tout les x car on part de (-1, -0.5)

            else if (date<=7)
            {
                date = date - 4.5;
                if (date<0.5) //accélération 
                {
                    PosRobot.Y = 0.5 * date * date-0.5;
                    PosRobot.Vy = date;
                }
                else if (date < 2) //v = cst
                {
                    PosRobot.Y = 0.5 * date + 0.125 -0.5;
                    PosRobot.Vy = 0.5;
                }
                else //descélération
                {
                    date -= 2;
                    PosRobot.Y = -0.5 * date * date + 0.5 * date + 0.375 - 0.5;
                    PosRobot.Vy = 0.5;
                    date += 2;
                }
            } // 2nd trajet : REMARQUE : j'ai mis -0.5 dans tout les y car on part de (1, -0.5)

            else if (date<=14)  
            {
                date -= 7;
                if (date<1) //accélération
                {
                    PosRobot.Theta = date * date * Math.PI / 12;
                    PosRobot.Vtheta = date * Math.PI / 6;
                }
                else if (date<6) //v=cst
                {
                    PosRobot.Theta = date * Math.PI / 6 + Math.PI /12;
                    PosRobot.Vtheta = Math.PI / 6;
                }
                else
                {
                    date -= 6;
                    PosRobot.Theta = -date * date * Math.PI / 12 + date * Math.PI / 6 + 11 * Math.PI / 12;
                    PosRobot.Vtheta = -date * Math.PI / 6 + Math.PI / 6;
                    date += 6;
                }
            } // 3eme trajet

            return PosRobot;
        }


        static void Main(string[] args)
        {
            // Set this code once in App.xaml.cs or application startup
            SciChartSurface.SetRuntimeLicenseKey("RJWA77RbaJDdCRJpg4Iunl5Or6/FPX1xT+Gzu495Eaa0ZahxWi3jkNFDjUb/w70cHXyv7viRTjiNRrYqnqGA+Dc/yzIIzTJlf1s4DJvmQc8TCSrH7MBeQ2ON5lMs/vO0p6rBlkaG+wwnJk7cp4PbOKCfEQ4NsMb8cT9nckfdcWmaKdOQNhHsrw+y1oMR7rIH+rGes0jGGttRDhTOBxwUJK2rBA9Z9PDz2pGOkPjy9fwQ4YY2V4WPeeqM+6eYxnDZ068mnSCPbEnBxpwAldwXTyeWdXv8sn3Dikkwt3yqphQxvs0h6a8Dd6K/9UYni3o8pRkTed6SWodQwICcewfHTyGKQowz3afARj07et2h+becxowq3cRHL+76RyukbIXMfAqLYoT2UzDJNsZqcPPq/kxeXujuhT4SrNF3444MU1GaZZ205KYEMFlz7x/aEnjM6p3BuM6ZuO3Fjf0A0Ki/NBfS6n20E07CTGRtI6AsM2m59orPpI8+24GFlJ9xGTjoRA==");

            PosRobot = new Location(-1, -0.5, 0, 0, 0, 0); // position initiale du robot 

            double anglePerceptionRobot = Math.PI ;

            List<List<double>>  liste_total_landmarks = fabrication_landmarks();

            liste_landmarks_vus = Landmarks_vus(PosRobot, liste_total_landmarks, anglePerceptionRobot);

            

        }

        //OnLandmarksAndOdoReceived(EKF, PolarSpeedArgs e, List<Landmarks> liste_landmarks_PoinD, int id, double freqEchOdometry); 


    }
}