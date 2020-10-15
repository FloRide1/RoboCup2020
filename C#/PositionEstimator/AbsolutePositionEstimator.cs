using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace PositionEstimator
{
    public class AbsolutePositionEstimator
    {
        List<PolarPointRssi> LidarPtList;

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            LidarPtList = e.PtList;
        }

        public void AbsolutePositionEvaluation(object sender, BitmapImageArgs e)
        {

        }


        // Event position dans l'image calculée
        public event EventHandler<PositionArgs> PositionEvent;
        public virtual void OnPositionCalculatedEvent(float x, float y, float angle, float reliability)
        {
            var handler = PositionEvent;
            if (handler != null)
            {
                handler(this, new PositionArgs { X = x, Y = y, Theta = angle, Reliability = reliability });
            }
        }

        // Event image postprocessée
        public event EventHandler<OpenCvMatImageArgs> OnOpenCvMatImageProcessedEvent;

        public virtual void OnOpenCvMatImageProcessedReady(Mat mat, string descriptor)
        {
            var handler = OnOpenCvMatImageProcessedEvent;
            if (handler != null)
            {
                handler(this, new OpenCvMatImageArgs { Mat = mat, Descriptor = descriptor });
            }
        }

        public event EventHandler<BitmapImageArgs> OnBitmapImageProcessedEvent;
        public virtual void OnBitmapImageProcessedReady(Bitmap image, string descriptor)
        {

            var handler = OnBitmapImageProcessedEvent;
            if (handler != null)
            {
                handler(this, new BitmapImageArgs { Bitmap = image, Descriptor = descriptor });
            }
        }
    }
}
