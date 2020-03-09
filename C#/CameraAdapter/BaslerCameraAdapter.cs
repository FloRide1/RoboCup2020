using Basler.Pylon;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Utilities;

namespace CameraAdapter
{
    public class BaslerCameraAdapter
    {

        private Camera camera = null;
        //private PixelDataConverter converter = new PixelDataConverter();

        PixelDataConverter pxConvert = new PixelDataConverter();

        bool GrabOver = false;
        System.Configuration.Configuration configFile;

        List<PolarPoint> lidarPts = new List<PolarPoint>();

        public void CameraInit()
        {
            // Ask the camera finder for a list of camera devices.
            List<ICameraInfo> allCameras = CameraFinder.Enumerate();            
            camera = new Camera("22427616");

            if (camera != null)
            {
                // Print the model name of the camera. 
                Console.WriteLine("Using camera {0}.", camera.CameraInfo[CameraInfoKey.ModelName]);
                camera.CameraOpened += Basler.Pylon.Configuration.AcquireContinuous;
                camera.ConnectionLost += Camera_ConnectionLost;
                camera.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
                camera.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
                camera.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;

                configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                camera.Open();
                SetUpCamera();
            }
            //SetValue(PLCamera.AcquisitionMode.Continuous);
        }

        bool initialStart = true;
        private void SetUpCamera()
        {
            try
            {
                camera.StreamGrabber.Stop();
                if (initialStart)
                {
                    initialStart = false;
                    camera.Parameters[PLCamera.GevSCPSPacketSize].SetValue(8192);       //Réglage du packet Size à 8192
                    camera.Parameters[PLCamera.GevSCPD].SetValue(10000);                //Réglage de l'inter packet delay à 10000
                    camera.Parameters[PLCamera.AcquisitionFrameRateAbs].SetValue(30);   //Réglage du framerate en fps
                    camera.Parameters[PLCamera.GevHeartbeatTimeout].SetValue(5000);     //Réglage du heart beat (timout)
                    camera.Parameters[PLCamera.ReverseX].SetValue(true);
                    camera.Parameters[PLCamera.ReverseY].SetValue(true);
                    //Parametre d'acquisition image
                    camera.Parameters[PLCamera.ExposureTimeAbs].SetValue(1000);        //Réglage du temps d'exposition à 40Hz - 25.000 us
                    camera.Parameters[PLCamera.GainRaw].SetValue(300);
                    camera.Parameters[PLCamera.LightSourceSelector].SetValue(PLCamera.LightSourceSelector.Daylight6500K);
                }

                if (configFile.AppSettings.Settings["FishEyeCalibrated"].Value == "1")
                    LoadCalibratedFishEyeParameters();
                else
                    LoadDefaultFishEyeParameters();

                new Thread(KeepShot).Start();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            GrabOver = true;
        }

        Stopwatch sw = new Stopwatch();

        bool calibrationRequired = false;
        private void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            try
            {
                IGrabResult grabResult = e.GrabResult;
                if (grabResult.IsValid)
                {
                    if (GrabOver)
                    {
                        Bitmap bitmap = GrabResult2Bmp(grabResult);

                        if (calibrationRequired)
                        {
                            calibrationRequired = false;
                            CircleF circleFocus;
                            bitmap = DetectCercleObjectifFishEye(bitmap, 100, out circleFocus);
                            //Petite manip pour rendre le radius pair
                            int radius = (int)circleFocus.Radius;
                            configFile.AppSettings.Settings["FishEyeCalibratedRadius"].Value = radius.ToString();
                            configFile.AppSettings.Settings["FishEyeCalibratedOffsetX"].Value = ((int)(circleFocus.Center.X - circleFocus.Radius)).ToString();
                            configFile.AppSettings.Settings["FishEyeCalibratedOffsetY"].Value = ((int)(circleFocus.Center.Y - circleFocus.Radius)).ToString();
                            configFile.AppSettings.Settings["FishEyeCalibrated"].Value = "1";
                            configFile.Save();                            



                            SetUpCamera();
                        }

                        OnBitmapFishEyeImageReceived(bitmap);

                        //Conversion en panorama
                        sw.Restart();
                        Bitmap BitmapPanoramaImage = FishEyeToPanorama(bitmap);
                        sw.Stop();
                        Console.WriteLine("FishEyeToPano3:" + sw.ElapsedMilliseconds);

                        OnBitmapPanoramaImageReceived(BitmapPanoramaImage);
                        //OnBitmapImageReceived(bitmap);
                        //Image<Bgr, Byte> imageCV = new Image<Bgr, byte>(bitmap); //Image Class from Emgu.CV
                        //Mat mat = imageCV.Mat;
                        //OnOpenCvMatImageReceived(mat);                
                    }
                }
            }
            catch(Exception exc)
            {                
                Console.WriteLine(exc);
            }
        }

