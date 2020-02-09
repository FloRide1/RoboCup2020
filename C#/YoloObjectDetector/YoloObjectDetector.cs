using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
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
        string defaultConfigurationPath = "..\\..\\..\\..\\_YoloConfiguration\\MSLRobotBallonButDetection\\";//
        string yoloCFGFileName = "yolov3-MSLRobotBallonButDetection.cfg";
        string yoloWeightsFileName = "yolov3-MSLRobotBallonButDetection_final.weights";
        string yoloNamesFileName = "MSLRobotBallonButDetection.names";
        public YoloObjectDetector(bool ignoreGPU)
        {
            try
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
            catch
            {
                throw new Exception("Couldn't open yolo");
            }
           


        }
        public void Dispose()
        {
            wrap?.Dispose();
        }
        public void DetectAndLabel(object sender, EventArgsLibrary.OpenCvMatImageArgs e)
        {
            e.Descriptor = "ImageDebug3";
            var sw = new Stopwatch();
            sw.Start();
            YoloMatItemInfo yoloInfo = DetectAndLabelAngGetItem(e.Mat);
            sw.Stop();
            OnYoloImageProcessedAndLabelledReady(yoloInfo.mat, e.Descriptor);
            string str="";
            foreach(YoloItem item in yoloInfo.items)
            {
                str += "Object: " + item.Type + " Conf: " + item.Confidence.ToString("F2") + " Pos: X: " + item.X + " Y: " + item.Y + " Width: " + item.Width + " Heigth: " +item.Height+ " Process Time:" + sw.ElapsedMilliseconds+"ms\n";
            }
            OnYoloImageProcessedAndLabelled_LabelReady(str);
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

        private Pen GetBrush(double confidence, int width)
        {
            var size = width / 100;

            if (confidence > 0.5)
            {
                return new Pen(Brushes.GreenYellow, size);
            }
            else if (confidence > 0.2 && confidence <= 0.5)
            {
                return new Pen(Brushes.Orange, size);
            }

            return new Pen(Brushes.DarkRed, size);
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

        public event EventHandler<StringEventArgs> OnYoloImageProcessedAndLabelled_LabelEvent;
        public virtual void OnYoloImageProcessedAndLabelled_LabelReady( string str)
        {
            var handler = OnYoloImageProcessedAndLabelled_LabelEvent;
            if (handler != null)
            {
                handler(this, new StringEventArgs {  value=str});
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
}
