using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alturos;
using Alturos.Yolo;
using Alturos.Yolo.Model;
using Emgu.CV;
using Emgu.CV.Structure;
using EventArgsLibrary;

namespace YoloObjectDetector
{
    public class YoloObjectDetector
    {
        YoloConfiguration yoloConfig = null;
        YoloWrapper wrap = null;
        bool useYoloV3 = true;
        bool useTiny = false;
        string defaultConfigurationPath = "..\\..\\..\\..\\_YoloConfiguration\\MSLRobotBallonButDetection\\";//
        string yoloCFGFileName = "yolov3-MSLRobotBallonButDetection.cfg";
        string yoloWeightsFileName = "yolov3-MSLRobotBallonButDetection_final.weights";
        string yoloNamesFileName = "MSLRobotBallonButDetection.names";
        
        public YoloObjectDetector(bool ignoreGPU)
        {
            if (useYoloV3)
            {
                defaultConfigurationPath = "..\\..\\..\\..\\_YoloConfiguration\\MSLRobotBallonButDetection\\";//
                yoloCFGFileName = "yolov3-MSLRobotBallonButDetection.cfg";
                yoloWeightsFileName = "yolov3-MSLRobotBallonButDetection_final.weights";
                yoloNamesFileName = "MSLRobotBallonButDetection.names";
            }
            else if(useTiny)
            {
                defaultConfigurationPath = "..\\..\\..\\..\\_YoloConfiguration\\TinyYolo\\";//
                yoloCFGFileName = "yolov2-tiny-voc.cfg";
                yoloWeightsFileName = "yolov2-tiny-voc.weights";
                yoloNamesFileName = "voc.names";
            }
            // try
            {
                if (this.wrap != null)
                {
                    this.wrap.Dispose();
                }

                string[] files = Directory.GetFiles(defaultConfigurationPath);
                yoloConfig = new YoloConfiguration(defaultConfigurationPath+yoloCFGFileName, defaultConfigurationPath+yoloWeightsFileName,defaultConfigurationPath+ yoloNamesFileName);
                wrap = new YoloWrapper(yoloConfig,ignoreGPU);       //By default GPU 0 is used if used

                var detectionSystemDetail = string.Empty;
                if (!string.IsNullOrEmpty(this.wrap.EnvironmentReport.GraphicDeviceName))
                {
                    detectionSystemDetail = $"({this.wrap.EnvironmentReport.GraphicDeviceName})";
                }
                Console.WriteLine(detectionSystemDetail);

            }
            //catch
            {
               // throw new Exception("Couldn't open yolo");
            }


            new Thread(DetectAndLabel).Start();

        }
        public void Dispose()
        {
            wrap?.Dispose();
        }

        bool newImageAvailable = true;
        Bitmap currentImage;
        string currentImageDescriptor;
        public void SetNewYoloImageToProcess(object sender, EventArgsLibrary.BitmapImageArgs e)
        {
            currentImage = e.Bitmap;
            currentImageDescriptor = e.Descriptor;
        }
        public void DetectAndLabel()
        {
            while (true)
            {
                if (newImageAvailable && currentImage!= null)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    YoloItemListInfo yoloOutput = DetectAndLabelAngGetItem(currentImage);
                    sw.Stop();
                    Console.WriteLine("Temps de calcul Yolo : " + sw.Elapsed.TotalMilliseconds);

                    //sortie items uniquement
                    OnYoloItemList(yoloOutput); 

                    //sortie bitmap et items entourés
                    YoloBitmapItemInfo yoloBitmapInfo = new YoloBitmapItemInfo(DrawBorder2Image(currentImage, yoloOutput.items), yoloOutput.items);
                    currentImageDescriptor = "ImageDebug3";
                    OnYoloImageProcessedAndLabelledReady(yoloBitmapInfo.bmp, currentImageDescriptor); 

                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }
        //convert image to bytearray
        public byte[] imgToByteArray(Image img)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                img.Save(mStream, img.RawFormat);
                return mStream.ToArray();
            }
        }
        static public byte[] BitmapDataFromBitmap(Bitmap objBitmap, ImageFormat imageFormat)

