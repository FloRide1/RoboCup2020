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
        List<PolarPoint> LidarPtList;

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            LidarPtList = e.PtList;
        }

        //public void AbsolutePositionEvaluation(object sender, OpenCvMatImageArgs e)
        //{
        //    //OnOpenCvMatImageProcessedReady(initialMat, "ImageFromCameraViaProcessing");
        //    Mat sourceImage = e.Mat;
        //    Mat panoramaImage;
        //    var sw = new Stopwatch();
        //    sw.Start();

        //    //panoramaImage = FishEyeToPanorama2(e.Mat);
        //    //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageFromCameraViaProcessing");
        //    //sw.Stop();
        //    //Console.WriteLine("FishEyeToPano:" + sw.ElapsedMilliseconds);
        //    //sw.Reset();
        //    //sw.Start();
        //    //panoramaImage = FishEyeToPanorama(e.Mat);
        //    //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageDebug3");
        //    //sw.Stop();
        //    //Console.WriteLine("FishEyeToPano2:" + sw.ElapsedMilliseconds);
        //    //sw.Reset();
        //    //sw.Start();
        //    panoramaImage = FishEyeToPanorama3(e.);              //Methode optimisée. A voir si on peux faire mieux avec les bons API EMGU
        //    OnOpenCvMatImageProcessedReady(panoramaImage, "ImageDebug2");
        //    sw.Stop();
        //    //Console.WriteLine("FishEyeToPano3:" + sw.ElapsedMilliseconds);
        //    if (LidarPtList!=null)
        //    {
        //        //Recherche des blobs saillants

        //        //Tri des blobs saillants par couleur et par taille et par distance 
        //    }

        //    ////Découpage de l'image
        //    //int RawMatCroppedSize = 300;
        //    //int cropOffsetX = 0;
        //    //int cropOffsetY = 0;
        //    //Range rgX = new Range(initialMat.Width / 2 - RawMatCroppedSize / 2 + cropOffsetX, initialMat.Width / 2 + RawMatCroppedSize / 2 + cropOffsetX);
        //    //Range rgY = new Range(initialMat.Height / 2 - RawMatCroppedSize / 2 + cropOffsetY, initialMat.Height / 2 + RawMatCroppedSize / 2 + cropOffsetY);
        //    //Mat RawMatCropped = new Mat(initialMat, rgY, rgX);

        //    ////Conversion en HSV
        //    //Mat HsvMatCropped = new Mat();
        //    //CvInvoke.CvtColor(RawMatCropped, HsvMatCropped, ColorConversion.Bgr2Hsv);
        //    //OnOpenCvMatImageProcessedReady(HsvMatCropped, "ImageDebug3");
        //    //OnOpenCvMatImageProcessedReady(TerrainTheoriqueVertical, "ImageDebug4");
        //}
        public void AbsolutePositionEvaluation(object sender, BitmapImageArgs e)
        {

            var sw = new Stopwatch();

            sw.Restart();
            Bitmap BitmapPanoramaImage = FishEyeToPanorama3(e.Bitmap);
            sw.Stop();
            Console.WriteLine("FishEyeToPano3:" + sw.ElapsedMilliseconds);
            
            ////Code fonctionnel à performance équivalentes au traitement sur Bimap, mais avec les couts de conversion en plus.
            //Image<Bgr, Byte> imageCV = new Image<Bgr, byte>(e.Bitmap); //Image Class from Emgu.CV
            //Mat mat = imageCV.Mat; //This is your Image converted to Mat
            //sw.Restart();
            //FishEyeToPanorama3(mat);
            //sw.Stop();
            //Console.WriteLine("FishEyeToPano3 Mat:" + sw.ElapsedMilliseconds);

            OnBitmapImageProcessedReady(BitmapPanoramaImage, "ImageDebug2");
            //OnOpenCvMatImageProcessedReady(panoramaImage, "ImageDebug2");          
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

        //public Mat FishEyeToPanorama2(Bitmap originalImage)
        //{
        //    int originalWidth = originalImage.Width;
        //    int originalHeight = originalImage.Height;

        //    double scaleCoeff = 0.2;


        //    //int heightPanorama = RayonCercle * 2;// * scaleCoeff);
        //    //int widthPanorama = (int)(RayonCercle * 2 * Math.PI);// * scaleCoeff);


        //    PointF center = new PointF((float)originalWidth / 2, (float)originalHeight / 2);
        //    int RayonCercle = (int)( Math.Min(center.Y, center.X));
        //    double M = 100;// RayonCercle;//200;// originalWidth / Math.Log(RayonCercle);
        //    Mat outMat = new Mat();// new Mat(new Size((int)(2 * Math.PI *RayonCercle),originalMat.Size.Width),DepthType.Cv32F,1);

        //    CvInvoke.LogPolar(originalMat,outMat , center, M, Inter.Linear, Warp.FillOutliers);
        //    //CvInvoke.Resize(outMat, outMat, new Size(  (int)(2 * Math.PI * RayonCercle), originalMat.Size.Width),0,0,Inter.Linear);
        //    CvInvoke.Resize(outMat, outMat, new Size( originalMat.Size.Width, (int)(2 * Math.PI * RayonCercle)), 0, 0, Inter.Linear);
        //    //CvInvoke.LinearPolar(originalMat, outMat, center, 100, Inter.Cubic);

        //    Image<Bgr, Byte> im = outMat.ToImage<Bgr, Byte>();
        //    im=im.Rotate(-90, new Bgr(),false);
        //    return im.Mat;
        //}

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
            if (initSize != widthPanorama)
            {
                PrepareLUTCoSi(widthPanorama, RayonCercle);
            }

            //Parallelisation des boucles en utilisant une expression lambda
            Parallel.For(0, widthPanorama, (i) =>
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

            //Image<Bgr, Byte> im = new Image<Bgr, Byte>(panoramaData);
            //im=im.Flip(FlipType.Horizontal);
            return new Image<Bgr, Byte>(panoramaData).Mat;//.Flip(FlipType.Horizontal).Mat;        // Plus rapide que d'instancier une variable
        }

        public Bitmap FishEyeToPanorama3(Bitmap originalImage)
        {
            //https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp

            //byte[,,] data = (byte[,,])originalImage.;
            int originalWidth = originalImage.Width;  
            int originalHeight = originalImage.Height;          
            byte bytesPerPixel = 3;    

            //On suppose qu'on a la bonne taille de cercle de départ
            int RayonCercle = (int)(originalImage.Height / 2);
            int heightPanorama = RayonCercle;
            int widthPanorama = (int)(RayonCercle * 2 * Math.PI);

            BitmapData bmpDataOriginal = originalImage.LockBits(new Rectangle(0, 0, originalImage.Width, originalImage.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte[] originalData = new byte[originalHeight * originalWidth * bytesPerPixel];
            System.Runtime.InteropServices.Marshal.Copy(bmpDataOriginal.Scan0, originalData, 0, originalHeight * originalWidth * bytesPerPixel);

            //Creation de la Bitmap Panorama
            Bitmap bmpPanorama = new Bitmap(widthPanorama, heightPanorama);
            BitmapData bmpDataPanorama = bmpPanorama.LockBits(new Rectangle(0, 0, widthPanorama, heightPanorama), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int sizePanorama = heightPanorama * widthPanorama * bytesPerPixel;
            byte[] panoramaData = new byte[sizePanorama];
                       
            //On calcule la lookup table des sinus et cosinus au premier passage uniquement (ou si la taille change)
            if (initSize != widthPanorama)
            {
                PrepareLUTCoSi(widthPanorama, RayonCercle);
            }

            //Parallelisation des boucles en utilisant une expression lambda
            Parallel.For(0, widthPanorama, (x) =>
            //for(int x=0; x< widthPanorama; x++)
            {
                for (int y = 0; y < heightPanorama; y++)
                {
                    int xPos = (int)(originalHeight / 2 + (heightPanorama - 1 - y) * cosRTab[x]);
                    int yPos = (int)(originalWidth / 2 + (heightPanorama - 1 - y) * sinRTab[x]);
                    if (xPos < originalHeight && xPos > 0 && yPos < originalWidth && yPos > 0)
                    {
                        int panoramaDataPos = x * 3 + y * bmpDataPanorama.Stride;
                        int originalDataPos = xPos * 3 + yPos * bmpDataOriginal.Stride;
                        if (panoramaDataPos < panoramaData.Length - 3)
                        {
                            panoramaData[panoramaDataPos] = originalData[originalDataPos];        //Methode d'acces la plus rapide apres l'acces par pointeur
                            panoramaData[panoramaDataPos + 1] = originalData[originalDataPos + 1];
                            panoramaData[panoramaDataPos + 2] = originalData[originalDataPos + 2];
                        }
                    }
                    else
                    {

                    }
                    //if (y<heightPanorama/2)
                    //{
                    //    panoramaData[(x * 3 + y * bmpDataPanorama.Stride) ] = 255;
                    //    panoramaData[(x * 3 + y * bmpDataPanorama.Stride) + 1] = 0;
                    //    panoramaData[(x * 3 + y * bmpDataPanorama.Stride) + 2] = 0;
                    //}
                }
            });

            // This override copies the data back into the location specified 
            System.Runtime.InteropServices.Marshal.Copy(panoramaData, 0, bmpDataPanorama.Scan0, sizePanorama);

            bmpPanorama.UnlockBits(bmpDataPanorama);
            originalImage.UnlockBits(bmpDataOriginal);
            return bmpPanorama;
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
                cosRTab[i] = (float)Math.Cos((float)i / rayon + Math.PI/2);
                sinRTab[i] = (float)Math.Sin((float)i / rayon + Math.PI/2);
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
