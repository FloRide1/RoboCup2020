using Constants;
using EventArgsLibrary;
using System;
using System.Text;
using System.Timers;
using Utilities;

namespace RobotMessageProcessor
{
    public enum Competition
    {
        RoboCup = 1,
        Eurobot=2,
    }
    public class RobotMsgProcessor
    {
        Competition chosenCompetition;
        Timer tmrComptageMessage;
        public RobotMsgProcessor(Competition type)
        {
            chosenCompetition = type;
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
                case (short)Commands.WelcomeMessage:
                    {
                        OnWelcomeMessageFromRobot();
                    }
                    break;

                case (short)Commands.XYTheta_Speed:
                    {
                        nbMessageSpeedReceived++;
                        timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                        tab = payload.GetRange(4, 4);
                        float vX = tab.GetFloat();
                        tab = payload.GetRange(8, 4);
                        float vY = tab.GetFloat();
                        tab = payload.GetRange(12, 4);
                        float vTheta = tab.GetFloat();
                        OnSpeedDataFromRobot(timeStamp, vX, vY, vTheta);

                    }
                    break;
                case (short)Commands.IMUData:
                    {
                        float accelX=0, accelY=0, accelZ=0, gyroX=0, gyroY=0, gyroZ=0;
                        timeStamp = 0;
                        switch (chosenCompetition)
                        {
                            case Competition.RoboCup:
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

                            case Competition.Eurobot: //La carte de mesure est placée verticalement
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

                case (short)Commands.MotorCurrents:
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
                case (short)Commands.MotorsVitesses:
                    uint time = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    tab = payload.GetRange(4, 4);
                    float vitesseMotor1 = tab.GetFloat();
                    tab = payload.GetRange(8, 4);
                    float vitesseMotor2 = tab.GetFloat();
                    tab = payload.GetRange(12, 4);
                    float vitesseMotor3 = tab.GetFloat();
                    tab = payload.GetRange(16, 4);
                    float vitesseMotor4 = tab.GetFloat();
                    tab = payload.GetRange(20, 4);
                    float vitesseMotor5 = tab.GetFloat();
                    tab = payload.GetRange(24, 4);
                    float vitesseMotor6 = tab.GetFloat();
                    tab = payload.GetRange(28, 4);
                    float vitesseMotor7 = tab.GetFloat();
                    //On envois l'event aux abonnés
                    OnMotorVitesseDataFromRobot(time, vitesseMotor1, vitesseMotor2, vitesseMotor3, vitesseMotor4, vitesseMotor5, vitesseMotor6, vitesseMotor7);
                    break;

                case (short)Commands.MotorsSpeedConsignes:
                    timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    tab = payload.GetRange(4, 4);
                    float consigneMotor1 = tab.GetFloat();
                    tab = payload.GetRange(8, 4);
                    float consigneMotor2 = tab.GetFloat();
                    tab = payload.GetRange(12, 4);
                    float consigneMotor3 = tab.GetFloat();
                    tab = payload.GetRange(16, 4);
                    float consigneMotor4 = tab.GetFloat();
                    tab = payload.GetRange(20, 4);
                    float consigneMotor5 = tab.GetFloat();
                    tab = payload.GetRange(24, 4);
                    float consigneMotor6 = tab.GetFloat();
                    tab = payload.GetRange(28, 4);
                    float consigneMotor7 = tab.GetFloat();
                    //On envois l'event aux abonnés
                    OnSpeedConsigneDataFromRobot(timeStamp, consigneMotor1, consigneMotor2, consigneMotor3, consigneMotor4, consigneMotor5, consigneMotor6, consigneMotor7);
                    break;

                case (short)Commands.EncoderRawData:
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

                case (short)Commands.IOValues:
                    timeStamp = (uint)(payload[3] | payload[2] << 8 | payload[1] << 16 | payload[0] << 24);
                    byte ioValue = payload[4];
                    OnIOValuesFromRobot(timeStamp, ioValue);
                    break;

                case (short)Commands.PowerMonitoringValues:
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

                case (short)Commands.PIDDebugData:
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
                    OnPIDDebugDataFromRobot(timeStamp, xError, yError, thetaError, xCorrection, yCorrection, thetaCorrection, xconsigne,yConsigne,thetaConsigne);
                    break;
                                       
                case (short)Commands.SpeedPidCorrectionData:
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
                    OnSpeedPidCorrectionDataFromRobot(CorrPx, CorrIx, CorrDx, CorrPy, CorrIy, CorrDy, CorrPTheta, CorrITheta, CorrDTheta);
                    break;

                case (short)Commands.EnableDisableMotors:
                    bool value = Convert.ToBoolean(payload[0]);
                    //On envois l'event aux abonnés
                    OnEnableDisableMotorsACKFromRobot(value);
                    break;
                case (short)Commands.EnableDisableTir:
                    value = Convert.ToBoolean(payload[0]);
                    //On envois l'event aux abonnés
                    OnEnableDisableTirACKFromRobot(value);
                    break;
                case (short)Commands.EnableAsservissement:
                    value = Convert.ToBoolean(payload[0]);
                    //On envois l'event aux abonnés
                    OnEnableAsservissementACKFromRobot(value);
                    break;
                case (short)Commands.EnableMotorCurrent:
                    value = Convert.ToBoolean(payload[0]);
                    //On envois l'event aux abonnés
                    OnEnableMotorCurrentACKFromRobot(value);
                    break;
                case (short)Commands.EnableEncoderRawData:
                    value = Convert.ToBoolean(payload[0]);
                    //On envois l'event aux abonnés
                    OnEnableEncoderRawDataACKFromRobot(value);
                    break;
                case (short)Commands.EnablePositionData:
                    value = Convert.ToBoolean(payload[0]);
                    //On envois l'event aux abonnés
                    OnEnablePositionDataACKFromRobot(value);
                    break;
                case (short)Commands.EnableMotorSpeedConsigne:
                    value = Convert.ToBoolean(payload[0]);
                    //On envois l'event aux abonnés
                    OnEnableMotorSpeedConsigneDataACKFromRobot(value);
                    break;
                case (short)Commands.ErrorTextMessage:
                    string errorMsg = Encoding.UTF8.GetString(payload);
                    //On envois l'event aux abonnés
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

        public event EventHandler<BoolEventArgs> OnEnableAsservissementACKFromRobotGeneratedEvent;
        public virtual void OnEnableAsservissementACKFromRobot(bool isEnabled)
        {
            var handler = OnEnableAsservissementACKFromRobotGeneratedEvent;
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

        public event EventHandler<BoolEventArgs> OnEnablePositionDataACKFromRobotGeneratedEvent;
        public virtual void OnEnablePositionDataACKFromRobot(bool isEnabled)
        {
            var handler = OnEnablePositionDataACKFromRobotGeneratedEvent;
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

        public event EventHandler<StringEventArgs> OnErrorTextFromRobotGeneratedEvent;
        public virtual void OnErrorTextFromRobot(string str)
        {
            var handler = OnErrorTextFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new StringEventArgs { value = str });
            }
        }

        public event EventHandler<SpeedDataEventArgs> OnSpeedDataFromRobotGeneratedEvent;
        public virtual void OnSpeedDataFromRobot(uint timeStamp, double vX, double vY, double vTheta)
        {
            var handler = OnSpeedDataFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new SpeedDataEventArgs
                {
                    EmbeddedTimeStampInMs = timeStamp,
                    Vx = (float)vX,
                    Vy = (float)vY,
                    Vtheta = (float)vTheta
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

        public event EventHandler<MotorsVitesseDataEventArgs> OnMotorVitesseDataFromRobotGeneratedEvent;
        public virtual void OnMotorVitesseDataFromRobot(uint timeStamp, double m1, double m2, double m3,
                                                                        double m4, double m5, double m6, double m7)
        {
            var handler = OnMotorVitesseDataFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new MotorsVitesseDataEventArgs {
                    timeStampMS = timeStamp,
                    vitesseMotor1 = m1,
                    vitesseMotor2 = m2,
                    vitesseMotor3 = m3,
                    vitesseMotor4 = m4,
                    vitesseMotor5 = m5,
                    vitesseMotor6 = m6,
                    vitesseMotor7 = m7
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

        public event EventHandler<MotorsVitesseDataEventArgs> OnSpeedConsigneDataFromRobotGeneratedEvent;
        public virtual void OnSpeedConsigneDataFromRobot(uint timeStamp, double m1, double m2, double m3,
                                                                        double m4, double m5, double m6, double m7)
        {
            var handler = OnSpeedConsigneDataFromRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new MotorsVitesseDataEventArgs
                {
                    timeStampMS = timeStamp,
                    vitesseMotor1 = m1,
                    vitesseMotor2 = m2,
                    vitesseMotor3 = m3,
                    vitesseMotor4 = m4,
                    vitesseMotor5 = m5,
                    vitesseMotor6 = m6,
                    vitesseMotor7 = m7
                });
            }
        }

        public event EventHandler<PIDDebugDataArgs> OnPIDDebugDataFromRobotGeneratedEvent;
        public virtual void OnPIDDebugDataFromRobot(uint timeStamp, double xError, double yError, double thetaError,
                                                                        double xCorrection, double yCorrection, double thetaCorrection, double xConsigneRobot, double yConsigneRobot, double thetaConsigneRobot)
        {
            OnPIDDebugDataFromRobotGeneratedEvent?.Invoke(this, new PIDDebugDataArgs
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


        public event EventHandler<PIDCorrectionArgs> OnSpeedPidCorrectionDataFromRobotEvent;
        public virtual void OnSpeedPidCorrectionDataFromRobot(double corrPx, double corrIx, double corrDx, double corrPy, double corrIy, double corrDy, double corrPTheta, double corrITheta, double corrDTheta)
        {
            OnSpeedPidCorrectionDataFromRobotEvent?.Invoke(this, new PIDCorrectionArgs
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
