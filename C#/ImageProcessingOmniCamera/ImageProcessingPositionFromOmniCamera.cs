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

namespace ImageProcessingOmniCamera
{
    public class ImageProcessingPositionFromOmniCamera
    {
        public Mat TerrainTheoriqueVertical = new Mat();
        double scaleTerrain = 20;

        public ImageProcessingPositionFromOmniCamera()
        {
            //TerrainTheoriqueVertical = GenerateImageTerrain();

        }

        //Event de réception d'imae depuis un flux
        //Bitmap lastImage;
        //public void CameraImageEventCB(object sender, EventArgsLibrary.CameraImageArgs e)
        //{
        //    lastImage = e.ImageBmp;
        //}
                
        public void ProcessOpenCvMatImage(object sender, EventArgsLibrary.OpenCvMatImageArgs e)
        {
            Mat initialMat = e.Mat;
            OnOpenCvMatImageProcessedReady(initialMat, "ImageFromCameraViaProcessing");
            
            //Découpage de l'image
            int RawMatCroppedSize = 300;
            int cropOffsetX = 0;
            int cropOffsetY = 0;
            Range rgX = new Range(initialMat.Width / 2 - RawMatCroppedSize / 2 + cropOffsetX, initialMat.Width / 2 + RawMatCroppedSize / 2 + cropOffsetX);
            Range rgY = new Range(initialMat.Height / 2 - RawMatCroppedSize / 2 + cropOffsetY, initialMat.Height / 2 + RawMatCroppedSize / 2 + cropOffsetY);
            Mat RawMatCropped = new Mat(initialMat, rgY, rgX);

            //Conversion en HSV
            Mat HsvMatCropped = new Mat();
            CvInvoke.CvtColor(RawMatCropped, HsvMatCropped, ColorConversion.Bgr2Hsv);
            OnOpenCvMatImageProcessedReady(HsvMatCropped, "ImageDebug3");
            OnOpenCvMatImageProcessedReady(TerrainTheoriqueVertical, "ImageDebug4");
        }

