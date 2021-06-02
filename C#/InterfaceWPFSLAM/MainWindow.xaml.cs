using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utilities;

namespace InterfaceWPFSLAM
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 
     

    public partial class MainWindow : Window
    {
        public int Time { get; }
        public int NextTime { get; }

        public 
        Location PosRobotQuandTuVeux(double date, Location PosRobot)
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

            else if (date <= 7)
            {
                date = date - 4.5;
                if (date < 0.5) //accélération 
                {
                    PosRobot.Y = 0.5 * date * date - 0.5;
                    PosRobot.Vy = date;
                }
                else if (date < 2) //v = cst
                {
                    PosRobot.Y = 0.5 * date + 0.125 - 0.5;
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

            else if (date <= 14)
            {
                date -= 7;
                if (date < 1) //accélération
                {
                    PosRobot.Theta = date * date * Math.PI / 12;
                    PosRobot.Vtheta = date * Math.PI / 6;
                }
                else if (date < 6) //v=cst
                {
                    PosRobot.Theta = date * Math.PI / 6 + Math.PI / 12;
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
        } // a faire : demander pourquoi on ne doit pas mettre de private ici

        private void DrawPose(Location pose, Color color)              //affichage d'une position robot 
        {
            Ellipse circle = new Ellipse()
            {
                Fill = new SolidColorBrush(color),
                Width = 10,
                Height = 10
            };

            Canvas.SetLeft(circle, (pose.X + 1.5) * 200);
            Canvas.SetTop(circle, (-pose.Y + 1) * 200);

            Line line = new Line()
            {
                Stroke = new SolidColorBrush(color),       // stick de position angulaire
                StrokeThickness = 1, //épaisseur
                X1 = (pose.X + 1.5) * 200 + 5,
                Y1 = (-pose.Y + 1) * 200 + 5,
                X2 = (pose.X + 1.5) * 200 + 5 + 5 * 3 * Math.Cos(-pose.Theta),  // longueur
                Y2 = (-pose.Y + 1) * 200 + 5 + 5 * 3 * Math.Sin(-pose.Theta),
            };

            //DrawArea.Children.Add(line);
            //DrawArea.Children.Add(circle);
        }

        private void DrawLandmark(List<double> pose)              //affichage de ld
        {
            Ellipse circle = new Ellipse()
            {
                Fill = new SolidColorBrush(Colors.Cyan),
                Width = 10,
                Height = 10
            };

            Canvas.SetLeft(circle, (pose[0] + 1.5) * 200);
            Canvas.SetTop(circle, (-pose[1] + 1) * 200);

            //DrawArea.Children.Add(circle);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }
        public MainWindow()
        {
            InitializeComponent();

            bool isRunning = true;
            double date = 0; 

            List<List<double>> List_landmarks = new List<List<double>> { };
            
            List<double> Testld = new List<double> {0,0};  
            List_landmarks.Add(Testld);
            Testld = new List<double> { 1.5, 1 };
            List_landmarks.Add(Testld);
            Testld = new List<double> { 1.5, 0 };
            List_landmarks.Add(Testld);
            Testld = new List<double> { 1.5, -1 };
            List_landmarks.Add(Testld);
            Testld = new List<double> { 0, -1 };
            List_landmarks.Add(Testld);
            Testld = new List<double> { -1.5, -1 };
            List_landmarks.Add(Testld);
            Testld = new List<double> { -1.5, 0 };
            List_landmarks.Add(Testld);
            Testld = new List<double> { -1.5, 1 };  //A FAIRE : remplacer par ld_vu
            List_landmarks.Add(Testld);
            Testld = new List<double> { 0, 1 };
            List_landmarks.Add(Testld);


            foreach (List<double> ld in List_landmarks)
            {
                DrawLandmark(ld);
            }

            Location PosRobot = new Location(-1, -0.5, 0, 0, 0, 0);

            while (isRunning)
            {

                PosRobot = PosRobotQuandTuVeux(date, PosRobot);
                DrawPose(PosRobot, Colors.Red);
                date += 0.05;
                Thread.Sleep(50);
            }
        }


    }


    
}
