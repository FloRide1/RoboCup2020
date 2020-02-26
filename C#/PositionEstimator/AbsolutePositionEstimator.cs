using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Mat sourceImage = e.Mat;
            Mat panoramaImage;
            var sw = new Stopwatch();
            sw.Start();

            //panoramaImage = FishEyeToPanorama2(e.Mat);
            //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageFromCameraViaProcessing");
            //sw.Stop();
            //Console.WriteLine("FishEyeToPano:" + sw.ElapsedMilliseconds);
            //sw.Reset();
            //sw.Start();
            //panoramaImage = FishEyeToPanorama(e.Mat);
            //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageDebug3");
            //sw.Stop();
            //Console.WriteLine("FishEyeToPano2:" + sw.ElapsedMilliseconds);
            //sw.Reset();
            //sw.Start();
            panoramaImage = FishEyeToPanorama3(e.Mat);              //Methode optimisée. A voir si on peux faire mieux avec les bons API EMGU
            OnOpenCvMatImageProcessedReady(panoramaImage, "ImageDebug2");
            sw.Stop();
            //Console.WriteLine("FishEyeToPano3:" + sw.ElapsedMilliseconds);
            if (LidarPtList!=null)
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
        public void AbsolutePositionEvaluation(object sender, BitmapImageArgs e)
        {
            //OnOpenCvMatImageProcessedReady(initialMat, "ImageFromCameraViaProcessing");
            Image<Bgr, Byte> imageCV = new Image<Bgr, byte>(e.Bitmap); //Image Class from Emgu.CV
            Mat sourceImage = imageCV.Mat;
            Mat panoramaImage;
            var sw = new Stopwatch();
            sw.Start();

            //panoramaImage = FishEyeToPanorama2(e.Mat);
            //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageFromCameraViaProcessing");
            //sw.Stop();
            //Console.WriteLine("FishEyeToPano:" + sw.ElapsedMilliseconds);
            //sw.Reset();
            //sw.Start();
            //panoramaImage = FishEyeToPanorama(e.Mat);
            //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageDebug3");
            //sw.Stop();
            //Console.WriteLine("FishEyeToPano2:" + sw.ElapsedMilliseconds);
            //sw.Reset();
            //sw.Start();


            //panoramaImage = FishEyeToPanorama3(sourceImage);              //Methode optimisée. A voir si on peux faire mieux avec les bons API EMGU

            //OnBitmapImageProcessedReady(panoramaImage.Bitmap, "ImageDebug2");
            //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageDebug2");
            sw.Stop();
            //Console.WriteLine("FishEyeToPano3:" + sw.ElapsedMilliseconds);
            if (LidarPtList != null)
            {
                //Recherche des blobs saillants

                //Tri des blobs saillants par couleur et par taille et par distance 
            }
        }

        //public  Mat FishEyeToPanorama(Mat originalMat)
        //{
        //    Image<Bgr, Byte> originalImg = originalMat.ToImage<Bgr, Byte>();
        //    int originalWidth = originalImg.Cols;
        //    int originalHeight = originalImg.Rows;
        //    byte[,,] data = (byte[,,])originalImg.Data;

        //    double scaleCoeff = 0.2;
        //    //Mat panorama = new Mat(height, width, originalImg.Depth, originalImg.NumberOfChannels);

        //    int RayonCercle = (int)(originalHeight / 2);
        //    int heightPanorama = RayonCercle * 2;// * scaleCoeff);
        //    int widthPanorama = (int)(RayonCercle * 2 * Math.PI);// * scaleCoeff);
        //    byte[,,] panoramaData = new byte[heightPanorama, widthPanorama, 3];


        //    for (int i = 0; i < widthPanorama; i++)
        //    {
        //        double cosR = Math.Cos((double)i / RayonCercle + Math.PI);
        //        double sinR = Math.Sin((double)i / RayonCercle + Math.PI);
        //        for (int j = 0; j < heightPanorama; j++)
        //        {
        //            int xPos = (int)(originalHeight / 2 + (heightPanorama - 1 - j) * cosR/*Math.Cos((double)i /RayonCercle + Math.PI)*/);
        //            int yPos = (int)(originalWidth / 2 + (heightPanorama - 1 - j) * sinR/*Math.Sin((double)i / RayonCercle + Math.PI)*/);
        //            if (xPos < originalHeight && xPos > 0 && yPos < originalWidth && yPos > 0)
        //            {
        //                panoramaData[j, i, 0] = data[xPos, yPos, 0];
        //                panoramaData[j, i, 1] = data[xPos, yPos, 1];
        //                panoramaData[j, i, 2] = data[xPos, yPos, 2];
        //            }
        //        }
        //    }

        //    Image<Bgr, Byte> im = new Image<Bgr, Byte>(panoramaData);
        //    return im.Mat;
        //}

        public Mat FishEyeToPanorama2(Mat originalMat)
        {
            Image<Bgr, Byte> originalImg = originalMat.ToImage<Bgr, Byte>();

            int originalWidth = originalImg.Cols;
            int originalHeight = originalImg.Rows;

            double scaleCoeff = 0.2;


            //int heightPanorama = RayonCercle * 2;// * scaleCoeff);
            //int widthPanorama = (int)(RayonCercle * 2 * Math.PI);// * scaleCoeff);
            

            PointF center = new PointF((float)originalWidth / 2, (float)originalHeight / 2);
            int RayonCercle = (int)( Math.Min(center.Y, center.X));
            double M = 100;// RayonCercle;//200;// originalWidth / Math.Log(RayonCercle);
            Mat outMat = new Mat();// new Mat(new Size((int)(2 * Math.PI *RayonCercle),originalMat.Size.Width),DepthType.Cv32F,1);
            
            CvInvoke.LogPolar(originalMat,outMat , center, M, Inter.Linear, Warp.FillOutliers);
            //CvInvoke.Resize(outMat, outMat, new Size(  (int)(2 * Math.PI * RayonCercle), originalMat.Size.Width),0,0,Inter.Linear);
            CvInvoke.Resize(outMat, outMat, new Size( originalMat.Size.Width, (int)(2 * Math.PI * RayonCercle)), 0, 0, Inter.Linear);
            //CvInvoke.LinearPolar(originalMat, outMat, center, 100, Inter.Cubic);

            Image<Bgr, Byte> im = outMat.ToImage<Bgr, Byte>();
            im=im.Rotate(-90, new Bgr(),false);
            return im.Mat;
        }

        public Mat FishEyeToPanorama3(Mat originalMat)
        {
            byte[,,] data = (byte[,,])originalMat.GetData();
            int originalWidth = originalMat.Cols;               //<--Couteux en temps
            int originalHeight = originalMat.Rows;               //<--Couteux en temps

            int RayonCercle = (int)(originalHeight / 2);
            int heightPanorama = RayonCercle * 2;
            int widthPanorama = (int)(RayonCercle * 2 * Math.PI);
            byte[,,] panoramaData = new byte[heightPanorama, widthPanorama, 3];
            //On test si on a initialisé le tableau des sinus/cosinus a la bonne taille
            if (initSize !=widthPanorama)
            {
                PrepareLUTCoSi(widthPanorama, RayonCercle);
            }

            //Parallelisation des boucles en utilisant une expression lambda
            Parallel.For (0, widthPanorama,(i)=>
            {
                for (int j = 0; j < heightPanorama; j++)
                {
                    int xPos = (int)(originalHeight / 2 + (heightPanorama - 1 - j) * cosRTab[i]);
                    int yPos = (int)(originalWidth / 2 + (heightPanorama - 1 - j) * sinRTab[i]);
                    if (xPos < originalHeight && xPos > 0 && yPos < originalWidth && yPos > 0)
                    {
                        panoramaData[j, i, 0] = data[xPos, yPos, 0];        //Methode d'acces la plus rapide apres l'acces par pointeur
                        panoramaData[j, i, 1] = data[xPos, yPos, 1];
                        panoramaData[j, i, 2] = data[xPos, yPos, 2];
                    }
                }
            });

            Image<Bgr, Byte> im = new Image<Bgr, Byte>(panoramaData);
            im=im.Flip(FlipType.Horizontal);
            return im.Mat;
        }

        float[] cosRTab = null;
        float[] sinRTab = null;
        int initSize = 0;
        void PrepareLUTCoSi(int length, int rayon)
        {
            initSize = length;
            if(cosRTab == null)
            {
                cosRTab = new float[length];
            }
            if (sinRTab == null)
            {
                sinRTab = new float[length];
            }
            for(int i=0;i<length;i++)
            {
                cosRTab[i]=(float)Math.Cos((float)i / rayon + Math.PI);
                sinRTab[i] = (float)Math.Sin((float)i / rayon + Math.PI);
            }
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
