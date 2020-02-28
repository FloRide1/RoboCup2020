using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using EventArgsLibrary;
using Newtonsoft.Json;
using Encoder = System.Drawing.Imaging.Encoder;

namespace ImageSaver
{
    public class ImageSaver
    {
        Bitmap myBitmap;
        ImageCodecInfo myImageCodecInfo;
        System.Drawing.Imaging.Encoder myEncoder;
        EncoderParameter myEncoderParameter;
        EncoderParameters myEncoderParameters;
        public ImageSaver()
        {

        }

        int subdivision = 0;
        public void OnSaveCVMatImage(object sender, OpenCvMatImageArgs e)
        {
            subdivision++;
            if (subdivision == 120)
            {
                subdivision = 0;
                string fileName = "Images/Omni_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".jpg";

                // Create a Bitmap object based on a BMP file.
                myBitmap = new Bitmap(e.Mat.Bitmap);

                // Get an ImageCodecInfo object that represents the JPEG codec.
                myImageCodecInfo = GetEncoderInfo("image/jpeg");

                // Create an Encoder object based on the GUID

                // for the Quality parameter category.
                myEncoder = Encoder.Quality;

                // Create an EncoderParameters object.

                // An EncoderParameters object has an array of EncoderParameter

                // objects. In this case, there is only one

                // EncoderParameter object in the array.
                myEncoderParameters = new EncoderParameters(1);

                // Save the bitmap as a JPEG file with quality level 25.
                myEncoderParameter = new EncoderParameter(myEncoder, 80L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                myBitmap.Save(fileName, myImageCodecInfo, myEncoderParameters);
            }
        }

        public void OnSaveBitmapImage(object sender, BitmapImageArgs e)
        {
            subdivision++;
            if (subdivision == 120)
            {
                subdivision = 0;
                string fileName = "Images/Omni_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".jpg";

                // Create a Bitmap object based on a BMP file.
               // myBitmap = new Bitmap(e.Mat.Bitmap);

                // Get an ImageCodecInfo object that represents the JPEG codec.
                myImageCodecInfo = GetEncoderInfo("image/jpeg");

                // Create an Encoder object based on the GUID

                // for the Quality parameter category.
                myEncoder = Encoder.Quality;

                // Create an EncoderParameters object.

                // An EncoderParameters object has an array of EncoderParameter

                // objects. In this case, there is only one

                // EncoderParameter object in the array.
                myEncoderParameters = new EncoderParameters(1);

                // Save the bitmap as a JPEG file with quality level 25.
                myEncoderParameter = new EncoderParameter(myEncoder, 80L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                e.Bitmap.Save(fileName, myImageCodecInfo, myEncoderParameters);
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

    }
}