        public void CalibrateFishEye(object sender, EventArgs e)
        {
            calibrationRequired = true;
        }
        
        public void ResetFishEyeCalibration(object sender, EventArgs e)
        {
            configFile.AppSettings.Settings["FishEyeCalibratedRadius"].Value = "1080";
            configFile.AppSettings.Settings["FishEyeCalibratedOffsetX"].Value = "0";
            configFile.AppSettings.Settings["FishEyeCalibratedOffsetY"].Value = "0";
            configFile.AppSettings.Settings["FishEyeCalibrated"].Value = "0";
            configFile.Save();
            SetUpCamera();
        }

        public void StartAcquisition(object sender, EventArgs e)
        {
            KeepShot();
        }
        
        public void StopAcquisition(object sender, EventArgs e)
        {
            Stop();
        }

        private void LoadDefaultFishEyeParameters()
        {
            int radius = 500;
            camera.Parameters[PLCamera.CenterX].SetValue(false);
            camera.Parameters[PLCamera.CenterY].SetValue(false);
            camera.Parameters[PLCamera.OffsetX].SetValue(0);
            camera.Parameters[PLCamera.OffsetY].SetValue(0);
            camera.Parameters[PLCamera.Width].SetValue(2*radius);
            camera.Parameters[PLCamera.Height].SetValue(2*radius);
            int xOffset = 0;
            int yOffset = 0;
            xOffset = (1920 - 2 * radius) / 2 + xOffset;
            if (xOffset % 2 != 0)
                xOffset += 1;
            yOffset = (1200 - 2 * radius) / 2 + yOffset;
            if (yOffset % 2 != 0)
                yOffset += 1;
            camera.Parameters[PLCamera.OffsetX].SetValue(xOffset);
            camera.Parameters[PLCamera.OffsetY].SetValue(yOffset);
        }

        private void LoadCalibratedFishEyeParameters()
        {
            int radius = Int32.Parse(configFile.AppSettings.Settings["FishEyeCalibratedRadius"].Value);
            int xOffset = Int32.Parse(configFile.AppSettings.Settings["FishEyeCalibratedOffsetX"].Value);
            int yOffset = Int32.Parse(configFile.AppSettings.Settings["FishEyeCalibratedOffsetY"].Value);
            
            if (radius < 380)
                radius = 380;
            if (Math.Abs(xOffset) > 30)
                xOffset = 0;
            if (Math.Abs(yOffset) > 30)
                yOffset = 0;
            camera.Parameters[PLCamera.CenterX].SetValue(false);
            camera.Parameters[PLCamera.CenterY].SetValue(false);
            camera.Parameters[PLCamera.OffsetX].SetValue(0);
            camera.Parameters[PLCamera.OffsetY].SetValue(0);
            camera.Parameters[PLCamera.Width].SetValue(radius * 2);
            camera.Parameters[PLCamera.Height].SetValue(radius * 2);
            xOffset = (1920 - 2 * radius) / 2 + xOffset;
            if (xOffset % 2 != 0)
                xOffset += 1;
            yOffset = (1200 - 2 * radius) / 2 + yOffset;
            if (yOffset % 2 != 0)
                yOffset += 1;
            camera.Parameters[PLCamera.OffsetX].SetValue(xOffset);
            camera.Parameters[PLCamera.OffsetY].SetValue(yOffset);
        }

