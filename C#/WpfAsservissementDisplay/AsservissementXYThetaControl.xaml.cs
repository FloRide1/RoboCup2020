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
    /// 
    public partial class AsservissementXYThetaControl : UserControl
    {
        int queueSize = 1;
        FixedSizedQueue<double> commandXList;
        FixedSizedQueue<double> commandYList;
        FixedSizedQueue<double> commandThetaList;

        FixedSizedQueue<double> consigneXList;
        FixedSizedQueue<double> consigneYList;
        FixedSizedQueue<double> consigneThetaList;

        FixedSizedQueue<double> measuredXList;
        FixedSizedQueue<double> measuredYList;
        FixedSizedQueue<double> measuredThetaList;

        FixedSizedQueue<double> errorXList;
        FixedSizedQueue<double> errorYList;
        FixedSizedQueue<double> errorThetaList;

        FixedSizedQueue<double> corrPXList;
        FixedSizedQueue<double> corrPYList;
        FixedSizedQueue<double> corrPThetaList;
        FixedSizedQueue<double> corrIXList;
        FixedSizedQueue<double> corrIYList;
        FixedSizedQueue<double> corrIThetaList;
        FixedSizedQueue<double> corrDXList;
        FixedSizedQueue<double> corrDYList;
        FixedSizedQueue<double> corrDThetaList;

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

            commandXList = new Utilities.FixedSizedQueue<double>(queueSize);
            commandYList = new Utilities.FixedSizedQueue<double>(queueSize);
            commandThetaList = new Utilities.FixedSizedQueue<double>(queueSize);

            consigneXList = new Utilities.FixedSizedQueue<double>(queueSize);
            consigneYList = new Utilities.FixedSizedQueue<double>(queueSize);
            consigneThetaList = new Utilities.FixedSizedQueue<double>(queueSize);

            measuredXList = new Utilities.FixedSizedQueue<double>(queueSize);
            measuredYList = new Utilities.FixedSizedQueue<double>(queueSize);
            measuredThetaList = new Utilities.FixedSizedQueue<double>(queueSize);

            errorXList = new Utilities.FixedSizedQueue<double>(queueSize);
            errorYList = new Utilities.FixedSizedQueue<double>(queueSize);
            errorThetaList = new Utilities.FixedSizedQueue<double>(queueSize);

            corrPXList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrPYList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrPThetaList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrIXList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrIYList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrIThetaList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrDXList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrDYList = new Utilities.FixedSizedQueue<double>(queueSize);
            corrDThetaList = new Utilities.FixedSizedQueue<double>(queueSize);

            consigneXList.Enqueue(0);
            consigneYList.Enqueue(0);
            consigneThetaList.Enqueue(0);

            commandXList.Enqueue(0);
            commandYList.Enqueue(0);
            commandThetaList.Enqueue(0);

            measuredXList.Enqueue(0);
            measuredYList.Enqueue(0);
            measuredThetaList.Enqueue(0);

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
            LabelConsigneX.Content = consigneXList.Average().ToString("N2");
            LabelConsigneY.Content = consigneYList.Average().ToString("N2");
            LabelConsigneTheta.Content = consigneThetaList.Average().ToString("N2");

            LabelMeasureX.Content = measuredXList.Average().ToString("N2");
            LabelMeasureY.Content = measuredYList.Average().ToString("N2");
            LabelMeasureTheta.Content = measuredThetaList.Average().ToString("N2");

            LabelErreurX.Content = errorXList.Average().ToString("N2");
            LabelErreurY.Content = errorYList.Average().ToString("N2");
            LabelErreurTheta.Content = errorThetaList.Average().ToString("N2");

            LabelCommandX.Content = commandXList.Average().ToString("N2");
            LabelCommandY.Content = commandYList.Average().ToString("N2");
            LabelCommandTheta.Content = commandThetaList.Average().ToString("N2");

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

        public void UpdateConsigneValues(double consigneX, double consigneY, double consigneTheta)
        {
            consigneXList.Enqueue(consigneX);
            consigneYList.Enqueue(consigneY);
            consigneThetaList.Enqueue(consigneTheta);
        }

        public void UpdateCommandValues(double commandX, double commandY, double commandTheta)
        {
            commandXList.Enqueue(commandX);
            commandYList.Enqueue(commandY);
            commandThetaList.Enqueue(commandTheta);
        }

        public void UpdateMeasuredValues(double valueX, double valueY, double valueTheta)
        {
            measuredXList.Enqueue(valueX);
            measuredYList.Enqueue(valueY);
            measuredThetaList.Enqueue(valueTheta);
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

