using SciChart.Charting.Model.DataSeries;
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

namespace WpfOscilloscopeControl
{
    /// <summary>
    /// Logique d'interaction pour UserControl1.xaml
    /// </summary>
    public partial class WpfOscilloscope : UserControl
    {
        Timer timerSimulation = new Timer(100);
        XyDataSeries<double, double> dataSeries = new XyDataSeries<double, double>(100) { SeriesName = "Speed Robot 1"};
        XyDataSeries<double, double> dataSeries2 = new XyDataSeries<double, double>(100) { SeriesName = "Speed Robot 2" };

        public WpfOscilloscope()
        {
            InitializeComponent();
            timerSimulation.Elapsed += TimerSimulation_Elapsed;
            timerSimulation.Start();
            lineRenderSeries.DataSeries = dataSeries;
            lineRenderSeries2.DataSeries = dataSeries2;
            //sciChart.ZoomExtents();
        }


        double currentTime = 0;
        private void TimerSimulation_Elapsed(object sender, ElapsedEventArgs e)
        {
            currentTime += 0.1;
            double value = Math.Sin(0.5 * currentTime);
            double value2 = Math.Cos(0.5 * currentTime);
            dataSeries.Append(currentTime, value);
            if(dataSeries.Count> dataSeries.Capacity)
                dataSeries.RemoveAt(0);
            dataSeries2.Append(currentTime, value2);
            if (dataSeries2.Count > dataSeries2.Capacity)
                dataSeries2.RemoveAt(0);
            //lineRenderSeries.DataSeries = dataSeries;
        }

        public void SetTitle(string title)
        {
            titleText.Text = title;
        }
    }
}
