using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public static class FileWriter
    {
        /// <summary>
        /// Write text on file using open file dialog
        /// </summary>
        /// <param name="text">Text to write</param>
        /// <param name="isWriteLine">True if Write line else write</param>
        /// <param name="extention">file extension</param>
        /// <returns>1 if write successfull else -1</returns>
        public static int WriteTextOnFile(string text, bool isWriteLine = true, string extention = "txt")
        {
            var filePath = string.Empty;
            Stream writerS;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (extention.Contains("."))
                extention = extention.Replace(".", "");
            saveFileDialog.Filter = "template files (*." + extention + ")|*." + extention;
            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                if ((writerS = saveFileDialog.OpenFile()) != null)
                {
                    StreamWriter writer = new StreamWriter(writerS);
                    if (!isWriteLine)
                        writer.Write(text);
                    else
                        writer.WriteLine(text);
                    writer.Close();
                    writerS.Close();
                    return 1;
                }
            }
            return -1;
        }
    }
}
