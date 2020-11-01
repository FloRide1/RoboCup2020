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

        Timer displayTimer = new Timer(100);

        public AsservissementXYThetaControl()
        {
            InitializeComponent();
            errorXList.Enqueue(0);
            errorYList.Enqueue(0);
            errorThetaList.Enqueue(0);

            displayTimer.Elapsed += DisplayTimer_Elapsed;
            displayTimer.Start();
        }

        private void DisplayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {            
            LabelErreurX.Content = errorXList.Average().ToString("N2");
            LabelErreurY.Content = errorYList.Average().ToString("N2");
            LabelErreurTheta.Content = errorThetaList.Average().ToString("N2");
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