        {

            MemoryStream ms = new MemoryStream();

            objBitmap.Save(ms, imageFormat);

            return (ms.GetBuffer());

        }
        //Methode de detection
        private YoloMatItemInfo DetectAndLabelAngGetItem(Mat mat)
        {

            if (this.wrap == null)
            {
                return null;
            }

            var memoryTransfer = true;

            //var imageInfo = this.GetCurrentImage();
            //var imageData = File.ReadAllBytes(imageInfo.Path);

            var sw = new Stopwatch();
            sw.Start();
            List<YoloItem> items = null;
            if (memoryTransfer)
            {

                byte[] byteArray = BitmapDataFromBitmap((mat.ToImage<Bgr, Byte>().ToBitmap()), ImageFormat.Bmp);
                items = this.wrap.Detect(byteArray).ToList();
            }
            else
            {
                //items = this.wrap.Detect(imageInfo.Path).ToList();
            }
            sw.Stop();
            return new YoloMatItemInfo(DrawBorder2Image(mat, items),items);
        }

        private YoloItemListInfo DetectAndLabelAngGetItem(Bitmap bmp)
        {

            if (this.wrap == null)
            {
                return null;
            }

            var memoryTransfer = true;

            //var imageInfo = this.GetCurrentImage();
            //var imageData = File.ReadAllBytes(imageInfo.Path);

            var sw = new Stopwatch();
            sw.Start();
            List<YoloItem> items = null;
            if (memoryTransfer)
            {

                byte[] byteArray = BitmapDataFromBitmap(bmp, ImageFormat.Bmp);
                items = this.wrap.Detect(byteArray).ToList();
            }
            else
            {
                //items = this.wrap.Detect(imageInfo.Path).ToList();
            }
            sw.Stop();
            return new YoloItemListInfo(items);
        }

        private Mat DetectAndLabel(Mat mat)
        {
            if (this.wrap == null)
            {
                return null;
            }

            var memoryTransfer = true;

            //var imageInfo = this.GetCurrentImage();
            //var imageData = File.ReadAllBytes(imageInfo.Path);

            var sw = new Stopwatch();
            sw.Start();
            List<YoloItem> items=null;
            if (memoryTransfer)
            {

                byte[] byteArray = BitmapDataFromBitmap((mat.ToImage<Bgr, Byte>().ToBitmap()),ImageFormat.Bmp);
                items = this.wrap.Detect(byteArray).ToList();
            }
            else
            {
                //items = this.wrap.Detect(imageInfo.Path).ToList();
            }
            sw.Stop();
            //this.groupBoxResult.Text = $"Result [ processed in {sw.Elapsed.TotalMilliseconds:0} ms ]";
            //this.richTextBoxConsole.Text += this.groupBoxResult.Text + '\n';
            //this.dataGridViewResult.DataSource = items;
            return DrawBorder2Image(mat, items);
            
        }

