using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
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
        Dictionary<int, XyDataSeries<double, double> > lineDictionary = new Dictionary<int, XyDataSeries<double, double>>();

        public WpfOscilloscope()
        {
            InitializeComponent();

            //lineRenderSeries.DataSeries = dataSeries;
            //lineRenderSeries2.DataSeries = dataSeries2;
            //sciChart.ZoomExtents();
        }

        public void AddOrUpdateLine(int id, int maxNumberOfPoints, string lineName)
        {
            if (lineDictionary.ContainsKey(id))
                lineDictionary[id] = new XyDataSeries<double, double>(maxNumberOfPoints) { SeriesName = lineName };
            else
            {
                lineDictionary.Add(id, new XyDataSeries<double, double>(maxNumberOfPoints) { SeriesName = lineName });
                var lineRenderableSerie = new FastLineRenderableSeries();
                lineRenderableSerie.Name = "lineRenderableSerie"+id.ToString();
                lineRenderableSerie.DataSeries = lineDictionary[id];
                
                //Ajout de la ligne dans le scichart
                sciChart.RenderableSeries.Add(lineRenderableSerie);
            }
        }


        public void SetTitle(string title)
        {
            titleText.Text = title;
        }
        public void SetSerieName(int serieID, string name)
        {
            if(lineDictionary.ContainsKey(serieID))
                lineDictionary[serieID].SeriesName = name;
        }

        public void ChangeLineColor(string lineName, Color color)
        {
            sciChart.RenderableSeries.Single(x => x.DataSeries.SeriesName == lineName).Stroke = color;
        }

        public void ChangeLineColor(int serieID, Color color)
        {
            sciChart.RenderableSeries.Single(x => x.DataSeries == lineDictionary[serieID]).Stroke=color;
        }

        public void AddPointToLine(int lineId, double x, double y)
        {
            lineDictionary[lineId].Append(x, y);
            if (lineDictionary[lineId].Count > lineDictionary[lineId].Capacity)
                lineDictionary[lineId].RemoveAt(0);
        }

        public void AddPointToLine(int lineId, Point point)
        {
            lineDictionary[lineId].Append(point.X, point.Y);
            if (lineDictionary[lineId].Count > lineDictionary[lineId].Capacity)
                lineDictionary[lineId].RemoveAt(0);
        }

        public void AddPointListToLine(int lineId, List<Point> pointList)
        {
            lineDictionary[lineId].Append( pointList.Select(e=>e.X).ToList(), pointList.Select(e2=> e2.Y).ToList());
            if (lineDictionary[lineId].Count > lineDictionary[lineId].Capacity)
                lineDictionary[lineId].RemoveAt(0);
        }
    }
}
