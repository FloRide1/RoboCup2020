using Constants;
using EventArgsLibrary;
using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Core.Utility.Mouse;
using SciChart.Drawing.VisualXcceleratorRasterizer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Utilities;
using SciChart.Charting.Visuals;
using WorldMap;
using System.Windows.Threading;

namespace WpfSlamInterface
{
    
    public partial class MainWindow : Window
    {

        DispatcherTimer timer;
        Location PosRobot;
        List<PointDExtended> PosLandmarks; 
        double date; 

        public MainWindow()
        {
            SciChartSurface.SetRuntimeLicenseKey("rviDeI6IkFYHxvazPlQ1Pg9nf76u8PuGK7wRqQ7R9khsGosRUfWiHG6ecygizFR+4c7wUOamApjxFMOAJv2kFhoF82yW1/KvejxkGTqWwFJ5FRYet4s7QSTUnPWuMCSkYUtvd9MBBox4tqaHYq2d9TW8wU0B6wWbgJ7T+XGIO5aW74SZKy+69WnCAbQq/yjjqpaxgSKBNZrApbrkuJupiE69geraLLhedlptkG2Jlvz5EpwJ2O5tOg9IXBP5A7P8YGQmQes9RhuXZuAA4htm4+cshLT7zfegpYm7L5I1zTJOwZNf6wXSFJ/Oa2VgC5It7LSvkuQqDbGhr7IHLPcRrhmplD0bvdM3DgRySzLva+y8ut+8ilI7vFwgRc+3HnEZQVTo92L5LnBkOSHX6IuTS4lw8NLb97WE2aQJnXwR9Apgg8aPxdy8cVmVoCTq35Z1HazmDgeVbp855bZekTNlS+htS/G/DPXNAvnCZlrG063NqDCHRZtRc+xr7RWuicGjPg==");

            InitializeComponent();

            PosRobot = new Location (-1, -0.5, 0, 0, 0, 0);
            date = 0;

            My_local_map.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, GameMode.RoboCup, "Wally");
            My_local_map.UpdateLocalWorldMap(new LocalWorldMap() { RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1, 
                robotLocation = PosRobot,
                lidarMap = PosLandmarks
            });
            



            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += UpdateGUITemp;
            timer.Start();

        }

        public void UpdateGUITemp(object sender, EventArgs e)
        {
            PosRobot = PosRobotQuandTuVeux(date, PosRobot);
            PosLandmarks = Landmarks_vus(PosRobot, fabrication_landmarks(), Math.PI);


            My_local_map.UpdateLocalWorldMap(new LocalWorldMap() { RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1,
                robotLocation = PosRobot,
                lidarMap = PosLandmarks,
            });

            date += 0.05;
        }
         
        public List<PointDExtended> Landmarks_vus(Location PosRobot, List<List<double>> liste_total_landmarks, double anglePerceptionRobot)
        {
            List<PointDExtended> MaListe = new List<PointDExtended> { };
            PointD pt =new PointD (0,0); 
            PointDExtended Obstacle = new PointDExtended(pt, System.Drawing.Color.Aqua, 5); 
            
            foreach (List<double> ld in liste_total_landmarks)
            {

                double alpha = (Math.Atan2((ld[1] - PosRobot.Y), (ld[0] - PosRobot.X))) ; 

                if (PosRobot.Theta <= -Math.PI)
                    PosRobot.Theta += 2 * Math.PI;
                if (PosRobot.Theta > Math.PI)
                    PosRobot.Theta -= 2 * Math.PI;

                double dif = Math.Abs(PosRobot.Theta - alpha);

                if (dif>Math.PI)
                {
                    dif = 2 * Math.PI - dif; 
                }
                if (dif < anglePerceptionRobot /2)
                {
                    pt = new PointD(0, 0);
                    pt.X = ld[0];
                    pt.Y = ld[1];
                    Obstacle = new PointDExtended(pt, System.Drawing.Color.Aqua, 5);
                    MaListe.Add(Obstacle);
                }
            }
            return MaListe;
        }

