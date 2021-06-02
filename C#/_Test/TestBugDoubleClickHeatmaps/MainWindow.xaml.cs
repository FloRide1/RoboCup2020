using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace TestBugDoubleClickHeatmaps
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Set this code once in App.xaml.cs or application startup
            SciChartSurface.SetRuntimeLicenseKey("wsCOsvBlAs2dax4o8qBefxMi4Qe5BVWax7TGOMLcwzWFYRNCa/f1rA5VA1ITvLHSULvhDMKVTc+niao6URAUXmGZ9W8jv/4jtziBzFZ6Z15ek6SLU49eIqJxGoQEFWvjANJqzp0asw+zvLV0HMirjannvDRj4i/WoELfYDubEGO1O+oAToiJlgD/e2lVqg3F8JREvC0iqBbNrmfeUCQdhHt6SKS2QpdmOoGbvtCossAezGNxv92oUbog6YIhtpSyGikCEwwKSDrlKlAab6302LLyFsITqogZychLYrVXJTFvFVnDfnkQ9cDi7017vT5flesZwIzeH497lzGp3B8fKWFQyZemD2RzlQkvj5GUWBwxiKAHrYMnQjJ/PsfojF1idPEEconVsh1LoYofNk2v/Up8AzXEAvxWUEcgzANeQggaUNy+OFet8b/yACa/bgYG7QYzFQZzgdng8IK4vCPdtg4/x7g5EdovN2PI9vB76coMuKnNVPnZN60kSjtd/24N8A==");

            InitializeComponent();
        }

        private void sciChart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Perform the hit test relative to the GridLinesPanel
                var hitTestPoint = e.GetPosition(sciChartSurface.GridLinesPanel as UIElement);
                textBoxDebug.Text = "HitPoint - X : " + hitTestPoint.X + " - Y : " + hitTestPoint.Y;
                foreach (var renderableSeries in sciChartSurface.RenderableSeries)
                {
                    // Get hit-test the RenderableSeries using interpolation
                    var hitTestInfo = renderableSeries.HitTestProvider.HitTest(hitTestPoint, true);

                    textBoxDebug.Text += "\nHitTestInfo - " + hitTestInfo.DataSeriesType;
                    if (hitTestInfo.DataSeriesType == DataSeriesType.Heatmap)
                    {
                        Console.WriteLine(hitTestInfo.DataSeriesType.ToString() + " Click on : x=" + hitTestInfo.XValue + " - y=" + hitTestInfo.YValue);
                        //OnCtrlClickOnHeatMap((double)hitTestInfo.XValue, (double)hitTestInfo.YValue);
                    }
                }
            }
        }

    }
}
