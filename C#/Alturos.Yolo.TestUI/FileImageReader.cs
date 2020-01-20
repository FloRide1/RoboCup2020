using Alturos.Yolo.TestUI.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Alturos.Yolo.TestUI
{
    public class FileImageReader
    {
        public ImageInfo Analyze(string path)
        {
            var allowedFileExtensions = new string[] { ".bmp", ".jpg", ".png" };

                if (!allowedFileExtensions.Any(o => path.EndsWith(o, StringComparison.OrdinalIgnoreCase)))
                {
                    return null;
                }

                var fileInfo = new FileInfo(path);
                var resolution = this.GetImageResolution(path);

                var imageInfo = new ImageInfo();
                imageInfo.Name = fileInfo.Name;
                imageInfo.Path = path;
                imageInfo.Width = resolution.Item1;
                imageInfo.Height = resolution.Item2;

                return imageInfo;
        }

        private Tuple<int, int> GetImageResolution(string imagePath)
        {
            try
            {
                using (var image = Image.FromFile(imagePath))
                {
                    return new Tuple<int, int>(image.Width, image.Height);
                }
            }
            catch (Exception)
            {
                return new Tuple<int, int>(0, 0);
            }
        }
    }
}
