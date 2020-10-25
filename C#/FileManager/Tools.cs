using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace FileManager
{
    public static class Tools
    {
        /// <summary>
        /// Get free name key on a directory
        /// </summary>
        /// <param name="directoryPath">Path of directory</param>
        /// <param name="searchPattern">Searching pattern</param>
        /// <returns>Free key for file</returns>
        public static int GetFreeFileName(string directoryPath, string searchPattern = "*.png")
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    var files = Directory.EnumerateFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
                    int freeNumber = 0;
                    foreach (var f in files)
                    {
                        string path;
                        path = f.Remove(0, directoryPath.Length + 1);
                        path = path.Replace(".png", "");

                        int fileNb;
                        if (int.TryParse(path, out fileNb))
                        {
                            if (freeNumber == fileNb)
                                freeNumber++;
                        }
                    }

                    return freeNumber;
                }
                else
                {
                    Directory.CreateDirectory(directoryPath);
                    return 0;
                }
            }
            catch (UnauthorizedAccessException uAEx)
            {
                MessageBox.Show(uAEx.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (PathTooLongException pathEx)
            {
                MessageBox.Show(pathEx.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch(DirectoryNotFoundException)
            {
                Directory.CreateDirectory(directoryPath);
                return 0;
            }

            return -1;
        }
    }
}