        public Location PosRobotQuandTuVeux(double date, Location PosRobot)
        {
            if (date <= 4)
            {
                if (date < 0.5) //accélération 
                {
                    PosRobot.X = 0.5 * date * date - 1;
                    PosRobot.Vx = date;
                    PosRobot.Y = -0.5;
                    PosRobot.Vy = 0;
                    PosRobot.Theta = 0;
                }
                else if (date < 3.5) // v =cst
                {
                    date -= 0.5;
                    PosRobot.X = 0.5 * date + 0.125 - 1;
                    PosRobot.Vx = 0.5;
                    PosRobot.Y = -0.5;
                    PosRobot.Vy = 0;
                    PosRobot.Theta = 0;
                }
                else //descélération
                {
                    date -= 3.5;
                    PosRobot.X = -0.5 * date * date + 0.5 * date + 1.875 - 1;
                    PosRobot.Vx = - date + 0.5;
                    PosRobot.Y = -0.5;
                    PosRobot.Vy = 0;
                    PosRobot.Theta = 0;
                }
            } //1er trajet, REMARQUE : j'ai mis -1 dans tout les x car on part de (-1, -0.5)

            else if (date <= 6.5)
            {
                date = date - 4;
                if (date <= 0.5) //accélération 
                {
                    PosRobot.Y = 0.5 * date * date - 0.5;
                    PosRobot.Vy = date;
                    PosRobot.X = 1;
                    PosRobot.Vx = 0;
                    PosRobot.Theta = 0;
                }
                else if (date <= 2) //v = cst
                {
                    date -= 0.5;
                    PosRobot.Y = 0.5 * date + 0.125 - 0.5;
                    PosRobot.Vy = 0.5;
                    PosRobot.X = 1;
                    PosRobot.Vx = 0;
                    PosRobot.Theta = 0;
                }
                else //descélération
                {
                    date -= 2;
                    PosRobot.Y = -0.5 * date * date + 0.5 * date + 0.875 - 0.5;
                    PosRobot.Vy = - date + 0.5;
                    PosRobot.X = 1;
                    PosRobot.Vx = 0;
                    PosRobot.Theta = 0;
                }
            } // 2nd trajet : REMARQUE : j'ai mis -0.5 dans tout les y car on part de (1, -0.5)

            else if (date <= 13.5)
            {
                date -= 6.5;
                if (date < 1) //accélération
                {
                    PosRobot.Theta = date * date * Math.PI / 12;
                    PosRobot.Vtheta = date * Math.PI / 6;
                    PosRobot.X = 1;
                    PosRobot.Y = 0.5;
                    PosRobot.Vx = PosRobot.Vy = 0;
                }
                else if (date <= 6) //v=cst
                {
                    date -= 1; 
                    PosRobot.Theta = date * Math.PI / 6 + Math.PI / 12;
                    PosRobot.Vtheta = Math.PI / 6;
                    PosRobot.X = 1;
                    PosRobot.Y = 0.5;
                    PosRobot.Vx = PosRobot.Vy = 0;
                }
                else
                {
                    date -= 6;
                    PosRobot.Theta = -date * date * Math.PI / 12 + date * Math.PI / 6 + 11 * Math.PI / 12;
                    PosRobot.Vtheta = -date * Math.PI / 6 + Math.PI / 6;
                    PosRobot.X = 1;
                    PosRobot.Y = 0.5;
                    PosRobot.Vx = PosRobot.Vy = 0;
                }
            } // 3eme trajet

            else if (date <= 17.5)
            {
                date -= 13.5;
                if (date < 0.5) //accélération 
                {
                    PosRobot.X = -0.5 * date * date + 1;
                    PosRobot.Vx = date;
                    PosRobot.Y = 0.5;
                    PosRobot.Vy = 0;
                    PosRobot.Theta = Math.PI;
                }
                else if (date < 3.5) // v =cst
                {
                    date -= 0.5;
                    PosRobot.X = -0.5 * date - 0.125 + 1;
                    PosRobot.Vx = 0.5;
                    PosRobot.Y = 0.5;
                    PosRobot.Vy = 0;
                    PosRobot.Theta = Math.PI;
                }
                else //descélération
                {
                    date -= 3.5;
                    PosRobot.X = 0.5 * date * date - 0.5 * date - 1.875 + 1;
                    PosRobot.Vx = - date + 0.5;
                    PosRobot.Y = 0.5;
                    PosRobot.Vy = 0;
                    PosRobot.Theta = Math.PI;
                }
            } //4eme trajet

            else if (date <= 21)
            {
                date -= 17.5;
                if (date < 1) //accélération
                {
                    PosRobot.Theta = date * date * Math.PI / 12 + Math.PI;
                    PosRobot.Vtheta = date * Math.PI / 6;
                    PosRobot.X = -1;
                    PosRobot.Y = 0.5;
                    PosRobot.Vx = PosRobot.Vy = 0;
                }
                else if (date <= 6) //v=cst
                {
                    date -= 1;
                    PosRobot.Theta = date * Math.PI / 6 + 13 * Math.PI / 12;
                    PosRobot.Vtheta = Math.PI / 6;
                    PosRobot.X = -1;
                    PosRobot.Y = 0.5;
                    PosRobot.Vx = PosRobot.Vy = 0;
                }
                else
                {
                    date -= 6;
                    PosRobot.Theta = -date * date * Math.PI / 12 + date * Math.PI / 6 + 11 * Math.PI / 12;
                    PosRobot.Vtheta = -date * Math.PI / 6 + Math.PI / 6;
                    PosRobot.X = -1;
                    PosRobot.Y = 0.5;
                    PosRobot.Vx = PosRobot.Vy = 0;
                }
            } // 5eme trajet

            else if (date <= 23.5)
            {
                date = date - 21;
                if (date <= 0.5) //accélération 
                {
                    PosRobot.Y = -0.5 * date * date + 0.5;
                    PosRobot.Vy = 0;
                    PosRobot.X = -1;
                    PosRobot.Vx = date;
                    PosRobot.Theta = -Math.PI/2;
                }
                else if (date <= 2) //v = cst
                {
                    date -= 0.5;
                    PosRobot.Y = -0.5 * date - 0.125 + 0.5;
                    PosRobot.Vy = 0;
                    PosRobot.X = -1;
                    PosRobot.Vx = 0.5;
                    PosRobot.Theta = -Math.PI / 2;
                }
                else //descélération
                {
                    date -= 2;
                    PosRobot.Y = 0.5 * date * date - 0.5 * date - 0.875 + 0.5;
                    PosRobot.Vy = 0;
                    PosRobot.X = -1;
                    PosRobot.Vx = -date + 0.5;
                    PosRobot.Theta = -Math.PI / 2;
                }
            } // 6nd trajet

            else if (date >28)
                PosRobot.Theta -= Math.PI/50 ;  // FIN

            return PosRobot;
        }
        