        private Mat DrawBorder2Image(Mat mat,List<YoloItem> items, YoloItem selectedItem = null)
        {
            //Load the image(probably from your stream)
            Bitmap image = mat.Bitmap;
            Image<Bgr, Byte> im = null;
            using (var canvas = Graphics.FromImage(image))
            {
                // Modify the image using g here... 
                // Create a brush with an alpha value and use the g.FillRectangle function
                foreach (var item in items)
                {
                    var x = item.X;
                    var y = item.Y;
                    var width = item.Width;
                    var height = item.Height;

                    using (var overlayBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 102)))
                    using (var pen = this.GetBrush(item.Confidence, image.Width))
                    {
                        //if (item.Equals(selectedItem))
                        //{
                        //    canvas.FillRectangle(overlayBrush, x, y, width, height);
                        //}

                        canvas.DrawRectangle(pen, x, y, width, height);
                        canvas.Flush();
                    }
                }
                im = new Image<Bgr, Byte>(image);
            }
            
            return im.Mat;
        }

        private Bitmap DrawBorder2Image(Bitmap bmp, List<YoloItem> items, YoloItem selectedItem = null)
        {
            //Load the image(probably from your stream)
            Image<Bgr, Byte> im = null;
            using (var canvas = Graphics.FromImage(bmp))
            {
                // Modify the image using g here... 
                // Create a brush with an alpha value and use the g.FillRectangle function
                foreach (var item in items)
                {
                    var x = item.X;
                    var y = item.Y;
                    var width = item.Width;
                    var height = item.Height;

                    using (var overlayBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 102)))
                    using (var pen = this.GetBrush(item.Confidence, bmp.Width))
                    {
                        //if (item.Equals(selectedItem))
                        //{
                        //    canvas.FillRectangle(overlayBrush, x, y, width, height);
                        //}

                        canvas.DrawRectangle(pen, x, y, width, height);
                        canvas.Flush();
                    }
                }
               // im = new Image<Bgr, Byte>(bmp);
            }

            return bmp;
        }

        private Pen GetBrush(double confidence, int width)
        {
            var size = width / 300; //100

            if (confidence > 0.5)
            {
                return new Pen(Brushes.Red, size);
            }
            else if (confidence > 0.2 && confidence <= 0.5)
            {
                return new Pen(Brushes.Red, size);
            }

            return new Pen(Brushes.Transparent, size);
        }

        public event EventHandler<OpenCvMatImageArgs> OnYoloImageProcessedAndLabelledEvent;
        public virtual void OnYoloImageProcessedAndLabelledReady(Mat mat, string descriptor)
        {
            var handler = OnYoloImageProcessedAndLabelledEvent;
            if (handler != null)
            {
                handler(this, new OpenCvMatImageArgs { Mat = mat, Descriptor = descriptor });
            }
        }

        public event EventHandler<BitmapImageArgs> OnYoloBitmapImageProcessedAndLabelledEvent;
        public virtual void OnYoloImageProcessedAndLabelledReady(Bitmap bmp, string descriptor)
        {
            var handler = OnYoloBitmapImageProcessedAndLabelledEvent;
            if (handler != null)
            {
                handler(this, new BitmapImageArgs {  Bitmap=bmp, Descriptor = descriptor });
            }
        }

        public event EventHandler<YoloItemListInfoArgs> OnYoloItemListEvent;
        public virtual void OnYoloItemList(YoloItemListInfo yoloInfo)
        {
            var handler = OnYoloItemListEvent;
            if (handler != null)
            {
                handler(this, new YoloItemListInfoArgs {  YoloResult = yoloInfo});
            }
        }

    }
    public class YoloMatItemInfo
    {
        public Mat mat;
        public List<YoloItem> items;

        public YoloMatItemInfo()
        {
            mat = new Mat();
            items = new List<YoloItem>();
        }
        public YoloMatItemInfo(Mat inMat, List<YoloItem> lst)
        {
            mat = inMat;
            items = lst;
        }
    }
    public class YoloBitmapItemInfo
    {
        public Bitmap bmp;
        public List<YoloItem> items;

        //public YoloBitmapItemInfo()
        //{
        //    bmp = new Bitmap(,;
        //    items = new List<YoloItem>();
        //}
        public YoloBitmapItemInfo(Bitmap inBmp, List<YoloItem> lst)
        {
            bmp = inBmp;
            items = lst;
        }
    }
    public class YoloItemListInfo
    {
        public List<YoloItem> items;

        public YoloItemListInfo(List<YoloItem> lst)
        {
            items = lst;
        }
    }
    
    public class YoloItemListInfoArgs : EventArgs
    {
        public YoloItemListInfo YoloResult { get; set; }
    }
}
