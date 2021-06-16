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
        //Paramètres
        bool tout_les_ld = false;
        bool usingEkf = true;

        static bool bruitage_odo = false;
        bool bruitage_ld = false;
        
        static bool translation_circulaire = true;             //A FAIRE : rajouter une dérive a la simu 
        static bool dont_moove = false; 

        double anglePerceptionRobot = Math.PI;
        private double tEch = 0.02;       // fEch = 50 dans ekf_positionning 
         

        DispatcherTimer timer;
        Location PosRobot = new Location(-1,-0.5,0,0,0,0);
        static Location PosRobotInconnue = new Location(0,0,0,0,0,0);
        List<PointDExtended> PosLandmarks;
        double date;
        
        static EKFPositionning eKFPositionning;
        public MainWindow()
        {
            InitializeComponent();

            LocationArgs a = new LocationArgs();
            a.RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1;
            a.Location = PosRobot;

            eKFPositionning = new EKFPositionning(a);
            Simulateur_ekf simulateur_ = new Simulateur_ekf();

            date = 0;

            My_local_map.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, GameMode.RoboCup, "Wally");
            My_local_map.UpdateLocalWorldMap(new LocalWorldMap() { RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1, 
                robotLocation = PosRobot,
                lidarMap = PosLandmarks
            });

            LocalMapLdSeen.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, GameMode.RoboCup, "Wally");
            LocalMapLdSeen.UpdateLocalWorldMap(new LocalWorldMap()
            {
                RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1,
                robotLocation = new Location(0, 0, 0, 0, 0, 0),
            });

            OnOdoCalculatedEvent += eKFPositionning.OnOdoReceived;                //On envoie la simu de l'odo à ekf 
            OnLandmarksFoundEvent += eKFPositionning.OnLandmarksReceived;         //On envoie la simu de landmarks à l'ekf 
            eKFPositionning.OnEKFLocationEvent += OnEkfFinished;                  //quand ekf a fini on le balance a interface

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += UpdateGUITemp;
            timer.Start();

        }   

        public void UpdateGUITemp(object sender, EventArgs e)
        {

            PosRobot = PosRobotQuandTuVeux(date, PosRobot);                             
            
            PosLandmarks = Landmarks_vus(PosRobotInconnue, anglePerceptionRobot);

            //List<PointDExtended>  Pos2Landmarks = new List<PointDExtended> (PassageRefRobot(PosLandmarks, new Location(0, 0, 0, 0, 0, 0)));

            LocalMapLdSeen.UpdateLocalWorldMap(new LocalWorldMap()
            {
                RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1,
                robotLocation = new Location(0,0,0,0,0,0),
                lidarMap= PassageRefTerrain(PosLandmarks, new Location(0, 0, 0, 0, 0, 0)),
            });

            if (usingEkf)
            {
                OnEKFOdo((int)TeamId.Team1 + (int)RobotId.Robot1, PosRobot);
                OnLandmarksFound((int)TeamId.Team1 + (int)RobotId.Robot1, PosLandmarks);
            }
            else
            {
                PosRobot.X += PosRobot.Vx * tEch * Math.Cos(PosRobot.Theta) - PosRobot.Vy * tEch * Math.Sin(PosRobot.Theta);
                PosRobot.Y += PosRobot.Vx * tEch * Math.Sin(PosRobot.Theta) + PosRobot.Vy * tEch * Math.Cos(PosRobot.Theta);
                PosRobot.Theta += PosRobot.Vtheta * tEch;
                PosLandmarks = PassageRefTerrain(PosLandmarks, PosRobot);
            }  // test de la simu 
            

            My_local_map.UpdateLocalWorldMap(new LocalWorldMap()
            {
                RobotId = (int)TeamId.Team1 + (int)RobotId.Robot1,
                robotLocation = PosRobot,
                lidarMap = PosLandmarks,
            });



            date += tEch;
        }

        #region events

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
        }


        #endregion events

        #region simu 
        static public Location PosRobotQuandTuVeux(double date, Location PosRobot)
        {
            #region pos=f(t)
            // 3modes : fixe, translation circulaire, parcours complet
            
            if (dont_moove)
            {
                PosRobot = new Location(-1, -0.5, 0, 0, 0, 0);
                PosRobotInconnue = PosRobot; 
            }
            else if (!translation_circulaire)
            {
                if (date <= 4.5)
                {
                    if (date < 0.5) //accélération 
                    {
                        PosRobotInconnue.X = 0.5 * date * date - 1;
                        PosRobot.Vx = date;
                        PosRobotInconnue.Y = -0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date < 4) // v =cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.X = 0.5 * date + 0.125 - 1;
                        PosRobot.Vx = 0.5;
                        PosRobotInconnue.Y = -0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 4;
                        PosRobotInconnue.X = -0.5 * date * date + 0.5 * date + 1.875 - 1;
                        PosRobot.Vx = -date + 0.5;
                        PosRobotInconnue.Y = -0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                } //1er trajet

                else if (date <= 7)
                {

                    date = date - 4.5;
                    if (date <= 0.5) //accélération 
                    {
                        PosRobotInconnue.Y = 0.5 * date * date - 0.5;
                        PosRobot.Vy = date;
                        PosRobotInconnue.X = 1;
                        PosRobot.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date <= 2) //v = cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.Y = 0.5 * date + 0.125 - 0.5;
                        PosRobot.Vy = 0.5;
                        PosRobot.X = 1;
                        PosRobotInconnue.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 2;
                        PosRobotInconnue.Y = -0.5 * date * date + 0.5 * date + 0.875 - 0.5;
                        PosRobot.Vy = -date + 0.5;
                        PosRobotInconnue.X = 1;
                        PosRobot.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                } // 2nd trajet

                else if (date <= 14)
                {
                    date -= 7;
                    if (date < 1) //accélération
                    {
                        PosRobotInconnue.Theta = date * date * Math.PI / 12;
                        PosRobot.Vtheta = date * Math.PI / 6;
                        PosRobotInconnue.X = 1;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vx = PosRobot.Vy = 0;
                    }
                    else if (date <= 6) //v=cst
                    {
                        date -= 1;
                        PosRobotInconnue.Theta = date * Math.PI / 6 + Math.PI / 12;
                        PosRobot.Vtheta = Math.PI / 6;
                        PosRobotInconnue.X = 1;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vx = PosRobot.Vy = 0;
                    }
                    else
                    {
                        date -= 6;
                        PosRobotInconnue.Theta = -date * date * Math.PI / 12 + date * Math.PI / 6 + 11 * Math.PI / 12;
                        PosRobot.Vtheta = -date * Math.PI / 6 + Math.PI / 6;
                        PosRobotInconnue.X = 1;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vx = PosRobot.Vy = 0;
                    }
                } // 3eme trajet

                else if (date <= 18.5)
                {
                    date -= 14;
                    if (date < 0.5) //accélération 
                    {
                        PosRobotInconnue.X = -0.5 * date * date + 1;
                        PosRobot.Vx = date;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = Math.PI;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date < 4) // v =cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.X = -0.5 * date - 0.125 + 1;
                        PosRobot.Vx = 0.5;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = Math.PI;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 4;
                        PosRobotInconnue.X = 0.5 * date * date - 0.5 * date - 1.875 + 1;
                        PosRobot.Vx = -date + 0.5;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = Math.PI;
                        PosRobot.Vtheta = 0;
                    }
                } //4eme trajet

                else if (date <= 22)
                {
                    date -= 18.5;
                    if (date < 1) //accélération
                    {
                        PosRobotInconnue.Theta = date * date * Math.PI / 12 + Math.PI;
                        PosRobot.Vtheta = date * Math.PI / 6;
                        PosRobotInconnue.X = -1;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vx = PosRobot.Vy = 0;
                    }
                    else if (date <= 6) //v=cst
                    {
                        date -= 1;
                        PosRobotInconnue.Theta = date * Math.PI / 6 + 13 * Math.PI / 12;
                        PosRobot.Vtheta = Math.PI / 6;
                        PosRobotInconnue.X = -1;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vx = PosRobot.Vy = 0;
                    }
                    else
                    {
                        date -= 6;
                        PosRobotInconnue.Theta = -date * date * Math.PI / 12 + date * Math.PI / 6 + 11 * Math.PI / 12;
                        PosRobot.Vtheta = -date * Math.PI / 6 + Math.PI / 6;
                        PosRobotInconnue.X = -1;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vx = PosRobot.Vy = 0;
                    }
                } // 5eme trajet

                else if (date <= 24.5)
                {
                    date = date - 22;
                    if (date <= 0.5) //accélération 
                    {
                        PosRobotInconnue.Y = -0.5 * date * date + 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.X = -1;
                        PosRobot.Vx = date;
                        PosRobotInconnue.Theta = -Math.PI / 2;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date <= 2) //v = cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.Y = -0.5 * date - 0.125 + 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.X = -1;
                        PosRobot.Vx = 0.5;
                        PosRobotInconnue.Theta = -Math.PI / 2;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 2;
                        PosRobotInconnue.Y = 0.5 * date * date - 0.5 * date - 0.875 + 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.X = -1;
                        PosRobot.Vx = -date + 0.5;
                        PosRobotInconnue.Theta = -Math.PI / 2;
                        PosRobot.Vtheta = 0;
                    }
                } // 6nd trajet

                else if (date > 28)
                {
                    PosRobot.Vx = PosRobot.Vy = 0;
                    PosRobotInconnue.Theta -= Math.PI / 100;
                    PosRobot.Vtheta = -Math.PI / 2;
                } // FIN
            }
            else
            {
                if (date <= 4.5)
                {
                    if (date < 0.5) //accélération 
                    {
                        PosRobotInconnue.X = 0.5 * date * date - 1;
                        PosRobot.Vx = date;
                        PosRobotInconnue.Y = -0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date < 4) // v =cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.X = 0.5 * date + 0.125 - 1;
                        PosRobot.Vx = 0.5;
                        PosRobotInconnue.Y = -0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 4;
                        PosRobotInconnue.X = -0.5 * date * date + 0.5 * date + 1.875 - 1;
                        PosRobot.Vx = -date + 0.5;
                        PosRobotInconnue.Y = -0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                } //1er trajet

                else if (date <= 7)
                {

                    date = date - 4.5;
                    if (date <= 0.5) //accélération 
                    {
                        PosRobotInconnue.Y = 0.5 * date * date - 0.5;
                        PosRobot.Vy = date;
                        PosRobotInconnue.X = 1;
                        PosRobot.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date <= 2) //v = cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.Y = 0.5 * date + 0.125 - 0.5;
                        PosRobot.Vy = 0.5;
                        PosRobot.X = 1;
                        PosRobotInconnue.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 2;
                        PosRobotInconnue.Y = -0.5 * date * date + 0.5 * date + 0.875 - 0.5;
                        PosRobot.Vy = -date + 0.5;
                        PosRobotInconnue.X = 1;
                        PosRobot.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                } // 2nd trajet

                else if (date <= 14)
                {
                    
                } // 3eme trajet

                else if (date <= 18.5)
                {
                    date -= 14;
                    if (date < 0.5) //accélération 
                    {
                        PosRobotInconnue.X = -0.5 * date * date + 1;
                        PosRobot.Vx = -date;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date < 4) // v =cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.X = -0.5 * date - 0.125 + 1;
                        PosRobot.Vx = -0.5;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 4;
                        PosRobotInconnue.X = 0.5 * date * date - 0.5 * date - 1.875 + 1;
                        PosRobot.Vx = date - 0.5;
                        PosRobotInconnue.Y = 0.5;
                        PosRobot.Vy = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                } //4eme trajet

                else if (date <= 22)
                {
                    
                } // 5eme trajet

                else if (date <= 24.5)
                {
                    date = date - 22;
                    if (date <= 0.5) //accélération 
                    {
                        PosRobotInconnue.Y = -0.5 * date * date + 0.5;
                        PosRobot.Vy = -date;
                        PosRobotInconnue.X = -1;
                        PosRobot.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else if (date <= 2) //v = cst
                    {
                        date -= 0.5;
                        PosRobotInconnue.Y = -0.5 * date - 0.125 + 0.5;
                        PosRobot.Vy = -0.5;
                        PosRobotInconnue.X = -1;
                        PosRobot.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                    else //descélération
                    {
                        date -= 2;
                        PosRobotInconnue.Y = 0.5 * date * date - 0.5 * date - 0.875 + 0.5;
                        PosRobot.Vy = -(-date + 0.5);
                        PosRobotInconnue.X = -1;
                        PosRobot.Vx = 0;
                        PosRobotInconnue.Theta = 0;
                        PosRobot.Vtheta = 0;
                    }
                } // 6nd trajet

            }
            #endregion

            if (bruitage_odo)
                PosRobot = Bruitage_position(PosRobot);

            PosRobotInconnue.Vx = PosRobot.Vx;
            PosRobotInconnue.Vy = PosRobot.Vy;
            PosRobotInconnue.Vtheta = PosRobot.Vtheta;

            return PosRobot;
        }
        public List<PointDExtended> Landmarks_vus(Location PosRobot, double anglePerceptionRobot)
        {
            List<List<double>> liste_total_landmarks = fabrication_landmarks();
            List<PointDExtended> MaListe = new List<PointDExtended> { };
            PointDExtended Obstacle ;

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
                    PointD pt = new PointD(0, 0);
                    pt.X = ld[0];
                    pt.Y = ld[1];
                    Obstacle = new PointDExtended(pt, System.Drawing.Color.Aqua, 5);
                    MaListe.Add(Obstacle);
                }
            }

            if (bruitage_ld)  
                MaListe = Bruitage_Landmarks(MaListe);

            
            MaListe = PassageRefRobot(MaListe, PosRobot);   // A partir de là on a une liste de x et theta dans le ref robot 

            return MaListe;
        }
        static List<PointDExtended> PassageRefRobot(List<PointDExtended> ListeLdRefTerrain, Location PosRobot)
        {
            List<PointDExtended> ListeLdRefRobot = new List<PointDExtended> { };

            foreach (PointDExtended Ld in ListeLdRefTerrain)
            {
                PointD newPtd = new PointD(0,0);
                newPtd.X = Math.Sqrt((Ld.Pt.X - PosRobot.X) * (Ld.Pt.X - PosRobot.X) + (Ld.Pt.Y - PosRobot.Y) * (Ld.Pt.Y - PosRobot.Y));
                newPtd.Y = Math.Atan2((Ld.Pt.Y - PosRobot.Y), (Ld.Pt.X - PosRobot.X)) - PosRobot.Theta;

                ListeLdRefRobot.Add(new PointDExtended(newPtd, System.Drawing.Color.Aqua, 5));
            }

            return ListeLdRefRobot;
        }
        static List<PointDExtended> PassageRefTerrain(List<PointDExtended> ListeLdRefRobot, Location PosRobot)
        {
            List<PointDExtended> ListeLdRefTerrain = new List<PointDExtended> { };

            foreach (PointDExtended Ld in ListeLdRefRobot)
            {
                PointD newPtd = new PointD(0, 0);
                newPtd.X = PosRobot.X + Ld.Pt.X * Math.Cos(PosRobot.Theta + Ld.Pt.Y);
                newPtd.Y = PosRobot.Y + Ld.Pt.X * Math.Sin(PosRobot.Theta + Ld.Pt.Y);
                ListeLdRefTerrain.Add(new PointDExtended(newPtd, System.Drawing.Color.Aqua, 5));
            }
            return ListeLdRefTerrain;
        }
        public List<List<double>> fabrication_landmarks()
        {
            List<List<double>> liste_total_landmarks = new List<List<double>> { };

            List<double> ld = new List<double> { -1.5, -1 };
            liste_total_landmarks.Add(ld);

            ld = new List<double> { -1.5, 1 };                  // on crée des ld artificiels là ou on veut 
            liste_total_landmarks.Add(ld);

            ld = new List<double> { 1.5, -1 };
            liste_total_landmarks.Add(ld);

            ld = new List<double> { 1.5, 1 };
            liste_total_landmarks.Add(ld);

            if (tout_les_ld)
            {
                ld = new List<double> { 0, -1 };
                liste_total_landmarks.Add(ld);

                ld = new List<double> { 0, 1 };
                liste_total_landmarks.Add(ld);

                ld = new List<double> { 1.5, 0 };
                liste_total_landmarks.Add(ld);                                    

                ld = new List<double> { -1.5, 0 };
                liste_total_landmarks.Add(ld);
            }


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

        #endregion

    }
}
