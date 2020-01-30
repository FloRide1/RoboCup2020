using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals;
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
            //SciChartSurface.SetRuntimeLicenseKey("fE00fIihccLO3oQbIBBZdrLh0ZWhRPxTSTGVMBVK51242lHVH3psTy6uZRTxKdk4UX2uSivlbh3c/m0SdZlgNHiMFONKSqo68Xtrcb8vjczLU9Usun6b7BtUIX5+Y3UfVsm+iN2Jg4Fc6l2/f5n0Sz4yG8204RdBexeifufMIkbd5LZBrfOPThBPC5iYnTS4W06S52QMCPjjQN0zALKG+0MmjBdQqeMidbmtCku6WVs6EVGJac/YNHi/jWHYC7XlVmWUf5KqivDvtKrQQLqtO88n1lHPz/aD/T0Bkw4bDlYcFy3GsYvinieGvLIGQsAIm45dA+/+WIoR9foMcfWVMMh2LtiKpbT3idmABMrsTn3/zzdrsiFfCbg6KmTBA55N9UWNxvKUQ+nhwoLxOmvznC6FszXSmrwR8qFUdbVUA58HzPfLVa6Ge40GwLIuCHBFQrd5uwzhh2JQKmkn2zWD3an92O66EsLptUT655MBXlXx9xoOd6iiUkdyfF2KHZsD18c=");
            //SciChartSurface.SetRuntimeLicenseKey("eRoTcgOMGS9VMzaHKtmQpF2fnC5C/0bgl63pGg8dxffYptgx+7dpwRM1pGDRFHna8fErF0PcwxAJlYl3XqvxRmD7Kpf6c3uVJEjxDniHOBoA3M7nmBGq+C2vVT7WOeegNDcF9K8K68YEwxaGMllwTnd27TPtTBJheMddjwIgHumeXhEgumtLx2L12i6hduJISeHPSXQWJ4cujFMFdTBp6JK571Y6OPrf53cR3a12m5ZYbl1wUeHVBFgYRIIHQjeq9uqluTZvVd+nAgBu55HXm8B/sNg1cPpEXK3236fhxIqs6JxZjxiZdD0zq6SdqXQPHQLOxsr+VdhUE12V3mKaRnb/OujNVm+IuYwaHASkHoeKIiOHHtm0+QiLSR4RpDpVkPgOdLHJhEruWX1/S1BujcdI0GtqvVVCSkGrodg4s0GDUaDB2JGm0htx+6CMxvSlDzHiENDv5O559QZHZEoJ7bSb2bbNUKwGNp7KnAq3IOoEWAgBtzqG5481wzKrZ3qdqVs=");
            //            SciChartSurface.SetRuntimeLicenseKey(@"<LicenseContract>
            //  < Customer > University of  Toulon </ Customer >
            //  < OrderId > EDUCATIONAL - USE - 0109 </ OrderId >
            //  < LicenseCount > 1 </ LicenseCount >
            //  < IsTrialLicense > false </ IsTrialLicense >
            //  < SupportExpires > 11 / 04 / 2019 00:00:00 </ SupportExpires >
            //  < ProductCode > SC - WPF - SDK - PRO - SITE </ ProductCode >
            //  < KeyCode > lwABAQEAAABZVzOfQ0zVAQEAewBDdXN0b21lcj1Vbml2ZXJzaXR5IG9mICBUb3Vsb247T3JkZXJJZD1FRFVDQVRJT05BTC1VU0UtMDEwOTtTdWJzY3JpcHRpb25WYWxpZFRvPTA0LU5vdi0yMDE5O1Byb2R1Y3RDb2RlPVNDLVdQRi1TREstUFJPLVNJVEWDf0QgB8GnCQXI6yAqNM2njjnGbUt2KsujTDzeE + k69K1XYVF1s1x1Hb / i / E3GHaU =</ KeyCode >
            //</ LicenseContract > ");

            InitializeComponent();

            //lineRenderSeries.DataSeries = dataSeries;
            //lineRenderSeries2.DataSeries = dataSeries2;
            //sciChart.ZoomExtents();
        }

        public void AddOrUpdateLine(int id, int maxNumberOfPoints, string lineName)
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

                //Ajout de la ligne dans le scichart
                sciChart.RenderableSeries.Add(lineRenderableSerie);
            }

            
        }

        public void RemoveLine(int id)
        {
            if (lineDictionary.ContainsKey(id))
            {
                
                sciChart.RenderableSeries.Remove(sciChart.RenderableSeries.Single(x => x.DataSeries == lineDictionary[id]));
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
    }
}
