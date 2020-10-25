using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace FileManager
{
    public static class JsonSerialize<T>
    {        
        /// <summary>
        /// Open save file dialog and Serialize object 
        /// </summary>
        /// <param name="o">object to serialize</param>
        /// <param name="extention">extension file to save</param>
        /// <returns>1 if serialize is ok else -1</returns>
        public static int SerializeObjectOnFile(T o, string extention = "json")
        {
            var filePath = string.Empty;
            Stream writerS;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (extention.Contains("."))
                extention = extention.Replace(".","");
            saveFileDialog.Filter = "template files (*."+extention+")|*."+extention;
            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                if ((writerS = saveFileDialog.OpenFile()) != null)
                {
                    StreamWriter writer = new StreamWriter(writerS);
                    string serializedO = JsonConvert.SerializeObject(o);
                    writer.Write(serializedO);
                    writer.Close();
                    writerS.Close();
                    return 1;
                }
            }
            return -1;
        }
        /// <summary>
        /// Open file and Serialize object
        /// </summary>
        /// <param name="directory path">path of directory</param>
        /// <param name="fileName">File name on directory</param>
        /// <param name="o">object to serialize</param>
        /// <param name="extention">extension file to save</param>
        /// <returns>1 if serialize is ok else -1</returns>
        public static int SerializeObjectOnFile(T o,string directoryPath, string fileName, string extention = "json")
        {
            string path = directoryPath + "\\" + fileName;
            if (Directory.Exists(directoryPath))
            {
                var filePath = string.Empty;
                if (extention.Contains("."))
                    extention = extention.Replace(".", "");
                FileStream writerS = new FileStream(path + "." + extention, FileMode.Create);
                StreamWriter writer = new StreamWriter(writerS);
                string serializedO = JsonConvert.SerializeObject(o);
                writer.Write(serializedO);
                writer.Close();
                writerS.Close();
            }
            else
            {
                Directory.CreateDirectory(directoryPath);
                SerializeObjectOnFile(o, directoryPath, fileName, extention);
            }
            return 1;
        }

        /// <summary>
        /// Open open file dialog and deserialize object
        /// </summary>
        /// <param name="extention">extension file to open</param>
        /// <returns>object deserialized</returns>
        public static T DeserializeObjectFromFile(string extention = "json")
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            openFileDialog.Filter = "Template files (*."+extention+")|*."+extention+"";
            openFileDialog.RestoreDirectory = true;

            if ((bool)openFileDialog.ShowDialog())
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;

                //Read the contents of the file into a stream
                var fileStream = openFileDialog.OpenFile();

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    fileContent = reader.ReadToEnd();
                }
            }

            T obj = JsonConvert.DeserializeObject<T>(fileContent);

            return obj;
        }

        /// <summary>
        /// Open file and deserialize object
        /// </summary>
        /// <param name="directoryPath">Path of directory</param>
        /// <param name="fileName">Name of file on directory</param>
        /// <param name="extention">extension file to open</param>
        /// <returns>object deserialized</returns>
        public static T DeserializeObjectFromFile(string directoryPath, string fileName, string extention = "json")
        {
            string path = directoryPath + "\\" + fileName;
            if (Directory.Exists(directoryPath))
            {
                var fileContent = string.Empty;
                if (extention.Contains("."))
                    extention = extention.Replace(".", "");
                //Read the contents of the file into a stream
                if(!File.Exists(path))
                    SerializeObjectOnFile((T)Activator.CreateInstance(typeof(T)), directoryPath, fileName, extention);
                var fileStream = new FileStream(path + "." + extention, FileMode.OpenOrCreate);
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    fileContent = reader.ReadToEnd();
                }

                T obj = JsonConvert.DeserializeObject<T>(fileContent);

                return obj;
            }
            else
            {
                Directory.CreateDirectory(directoryPath);
                DeserializeObjectFromFile(directoryPath, fileName, extention);
            }
            T obj1 = JsonConvert.DeserializeObject<T>("");
            return obj1;
        }
    }
}
