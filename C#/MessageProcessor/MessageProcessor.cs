using Constants;
using EventArgsLibrary;
using PerformanceMonitorTools;
using System;
using System.Text;
using System.Timers;
using Utilities;

namespace MessageProcessorNS
{
    public class MsgProcessor
    {
        GameMode competition;
        Timer tmrComptageMessage;
        int robotID;
        
        public MsgProcessor(int robotId, GameMode compet)
        {
            robotID = robotId;
            competition = compet;
            tmrComptageMessage = new Timer(1000);
            tmrComptageMessage.Elapsed += TmrComptageMessage_Elapsed;
            tmrComptageMessage.Start();
        }

        int nbMessageIMUReceived = 0;
        int nbMessageSpeedReceived = 0;
        private void TmrComptageMessage_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnMessageCounter(nbMessageIMUReceived, nbMessageSpeedReceived);
            nbMessageIMUReceived = 0;
            nbMessageSpeedReceived = 0;
        }

        //Input CallBack        
        public void ProcessRobotDecodedMessage(object sender, MessageDecodedArgs e)
        {
            ProcessDecodedMessage((Int16)e.MsgFunction,(Int16) e.MsgPayloadLength, e.MsgPayload);
        }
         
        //Processeur de message en provenance du robot...
        //Une fois processé, le message sera transformé en event sortant
        public void ProcessDecodedMessage(Int16 command, Int16 payloadLength, byte[] payload)
        {
            byte[] tab;
            uint timeStamp;
            switch (command)
            {
                case (short)Commands.R2PC_WelcomeMessage:
                    {
                        OnWelcomeMessageFromRobot();
                    }
                    break;

                case (short)Commands.R2PC_SpeedPolarAndIndependantOdometry:
                    {
                        nbMessageSpeedReceived++;
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float vX = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float vY = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float vTheta = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float vM1 = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float vM2 = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float vM3 = tab.GetFloat();
                        tab = payload.GetRange(28, 4);
                        float vM4 = tab.GetFloat();
                        OnPolarOdometrySpeedFromRobot(robotID, timeStamp, vX, vY, vTheta);
                        OnIndependantOdometrySpeedFromRobot(timeStamp, vM1, vM2, vM3, vM4);
                    }
                    break;
                                    
                case (short)Commands.R2PC_IMUData:
                    {
                        float accelX=0, accelY=0, accelZ=0, gyroX=0, gyroY=0, gyroZ=0;
                        timeStamp = 0;
                        switch (competition)
                        {
                            case GameMode.RoboCup:
                                nbMessageIMUReceived++;
                                timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                                tab  = payload.GetRange(4, 4);
                                accelX = tab.GetFloat();
                                tab = payload.GetRange(8, 4);
                                accelY = tab.GetFloat();
                                tab = payload.GetRange(12, 4);
                                accelZ = tab.GetFloat();
                                tab = payload.GetRange(16, 4);
                                gyroX = tab.GetFloat();
                                tab = payload.GetRange(20, 4);
                                gyroY = tab.GetFloat();
                                tab = payload.GetRange(24, 4);
                                gyroZ = tab.GetFloat();
                                break;

                            case GameMode.Eurobot: //La carte de mesure est placée verticalement
                                nbMessageIMUReceived++;
                                timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                                tab = payload.GetRange(4, 4);
                                accelY = -tab.GetFloat();
                                tab = payload.GetRange(8, 4);
                                accelZ = tab.GetFloat();
                                tab = payload.GetRange(12, 4);
                                accelX = -tab.GetFloat();
                                tab = payload.GetRange(16, 4);
                                gyroY = -tab.GetFloat();
                                tab = payload.GetRange(20, 4);
                                gyroZ = tab.GetFloat();
                                tab = payload.GetRange(24, 4);
                                gyroX = -tab.GetFloat();
                                break;
                        }

                    Point3D accelXYZ = new Point3D(accelX, accelY, accelZ);
                        Point3D gyroXYZ = new Point3D(gyroX, gyroY, gyroZ);

                        //On envois l'event aux abonnés
                        OnIMUDataFromRobot(timeStamp, accelXYZ, gyroXYZ);
                    }
                    break;

                case (short)Commands.R2PC_MotorCurrentsMonitoring:
                    {
                        uint time2 = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        byte[] tab2 = payload.GetRange(4, 4);
                        float motor1Current = tab2.GetFloat();
                        tab2 = payload.GetRange(8, 4);
                        float motor2Current = tab2.GetFloat();
                        tab2 = payload.GetRange(12, 4);
                        float motor3Current = tab2.GetFloat();
                        tab2 = payload.GetRange(16, 4);
                        float motor4Current = tab2.GetFloat();
                        tab2 = payload.GetRange(20, 4);
                        float motor5Current = tab2.GetFloat();
                        tab2 = payload.GetRange(24, 4);
                        float motor6Current = tab2.GetFloat();
                        tab2 = payload.GetRange(28, 4);
                        float motor7Current = tab2.GetFloat();
                        //On envois l'event aux abonnés
                        OnMotorsCurrentsFromRobot(time2, motor1Current, motor2Current, motor3Current, motor4Current, motor5Current, motor6Current, motor7Current);
                    }
                    break;
                case (short)Commands.R2PC_SpeedAuxiliaryOdometry:
                    uint time = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    tab = payload.GetRange(4, 4);
                    float vitesseMotor5 = tab.GetFloat();
                    tab = payload.GetRange(8, 4);
                    float vitesseMotor6 = tab.GetFloat();
                    tab = payload.GetRange(12, 4);
                    float vitesseMotor7 = tab.GetFloat();
                    //On envois l'event aux abonnés
                    
                    OnAuxiliaryOdometrySpeedFromRobot(time, vitesseMotor5, vitesseMotor6, vitesseMotor7);
                    break;

                case (short)Commands.R2PC_SpeedAuxiliaryMotorsConsignes:
                    timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    tab = payload.GetRange(4, 4);
                    float consigneMotor5 = tab.GetFloat();
                    tab = payload.GetRange(8, 4);
                    float consigneMotor6 = tab.GetFloat();
                    tab = payload.GetRange(12, 4);
                    float consigneMotor7 = tab.GetFloat();
                    //On envois l'event aux abonnés
                    OnAuxiliarySpeedConsigneDataFromRobot(timeStamp, consigneMotor5, consigneMotor6, consigneMotor7);
                    break;

                case (short)Commands.R2PC_EncoderRawData:
                    timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    int enc1RawVal = (int)(payload[7] | payload[6] << 8 | payload[5] << 16 | payload[4] << 24);
                    int enc2RawVal = (int)(payload[11] | payload[10] << 8 | payload[9] << 16 | payload[8] << 24);
                    int enc3RawVal = (int)(payload[15] | payload[14] << 8 | payload[13] << 16 | payload[12] << 24);
                    int enc4RawVal = (int)(payload[19] | payload[18] << 8 | payload[17] << 16 | payload[16] << 24);
                    int enc5RawVal = (int)(payload[23] | payload[22] << 8 | payload[21] << 16 | payload[20] << 24);
                    int enc6RawVal = (int)(payload[27] | payload[26] << 8 | payload[25] << 16 | payload[24] << 24);
                    int enc7RawVal = (int)(payload[31] | payload[30] << 8 | payload[29] << 16 | payload[28] << 24);

                    //On envois l'event aux abonnés
                    OnEncoderRawDataFromRobot(timeStamp, enc1RawVal, enc2RawVal, enc3RawVal, enc4RawVal, enc5RawVal, enc6RawVal, enc7RawVal);
                    break;

                case (short)Commands.R2PC_IOMonitoring:
                    timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    byte ioValue = payload[4];
                    OnIOValuesFromRobot(timeStamp, ioValue);
                    break;

                case (short)Commands.R2PC_PowerMonitoring:
                    timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    tab = payload.GetRange(4, 4);
                    float battCMDVoltage = tab.GetFloat();
                    tab = payload.GetRange(8, 4);
                    float battCMDCurrent = tab.GetFloat();
                    tab = payload.GetRange(12, 4);
                    float battPWRVoltage = tab.GetFloat();
                    tab = payload.GetRange(16, 4);
                    float battPWRCurrent = tab.GetFloat();
                    OnPowerMonitoringValuesFromRobot(timeStamp, battCMDVoltage, battCMDCurrent, battPWRVoltage, battPWRCurrent);
                    break;

                case (short)Commands.R2PC_4WheelsSpeedPolarPidCommandErrorCorrectionConsigne:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float xError = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float yError = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float thetaError = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float xCorrection = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float yCorrection = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float thetaCorrection = tab.GetFloat();

                        tab = payload.GetRange(28, 4);
                        float xconsigne = tab.GetFloat();
                        tab = payload.GetRange(32, 4);
                        float yConsigne = tab.GetFloat();
                        tab = payload.GetRange(36, 4);
                        float thetaConsigne = tab.GetFloat();
                        //On envois l'event aux abonnés
                        On4WheelsPolarPidErrorCorrectionConsigneDataFromRobot(timeStamp, xError, yError, thetaError, xCorrection, yCorrection, thetaCorrection, xconsigne, yConsigne, thetaConsigne);
                    }
                    break;

                case (short)Commands.R2PC_4WheelsSpeedIndependantPidCommandErrorCorrectionConsigne:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float M1Error = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float M2Error = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float M3Error = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float M4Error = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float M1Correction = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float M2Correction = tab.GetFloat();

                        tab = payload.GetRange(28, 4);
                        float M3Correction = tab.GetFloat();
                        tab = payload.GetRange(32, 4);
                        float M4Correction = tab.GetFloat();
                        tab = payload.GetRange(36, 4);
                        float M1Consigne = tab.GetFloat();
                        tab = payload.GetRange(40, 4);
                        float M2Consigne = tab.GetFloat();
                        tab = payload.GetRange(44, 4);
                        float M3Consigne = tab.GetFloat();
                        tab = payload.GetRange(48, 4);
                        float M4Consigne = tab.GetFloat();
                        //On envois l'event aux abonnés
                        On4WheelsSpeedIndependantPidDebugDataFromRobot(timeStamp,
                            M1Error, M2Error, M3Error, M4Error,
                            M1Correction, M2Correction, M3Correction, M4Correction,
                            M1Consigne, M2Consigne, M3Consigne, M4Consigne);
                    }
                    break;

