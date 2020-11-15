using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        }

        public void AddOrUpdateLine(int id, int maxNumberOfPoints, string lineName, bool useYAxisRight = true)
        {
            if (lineDictionary.ContainsKey(id))
            {
                lineDictionary[id] = new XyDataSeries<double, double>(maxNumberOfPoints) { SeriesName = lineName };
                //sciChart.RenderableSeries.RemoveAt(id);
            }
            else
            {
                lineDictionary.Add(id, new XyDataSeries<double, double>(maxNumberOfPoints) { SeriesName = lineName });
           
                var lineRenderableSerie = new FastLineRenderableSeries();
                lineRenderableSerie.Name = "lineRenderableSerie"+id.ToString();
                lineRenderableSerie.DataSeries = lineDictionary[id];
                lineRenderableSerie.DataSeries.AcceptsUnsortedData = true;
                if(useYAxisRight)
                    lineRenderableSerie.YAxisId = "RightYAxis";
                else
                    lineRenderableSerie.YAxisId = "LeftYAxis";

                //Ajout de la ligne dans le scichart
                sciChart.RenderableSeries.Add(lineRenderableSerie);
            }             
        }

        public void RemoveLine(int id)
        {
            if (lineDictionary.ContainsKey(id))
            {
                
                sciChart.RenderableSeries.Remove(sciChart.RenderableSeries.Single(x => x.DataSeries.SeriesName == lineDictionary[id].SeriesName));
                lineDictionary.Remove(id);
            }
            else
            {

            }
        }

        public void ResetGraph()
        {
            foreach(var serie in sciChart.RenderableSeries)
            {
                serie.DataSeries.Clear();
            }
        }
        public void ResetLine(int id)
        {

            if (lineDictionary.ContainsKey(id))
                sciChart.RenderableSeries.Single(x => x.DataSeries.SeriesName == lineDictionary[id].SeriesName).DataSeries.Clear();
        }
        public bool LineExist(int id)
        {
            if (lineDictionary.ContainsKey(id))
                return true;
            return false;
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

            sciChart.RenderableSeries.Single(x => x.DataSeries.SeriesName == lineDictionary[serieID].SeriesName).Stroke=color;
        }

        public void DrawOnlyPoints(int serieID)
        {
            sciChart.RenderableSeries.Single(x => x.DataSeries.SeriesName == lineDictionary[serieID].SeriesName).Stroke = Color.FromArgb(0, 255, 255, 255);
        }

        public void AddPointToLine(int lineId, double x, double y)
        {
            if (LineExist(lineId))
            {
                lineDictionary[lineId].Append(x, y);
                if (lineDictionary[lineId].Count > lineDictionary[lineId].Capacity)
                    lineDictionary[lineId].RemoveAt(0);
            }
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
        public void UpdatePointListOfLine(int lineId, List<Point> pointList)
        {
            lineDictionary[lineId].Clear();
            lineDictionary[lineId].Append(pointList.Select(e => e.X).ToList(), pointList.Select(e2 => e2.Y).ToList());            
        }
    }
}
