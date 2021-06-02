using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RobotMonitor
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class WpfCameraMonitor : Window
    {
        public WpfCameraMonitor()
        {
            InitializeComponent();
        }
        int nbMsgSent = 0;

        int nbMsgReceived = 0;
        public void DisplayMessageDecoded(object sender, MessageDecodedArgs e)
        {
            nbMsgReceived += 1;
        }

        int nbMsgReceivedErrors = 0;
        public void MessageDecodedErrorCB(object sender, MessageDecodedArgs e)
        {
            nbMsgReceivedErrors += 1;
        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void DisplayMessageInConsole(object sender, StringEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            //Utilisation ici d'une methode anonyme
            textBoxDebug.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                textBoxDebug.Text += e.value;
                if (textBoxDebug.Text.Length > 2000)
                    textBoxDebug.Text = textBoxDebug.Text.Remove(0, 200);
            }));
        }

        Stopwatch sw = new Stopwatch();
        public void DisplayBitmapImage(object sender, EventArgsLibrary.BitmapImageArgs e)
        {
            string descriptor = e.Descriptor;
            switch (descriptor)
            {
                case "FishEyeImageFromCamera":
                    Dispatcher.Invoke(new Action(delegate ()
                    {
                        imageCamera1.Source = ImageSourceFromBitmap(e.Bitmap);
                    }));
                    break;
                case "PanoramaImageFromCamera":
                    sw.Restart();
                    Dispatcher.Invoke(new Action(delegate ()
                    {
                        imageCamera2.Source = ImageSourceFromBitmap(e.Bitmap);
                    }));
                    sw.Stop();
                    //Console.WriteLine("BitmapToImageSource: " + sw.ElapsedMilliseconds);
                    break;
                case "ImageDebug2":
                    Dispatcher.Invoke(new Action(delegate ()
                    {
                        imageCamera2.Source = ImageSourceFromBitmap(e.Bitmap);
                    }));
                    break;
                case "ImageDebug3":
                    Dispatcher.Invoke(new Action(delegate ()
                    {
                        imageCamera3.Source = ImageSourceFromBitmap(e.Bitmap);
                    }));
                    break;
                case "ImageDebug4":
                    //imageCamera4.Source = ImageSourceFromBitmap(image);
                    break;
                default:
                    imageCamera4.Source = ImageSourceFromBitmap(e.Bitmap);
                    break;
            }

        }
        
        //If you get 'dllimport unknown'-, then add 'using System.Runtime.InteropServices;'
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
        

        double zoomFactor = 5;
        bool isZoomed = false;
        int lastZoomedRow = 0;
        int lastZoomedCol = 0;
        private void ImageDebug1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int row = 0, column = 0;
            if (sender.GetType() == typeof(GroupBox))
            {
                GroupBox s = (GroupBox)sender;
                if (s != null)
                {
                    row = Grid.GetRow(s);
                    column = Grid.GetColumn(s);
                }
            }
            else if (sender.GetType() == typeof(GroupBox))
            {
                GroupBox s = (GroupBox)sender;
                if (s != null)
                {
                    row = Grid.GetRow(s);
                    column = Grid.GetColumn(s);
                }
            }


            if (!isZoomed)
            {
                GridAffichageCamera.ColumnDefinitions[column].Width = new GridLength(GridAffichageCamera.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                GridAffichageCamera.RowDefinitions[row].Height = new GridLength(GridAffichageCamera.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                lastZoomedCol = column;
                lastZoomedRow = row;
                isZoomed = true;
            }
            else
            {
                GridAffichageCamera.ColumnDefinitions[lastZoomedCol].Width = new GridLength(GridAffichageCamera.ColumnDefinitions[lastZoomedCol].Width.Value / zoomFactor, GridUnitType.Star);
                GridAffichageCamera.RowDefinitions[lastZoomedRow].Height = new GridLength(GridAffichageCamera.RowDefinitions[lastZoomedRow].Height.Value / zoomFactor, GridUnitType.Star);
                isZoomed = false;
                if (lastZoomedRow != row || lastZoomedCol != column)
                {
                    GridAffichageCamera.ColumnDefinitions[column].Width = new GridLength(GridAffichageCamera.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                    GridAffichageCamera.RowDefinitions[row].Height = new GridLength(GridAffichageCamera.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                    lastZoomedCol = column;
                    lastZoomedRow = row;
                    isZoomed = true;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }

            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;
            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void Calibrate_Button_Click(object sender, RoutedEventArgs e)
        {
            OnCalibrateCameraEventReceived();
        }

        private void Reset_Calibration_Button_Click(object sender, RoutedEventArgs e)
        {
            OnResetCalibrationCameraEventReceived();
        }
        public void Stop_Camera_Button_Click(object sender, RoutedEventArgs e)
        {
            OnStopCameraEventReceived();
        }

        public void Start_Camera_Button_Click(object sender, RoutedEventArgs e)
        {
            OnStartCameraEventReceived();
        }

        public event EventHandler<EventArgs> CalibrateCameraEvent;
        public virtual void OnCalibrateCameraEventReceived()
        {
            var handler = CalibrateCameraEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler<EventArgs> ResetCalibrationCameraEvent;
        public virtual void OnResetCalibrationCameraEventReceived()
        {
            var handler = ResetCalibrationCameraEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler<EventArgs> StartCameraEvent;
        public virtual void OnStartCameraEventReceived()
        {
            var handler = StartCameraEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public event EventHandler<EventArgs> StopCameraEvent;
        public virtual void OnStopCameraEventReceived()
        {
            var handler = StopCameraEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

    }
}