        private Bitmap DetectCercleObjectifFishEye(Bitmap bitmap, int seuilGris, out CircleF circle)
        {
            Mat imgOriginal = null;
            circle = new CircleF();
            try
            {
                Image<Bgr, Byte> imageCV = new Image<Bgr, byte>(bitmap); //Image Class from Emgu.CV
                imgOriginal = imageCV.Mat; //This is your Image converted to Mat
                Mat imgGray = new Mat();
                CvInvoke.CvtColor(imgOriginal, imgGray, ColorConversion.Bgr2Gray);
                Mat imgGray2 = new Mat();
                Mat imgBW = new Mat();
                CvInvoke.PyrDown(imgGray, imgGray2);
                CvInvoke.PyrUp(imgGray2, imgGray);
                CvInvoke.Threshold(imgGray, imgBW, 100, 150, ThresholdType.Otsu);

                //

                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                Mat hierarchy = new Mat();

                CvInvoke.FindContours(imgBW, contours, hierarchy, RetrType.Tree, ChainApproxMethod.ChainApproxTc89L1);

                CvInvoke.DrawContours(imgOriginal, contours, -1, new MCvScalar(0, 0, 255), 2);

                List<PointF> listPoints = new List<PointF>();

                int border = 2;
                for (int i = 0; i < contours.Size; i++)
                {
                    var c = contours[i];
                    for(int j = 0; j<c.Size; j++)
                    {
                        if(c[j].X> border && c[j].X < imgOriginal.Width- border && c[j].Y > border && c[j].Y < imgOriginal.Height - border)
                        listPoints.Add(c[j]);
                    }
                }
                               
                circle = CvInvoke.MinEnclosingCircle(listPoints.ToArray());

                CvInvoke.Circle(imgOriginal, new Point((int)circle.Center.X, (int)circle.Center.Y), (int)circle.Radius, new MCvScalar(0, 0, 255), 3);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return imgOriginal.Bitmap;
        }

        private void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            GrabOver = false;
        }

        private void Camera_ConnectionLost(object sender, EventArgs e)
        {
            camera.StreamGrabber.Stop();
            DestroyCamera();
        }

        public void OneShot()
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
                camera.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
        }

        public void KeepShot()
        {
            if (camera != null)
            {
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
        }

        public void Stop()
        {
            if (camera != null)
            {
                camera.StreamGrabber.Stop();
            }
        }

        Bitmap GrabResult2Bmp(IGrabResult grabResult)
        {
            Bitmap b = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
            BitmapData bmpData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, b.PixelFormat);
            pxConvert.OutputPixelFormat = PixelType.BGRA8packed;
            IntPtr bmpIntpr = bmpData.Scan0;
            pxConvert.Convert(bmpIntpr, bmpData.Stride * b.Height, grabResult);
            b.UnlockBits(bmpData);
            return b;
        }

        public void DestroyCamera()
        {
            if (camera != null)
            {
                camera.Close();
                camera.Dispose();
                camera = null;
            }
        }

