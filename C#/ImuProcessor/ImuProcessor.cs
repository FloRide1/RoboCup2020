using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Timers;
using Utilities;

namespace ImuProcessor
{
    public class ImuProcessor
    {
        int robotId = 0;
        double offsetAccelX = 0;
        double offsetAccelY = 0;
        double offsetAccelZ = 0;
        double offsetGyroX = 0;
        double offsetGyroY = 0;
        double offsetGyroZ = 0;
        Timer TimerCalibration = new Timer(3000);
        bool calibrationInProgress = false;
        List<double> accelXCalibrationList = new List<double>();
        List<double> accelYCalibrationList = new List<double>();
        List<double> accelZCalibrationList = new List<double>();
        List<double> gyroXCalibrationList = new List<double>();
        List<double> gyroYCalibrationList = new List<double>();
        List<double> gyroZCalibrationList = new List<double>();
        System.Configuration.Configuration configFile;

        bool replayModeActivated = false;

        public ImuProcessor(int id)
        {
            robotId = id;
            TimerCalibration.Elapsed += CalibrationFinished_Event;
            configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            Double.TryParse(configFile.AppSettings.Settings["AccelOffsetX"].Value.Replace(',', '.'), out offsetAccelX);
            Double.TryParse(configFile.AppSettings.Settings["AccelOffsetY"].Value.Replace(',', '.'), out offsetAccelY);
            Double.TryParse(configFile.AppSettings.Settings["AccelOffsetZ"].Value.Replace(',', '.'), out offsetAccelZ);
            Double.TryParse(configFile.AppSettings.Settings["GyroOffsetX"].Value.Replace(',', '.'), out offsetGyroX);
            Double.TryParse(configFile.AppSettings.Settings["GyroOffsetY"].Value.Replace(',','.'), out offsetGyroY);
            Double.TryParse(configFile.AppSettings.Settings["GyroOffsetZ"].Value.Replace(',', '.'), out offsetGyroZ);
        }
        
        public void OnEnableDisableLogReplayEvent(object sender, BoolEventArgs e)
        {
            replayModeActivated = e.value;
        }

        private void CalibrationFinished_Event(object sender, ElapsedEventArgs e)
        {
            calibrationInProgress = false;
            TimerCalibration.Stop();
            offsetAccelX = accelXCalibrationList.Average();
            configFile.AppSettings.Settings["AccelOffsetX"].Value = offsetAccelX.ToString();
            offsetAccelY = accelYCalibrationList.Average();
            configFile.AppSettings.Settings["AccelOffsetY"].Value = offsetAccelY.ToString();
            offsetAccelZ = accelZCalibrationList.Average();
            configFile.AppSettings.Settings["AccelOffsetZ"].Value = offsetAccelZ.ToString();
            offsetGyroX = gyroXCalibrationList.Average();
            configFile.AppSettings.Settings["GyroOffsetX"].Value = offsetGyroX.ToString();
            offsetGyroY = gyroYCalibrationList.Average();
            configFile.AppSettings.Settings["GyroOffsetY"].Value = offsetGyroY.ToString();
            offsetGyroZ = gyroZCalibrationList.Average();
            configFile.AppSettings.Settings["GyroOffsetZ"].Value = offsetGyroZ.ToString();
            configFile.Save();

        }

        public void OnIMURawDataReceived(object sender, IMUDataEventArgs e)
        {
            if (!replayModeActivated)
            {
                ProcessImuRawData(e);
            }
        }

        public void OnIMUReplayRawDataReceived(object sender, IMUDataEventArgs e)
        {
            if (replayModeActivated)
            {
                ProcessImuRawData(e);
            }
        }

        private void ProcessImuRawData(IMUDataEventArgs e)
        {
            //Point3D accelXYZ = new Point3D(e.accelX - offsetAccelX, e.accelY - offsetAccelY, e.accelZ - offsetAccelZ);
            Point3D accelXYZ = new Point3D(e.accelX, e.accelY, e.accelZ);
            Point3D gyroXYZ = new Point3D(e.gyroX - offsetGyroX, e.gyroY - offsetGyroY, e.gyroZ - offsetGyroZ);

            if (calibrationInProgress)
            {
                accelXCalibrationList.Add(e.accelX);
                accelYCalibrationList.Add(e.accelY);
                accelZCalibrationList.Add(e.accelZ);
                gyroXCalibrationList.Add(e.gyroX);
                gyroYCalibrationList.Add(e.gyroY);
                gyroZCalibrationList.Add(e.gyroZ);
            }

            //On envois l'event aux abonnés
            OnIMUProcessedData(e.EmbeddedTimeStampInMs, accelXYZ, gyroXYZ);
            OnGyroSpeed(robotId, e.gyroZ - offsetGyroZ);
        }

        public void OnCalibrateGyroFromInterfaceGeneratedEvent(object sender, EventArgs e)
        {
            TimerCalibration.Start();
            calibrationInProgress = true;
            accelXCalibrationList.Clear();
            accelYCalibrationList.Clear();
            accelZCalibrationList.Clear();
            gyroXCalibrationList.Clear();
            gyroYCalibrationList.Clear();
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

        public event EventHandler<GyroArgs> OnGyroSpeedEvent;
        public virtual void OnGyroSpeed(int id, double vtheta)
        {
            var handler = OnGyroSpeedEvent;
            if (handler != null)
            {
                handler(this, new GyroArgs { RobotId = id, Vtheta = vtheta });
            }
        }
    }
}
