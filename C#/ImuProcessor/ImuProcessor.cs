using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;

namespace ImuProcessor
{
    public class ImuProcessor
    {
        int robotId = 0;
        double offsetGyroZ = 0;
        Timer TimerCalibration = new Timer(3000);
        bool calibrationInProgress = false;
        List<double> gyroZCalibrationList = new List<double>();
        System.Configuration.Configuration configFile;

        public ImuProcessor(int id)
        {
            robotId = id;
            TimerCalibration.Elapsed += CalibrationFinished_Event;
            configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            Double.TryParse(configFile.AppSettings.Settings["GyroOffsetZ"].Value, out offsetGyroZ);
        }

        private void CalibrationFinished_Event(object sender, ElapsedEventArgs e)
        {
            calibrationInProgress = false;
            TimerCalibration.Stop();
            offsetGyroZ = gyroZCalibrationList.Average();
            configFile.AppSettings.Settings["GyroOffsetZ"].Value = offsetGyroZ.ToString();
            configFile.Save();

        }

        public void OnIMURawDataReceived(object sender, IMUDataEventArgs e)
        {
            Point3D accelXYZ = new Point3D(e.accelX, e.accelY, e.accelZ);
            Point3D gyroXYZ = new Point3D(e.gyroX, e.gyroY, e.gyroZ-offsetGyroZ);

            if(calibrationInProgress)            
            { 
                gyroZCalibrationList.Add(e.gyroZ);
            }

            //On envois l'event aux abonnés
            OnIMUProcessedData(e.EmbeddedTimeStampInMs, accelXYZ, gyroXYZ);
        }

        public void OnCalibrateGyroFromInterfaceGeneratedEvent(object sender, EventArgs e)
        {
            TimerCalibration.Start();
            calibrationInProgress = true;
            gyroZCalibrationList.Clear();
        }

        private void CalibrateGyro()
        {

        }

        //Output events
        public event EventHandler<IMUDataEventArgs> OnIMUProcessedDataGeneratedEvent;
        public virtual void OnIMUProcessedData(uint timeStamp, Point3D accelxyz, Point3D gyroxyz)
        {
            var handler = OnIMUProcessedDataGeneratedEvent;
            if (handler != null)
            {
                handler(this, new IMUDataEventArgs { EmbeddedTimeStampInMs = timeStamp, accelX = accelxyz.X, accelY = accelxyz.Y, accelZ = accelxyz.Z, gyroX = gyroxyz.X, gyroY = gyroxyz.Y, gyroZ = gyroxyz.Z });
            }
        }
    }
}