                case (short)Commands.R2PC_2WheelsSpeedPolarPidCommandErrorCorrectionConsigne:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float xError = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float thetaError = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float xCorrection = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float thetaCorrection = tab.GetFloat();

                        tab = payload.GetRange(20, 4);
                        float xconsigne = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float thetaConsigne = tab.GetFloat();
                        //On envois l'event aux abonnés
                        On2WheelsPolarPidErrorCorrectionConsigneDataFromRobot(timeStamp, xError, thetaError, xCorrection, thetaCorrection, xconsigne, thetaConsigne);
                    }
                    break;

                case (short)Commands.R2PC_2WheelsSpeedIndependantPidCommandErrorCorrectionConsigne:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float M1Error = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float M2Error = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float M1Correction = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float M2Correction = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float M1Consigne = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float M2Consigne = tab.GetFloat();
                        
                        //On envois l'event aux abonnés
                        On2WheelsSpeedIndependantPidDebugDataFromRobot(timeStamp,
                            M1Error, M2Error,
                            M1Correction, M2Correction,
                            M1Consigne, M2Consigne);
                    }
                    break;

                case (short)Commands.R2PC_4WheelsSpeedPolarPidCorrections:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float CorrPx = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float CorrIx = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float CorrDx = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float CorrPy = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float CorrIy = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float CorrDy = tab.GetFloat();
                        tab = payload.GetRange(28, 4);
                        float CorrPTheta = tab.GetFloat();
                        tab = payload.GetRange(32, 4);
                        float CorrITheta = tab.GetFloat();
                        tab = payload.GetRange(36, 4);
                        float CorrDTheta = tab.GetFloat();
                        //On envois l'event aux abonnés
                        On4WheelsSpeedPolarPidCorrectionDataFromRobot(CorrPx, CorrIx, CorrDx, CorrPy, CorrIy, CorrDy, CorrPTheta, CorrITheta, CorrDTheta);
                    }
                    break;

                case (short)Commands.R2PC_4WheelsSpeedIndependantPidCorrections:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float CorrPM1 = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float CorrIM1 = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float CorrDM1 = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float CorrPM2 = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float CorrIM2 = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float CorrDM2 = tab.GetFloat();
                        tab = payload.GetRange(28, 4);
                        float CorrPM3 = tab.GetFloat();
                        tab = payload.GetRange(32, 4);
                        float CorrIM3 = tab.GetFloat();
                        tab = payload.GetRange(36, 4);
                        float CorrDM3 = tab.GetFloat();
                        tab = payload.GetRange(40, 4);
                        float CorrPM4 = tab.GetFloat();
                        tab = payload.GetRange(44, 4);
                        float CorrIM4 = tab.GetFloat();
                        tab = payload.GetRange(48, 4);
                        float CorrDM4 = tab.GetFloat();
                        //On envois l'event aux abonnés
                        On4WheelsSpeedIndependantPidCorrectionDataFromRobot(CorrPM1, CorrIM1, CorrDM1, CorrPM2, CorrIM2, CorrDM2, CorrPM3, CorrIM3, CorrDM3, CorrPM4, CorrIM4, CorrDM4);
                    }
                    break;

                case (short)Commands.R2PC_2WheelsSpeedPolarPidCorrections:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float CorrPx = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float CorrIx = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float CorrDx = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float CorrPTheta = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float CorrITheta = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float CorrDTheta = tab.GetFloat();
                        //On envois l'event aux abonnés
                        On2WheelsSpeedPolarPidCorrectionDataFromRobot(CorrPx, CorrIx, CorrDx, CorrPTheta, CorrITheta, CorrDTheta);
                    }
                    break;

                case (short)Commands.R2PC_2WheelsSpeedIndependantPidCorrections:
                    {
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float CorrPM1 = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float CorrIM1 = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float CorrDM1 = tab.GetFloat();
                        tab = payload.GetRange(16, 4);
                        float CorrPM2 = tab.GetFloat();
                        tab = payload.GetRange(20, 4);
                        float CorrIM2 = tab.GetFloat();
                        tab = payload.GetRange(24, 4);
                        float CorrDM2 = tab.GetFloat();
                        //On envois l'event aux abonnés
                        On2WheelsSpeedIndependantPidCorrectionDataFromRobot(CorrPM1, CorrIM1, CorrDM1, CorrPM2, CorrIM2, CorrDM2);
                    }
                    break;

                case (short)Commands.R2PC_MotorsEnableDisableStatus:
                    bool value = Convert.ToBoolean(payload[0]);
                    OnEnableDisableMotorsACKFromRobot(value);
                    break;
                case (short)Commands.R2PC_TirEnableDisableStatus:
                    value = Convert.ToBoolean(payload[0]);
                    OnEnableDisableTirACKFromRobot(value);
                    break;
                case (short)Commands.R2PC_AsservissementModeStatus:
                    AsservissementMode asservMode = (AsservissementMode)payload[0];
                    OnAsservissementModeStatusFromRobot(asservMode);
                    break;
                case (short)Commands.R2PC_SpeedPIDEnableDebugInternalStatus:
                    value = Convert.ToBoolean(payload[0]);
                    OnEnableAsservissementDebugDataACKFromRobot(value);
                    break;
                case (short)Commands.R2PC_MotorCurrentMonitoringEnableStatus:
                    value = Convert.ToBoolean(payload[0]);
                    OnEnableMotorCurrentACKFromRobot(value);
                    break;
                case (short)Commands.R2PC_EncoderRawMonitoringEnableStatus:
                    value = Convert.ToBoolean(payload[0]);
                    OnEnableEncoderRawDataACKFromRobot(value);
                    break;
                case (short)Commands.R2PC_SpeedConsigneMonitoringEnableStatus:
                    value = Convert.ToBoolean(payload[0]);
                    OnEnableMotorSpeedConsigneDataACKFromRobot(value);
                    break;
                case (short)Commands.R2PC_PowerMonitoringEnableStatus:
                    value = Convert.ToBoolean(payload[0]);
                    OnEnablePowerMonitoringDataACKFromRobot(value);
                    break;
                case (short)Commands.R2PC_ErrorMessage:
                    string errorMsg = Encoding.UTF8.GetString(payload);
                    OnErrorTextFromRobot(errorMsg);
                    break;
                default: break;
            }
        }

        public event EventHandler<EventArgs> OnWelcomeMessageFromRobotGeneratedEvent;
        public virtual void OnWelcomeMessageFromRobot()
        {
            var handler = OnWelcomeMessageFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        


        //Output events
        public event EventHandler<IMUDataEventArgs> OnIMURawDataFromRobotGeneratedEvent;
        public virtual void OnIMUDataFromRobot(uint timeStamp, Point3D accelxyz, Point3D gyroxyz)
        {
            var handler = OnIMURawDataFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new IMUDataEventArgs { EmbeddedTimeStampInMs = timeStamp, accelX = accelxyz.X, accelY = accelxyz.Y, accelZ= accelxyz.Z , gyroX=gyroxyz.X, gyroY=gyroxyz.Y, gyroZ=gyroxyz.Z });
            }
        }

        public event EventHandler<BoolEventArgs> OnEnableDisableMotorsACKFromRobotGeneratedEvent;
        public virtual void OnEnableDisableMotorsACKFromRobot(bool isEnabled)
        {
            var handler = OnEnableDisableMotorsACKFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value=isEnabled });
            }
        }

        public event EventHandler<BoolEventArgs> OnEnableDisableTirACKFromRobotGeneratedEvent;
        public virtual void OnEnableDisableTirACKFromRobot(bool isEnabled)
        {
            var handler = OnEnableDisableTirACKFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = isEnabled });
            }
        }

        public event EventHandler<AsservissementModeEventArgs> OnAsservissementModeStatusFromRobotGeneratedEvent;
        public virtual void OnAsservissementModeStatusFromRobot(AsservissementMode asservMode)
        {
            var handler = OnAsservissementModeStatusFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new AsservissementModeEventArgs { mode = asservMode});
            }
        }

        public event EventHandler<BoolEventArgs> OnEnableAsservissementDebugDataACKFromRobotEvent;
        public virtual void OnEnableAsservissementDebugDataACKFromRobot(bool isEnabled)
        {
            var handler = OnEnableAsservissementDebugDataACKFromRobotEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = isEnabled });
            }
        }

        public event EventHandler<BoolEventArgs> OnEnableMotorCurrentACKFromRobotGeneratedEvent;
        public virtual void OnEnableMotorCurrentACKFromRobot(bool isEnabled)
        {
            var handler = OnEnableMotorCurrentACKFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = isEnabled });
            }
        }

        public event EventHandler<BoolEventArgs> OnEnableEncoderRawDataACKFromRobotGeneratedEvent;
        public virtual void OnEnableEncoderRawDataACKFromRobot(bool isEnabled)
        {
            var handler = OnEnableEncoderRawDataACKFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = isEnabled });
            }
        }

        public event EventHandler<BoolEventArgs> OnEnableMotorSpeedConsigneDataACKFromRobotGeneratedEvent;
        public virtual void OnEnableMotorSpeedConsigneDataACKFromRobot(bool isEnabled)
        {
            var handler = OnEnableMotorSpeedConsigneDataACKFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = isEnabled });
            }
        }

        public event EventHandler<BoolEventArgs> OnEnablePowerMonitoringDataACKFromRobotGeneratedEvent;
        public virtual void OnEnablePowerMonitoringDataACKFromRobot(bool isEnabled)
        {
            var handler = OnEnablePowerMonitoringDataACKFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = isEnabled });
            }
        }
        public event EventHandler<StringEventArgs> OnErrorTextFromRobotGeneratedEvent;
        public virtual void OnErrorTextFromRobot(string str)
        {
            var handler = OnErrorTextFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new StringEventArgs { value = str });
            }
        }

        public event EventHandler<PolarSpeedEventArgs> OnSpeedPolarOdometryFromRobotEvent;
        public virtual void OnPolarOdometrySpeedFromRobot(int robotID,uint timeStamp, double vX, double vY, double vTheta)
        {
            var handler = OnSpeedPolarOdometryFromRobotEvent;
            if (handler != null)
            {
                handler(this, new PolarSpeedEventArgs
                {
                    RobotId=robotID,
                    timeStampMs = timeStamp,
                    Vx = (float)vX,
                    Vy = (float)vY,
                    Vtheta = (float)vTheta
                });
            }
        }
        public virtual void OnPolarOdometrySpeedFromRobot(PolarSpeedEventArgs e)
        {
            var handler = OnSpeedPolarOdometryFromRobotEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<IndependantSpeedEventArgs> OnIndependantOdometrySpeedFromRobotEvent;
        public virtual void OnIndependantOdometrySpeedFromRobot(uint timeStamp, double vM1, double vM2, double vM3, double vM4)
        {
            var handler = OnIndependantOdometrySpeedFromRobotEvent;
            if (handler != null)
            {
                handler(this, new IndependantSpeedEventArgs
                {
                    timeStampMs = timeStamp,
                    VitesseMoteur1 = (float)vM1,
                    VitesseMoteur2 = (float)vM2,
                    VitesseMoteur3 = (float)vM3,
                    VitesseMoteur4 = (float)vM4
                });
            }
        }

        public event EventHandler<MotorsCurrentsEventArgs> OnMotorsCurrentsFromRobotGeneratedEvent;
        public virtual void OnMotorsCurrentsFromRobot(uint timeStamp, double m1A, double m2A, double m3A,
                                                                        double m4A, double m5A, double m6A, double m7A)
        {
            var handler = OnMotorsCurrentsFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new MotorsCurrentsEventArgs { timeStampMS = timeStamp,
                                                                motor1 = m1A,
                                                                motor2 = m2A,
                                                                motor3 = m3A,
                                                                motor4 = m4A,
                                                                motor5 = m5A,
                                                                motor6 = m6A,
                                                                motor7 = m7A});
            }
        }

        public event EventHandler<AuxiliarySpeedArgs> OnAuxiliaryOdometrySpeedGeneratedEvent;
        public virtual void OnAuxiliaryOdometrySpeedFromRobot(uint timeStamp, double m5, double m6, double m7)
        {
            var handler = OnAuxiliaryOdometrySpeedGeneratedEvent;
            if (handler != null)
            {
                handler(this, new AuxiliarySpeedEventArgs
                {                    
                    timeStampMs = timeStamp,
                    VitesseMoteur5 = m5,
                    VitesseMoteur6 = m6,
                    VitesseMoteur7 = m7
                });
            }
        }

        public event EventHandler<EncodersRawDataEventArgs> OnEncoderRawDataFromRobotGeneratedEvent;
        public virtual void OnEncoderRawDataFromRobot(uint timeStamp, int m1, int m2, int m3,
                                                                        int m4, int m5, int m6, int m7)
        {
            var handler = OnEncoderRawDataFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new EncodersRawDataEventArgs
                {
                    timeStampMS = timeStamp,
                    motor1 = m1,
                    motor2 = m2,
                    motor3 = m3,
                    motor4 = m4,
                    motor5 = m5,
                    motor6 = m6,
                    motor7 = m7
                });
            }
        }

        public event EventHandler<IOValuesEventArgs> OnIOValuesFromRobotGeneratedEvent;
        public virtual void OnIOValuesFromRobot(uint timeStamp, byte ioValues)
        {
            var handler = OnIOValuesFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new IOValuesEventArgs
                {
                    timeStampMS = timeStamp,
                    ioValues=ioValues
                });
            }
        }

        public event EventHandler<PowerMonitoringValuesEventArgs> OnPowerMonitoringValuesFromRobotGeneratedEvent;
        public virtual void OnPowerMonitoringValuesFromRobot(uint timeStamp, double battCMDVoltage, double battCMDCurrent, double battPWRVoltage, double battPWRCurrent)
        {
            var handler = OnPowerMonitoringValuesFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new PowerMonitoringValuesEventArgs
                {
                    timeStampMS = timeStamp,
                    battCMDVoltage = battCMDVoltage,
                    battCMDCurrent= battCMDCurrent,
                    battPWRVoltage=battPWRVoltage,
                    battPWRCurrent=battPWRCurrent
                });
            }
        }

        public event EventHandler<AuxiliaryMotorsVitesseDataEventArgs> OnAuxiliarySpeedConsigneDataFromRobotGeneratedEvent;
        public virtual void OnAuxiliarySpeedConsigneDataFromRobot(uint timeStamp, double m5, double m6, double m7)
        {
            var handler = OnAuxiliarySpeedConsigneDataFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new AuxiliaryMotorsVitesseDataEventArgs
                {
                    timeStampMS = timeStamp,
                    vitesseMotor5 = m5,
                    vitesseMotor6 = m6,
                    vitesseMotor7 = m7
                });
            }
        }

        public event EventHandler<Polar4WheelsPidErrorCorrectionConsigneDataArgs> On4WheelsSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent;
        public virtual void On4WheelsPolarPidErrorCorrectionConsigneDataFromRobot(uint timeStamp, double xError, double yError, double thetaError,
                                                                        double xCorrection, double yCorrection, double thetaCorrection, double xConsigneRobot, double yConsigneRobot, double thetaConsigneRobot)
        {
            On4WheelsSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent?.Invoke(this, new Polar4WheelsPidErrorCorrectionConsigneDataArgs
            {
                timeStampMS = timeStamp,
                xErreur = xError,
                yErreur = yError,
                thetaErreur = thetaError,
                xCorrection = xCorrection,
                yCorrection = yCorrection,
                thetaCorrection = thetaCorrection,
                xConsigneFromRobot = xConsigneRobot,
                yConsigneFromRobot = yConsigneRobot,
                thetaConsigneFromRobot = thetaConsigneRobot
            });
        }



        public event EventHandler<Polar2WheelsPidErrorCorrectionConsigneDataArgs> On2WheelsSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent;
        public virtual void On2WheelsPolarPidErrorCorrectionConsigneDataFromRobot(uint timeStamp, double xError, double thetaError,
                                                                        double xCorrection, double thetaCorrection, double xConsigneRobot, double thetaConsigneRobot)
        {
            On2WheelsSpeedPolarPidErrorCorrectionConsigneDataFromRobotGeneratedEvent?.Invoke(this, new Polar2WheelsPidErrorCorrectionConsigneDataArgs
            {
                timeStampMS = timeStamp,
                xErreur = xError,
                thetaErreur = thetaError,
                xCorrection = xCorrection,
                thetaCorrection = thetaCorrection,
                xConsigneFromRobot = xConsigneRobot,
                thetaConsigneFromRobot = thetaConsigneRobot
            });
        }

        public event EventHandler<Independant4WheelsPidErrorCorrectionConsigneDataArgs> On4WheelsSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent;
        public virtual void On4WheelsSpeedIndependantPidDebugDataFromRobot(uint timeStamp, double M1Error, double M2Error, double M3Error, double M4Error,
                                                                        double M1Correction, double M2Correction, double M3Correction, double M4Correction,
                                                                        double M1ConsigneRobot, double M2ConsigneRobot, double M3ConsigneRobot, double M4ConsigneRobot)
        {
            On4WheelsSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent?.Invoke(this, new Independant4WheelsPidErrorCorrectionConsigneDataArgs
            {
                timeStampMS = timeStamp,
                M1Erreur = M1Error,
                M2Erreur = M2Error,
                M3Erreur = M3Error,
                M4Erreur = M4Error,
                M1Correction = M1Correction,
                M2Correction = M2Correction,
                M3Correction = M3Correction,
                M4Correction = M4Correction,
                M1ConsigneFromRobot = M1ConsigneRobot,
                M2ConsigneFromRobot = M2ConsigneRobot,
                M3ConsigneFromRobot = M3ConsigneRobot,
                M4ConsigneFromRobot = M4ConsigneRobot
            });
        }

        public event EventHandler<Independant2WheelsPidErrorCorrectionConsigneDataArgs> On2WheelsSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent;
        public virtual void On2WheelsSpeedIndependantPidDebugDataFromRobot(uint timeStamp, double M1Error, double M2Error, 
                                                                        double M1Correction, double M2Correction, 
                                                                        double M1ConsigneRobot, double M2ConsigneRobot)
        {
            On2WheelsSpeedIndependantPidErrorCorrectionConsigneDataFromRobotGeneratedEvent?.Invoke(this, new Independant2WheelsPidErrorCorrectionConsigneDataArgs
            {
                timeStampMS = timeStamp,
                M1Erreur = M1Error,
                M2Erreur = M2Error,
                M1Correction = M1Correction,
                M2Correction = M2Correction,
                M1ConsigneFromRobot = M1ConsigneRobot,
                M2ConsigneFromRobot = M2ConsigneRobot,
            });
        }


        public event EventHandler<PolarPidCorrectionArgs> On4WheelsSpeedPolarPidCorrectionsFromRobotEvent;
        public virtual void On4WheelsSpeedPolarPidCorrectionDataFromRobot(double corrPx, double corrIx, double corrDx, double corrPy, double corrIy, double corrDy, double corrPTheta, double corrITheta, double corrDTheta)
        {
            On4WheelsSpeedPolarPidCorrectionsFromRobotEvent?.Invoke(this, new PolarPidCorrectionArgs
            {
                CorrPx = corrPx,
                CorrIx = corrIx,
                CorrDx = corrDx,
                CorrPy = corrPy,
                CorrIy = corrIy,
                CorrDy = corrDy,
                CorrPTheta = corrPTheta,
                CorrITheta = corrITheta,
                CorrDTheta = corrDTheta
            });
        }

        public event EventHandler<IndependantPidCorrectionArgs> On4WheelsSpeedIndependantPidCorrectionsFromRobotEvent;
        public virtual void On4WheelsSpeedIndependantPidCorrectionDataFromRobot(double corrPM1, double corrIM1, double corrDM1, double corrPM2, double corrIM2, double corrDM2, double corrPM3, double corrIM3, double corrDM3, double corrPM4, double corrIM4, double corrDM4)
        {
            On4WheelsSpeedIndependantPidCorrectionsFromRobotEvent?.Invoke(this, new IndependantPidCorrectionArgs
            {
                CorrPM1 = corrPM1,
                CorrIM1 = corrIM1,
                CorrDM1 = corrDM1,
                CorrPM2 = corrPM2,
                CorrIM2 = corrIM2,
                CorrDM2 = corrDM2,
                CorrPM3 = corrPM3,
                CorrIM3 = corrIM3,
                CorrDM3 = corrDM3,
                CorrPM4 = corrPM4,
                CorrIM4 = corrIM4,
                CorrDM4 = corrDM4
            });
        }


        public event EventHandler<PolarPidCorrectionArgs> On2WheelsSpeedPolarPidCorrectionDataFromRobotEvent;
        public virtual void On2WheelsSpeedPolarPidCorrectionDataFromRobot(double corrPx, double corrIx, double corrDx, double corrPTheta, double corrITheta, double corrDTheta)
        {
            On2WheelsSpeedPolarPidCorrectionDataFromRobotEvent?.Invoke(this, new PolarPidCorrectionArgs
            {
                CorrPx = corrPx,
                CorrIx = corrIx,
                CorrDx = corrDx,
                CorrPTheta = corrPTheta,
                CorrITheta = corrITheta,
                CorrDTheta = corrDTheta
            });
        }

        public event EventHandler<IndependantPidCorrectionArgs> On2WheelsSpeedIndependantPidCorrectionDataFromRobotEvent;
        public virtual void On2WheelsSpeedIndependantPidCorrectionDataFromRobot(double corrPM1, double corrIM1, double corrDM1, double corrPM2, double corrIM2, double corrDM2)
        {
            On2WheelsSpeedIndependantPidCorrectionDataFromRobotEvent?.Invoke(this, new IndependantPidCorrectionArgs
            {
                CorrPM1 = corrPM1,
                CorrIM1 = corrIM1,
                CorrDM1 = corrDM1,
                CorrPM2 = corrPM2,
                CorrIM2 = corrIM2,
                CorrDM2 = corrDM2
            });
        }

        public event EventHandler<MsgCounterArgs> OnMessageCounterEvent;
        public virtual void OnMessageCounter(int nbMessageFromImu, int nbMessageFromOdometry)
        {
            var handler = OnMessageCounterEvent;
            if (handler != null)
            {
                handler(this, new MsgCounterArgs { nbMessageIMU = nbMessageFromImu, nbMessageOdometry = nbMessageFromOdometry });
            }
        }
        
    }
}
