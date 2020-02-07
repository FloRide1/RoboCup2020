using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace PositionEstimator
{
    public class AbsolutePositionEstimator
    {
        List<PolarPoint> LidarPtList;

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            LidarPtList = e.PtList;
        }

        public void AbsolutePositionEvaluation(object sender, OpenCvMatImageArgs e)
        {
            //OnOpenCvMatImageProcessedReady(initialMat, "ImageFromCameraViaProcessing");

            var panoramaImage = FishEyeToPanorama(e.Mat);
            OnOpenCvMatImageProcessedReady(panoramaImage, "ImageFromCameraViaProcessing");

            if(LidarPtList!=null)
            {
                //Recherche des blobs saillants

                //Tri des blobs saillants par couleur et par taille et par distance 
            }

            ////Découpage de l'image
            //int RawMatCroppedSize = 300;
            //int cropOffsetX = 0;
            //int cropOffsetY = 0;
            //Range rgX = new Range(initialMat.Width / 2 - RawMatCroppedSize / 2 + cropOffsetX, initialMat.Width / 2 + RawMatCroppedSize / 2 + cropOffsetX);
            //Range rgY = new Range(initialMat.Height / 2 - RawMatCroppedSize / 2 + cropOffsetY, initialMat.Height / 2 + RawMatCroppedSize / 2 + cropOffsetY);
            //Mat RawMatCropped = new Mat(initialMat, rgY, rgX);

            ////Conversion en HSV
            //Mat HsvMatCropped = new Mat();
            //CvInvoke.CvtColor(RawMatCropped, HsvMatCropped, ColorConversion.Bgr2Hsv);
            //OnOpenCvMatImageProcessedReady(HsvMatCropped, "ImageDebug3");
            //OnOpenCvMatImageProcessedReady(TerrainTheoriqueVertical, "ImageDebug4");
        }

        public Mat FishEyeToPanorama(Mat originalMat)
        {
            Image<Bgr, Byte> originalImg = originalMat.ToImage<Bgr, Byte>();
            int originalWidth = originalImg.Cols;
            int originalHeight = originalImg.Rows;
            byte[,,] data = (byte[,,])originalImg.Data;

            double scaleCoeff = 0.2;
            //Mat panorama = new Mat(height, width, originalImg.Depth, originalImg.NumberOfChannels);

            int RayonCercle = (int)(originalHeight / 2);
            int heightPanorama = RayonCercle * 2;// * scaleCoeff);
            int widthPanorama = (int)(RayonCercle * 2 * Math.PI);// * scaleCoeff);
            byte[,,] panoramaData = new byte[heightPanorama, widthPanorama, 3];


            for (int i = 0; i < widthPanorama; i++)
            {
                double cosR = Math.Cos((double)i / RayonCercle + Math.PI);
                double sinR = Math.Sin((double)i / RayonCercle + Math.PI);
                for (int j = 0; j < heightPanorama; j++)
                {
                    int xPos = (int)(originalHeight / 2 + (heightPanorama - 1 - j) * cosR/*Math.Cos((double)i /RayonCercle + Math.PI)*/);
                    int yPos = (int)(originalWidth / 2 + (heightPanorama - 1 - j) * sinR/*Math.Sin((double)i / RayonCercle + Math.PI)*/);
                    if (xPos < originalHeight && xPos > 0 && yPos < originalWidth && yPos > 0)
                    {
                        panoramaData[j, i, 0] = data[xPos, yPos, 0];
                        panoramaData[j, i, 1] = data[xPos, yPos, 1];
                        panoramaData[j, i, 2] = data[xPos, yPos, 2];
                    }
                }
            }

            Image<Bgr, Byte> im = new Image<Bgr, Byte>(panoramaData);
            return im.Mat;
        }

        public Mat FishEyeToPanorama2(Mat originalMat)
        {
            Image<Bgr, Byte> originalImg = originalMat.ToImage<Bgr, Byte>();

            int originalWidth = originalImg.Cols;
            int originalHeight = originalImg.Rows;

            double scaleCoeff = 0.2;


            //int heightPanorama = RayonCercle * 2;// * scaleCoeff);
            //int widthPanorama = (int)(RayonCercle * 2 * Math.PI);// * scaleCoeff);
            Mat outMat = new Mat();

            PointF center = new PointF((float)originalWidth / 2, (float)originalHeight / 2);
            int RayonCercle = (int)(0.8 * Math.Min(center.Y, center.X));
            double M = 100;// originalWidth / Math.Log(RayonCercle);
            CvInvoke.LogPolar(originalMat, outMat, center, M, Inter.Linear, Warp.FillOutliers);

            //byte[,,] panoramaData = new byte[heightPanorama, widthPanorama, 3];


            //for (int i = 0; i < widthPanorama; i++)
            //{
            //    double cosR = Math.Cos((double)i / RayonCercle + Math.PI);
            //    double sinR = Math.Sin((double)i / RayonCercle + Math.PI);
            //    for (int j = 0; j < heightPanorama; j++)
            //    {
            //        int xPos = (int)(originalHeight / 2 + (heightPanorama - 1 - j) * cosR/*Math.Cos((double)i /RayonCercle + Math.PI)*/);
            //        int yPos = (int)(originalWidth / 2 + (heightPanorama - 1 - j) * sinR/*Math.Sin((double)i / RayonCercle + Math.PI)*/);
            //        if (xPos < originalHeight && xPos > 0 && yPos < originalWidth && yPos > 0)
            //        {
            //            panoramaData[j, i, 0] = data[xPos, yPos, 0];
            //            panoramaData[j, i, 1] = data[xPos, yPos, 1];
            //            panoramaData[j, i, 2] = data[xPos, yPos, 2];
            //        }
            //    }
            //}

            Image<Bgr, Byte> im = outMat.ToImage<Bgr, Byte>();
            return im.Mat;
        }

        // Event position dans l'image calculée
        public event EventHandler<PositionArgs> PositionEvent;
        public virtual void OnPositionCalculatedEvent(float x, float y, float angle, float reliability)
        {
            var handler = PositionEvent;
            if (handler != null)
            {
                handler(this, new PositionArgs { X = x, Y = y, Angle = angle, Reliability = reliability });
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
    }
}
