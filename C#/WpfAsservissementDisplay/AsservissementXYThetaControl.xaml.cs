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
using Utilities;

namespace WpfAsservissementDisplay
{
    /// <summary>
    /// Logique d'interaction pour UserControl1.xaml
    /// </summary>
    public partial class AsservissementXYThetaControl : UserControl
    {
        FixedSizedQueue<double> errorXList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> errorYList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> errorThetaList = new Utilities.FixedSizedQueue<double>(50);

        FixedSizedQueue<double> corrPXList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrPYList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrPThetaList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrIXList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrIYList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrIThetaList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrDXList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrDYList = new Utilities.FixedSizedQueue<double>(50);
        FixedSizedQueue<double> corrDThetaList = new Utilities.FixedSizedQueue<double>(50);

        double corrLimitPX, corrLimitPY, corrLimitPTheta;
        double corrLimitIX, corrLimitIY, corrLimitITheta;
        double corrLimitDX, corrLimitDY, corrLimitDTheta;

        double KpX, KpY, KpTheta;
        double KiX, KiY, KiTheta;
        double KdX, KdY, KdTheta;

        System.Timers.Timer displayTimer;

        public AsservissementXYThetaControl()
        {
            InitializeComponent();
            errorXList.Enqueue(0);
            errorYList.Enqueue(0);
            errorThetaList.Enqueue(0);

            displayTimer = new Timer(100);
            displayTimer.Elapsed += DisplayTimer_Elapsed;
            displayTimer.Start();
        }

        public void SetTitle(string titre)
        {
            LabelTitre.Content = titre;
        }

        private void DisplayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                UpdateDisplay();
            }));
        }

        public void UpdateDisplay()
        {
            LabelErreurX.Content = errorXList.Average().ToString("N2");
            LabelErreurY.Content = errorYList.Average().ToString("N2");
            LabelErreurTheta.Content = errorThetaList.Average().ToString("N2");

            LabelKpX.Content = KpX.ToString("N2");
            LabelKpY.Content = KpY.ToString("N2");
            LabelKpTheta.Content = KpTheta.ToString("N2");
            LabelKiX.Content = KiX.ToString("N2");
            LabelKiY.Content = KiY.ToString("N2");
            LabelKiTheta.Content = KiTheta.ToString("N2");
            LabelKdX.Content = KdX.ToString("N2");
            LabelKdY.Content = KdY.ToString("N2");
            LabelKdTheta.Content = KdTheta.ToString("N2");

            LabelCorrMaxPX.Content = corrLimitPX.ToString("N2");
            LabelCorrMaxPY.Content = corrLimitPY.ToString("N2");
            LabelCorrMaxPTheta.Content = corrLimitPTheta.ToString("N2");
            LabelCorrMaxIX.Content = corrLimitIX.ToString("N2");
            LabelCorrMaxIY.Content = corrLimitIY.ToString("N2");
            LabelCorrMaxITheta.Content = corrLimitITheta.ToString("N2"); 
            LabelCorrMaxDX.Content = corrLimitDX.ToString("N2");
            LabelCorrMaxDY.Content = corrLimitDY.ToString("N2");
            LabelCorrMaxDTheta.Content = corrLimitDTheta.ToString("N2");


            if (corrPXList.Count > 0)
            {
                LabelCorrPX.Content = corrPXList.Average().ToString("N2");
                LabelCorrPY.Content = corrPYList.Average().ToString("N2");
                LabelCorrPTheta.Content = corrPThetaList.Average().ToString("N2");
                LabelCorrIX.Content = corrIXList.Average().ToString("N2");
                LabelCorrIY.Content = corrIYList.Average().ToString("N2");
                LabelCorrITheta.Content = corrIThetaList.Average().ToString("N2");
                LabelCorrDX.Content = corrDXList.Average().ToString("N2");
                LabelCorrDY.Content = corrDYList.Average().ToString("N2");
                LabelCorrDTheta.Content = corrDThetaList.Average().ToString("N2");
            }
        }

        public void UpdateErrorValues(double errorX, double errorY, double errorTheta)
        {
            errorXList.Enqueue(errorX);
            errorYList.Enqueue(errorY);
            errorThetaList.Enqueue(errorTheta);
        }

        public void UpdateCorrectionValues(double corrPX, double corrPY, double corrPTheta,
            double corrIX, double corrIY, double corrITheta,
            double corrDX, double corrDY, double corrDTheta)
        {
            corrPXList.Enqueue(corrPX);
            corrPYList.Enqueue(corrPY);
            corrPThetaList.Enqueue(corrPTheta);
            corrIXList.Enqueue(corrIX);
            corrIYList.Enqueue(corrIY);
            corrIThetaList.Enqueue(corrITheta);
            corrDXList.Enqueue(corrDX);
            corrDYList.Enqueue(corrDY);
            corrDThetaList.Enqueue(corrDTheta);
        }


        public void UpdateCorrectionGains(double KpX, double KpY, double KpTheta,
            double KiX, double KiY, double KiTheta,
            double KdX, double KdY, double KdTheta)
        {
            this.KpX = KpX;
            this.KpY = KpY;
            this.KpTheta = KpTheta;
            this.KiX = KiX;
            this.KiY = KiY;
            this.KiTheta = KiTheta;
            this.KdX = KdX;
            this.KdY = KdY;
            this.KdTheta = KdTheta;
        }

        public void UpdateCorrectionLimits(double corrLimitPX, double corrLimitPY, double corrLimitPTheta,
            double corrLimitIX, double corrLimitIY, double corrLimitITheta,
            double corrLimitDX, double corrLimitDY, double corrLimitDTheta)
        {
            this.corrLimitPX = corrLimitPX;
            this.corrLimitPY = corrLimitPY;
            this.corrLimitPTheta = corrLimitPTheta;
            this.corrLimitIX = corrLimitIX;
            this.corrLimitIY = corrLimitIY;
            this.corrLimitITheta = corrLimitITheta;
            this.corrLimitDX = corrLimitDX;
            this.corrLimitDY = corrLimitDY;
            this.corrLimitDTheta = corrLimitDTheta;
        }
    }
}