        bool IsProcessingPanorama = false;
        public Bitmap FishEyeToPanorama(Bitmap originalImage)
        {
            if(!IsProcessingPanorama)
            //try
            {
                IsProcessingPanorama = true;
                //DOc fonction de manipulation des bitmap en natif
                //https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
                //Perf à peu près identiques à celles de OpenCV, mais sans avoir besoind e faire les conversions au départ.

                double panoramaGlobalScale = 0.35;
                double panoramaYScale = 1.85;

                //byte[,,] data = (byte[,,])originalImage.;
                int originalWidth = originalImage.Width;
                int originalHeight = originalImage.Height;
                byte bytesPerPixel = 3;

                //On suppose qu'on a la bonne taille de cercle de départ
                int RayonCercle = (int)(originalImage.Height / 2);

                int unprocessedRadius = 100;
                int heightPanorama = (int)((RayonCercle/*-unprocessedRadius*/) * panoramaGlobalScale * panoramaYScale);
                int widthPanorama = (int)(RayonCercle * 2 * Math.PI * panoramaGlobalScale);
                int bottomMargin = (int)(unprocessedRadius * panoramaGlobalScale * panoramaYScale);
                int heightPanoramaCropped = heightPanorama - bottomMargin;

                BitmapData bmpDataOriginal = originalImage.LockBits(new Rectangle(0, 0, originalImage.Width, originalImage.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                byte[] originalData = new byte[originalHeight * originalWidth * bytesPerPixel];
                System.Runtime.InteropServices.Marshal.Copy(bmpDataOriginal.Scan0, originalData, 0, originalHeight * originalWidth * bytesPerPixel);

                //Creation de la Bitmap Panorama
                Bitmap bmpPanorama = new Bitmap(widthPanorama, heightPanoramaCropped);
                BitmapData bmpDataPanorama = bmpPanorama.LockBits(new Rectangle(0, 0, widthPanorama, heightPanoramaCropped), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                int sizePanorama = heightPanoramaCropped * widthPanorama * bytesPerPixel;
                byte[] panoramaData = new byte[sizePanorama];

                //On calcule la lookup table des sinus et cosinus au premier passage uniquement (ou si la taille change)
                if (initSize != widthPanorama)
                {
                    PrepareLUTCoSi((int)(widthPanorama), RayonCercle, panoramaGlobalScale);
                }

                //Parallelisation des boucles en utilisant une expression lambda
                Parallel.For(0, widthPanorama, (x) =>
                //for(int x=0; x< widthPanorama; x++)
                {
                    //Parallel.For(0, heightPanorama, (j) =>
                    for (int j = 0; j < heightPanorama - bottomMargin; j++)
                    {
                        int y = heightPanorama - j - 1;
                        if (y > bottomMargin)
                        {
                            int xPos = (int)(originalHeight / 2 + (y / panoramaGlobalScale / panoramaYScale) * cosRTab[x]);
                            int yPos = (int)(originalWidth / 2 + (y / panoramaGlobalScale / panoramaYScale) * sinRTab[x]);
                            if (xPos < originalHeight && xPos > 0 && yPos < originalWidth && yPos > 0)
                            {
                                if (x == widthPanorama - 1)
                                    ;
                                int panoramaDataPos = (int)(x * 3 + (j) * bmpDataPanorama.Stride);
                                int originalDataPos = (int)(xPos * 3 + yPos * bmpDataOriginal.Stride);
                                if (panoramaDataPos < panoramaData.Length - 3)
                                {
                                    if (originalDataPos < originalData.Length)
                                    {
                                        panoramaData[panoramaDataPos] = originalData[originalDataPos];        //Methode d'acces la plus rapide apres l'acces par pointeur
                                        panoramaData[panoramaDataPos + 1] = originalData[originalDataPos + 1];
                                        panoramaData[panoramaDataPos + 2] = originalData[originalDataPos + 2];
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                });

                // This override copies the data back into the location specified 
                System.Runtime.InteropServices.Marshal.Copy(panoramaData, 0, bmpDataPanorama.Scan0, sizePanorama);

                bmpPanorama.UnlockBits(bmpDataPanorama);
                originalImage.UnlockBits(bmpDataOriginal);
                IsProcessingPanorama = false;
                return bmpPanorama;
            }
            else
            { 
                return null; 
            }
            //catch (Exception exc)
            //{
            //    throw new IndexOutOfRangeException(exc.ToString());
            //}
        }

        public Bitmap FishEyeToPanoramaLidar(Bitmap originalImage)
        {
            if (!IsProcessingPanorama)
            //try
            {
                IsProcessingPanorama = true;
                //DOc fonction de manipulation des bitmap en natif
                //https://stackoverflow.com/questions/1563038/fast-work-with-bitmaps-in-c-sharp
                //Perf à peu près identiques à celles de OpenCV, mais sans avoir besoind e faire les conversions au départ.

                double panoramaGlobalScale = 0.35;
                double panoramaYScale = 1.85;

                //byte[,,] data = (byte[,,])originalImage.;
                int originalWidth = originalImage.Width;
                int originalHeight = originalImage.Height;
                byte bytesPerPixel = 3;
                byte bytesPerPixelData = 4;

                //On suppose qu'on a la bonne taille de cercle de départ
                int RayonCercle = (int)(originalImage.Height / 2);

                int unprocessedRadius = 100;
                int heightPanorama = (int)((RayonCercle/*-unprocessedRadius*/) * panoramaGlobalScale * panoramaYScale);
                int widthPanorama = (int)(RayonCercle * 2 * Math.PI * panoramaGlobalScale);
                int bottomMargin = (int)(unprocessedRadius * panoramaGlobalScale * panoramaYScale);
                int heightPanoramaCropped = heightPanorama - bottomMargin;

                BitmapData bmpDataOriginal = originalImage.LockBits(new Rectangle(0, 0, originalImage.Width, originalImage.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                byte[] originalData = new byte[originalHeight * originalWidth * bytesPerPixel];
                System.Runtime.InteropServices.Marshal.Copy(bmpDataOriginal.Scan0, originalData, 0, originalHeight * originalWidth * bytesPerPixel);

                //Creation de la Bitmap Panorama
                Bitmap bmpPanorama = new Bitmap(widthPanorama, heightPanoramaCropped);
                BitmapData bmpDataPanorama = bmpPanorama.LockBits(new Rectangle(0, 0, widthPanorama, heightPanoramaCropped), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                int sizePanorama = heightPanoramaCropped * widthPanorama * bytesPerPixelData;
                byte[] panoramaData = new byte[sizePanorama];

                //On calcule la lookup table des sinus et cosinus au premier passage uniquement (ou si la taille change)
                if (initSize != widthPanorama)
                {
                    PrepareLUTCoSi((int)(widthPanorama), RayonCercle, panoramaGlobalScale);
                }

                //Parallelisation des boucles en utilisant une expression lambda
                Parallel.For(0, widthPanorama, (x) =>
                //for(int x=0; x< widthPanorama; x++)
                {
                    //Parallel.For(0, heightPanorama, (j) =>
                    for (int j = 0; j < heightPanorama - bottomMargin; j++)
                    {
                        int y = heightPanorama - j - 1;
                        if (y > bottomMargin)
                        {
                            int xPos = (int)(originalHeight / 2 + (y / panoramaGlobalScale / panoramaYScale) * cosRTab[x]);
                            int yPos = (int)(originalWidth / 2 + (y / panoramaGlobalScale / panoramaYScale) * sinRTab[x]);
                            if (xPos < originalHeight && xPos > 0 && yPos < originalWidth && yPos > 0)
                            {
                                if (x == widthPanorama - 1)
                                    ;
                                int panoramaDataPos = (int)(x * 4 + (j) * bmpDataPanorama.Stride);
                                int originalDataPos = (int)(xPos * 3 + yPos * bmpDataOriginal.Stride);
                                //trace une ligne d'horizon
                                if (y == (heightPanorama / 2 + bottomMargin))
                                {
                                    originalData[originalDataPos] = 255;
                                    originalData[originalDataPos + 1] = 255;
                                    originalData[originalDataPos + 2] = 255;
                                }
                                if (panoramaDataPos < panoramaData.Length - 4)
                                {
                                    if (originalDataPos < originalData.Length)
                                    {
                                        panoramaData[panoramaDataPos] = originalData[originalDataPos];        //Methode d'acces la plus rapide apres l'acces par pointeur
                                        panoramaData[panoramaDataPos + 1] = originalData[originalDataPos + 1];
                                        panoramaData[panoramaDataPos + 2] = originalData[originalDataPos + 2];
                                        int indexLidar = (int)((double)(widthPanorama - 1 - x) / widthPanorama * lidarPts.Count());
                                        if (indexLidar + lidarPts.Count() / 2 > +lidarPts.Count())
                                            indexLidar -= lidarPts.Count() / 2;
                                        else
                                            indexLidar += lidarPts.Count() / 2;
                                        panoramaData[panoramaDataPos + 3] = (byte)(255 - Math.Min(255, lidarPts[indexLidar].Distance * 50));
                                    }
                                }
                            }
                            else
                            {

                            }
                        }
                    }
                });

                // This override copies the data back into the location specified 
                System.Runtime.InteropServices.Marshal.Copy(panoramaData, 0, bmpDataPanorama.Scan0, sizePanorama);

                bmpPanorama.UnlockBits(bmpDataPanorama);
                originalImage.UnlockBits(bmpDataOriginal);
                IsProcessingPanorama = false;
                return bmpPanorama;
            }
            else
            {
                return null;
            }
            //catch (Exception exc)
            //{
            //    throw new IndexOutOfRangeException(exc.ToString());
            //}
        }

        float[] cosRTab = null;
        float[] sinRTab = null;
        int initSize = 0;
        void PrepareLUTCoSi(int length, int rayon, double scale)
        {
            initSize = length;
            bool recalculateLUT = false;
            if (cosRTab == null || sinRTab == null)
            {
                recalculateLUT = true;
            }
            else if(cosRTab.Length != length || sinRTab.Length != length)
            {
                recalculateLUT = true;
            }
            if (recalculateLUT)
            {
                cosRTab = new float[length];
                sinRTab = new float[length];
                for (int i = 0; i < length; i++)
                {
                    cosRTab[i] = (float)Math.Cos((float)i / scale / rayon + Math.PI / 2);
                    sinRTab[i] = (float)Math.Sin((float)i / scale / rayon + Math.PI / 2);
                }
            }
        }

        //Output events
        //public delegate void CameraImageEventHandler(object sender, CameraImageArgs e);
        //public event EventHandler<CameraImageArgs> CameraImageEvent;

        //public virtual void OnCameraImageReceived(Bitmap image)
        //{
        //    var handler = CameraImageEvent;
        //    if (handler != null)
        //    {
        //        handler(this, new CameraImageArgs { ImageBmp = image });
        //    }
        //}


        //public delegate void OpenCvMatImageEventHandler(object sender, CameraImageArgs e);
        //public event EventHandler<OpenCvMatImageArgs> OpenCvMatImageEvent;

        //public virtual void OnOpenCvMatImageReceived(Mat mat)
        //{
        //    var handler = OpenCvMatImageEvent;
        //    if (handler != null)
        //    {
        //        handler(this, new OpenCvMatImageArgs { Mat = mat, Descriptor = "ImageFromCamera" });
        //    }
        //}

        public event EventHandler<BitmapImageArgs> BitmapFishEyeImageEvent;
        public virtual void OnBitmapFishEyeImageReceived(Bitmap bmp)
        {
            var handler = BitmapFishEyeImageEvent;
            if (handler != null)
            {
                handler(this, new BitmapImageArgs { Bitmap = bmp, Descriptor = "FishEyeImageFromCamera" });
            }
        }

        public event EventHandler<BitmapImageArgs> BitmapPanoramaImageEvent;
        public virtual void OnBitmapPanoramaImageReceived(Bitmap bmp)
        {
            var handler = BitmapPanoramaImageEvent;
            if (handler != null)
            {
                handler(this, new BitmapImageArgs { Bitmap = bmp, Descriptor = "PanoramaImageFromCamera" });
            }
        }
    }
}