        private void CalculatePositioning(Mat RawMat, out Mat RawMatCropped, out Mat OtherZonesInGreen, out Mat TerrainFilledGreenRotated, out Mat TerrainTheoriqueDisplay, out double coefficientCorrelation)
        {
            //Découpage de l'image
            int RawMatCroppedSize = 300;
            int cropOffsetX = 0;
            int cropOffsetY = 0;
            Range rgX = new Range(RawMat.Width / 2 - RawMatCroppedSize / 2 + cropOffsetX, RawMat.Width / 2 + RawMatCroppedSize / 2 + cropOffsetX);
            Range rgY = new Range(RawMat.Height / 2 - RawMatCroppedSize / 2 + cropOffsetY, RawMat.Height / 2 + RawMatCroppedSize / 2 + cropOffsetY);
            RawMatCropped = new Mat(RawMat, rgY, rgX);

            //Test en utilisant une image théorique
            //Range rgX = new Range(TerrainTheorique.Width / 2 - tailleCroppedImage / 2 + cropOffsetX, TerrainTheorique.Width / 2 + tailleCroppedImage / 2 + cropOffsetX);
            //Range rgY = new Range(TerrainTheorique.Height / 2 - tailleCroppedImage / 2 + cropOffsetY, TerrainTheorique.Height / 2 + tailleCroppedImage / 2 + cropOffsetY);
            //Mat cropRawMat = new Mat(TerrainTheorique, rgY, rgX);

            //UpdateValues(RawMatCropped.Bitmap, 1);  //TODO mise à jour des affichages

            //Initialisation de la mesure du temps de calcul
            Stopwatch watch = Stopwatch.StartNew();

            //Conversion en HSV
            Mat HsvMatCropped = new Mat();
            CvInvoke.CvtColor(RawMatCropped, HsvMatCropped, ColorConversion.Bgr2Hsv);

            //Filtrage du vert du terrain
            int HueOrange = 24;
            int HueVert = 180;


            Mat maskVert = new Mat();
            int HTolVert = 20;
            int HInfVert = Math.Max(0, HueVert - HTolVert);
            int HSupVert = Math.Min(360, HueVert + HTolVert);
            int SInfVert = 0;
            int SSupVert = 255;
            int VInfVert = 80;
            int VSupVert = 255;
            CvInvoke.InRange(HsvMatCropped, new ScalarArray(new MCvScalar(HInfVert / 2, SInfVert, VInfVert)), new ScalarArray(new MCvScalar(HSupVert / 2, SSupVert, VSupVert)), maskVert);
            //Mat maskBlanc = new Mat();
            //int HInfBlanc = 0;
            //int HSupBlanc = 360;
            //int SInfBlanc = 0;
            //int SSupBlanc = 255;
            //int VInfBlanc = 230;
            //int VSupBlanc = 255;
            //CvInvoke.InRange(HsvMatCropped, new ScalarArray(new MCvScalar(HInfBlanc / 2, SInfBlanc, VInfBlanc)), new ScalarArray(new MCvScalar(HSupBlanc / 2, SSupBlanc, VSupBlanc)), maskBlanc);
            var maskWhiteGreen = /*maskBlanc + */maskVert;

            int interations = 7;
            //var element = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(4, 4), new Point(-1, -1));
            int erosionSize = 2;
            int dilatationSize = 2;
            var elementErosion = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2 * erosionSize + 1, 2 * erosionSize + 1), new Point(erosionSize, erosionSize));
            var elementDilatation = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(2 * dilatationSize + 1, 2 * dilatationSize + 1), new Point(dilatationSize, dilatationSize));

            //CvInvoke.Erode(maskVertBlanc, maskVertBlanc, elementErosion, new Point(-1, -1), interations, BorderType.Default, default(MCvScalar));
            CvInvoke.Dilate(maskWhiteGreen, maskWhiteGreen, elementDilatation, new Point(-1, -1), interations + 2, BorderType.Default, default(MCvScalar));
            CvInvoke.Erode(maskWhiteGreen, maskWhiteGreen, elementErosion, new Point(-1, -1), interations, BorderType.Default, default(MCvScalar));


            Mat maskNotWhiteGreen = new Mat();
            CvInvoke.BitwiseNot(maskWhiteGreen, maskNotWhiteGreen);

            //Filtrage de l'image pour ne garder que le terrain
            Mat TerrainFilteredGreenWhite = new Mat();
            CvInvoke.BitwiseAnd(RawMatCropped, RawMatCropped, TerrainFilteredGreenWhite, maskWhiteGreen);
            var img = TerrainFilteredGreenWhite.ToImage<Bgr, byte>();
            //img._EqualizeHist();
            //img.Laplace(15);
            //img._GammaCorrect(5);
            TerrainFilteredGreenWhite = img.Mat;
            //var newImage = TerrainFilteredGreenWhite.Convert(b => SaturateCast(alpha * b + beta));
            //Validé jusqu'ici

            //Détectiond de contours par Canny
            double cannyThreshold = 180.0;
            double cannyThresholdLinking = 200.0;
            UMat cannyEdges = new UMat();
            CvInvoke.Canny(RawMatCropped, cannyEdges, cannyThreshold, cannyThresholdLinking);

            //Retrait de ce qui n'est pas terrain dans l'image et dans les contours de Canny
            Mat CannyTerrain = new Mat();
            CvInvoke.BitwiseAnd(cannyEdges, cannyEdges, CannyTerrain, maskWhiteGreen);

            //Filtrage du noir
            Mat maskBlack = new Mat();
            Mat maskNotBlack = new Mat();
            int HInfBlack = 0;
            int HSupBlack = 360;
            int SInfBlack = 0;
            int SSupBlack = 255;
            int VInfBlack = 0;
            int VSupBlack = 100;

            //Sélection des zones noires
            CvInvoke.InRange(HsvMatCropped, new ScalarArray(new MCvScalar(HInfBlack / 2, SInfBlack, VInfBlack)), new ScalarArray(new MCvScalar(HSupBlack / 2, SSupBlack, VSupBlack)), maskBlack);
            //Dilatation des zones noires pour "manger" les bords
            CvInvoke.Dilate(maskBlack, maskBlack, elementDilatation, new Point(-1, -1), iterations: 1, BorderType.Default, default(MCvScalar));

            CvInvoke.BitwiseNot(maskBlack, maskNotBlack);

            //On crée une image remplie de vert
            Mat MatGreen = new Mat(RawMatCropped.Size, DepthType.Cv8U, 3);
            MatGreen.SetTo(new Bgr(Color.Green).MCvScalar);


            //Suppression des bords de zones noires dans l'image des contours de Canny
            Mat CannyMapWithoutBlack = new Mat();
            CvInvoke.BitwiseAnd(CannyTerrain, CannyTerrain, CannyMapWithoutBlack, maskNotBlack);


            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               CannyMapWithoutBlack,
               1, //Distance resolution in pixel-related units
               Math.PI / 90.0, //Angle resolution measured in radians.
               20, //threshold
               20, //min Line width
               10); //gap between lines

            #region draw lines
            foreach (LineSegment2D line in lines)
            {
                CvInvoke.Line(RawMatCropped, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 2);
            }
            #endregion

            //Détermination de la liste des angles de chaque segment dans l'image
            List<double> listAngleSegments = new List<double>();
            foreach (var segment in lines)
            {
                double angle = Math.Atan2(segment.P2.Y - segment.P1.Y, segment.P2.X - segment.P1.X);
                angle = Utilities.Toolbox.ModuloPiDivTwoAngleRadian(angle);
                listAngleSegments.Add(angle);
            }

            //Détermination de l'angle orientation terrain : angle médian de la liste des angles segments
            listAngleSegments.Sort();
            double bestAngle = 0;
            if (listAngleSegments.Count > 0)
            {
                bestAngle = listAngleSegments[(int)(listAngleSegments.Count / 2)];
                //Calcul de l'indice de confiance TODO
            }

            Console.WriteLine("Angle Terrain : " + Utilities.Toolbox.RadToDeg(bestAngle));

            Mat rotMatrix = new Mat();
            Mat TerrainFilteredGreenRotated = new Mat();

            //Rotation du template de terrain récupéré
            CvInvoke.GetRotationMatrix2D(new PointF((float)TerrainFilteredGreenWhite.Width / 2, (float)RawMatCropped.Height / 2), Toolbox.RadToDeg(bestAngle), 1.0, rotMatrix);
            CvInvoke.WarpAffine(TerrainFilteredGreenWhite, TerrainFilteredGreenRotated, rotMatrix, new Size(RawMatCropped.Width, RawMatCropped.Height));

            //Rotation du mask du template de terrain récupéré
            Mat maskWhiteGreenRotated = new Mat();
            CvInvoke.WarpAffine(maskWhiteGreen, maskWhiteGreenRotated, rotMatrix, new Size(RawMatCropped.Width, RawMatCropped.Height));

            //Remplacement des zones noires par des zones vertes
            Mat maskBlack2 = new Mat();
            var lowerBound = new ScalarArray(new Bgr(Color.FromArgb(0, 0, 0)).MCvScalar);
            var upperBound = new ScalarArray(new Bgr(Color.FromArgb(255, 50, 255)).MCvScalar);
            CvInvoke.InRange(TerrainFilteredGreenRotated, lowerBound, upperBound, maskBlack2);

            //On crée une image avec les zones non vertes précédentes remplies en vert
            OtherZonesInGreen = new Mat();
            CvInvoke.BitwiseAnd(MatGreen, MatGreen, OtherZonesInGreen, maskBlack2);

            TerrainFilledGreenRotated = new Mat();
            TerrainFilledGreenRotated = OtherZonesInGreen + TerrainFilteredGreenRotated;// + ;
                                                                                        //CvInvoke.BitwiseOr(BlackZonesInGreen, TerrainFilteredGreen, Te//rrainFilledGreen);
                                                                                        ////Correlation Terrain théorique vertical !!!!
                                                                                        //var w = (TerrainTheoriqueVertical.Width - RawMatCropped.Width) + 1;
                                                                                        //var h = (TerrainTheoriqueVertical.Height - RawMatCropped.Height) + 1;
            Mat resultCorrelTerrainVertical = new Mat();
            Mat TerrainFilledGreenRotated0degre = TerrainFilledGreenRotated.Clone();

            CvInvoke.MatchTemplate(TerrainTheoriqueVertical, TerrainFilledGreenRotated0degre, resultCorrelTerrainVertical,
                TemplateMatchingType.CcorrNormed); // throws exception
            double minValV = 0, maxValV = 0;
            Point minLocV = new Point();
            Point maxLocV = new Point();
            CvInvoke.MinMaxLoc(resultCorrelTerrainVertical, ref minValV, ref maxValV, ref minLocV, ref maxLocV);

            Mat resultCorrelTerrainHorizontal = new Mat();
            Mat TerrainFilledGreenRotated90 = new Mat();
            CvInvoke.Rotate(TerrainFilledGreenRotated, TerrainFilledGreenRotated90, RotateFlags.Rotate90Clockwise);
            CvInvoke.MatchTemplate(TerrainTheoriqueVertical, TerrainFilledGreenRotated90,
                resultCorrelTerrainHorizontal,
                TemplateMatchingType.CcorrNormed,
                maskWhiteGreenRotated); // throws exception
            double minValH = 0, maxValH = 0;
            Point minLocH = new Point();
            Point maxLocH = new Point();
            CvInvoke.MinMaxLoc(resultCorrelTerrainHorizontal, ref minValH, ref maxValH, ref minLocH, ref maxLocH);

            Point maxLoc = new Point();
            if (maxValV > maxValH)
            {
                maxLoc = maxLocV;
                coefficientCorrelation = maxValV;
            }
            else
            {
                maxLoc = maxLocH;
                coefficientCorrelation = maxValH;
            }

            //Affichage final
            //On clone le terrain théorique pour faire l'affichage sans détériorer le terrain de référence
            TerrainTheoriqueDisplay = TerrainTheoriqueVertical.Clone();

            Mat MorceauTerrainAcquisRotated = new Mat(TerrainTheoriqueVertical.Size, DepthType.Cv8U, 3);

            //System.Drawing.Rectangle roi = new System.Drawing.Rectangle(20, 20, TerrainFilteredGreenRotated.Width, TerrainFilteredGreenRotated.Height);
            //Mat dstROI = new Mat(MorceauTerrainAcquisRotated, roi);
            System.Drawing.Rectangle roi2 = new System.Drawing.Rectangle(maxLoc.X, maxLoc.Y,
                TerrainFilledGreenRotated.Width, TerrainFilledGreenRotated.Height);
            Mat dstROI2 = new Mat(TerrainTheoriqueDisplay, roi2);
            //TerrainFilledGreenRotated.CopyTo(dstROI);
            if (maxValV > maxValH)
            {
                TerrainFilledGreenRotated0degre.CopyTo(dstROI2);
            }
            else
            {
                TerrainFilledGreenRotated90.CopyTo(dstROI2);
            }
            //CvInvoke. MorceauTerrainAcquisRotated

            //CvInvoke.Rectangle(TerrainTheoriqueDisplay, new System.Drawing.Rectangle(maxLoc.X, maxLoc.Y, 
            //    TerrainFilteredGreenRotated.Width, TerrainFilteredGreenRotated.Height), new MCvScalar(Color.Yellow.ToArgb()),3);

            //InformationString = 
            //    "Best correlation X : " + (minLoc.X+imgRotated.)+ " // Y : " + (yIndex-13) + " Score : "+minGlobal;

            long ComputeTime = watch.ElapsedMilliseconds;
        }

        //Mat GenerateImageTerrain()
        //{
        //    //Tracé du terrain
        //    List<Segment> listSegmentsTerrainRefTerrain = new List<Segment>();
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-7, 11), new Point3D(7, 11)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-7, -11), new Point3D(7, -11)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-7, 11), new Point3D(-7, -11)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(7, -11), new Point3D(7, 11)));

        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(1.95, 11), new Point3D(1.95, 10.25)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-1.95, 11), new Point3D(-1.95, 10.25)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(1.95, -11), new Point3D(1.95, -10.25)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-1.95, -11), new Point3D(-1.95, -10.25)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-1.95, -10.25), new Point3D(1.95, -10.25)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-1.95, 10.25), new Point3D(1.95, 10.25)));

        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(3.45, 11), new Point3D(3.45, 8.75)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-3.45, 11), new Point3D(-3.45, 8.75)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(3.45, -11), new Point3D(3.45, -8.75)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-3.45, -11), new Point3D(-3.45, -8.75)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-3.45, -8.75), new Point3D(3.45, -8.75)));
        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-3.45, 8.75), new Point3D(3.45, 8.75)));

        //    listSegmentsTerrainRefTerrain.Add(new Segment(new Point3D(-7, 0), new Point3D(7, 0)));

        //    double widthTerrainInMeter = 14;
        //    double heightTerrainInMeter = 22;
        //    double margin = 10;
        //    double largeurLigne = 0.20;

        //    int widthTerrain = (int)(scaleTerrain * widthTerrainInMeter);
        //    int heightTerrain = (int)(scaleTerrain * heightTerrainInMeter);

        //    int width = (int)(scaleTerrain * (widthTerrainInMeter + 2 * margin));
        //    int height = (int)(scaleTerrain * (heightTerrainInMeter + 2 * margin));

        //    Mat ImageTerrainTheorique = new Mat(height, width, DepthType.Cv8U, 3);

        //    //Fond noir
        //    CvInvoke.Rectangle(ImageTerrainTheorique, new System.Drawing.Rectangle(0, 0, width, height), new Bgr(Color.Red).MCvScalar, -1);
        //    //Terrain
        //    CvInvoke.Rectangle(ImageTerrainTheorique, new System.Drawing.Rectangle((int)(scaleTerrain * margin), (int)(scaleTerrain * margin), widthTerrain, heightTerrain), new Bgr(Color.Green).MCvScalar, -1);
        //    //Cercle central
        //    CvInvoke.Circle(ImageTerrainTheorique, new Point(width / 2, height / 2), (int)(scaleTerrain * 2), new Bgr(Color.White).MCvScalar, (int)(scaleTerrain * largeurLigne));

        //    //Segments du terrain
        //    foreach (var seg in listSegmentsTerrainRefTerrain)
        //    {
        //        CvInvoke.Line(ImageTerrainTheorique, ChangeCoordTerrainToImage(seg.A, width / 2, height / 2), ChangeCoordTerrainToImage(seg.B, width / 2, height / 2), new Bgr(Color.White).MCvScalar, (int)(scaleTerrain * largeurLigne));
        //    }

        //    //Corners
        //    CvInvoke.Circle(ImageTerrainTheorique, new Point((width + widthTerrain) / 2, (height + heightTerrain) / 2), (int)(0.75 * scaleTerrain), new Bgr(Color.White).MCvScalar, (int)(scaleTerrain * largeurLigne));
        //    CvInvoke.Circle(ImageTerrainTheorique, new Point((width - widthTerrain) / 2, (height + heightTerrain) / 2), (int)(0.75 * scaleTerrain), new Bgr(Color.White).MCvScalar, (int)(scaleTerrain * largeurLigne));
        //    CvInvoke.Circle(ImageTerrainTheorique, new Point((width - widthTerrain) / 2, (height - heightTerrain) / 2), (int)(0.75 * scaleTerrain), new Bgr(Color.White).MCvScalar, (int)(scaleTerrain * largeurLigne));
        //    CvInvoke.Circle(ImageTerrainTheorique, new Point((width + widthTerrain) / 2, (height - heightTerrain) / 2), (int)(0.75 * scaleTerrain), new Bgr(Color.White).MCvScalar, (int)(scaleTerrain * largeurLigne));

        //    List<Segment> listGreenMaskTerrainRefTerrain = new List<Segment>();
        //    double shift = 0.5;
        //    listGreenMaskTerrainRefTerrain.Add(new Segment(new Point3D(-7, 11 + (shift + largeurLigne / 2.0)), new Point3D(7, 11 + (shift + largeurLigne / 2))));
        //    listGreenMaskTerrainRefTerrain.Add(new Segment(new Point3D(-7, -11 - (shift + largeurLigne / 2.0)), new Point3D(7, -11 - (shift + largeurLigne / 2))));
        //    listGreenMaskTerrainRefTerrain.Add(new Segment(new Point3D(-7 - (shift + largeurLigne / 2.0), 11), new Point3D(-7 - (shift + largeurLigne / 2.0), -11)));
        //    listGreenMaskTerrainRefTerrain.Add(new Segment(new Point3D(7 + (shift + largeurLigne / 2.0), -11), new Point3D(7 + (shift + largeurLigne / 2.0), 11)));

        //    foreach (var seg in listGreenMaskTerrainRefTerrain)
        //    {
        //        CvInvoke.Line(ImageTerrainTheorique, ChangeCoordTerrainToImage(seg.A, width / 2, height / 2), ChangeCoordTerrainToImage(seg.B, width / 2, height / 2), new Bgr(Color.Green).MCvScalar, (int)(scaleTerrain * 2 * shift));
        //    }

        //    //CvInvoke.Rectangle(ImageTerrainTheorique, new System.Drawing.Rectangle(0, 0, width, height), new Bgr(Color.Green).MCvScalar, -1);
        //    //CvInvoke.Rectangle(ImageTerrainTheorique, new System.Drawing.Rectangle(0, 0, width, height), new Bgr(Color.Green).MCvScalar, -1);
        //    //CvInvoke.Rectangle(ImageTerrainTheorique, new System.Drawing.Rectangle(0, 0, width, height), new Bgr(Color.Green).MCvScalar, -1);


        //    return ImageTerrainTheorique;
        //}
        //Point ChangeCoordTerrainToImage(Point3D pt, double offsetX, double offsetY)
        //{
        //    return new Point((int)(pt.X * scaleTerrain + offsetX), (int)(pt.Y * scaleTerrain + offsetY));
        //}

        //************************************************************************ Events de sortie *****************************************************************************//
        // Event position dans l'image calculée
        public delegate void PositionEventHandler(object sender, PositionArgs e);
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
        public delegate void OpenCvMatImageEventHandler(object sender, OpenCvMatImageArgs e);
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
