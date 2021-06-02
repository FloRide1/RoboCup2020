using SciChart.Charting.Visuals;
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
using System.Windows.Threading;

namespace WpfSimplifiedWorldMapDisplayNS
{
    /// <summary>
    /// Logique d'interaction pour UserControl1.xaml
    /// </summary>
    public partial class WpfSimplifiedWorldMapDisplay : UserControl
    {
        BindingClass imageBinding = new BindingClass();
        double LengthGameArea = 0;
        double WidthGameArea = 0;
        double LengthDisplayArea = 0;
        double WidthDisplayArea = 0;

        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        //Timer timerAffichage = new Timer();

        Location robotLocation = new Location();
        Location ghostLocation = new Location();

        enum PolygonId
        {
            RobotPolygonId,
            GhostPolygonId,

        }

        public WpfSimplifiedWorldMapDisplay()
        {
            
            // Set this code once in App.xaml.cs or application startup
            SciChartSurface.SetRuntimeLicenseKey("wsCOsvBlAs2dax4o8qBefxMi4Qe5BVWax7TGOMLcwzWFYRNCa/f1rA5VA1ITvLHSULvhDMKVTc+niao6URAUXmGZ9W8jv/4jtziBzFZ6Z15ek6SLU49eIqJxGoQEFWvjANJqzp0asw+zvLV0HMirjannvDRj4i/WoELfYDubEGO1O+oAToiJlgD/e2lVqg3F8JREvC0iqBbNrmfeUCQdhHt6SKS2QpdmOoGbvtCossAezGNxv92oUbog6YIhtpSyGikCEwwKSDrlKlAab6302LLyFsITqogZychLYrVXJTFvFVnDfnkQ9cDi7017vT5flesZwIzeH497lzGp3B8fKWFQyZemD2RzlQkvj5GUWBwxiKAHrYMnQjJ/PsfojF1idPEEconVsh1LoYofNk2v/Up8AzXEAvxWUEcgzANeQggaUNy+OFet8b/yACa/bgYG7QYzFQZzgdng8IK4vCPdtg4/x7g5EdovN2PI9vB76coMuKnNVPnZN60kSjtd/24N8A==");

            InitializeComponent();

            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();

            //timerAffichage.Elapsed += TimerAffichage_Elapsed;
            //timerAffichage.Interval = 100;
            //timerAffichage.Start();
        }


        public void Init(double displayAreaLength, double displayAreaWidth, double gameAreaLength, double gameAreaWidth, string imagePath)
        {
            LengthDisplayArea = displayAreaLength;
            WidthDisplayArea = displayAreaWidth;
            LengthGameArea = gameAreaLength;
            WidthGameArea = gameAreaWidth;
            
            this.sciChartSurface.XAxis.VisibleRange.SetMinMax(-LengthDisplayArea / 2, LengthDisplayArea / 2);
            this.sciChartSurface.YAxis.VisibleRange.SetMinMax(-WidthDisplayArea / 2, WidthDisplayArea / 2);
            SetFieldImageBackGround(imagePath);

        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            //Routine de mise à jour de l'affichage en fonction de ce qui a été reçu
            PolygonSeries.AddOrUpdatePolygonExtended((int)PolygonId.GhostPolygonId, GetRobotGhostPolygon());
            PolygonSeries.AddOrUpdatePolygonExtended((int)PolygonId.RobotPolygonId, GetRobotPolygon());
            PolygonSeries.RedrawAll();

        }

        public void SetRobotShape(PolygonExtended rbtShape)
        {
            robotShape = rbtShape;
        }
        public void SetGhostShape(PolygonExtended rbtShape)
        {
            ghostShape = rbtShape;
        }

        public void UpdateRobotLocation(Location location)
        {
            robotLocation = location;
        }

        public void UpdateGhostLocation(Location location)
        {
            ghostLocation = location;
        }

        /// <summary>
        /// Définit l'image en fond de carte
        /// </summary>
        /// <param name="imagePath">Chemin de l'image</param>
        public void SetFieldImageBackGround(string imagePath)
        {
            imageBinding.ImagePath = imagePath;
            imageBinding.X1 = -LengthGameArea / 2;
            imageBinding.X2 = +LengthGameArea / 2;
            imageBinding.Y1 = -WidthGameArea / 2;
            imageBinding.Y2 = +WidthGameArea / 2;
        }

        private PolygonExtended robotShape;
        private PolygonExtended GetRobotPolygon()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            foreach (var pt in robotShape.polygon.Points)
            {
                Point polyPt = new Point(pt.X * Math.Cos(robotLocation.Theta) - pt.Y * Math.Sin(robotLocation.Theta), pt.X * Math.Sin(robotLocation.Theta) + pt.Y * Math.Cos(robotLocation.Theta));
                polyPt.X += robotLocation.X;
                polyPt.Y += robotLocation.Y;
                polygonToDisplay.polygon.Points.Add(polyPt);
                polygonToDisplay.backgroundColor = robotShape.backgroundColor;
                polygonToDisplay.borderColor = robotShape.borderColor;
                polygonToDisplay.borderWidth = robotShape.borderWidth;
            }
            return polygonToDisplay;
        }

        private PolygonExtended ghostShape;
        private PolygonExtended GetRobotGhostPolygon()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            foreach (var pt in ghostShape.polygon.Points)
            {
                Point polyPt = new Point(pt.X * Math.Cos(ghostLocation.Theta) - pt.Y * Math.Sin(ghostLocation.Theta), pt.X * Math.Sin(ghostLocation.Theta) + pt.Y * Math.Cos(ghostLocation.Theta));
                polyPt.X += ghostLocation.X;
                polyPt.Y += ghostLocation.Y;
                polygonToDisplay.polygon.Points.Add(polyPt);
                polygonToDisplay.backgroundColor = ghostShape.backgroundColor;
                polygonToDisplay.borderColor = ghostShape.borderColor;
                polygonToDisplay.borderWidth = ghostShape.borderWidth;
            }
            return polygonToDisplay;
        }
    }

    public class BindingClass
    {
        private string imagePath;

        public string ImagePath
        {
            get { return imagePath; }
            set { imagePath = value; }
        }

        private double x1, x2, y1, y2;
        public double X1 { get { return x1; } set { x1 = value; } }
        public double X2 { get { return x2; } set { x2 = value; } }
        public double Y1 { get { return y1; } set { y1 = value; } }
        public double Y2 { get { return y2; } set { y2 = value; } }
    }
}
