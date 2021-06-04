using Constants;
using EKF; 
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
using System.Web.UI.MobileControls;

namespace WpfSlamInterface
{
    
    public partial class MainWindow : Window
    {

        DispatcherTimer timer;
        Location PosRobot;
        List<PointDExtended> PosLandmarks; 
        double date;
        double anglePerceptionRobot = Math.PI;
        bool ekfFinished = false;
        static EKFPositionning eKFPositionning;
        static MainWindow slamInterface;
        public void Main(string[] args)
        {
            InitializeComponent();

            slamInterface.OnOdoCalculatedEvent += eKFPositionning.OnOdoReceived;                //On envoie la simu de l'odo à ekf 
            slamInterface.OnLandmarksFoundEvent += eKFPositionning.OnLandmarksReceived;         //On envoie la simu de landmarks à l'ekf 
            eKFPositionning.OnEKFLocationEvent += slamInterface.OnEkfFinished;                  //quand ekf a fini on le balance a interface
            
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
            OnEKFOdo((int)TeamId.Team1 + (int)RobotId.Robot1, PosRobot);
            PosLandmarks = Landmarks_vus(PosRobot, anglePerceptionRobot);
            OnLandmarksFound((int)TeamId.Team1 + (int)RobotId.Robot1, PosLandmarks);

            while (!ekfFinished)
            {

            }

            ekfFinished = false;
            My_local_map.UpdateLocalWorldMap(new LocalWorldMap() { RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1,
                robotLocation = PosRobot,
                lidarMap = PosLandmarks,
            }); 

            date += 0.05;
        }


        public event EventHandler<LocationArgs> OnOdoCalculatedEvent;
        public virtual void OnEKFOdo(int id, Location locationRefTerrain)
        {
            var handler = OnOdoCalculatedEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = PosRobot });
            }
        }


        public event EventHandler<PointDExtendedListArgs> OnLandmarksFoundEvent;
        public virtual void OnLandmarksFound(int id, List<PointDExtended> PosLandmarks)
        {
            var handler = OnLandmarksFoundEvent;
            if (handler != null)
            {
                handler(this, new PointDExtendedListArgs { RobotId = id, LandmarkList= PosLandmarks });
            }
        }

        public void OnEkfFinished(object sender, PosRobotAndLandmarksArgs e)
        {
            PosRobot = e.PosRobot;
            PosLandmarks = e.PosLandmarkList;
            ekfFinished = true; 
        }

        static void Ma_fonction()
        {
            //doit actualiser PosRobot et PosLandmarks
        }

        public List<PointDExtended> Landmarks_vus(Location PosRobot, double anglePerceptionRobot)
        {
            List<List<double>> liste_total_landmarks = fabrication_landmarks();
            List<PointDExtended> MaListe = new List<PointDExtended> { };
            PointD pt = new PointD(0, 0);
            PointDExtended Obstacle = new PointDExtended(pt, System.Drawing.Color.Aqua, 5);

            foreach (List<double> ld in liste_total_landmarks)
            {

                double alpha = (Math.Atan2((ld[1] - PosRobot.Y), (ld[0] - PosRobot.X)));

                if (PosRobot.Theta <= -Math.PI)
                    PosRobot.Theta += 2 * Math.PI;
                if (PosRobot.Theta > Math.PI)
                    PosRobot.Theta -= 2 * Math.PI;

                double dif = Math.Abs(PosRobot.Theta - alpha);

                if (dif > Math.PI)
                {
                    dif = 2 * Math.PI - dif;
                }
                if (dif < anglePerceptionRobot / 2)
                {
                    pt = new PointD(0, 0);
                    pt.X = ld[0];
                    pt.Y = ld[1];
                    Obstacle = new PointDExtended(pt, System.Drawing.Color.Aqua, 5);
                    MaListe.Add(Obstacle);
                }
            }

            MaListe = Bruitage_Landmarks(MaListe);

            return MaListe;
        }
        static public Location PosRobotQuandTuVeux(double date, Location PosRobot)
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
                    PosRobot.Vx = -date + 0.5;
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
                    PosRobot.Vy = -date + 0.5;
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
                    PosRobot.Vx = -date + 0.5;
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
                    PosRobot.Theta = -Math.PI / 2;
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

            else if (date > 28)
                PosRobot.Theta -= Math.PI / 50;  // FIN

            PosRobot = Bruitage_position(PosRobot);
            return PosRobot;
        }


        public List<List<double>> fabrication_landmarks()
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
        static public Location Bruitage_position(Location PosPropre)
        {
            Location PosSale = new Location(0, 0, 0, 0, 0, 0);

            Random rd = new Random();

            double eps = rd.NextDouble();
            double signe = rd.Next(-1, 2);
            PosSale.X = PosPropre.X + eps * signe / 100; //on rajoute une erreur de entre 0 et 1 cm en positif ou négatif 
            eps = rd.NextDouble();
            signe = rd.Next(-1, 2);
            PosSale.Y = PosPropre.Y + eps * signe / 100;

            eps = rd.NextDouble();
            signe = rd.Next(-1, 2);
            PosSale.Theta = PosPropre.Theta + eps * signe * 0.00024434; // ici on rajoute une erreur positive ou négative inférieure à 0.014 deg 

            eps = rd.NextDouble() / 100;
            signe = rd.Next(-1, 2);
            PosSale.Vx = PosPropre.Vx + eps * signe; //erreur de 1cm/s 

            eps = rd.NextDouble() / 100;
            signe = rd.Next(-1, 2);
            PosSale.Vy = PosPropre.Vy + eps * signe; //erreur de 1cm/s 

            eps = rd.NextDouble() * 2 * Math.PI / 360;
            signe = rd.Next(-1, 2);
            PosSale.Vtheta = PosPropre.Vtheta + eps * signe; //erreur de 1deg/s

            return PosSale;
        }
        public List<PointDExtended> Bruitage_Landmarks(List<PointDExtended> ListPropre)
        {
            List<PointDExtended> ListSale = new List<PointDExtended>();

            Random rd = new Random();

            foreach (PointDExtended ld in ListPropre)
            {
                double eps = rd.NextDouble() / 100;
                double signe = rd.Next(-1, 2);

                double eps2 = rd.NextDouble() / 100;
                double signe2 = rd.Next(-1, 2);

                PointD a = new PointD(ld.Pt.X + eps * signe, ld.Pt.Y + eps2 * signe2);
                PointDExtended Pt_sale = new PointDExtended(a, ld.Color, ld.Width);

                ListSale.Add(Pt_sale);
            }

            return ListSale;
        }





    }
}