        //C'est le plus beau jour de ma vie 

        public List<List<double>> fabrication_landmarks()
        {
            List<List<double>> liste_total_landmarks = new List<List<double>> { };

            List<double>  ld = new List<double> { -1.5, -1 };
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

        //private void OnDataToPrintCalculated(object sender, RoutedEventArgs e)
        //{

        //    //guetho mais après deux heures a chercher comment faire j'ai pas pu faire mieux 
        //    var Pointnuls = new XyDataSeries<double>();
        //    Pointnuls.Append(-2, -1.5);
        //    Pointnuls.Append( 2,  1.5);           
        //    limfenêtre.DataSeries = Pointnuls;

        //    //Affichage du robot
        //    PosRobot = PosRobotQuandTuVeux(date, PosRobot);    //Update posRobot 
        //    var ScatterData = new XyDataSeries<double>();
        //    var lineData = new XyDataSeries<double>();
        //    ScatterData.Append(PosRobot.X, PosRobot.Y); //Point 
        //    lineData.Append(PosRobot.X, PosRobot.Y);    //Stick angulaire
        //    lineData.Append(PosRobot.X + 0.1 * Math.Cos(PosRobot.Theta) , PosRobot.Y + 0.1 * Math.Sin(PosRobot.Theta));
        //    // assigne les pos robot a ce qui s'affiche après 
        //    StickRobot.DataSeries = lineData;
        //    PointRobot.DataSeries = ScatterData;


        //    //Affichage landmark

        //    var PointLd = new XyDataSeries<double>();
        //    List<List<double>> Ma_Liste = fabrication_landmarks(); // WTF ne fonctionne que si la liste est triée 
        //    Ma_Liste = Landmarks_vus(PosRobot,Ma_Liste, Math.PI );


        //    foreach (List<double> ld in Ma_Liste)
        //    {
        //        PointLd.Append(ld[0], ld[1]);
        //    }
        //    AffichageLandmark.DataSeries = PointLd;
        //}

    }
}
