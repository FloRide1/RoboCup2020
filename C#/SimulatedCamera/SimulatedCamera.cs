using Emgu.CV;
using EventArgsLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimulatedCamera
{
    public class SimulatedCamera
    {
        private VideoCapture SimulatedVideo { get; set; }
        static double frameRate = 30;

        public SimulatedCamera()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.ShowDialog();
            //string fileName = ofd.FileName);
            string fileName = "C:\\Github\\RoboCup2020\\Video\\Videos Positionnement\\Positionnement\\ballonFixe1.mp4";
            SimulatedVideo = new Emgu.CV.VideoCapture(fileName);
            SimulatedVideo.ImageGrabbed += SimulatedVideo_ImageGrabbed;
            double frameRate = SimulatedVideo.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
            SimulatedVideo.Start();
            SimulatedVideo.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps, 0.12);
        }

        //********************************************** Input events **********************************************************************************//
        Mat grabbedFrame;
        private void SimulatedVideo_ImageGrabbed(object sender, EventArgs e)
        {
            //Par défault, la lecture se fait aussi vite que possible.
            //Il faut donc ajouter une tempo à chaque frame dans le thread de lecture
            if (SimulatedVideo != null)
            {
                grabbedFrame = new Mat();
                SimulatedVideo.Retrieve(grabbedFrame);
                OnOpenCvMatImageReceived(grabbedFrame, "ImageFromCamera");

                //Ajout d'une tempo pour respecter le frame rate
                Thread.Sleep((int)(1000.0 / (frameRate*2))); 
            }
        }

        private void OnOpenCvMatImageReceived(Mat frame)
        {
            throw new NotImplementedException();
        }

        //********************************************** Output events **********************************************************************************//
        public delegate void OpenCvMatImageEventHandler(object sender, OpenCvMatImageArgs e);
        public event EventHandler<OpenCvMatImageArgs> OnOpenCvMatImageReadyEvent;

        public virtual void OnOpenCvMatImageReceived(Mat image, string descriptor)
        {
            OnOpenCvMatImageReadyEvent?.Invoke(this, new OpenCvMatImageArgs { Mat = image, Descriptor = descriptor });
        }
    }
}
