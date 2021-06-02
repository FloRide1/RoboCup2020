using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfSimplifiedWorldMapDisplayNS;

namespace WPFTestSimplifiedWorldMapDisplayNS
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Timer timerTestPosition = new Timer();

        public MainWindow()
        {
            InitializeComponent();
            WorldMap.Init(3.4, 2.4, 3, 2, @"C:\\GITHUB\\RoboCup2020\\C#\\Images\\Eurobot2020.png");

            PolygonExtended robotShape = new PolygonExtended();
            robotShape.polygon.Points.Add(new System.Windows.Point(-0.14, -0.18));
            robotShape.polygon.Points.Add(new System.Windows.Point(0.14, -0.18));
            robotShape.polygon.Points.Add(new System.Windows.Point(0.10, 0));
            robotShape.polygon.Points.Add(new System.Windows.Point(0.14, 0.18));
            robotShape.polygon.Points.Add(new System.Windows.Point(-0.14, 0.18));
            robotShape.polygon.Points.Add(new System.Windows.Point(-0.14, -0.18));
            robotShape.borderColor = System.Drawing.Color.Blue;
            robotShape.backgroundColor = System.Drawing.Color.Red;

            PolygonExtended ghostShape = new PolygonExtended();
            ghostShape.polygon.Points.Add(new Point(-0.16, -0.2));
            ghostShape.polygon.Points.Add(new Point(0.16, -0.2));
            ghostShape.polygon.Points.Add(new Point(0.12, 0));
            ghostShape.polygon.Points.Add(new Point(0.16, 0.2));
            ghostShape.polygon.Points.Add(new Point(-0.16, 0.2));
            ghostShape.polygon.Points.Add(new Point(-0.16, -0.2));
            ghostShape.backgroundColor = System.Drawing.Color.FromArgb(20, 0, 255, 0);
            ghostShape.borderColor = System.Drawing.Color.Black;

            WorldMap.SetRobotShape(robotShape);
            WorldMap.SetGhostShape(ghostShape);

            timerTestPosition = new Timer(100);
            timerTestPosition.Elapsed += TimerTestPosition_Elapsed;
            timerTestPosition.Start();
        }

        Random rnd = new Random();

        private void TimerTestPosition_Elapsed(object sender, ElapsedEventArgs e)
        {
            WorldMap.UpdateRobotLocation(new Location(rnd.NextDouble() - 0.5, rnd.NextDouble() - 0.5, rnd.NextDouble() - 0.5, 0, 0, 0));
            WorldMap.UpdateGhostLocation(new Location(rnd.NextDouble() - 0.5, rnd.NextDouble() - 0.5, rnd.NextDouble() - 0.5, 0, 0, 0));
        }
    }
}
