using EventArgsLibrary;
using ExtendedSerialPort;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static HerkulexManagerNS.HerkulexEventArgs;

namespace HerkulexManagerNS
{
    public class HerkulexManager
    {
        private int pollingTimeoutMs = 5000;

        #region classInst

        private AutoResetEvent WaitingForAck = new AutoResetEvent(false);
        private ConcurrentDictionary<ServoId, Servo> Servos = new ConcurrentDictionary<ServoId, Servo>();
        private ReliableSerialPort serialPort { get; set; }
        private HerkulexDecoder decoder;

        private System.Timers.Timer pollingTimer = new System.Timers.Timer(100);

        private Stopwatch sw = new Stopwatch();

        private ConcurrentQueue<byte[]> messageQueue = new ConcurrentQueue<byte[]>();

        Thread SendingThread;

        #endregion classInst

        public HerkulexManager(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {

            serialPort = new ReliableSerialPort(portName, baudRate, parity, dataBits, stopBits);
            serialPort.Open();

            decoder = new HerkulexDecoder();
            pollingTimer.Interval = 250;

            serialPort.OnDataReceivedEvent += decoder.DecodePacket;
            decoder.OnAnyAckEvent += AckReceived;
            decoder.OnRamReadAckEvent += Decoder_OnRamReadAckEvent;
            pollingTimer.Elapsed += PollingTimer_Elapsed;

            sw.Start();

            SendingThread = new Thread(SendingThreadProcessing);
            SendingThread.IsBackground = true;
            SendingThread.Start();
        }

        void SendingThreadProcessing()
        {
            while(true)
            {
                while(messageQueue.Count()>0)
                {
                    byte[] message;
                    if (messageQueue.TryDequeue(out message))
                    {
                        if (message != null)
                        {
                            if (serialPort.IsOpen)
                            {
                                serialPort.Write(message, 0, message.Length);
                                //Thread.Sleep(4);
                                bool IsAckReceived = WaitingForAck.WaitOne(pollingTimeoutMs);
                            }
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }
        
        #region userMethods

        public void SetPollingInterval(int intervalMs)
        {
            pollingTimer.Interval = intervalMs;
        }

        public void StartPolling()
        {
            pollingTimer.Start();
        }

        public void StopPolling()
        {
            pollingTimer.Stop();
        }

        /// <summary>
        /// Sets the servo acknowledge timeout
        /// </summary>
        /// <param name="TimeoutMs">Timeout in ms</param>
        public void SetAckTimeout(int TimeoutMs)
        {
            pollingTimeoutMs = TimeoutMs;
        }

        /// <summary>
        /// Sets the torque mode on the servo
        /// </summary>
        /// <param name="ID">Servo ID</param>
        /// <param name="mode">Torque mode</param>
        public void SetTorqueMode(ServoId ID, HerkulexDescription.TorqueControl mode)
        {
            if (Servos.ContainsKey(ID))
                _SetTorqueMode(ID, mode);
            else
                throw new Exception("The servo ID is not in the dictionary");
        }

        /// <summary>
        /// Changes the servo led color
        /// </summary>
        /// <param name="ID">Servo ID</param>
        /// <param name="color">Led color</param>
        public void SetLedColor(ServoId ID, HerkulexDescription.LedColor color)
        {
            if (Servos.ContainsKey(ID))
            {
                Servos[ID].SetLedColor(color);
                _SetLedColor(ID, color);
            }
            else
                throw new Exception("The servo ID is not in the dictionnary");
        }

        /// <summary>
        /// Sends the synchronous servo buffer
        /// </summary>
        public void SendSynchronous(byte playtime)
        {
            S_JOG(Servos, playtime);
        }


        ///// <summary>
        ///// Sets the target absolute position of the servo
        ///// </summary>
        ///// <param name="ID">Servo ID</param>
        ///// <param name="absolutePosition">Absolute position</param>
        ///// <param name="playTime">Playtime</param>
        //public void SetPosition(ServoId ID, ushort absolutePosition, byte playTime, bool IsSynchronous = false)
        //{
        //    if (Servos.ContainsKey(ID))
        //    {
        //        SetPosition(ID, absolutePosition, playTime);
        //        //Servos[ID].SetAbsolutePosition(absolutePosition);
        //        //Servos[ID].SetPlayTime(playTime);

        //        ////if (IsSynchronous)
        //        ////{
        //        ////    Servos[ID].IsNextOrderSynchronous = true; //On clear le flag à l'envoi synchrone
        //        ////}
        //        //else
        //        //{
        //        //    foreach (KeyValuePair<ServoId, Servo> IdServoPair in Servos)
        //        //    {
        //        //        I_JOG(IdServoPair.Value);
        //        //        IdServoPair.Value.IsNextOrderSynchronous = false;
        //        //    }
        //        //}
        //    }
        //    else
        //        throw new Exception("The servo ID is not in the dictionnary");
        //}

        /// <summary>
        /// Sets the maximum absolute position of the servo
        /// </summary>
        /// <param name="ID">Servo ID</param>
        /// <param name="position">Maximum absolute position</param>
        /// <param name="keepAfterReboot">weither to keep the change after a servo reboot</param>
        public void SetMaximumPosition(ServoId ID, UInt16 position, bool keepAfterReboot = true)
        {
            _SetMaxAbsolutePosition(ID, position, keepAfterReboot);
        }

        /// <summary>
        /// Sets the minimum absolute position of the servo
        /// </summary>
        /// <param name="ID">Servo ID</param>
        /// <param name="position">Minimum absolute position</param>
        /// <param name="keepAfterReboot">weither to keep the change after a servo reboot</param>
        public void SetMinimumPosition(ServoId ID, UInt16 position, bool keepAfterReboot = true)
        {
            _SetMinAbsolutePosition(ID, position, keepAfterReboot);
        }

        /// <summary>
        /// Adds a servo to the controller
        /// </summary>
        /// <param name="ID">Servo ID</param>
        /// <param name="mode">JOG mode</param>
        public void AddServo(ServoId ID, HerkulexDescription.JOG_MODE mode)
        {
            Servo servo = new Servo(ID, mode);
            while (!Servos.TryAdd(ID, servo)) ; //ON tente l'ajout tant qu'il n'est pas validé
            //reply to all packets
            RAM_WRITE(ID, HerkulexDescription.RAM_ADDR.ACK_Policy, 1, 0x02); //reply to I_JOG / S_JOG
            RecoverErrors(ID);
            Thread.Sleep(100);
        }
        //public void AddServo(ServoId ID, HerkulexDescription.JOG_MODE mode, UInt16 initialPosition)
        //{
        //    Servo servo = new Servo(ID, mode);

        //    while (!Servos.TryAdd(ID, servo)) ; //ON tente l'ajout tant qu'il n'est pas validé

        //    Servos[ID].SetAbsolutePosition(initialPosition);
        //    //reply to all packets
        //    RAM_WRITE(ID, HerkulexDescription.RAM_ADDR.ACK_Policy, 1, 0x02); //reply to I_JOG / S_JOG
        //    RecoverErrors(servo);
        //    //RAM_WRITE(ID, HerkulexDescription.RAM_ADDR.Absolute_Goal_Position, 1, 512);
        //    I_JOG(Servos[ID]);
        //    Thread.Sleep(100);
        //}

        /// <summary>
        /// Recovers the servo from error state
        /// </summary>
        /// <param name="servo">Servo instance</param>
        public void RecoverErrors(ServoId servo)
        {
            ClearAllErrors(servo);
            SetTorqueMode(servo, HerkulexDescription.TorqueControl.TorqueOn);
        }

        #endregion userMethods


        #region internalMethods

        //polling timer
        private void PollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            for (int i = 0; i < Servos.Keys.Count; i++)
            {
                var key = Servos.Keys.ElementAt(i);
                RAM_READ(key, HerkulexDescription.RAM_ADDR.Absolute_Position, 2);
                RAM_READ(key, HerkulexDescription.RAM_ADDR.Calibrated_Position, 2);
            }
        }

        //Resultat du polling
        private void Decoder_OnRamReadAckEvent(object sender, HerkulexEventArgs.Hklx_RAM_READ_Ack_Args e)
        {
            Servo pulledServo = null;
            UInt16 result = 0;
            if (Servos.ContainsKey(e.PID))
            {
                //On récupère le servo dans le dictionnaire pour l'updater
                Servos.TryGetValue(e.PID, out pulledServo);

                switch ((HerkulexDescription.RAM_ADDR)e.Address)
                {
                    case HerkulexDescription.RAM_ADDR.Absolute_Position:
                        result = (UInt16)(e.ReceivedData[1] << 8);
                        result += (UInt16)(e.ReceivedData[0] << 0);
                        pulledServo.AbsolutePosition = result;
                        break;
                    case HerkulexDescription.RAM_ADDR.Calibrated_Position:
                        result = (UInt16)(e.ReceivedData[1] << 8);
                        result += (UInt16)(e.ReceivedData[0] << 0);
                        //pulledServo.CalibratedPosition = (UInt16)(result); //Résultat sur 10 bits
                        pulledServo.CalibratedPosition = (UInt16)(result & 0x03FF); //Résultat sur 10 bits
                        break;
                    default:
                        //Autre trame reçue;
                        break;
                }

                //getting flags and error details
                pulledServo.IsMoving = (e.StatusDetails.Contains(HerkulexDescription.ErrorStatusDetail.Moving_flag) ? (true) : (false));
                pulledServo.IsInposition = (e.StatusDetails.Contains(HerkulexDescription.ErrorStatusDetail.Inposition_flag) ? (true) : (false));
                pulledServo.IsMotorOn = (e.StatusDetails.Contains(HerkulexDescription.ErrorStatusDetail.MOTOR_ON_flag) ? (true) : (false));
                pulledServo.CheckSumError = (e.StatusDetails.Contains(HerkulexDescription.ErrorStatusDetail.CheckSumError) ? (true) : (false));
                pulledServo.UnknownCommandError = (e.StatusDetails.Contains(HerkulexDescription.ErrorStatusDetail.Unknown_Command) ? (true) : (false));
                pulledServo.ExceedRegRangeError = (e.StatusDetails.Contains(HerkulexDescription.ErrorStatusDetail.Exceed_REG_RANGE) ? (true) : (false));
                pulledServo.GarbageDetectedError = (e.StatusDetails.Contains(HerkulexDescription.ErrorStatusDetail.Garbage_detected) ? (true) : (false));
                
                //getting errors
                pulledServo.Exceed_input_voltage_limit = (e.StatusErrors.Contains(HerkulexDescription.ErrorStatus.Exceed_input_voltage_limit)) ? (true) : (false);
                pulledServo.Exceed_allowed_pot_limit = (e.StatusErrors.Contains(HerkulexDescription.ErrorStatus.Exceed_allowed_pot_limit)) ? (true) : (false);
                pulledServo.Exceed_Temperature_limit = (e.StatusErrors.Contains(HerkulexDescription.ErrorStatus.Exceed_Temperature_limit)) ? (true) : (false);
                pulledServo.Invalid_packet = (e.StatusErrors.Contains(HerkulexDescription.ErrorStatus.Invalid_packet)) ? (true) : (false);
                pulledServo.Overload_detected = (e.StatusErrors.Contains(HerkulexDescription.ErrorStatus.Overload_detected)) ? (true) : (false);
                pulledServo.Driver_fault_detected = (e.StatusErrors.Contains(HerkulexDescription.ErrorStatus.Driver_fault_detected)) ? (true) : (false);
                pulledServo.EEP_REG_distorted = (e.StatusErrors.Contains(HerkulexDescription.ErrorStatus.EEP_REG_distorted)) ? (true) : (false);

                //if any error flag is true, set HerkulexErrorEvent
                if (pulledServo.Exceed_input_voltage_limit == true ||
                    pulledServo.Exceed_allowed_pot_limit == true ||
                    pulledServo.Exceed_Temperature_limit == true ||
                    pulledServo.Invalid_packet == true ||
                    pulledServo.Overload_detected == true ||
                    pulledServo.Driver_fault_detected == true ||
                    pulledServo.EEP_REG_distorted == true)
                {
                    OnHerkulexError(pulledServo);
                    //if (AutoRecoverMode == true)
                    //    RecoverErrors(pulledServo);
                }
                OnHerkulexServoInformation(pulledServo);
            }

        }

        //le seul endroit où le ACK reset event doit être set
        private void AckReceived(object sender, Hklx_AnyAck_Args e)
        {
            WaitingForAck.Set();
        }



        #endregion internalMethods

        #region outputEvents


        public void OnHerkulexPositionRequestEvent(object sender, HerkulexEventArgs.HerkulexPositionsArgs e)
        {
            foreach(var positionCommand in e.servoPositions)
            {
                SetPosition((ServoId)positionCommand.Key, (UInt16)positionCommand.Value, 5); //TODO : fgaut pas déconner non plus !
            }
        }

        public void OnEnableDisableServosRequestEvent(object sender, BoolEventArgs e)
        {
            foreach (var servo in Servos)
            {
                if (e.value == false)
                    SetTorqueMode(servo.Key, HerkulexDescription.TorqueControl.TorqueFree);
                else
                    SetTorqueMode(servo.Key, HerkulexDescription.TorqueControl.TorqueOn);
            }
        }

        public event EventHandler<HerkulexServoInformationArgs> OnHerkulexServoInformationEvent;
        public event EventHandler<HerkulexErrorArgs> HerkulexErrorEvent;

        /// <summary>
        /// Sets the torque control mode of the specified servo I.e BreakOn / TorqueOn / TorqueFree
        /// </summary>
        /// <param name="pID">Servo ID</param>
        /// <param name="mode">torque mode (TorqueControl enum)</param>
        private void _SetTorqueMode(ServoId pID, HerkulexDescription.TorqueControl mode)
        {
            RAM_WRITE(pID, HerkulexDescription.RAM_ADDR.Torque_Control, 1, (ushort)mode);
        }

        /// <summary>
        /// Servo polled event
        /// </summary>
        /// <param name="servo"></param>
        public virtual void OnHerkulexServoInformation(Servo servo)
        {
            var handler = OnHerkulexServoInformationEvent;
            if (handler != null)
            {
                handler(this, new HerkulexServoInformationArgs
                {
                    Servo = servo
                });
            }
        }

        /// <summary>
        /// Error occured event
        /// </summary>
        /// <param name="servo"></param>
        public virtual void OnHerkulexError(Servo servo)
        {
            //Ne doit être appelé que si il y a une erreur
            var handler = HerkulexErrorEvent;
            if (handler != null)
            {
                handler(this, new HerkulexManagerNS.HerkulexEventArgs.HerkulexErrorArgs
                {
                    Servo = servo
                });
            }
        }

        #endregion outputEvents

        #region LowLevelMethods

        private void S_JOG(ConcurrentDictionary<ServoId, Servo> servos, byte playTime)
        {
            byte[] dataToSend = new byte[1 + 4 * servos.Count];
            dataToSend[0] = playTime;
            byte dataOffset = 1;

            foreach (KeyValuePair<ServoId, Servo> servoIdPair in servos)
            {
                if (servoIdPair.Value.IsNextOrderSynchronous)
                {
                    dataToSend[dataOffset + 0] = (byte)(servoIdPair.Value.GetTargetAbsolutePosition() >> 0);
                    dataToSend[dataOffset + 1] = (byte)(servoIdPair.Value.GetTargetAbsolutePosition() >> 8);
                    dataToSend[dataOffset + 2] = servoIdPair.Value.GetSETByte();
                    dataToSend[dataOffset + 3] = (byte)servoIdPair.Value.GetID();
                    dataOffset += 4;
                    servoIdPair.Value.IsNextOrderSynchronous = false;
                }
            }

            EncodeAndEnqueuePacket(serialPort, ServoId.BroadCast, (byte)HerkulexDescription.CommandSet.S_JOG, dataToSend);
        }

        /// <summary>
        /// Writes to the specified EEP address, up to 2 bytes
        /// </summary>
        /// <param name="port"></param>
        /// <param name="pID"></param>
        /// <param name="startAddr"></param>
        /// <param name="length"></param>
        /// <param name="value"></param>
        private void EEP_WRITE(ServoId pID, byte startAddr, byte length, UInt16 value)
        {
            if (length > 2)
                return;

            byte[] data = new byte[2 + length];
            data[0] = (byte)startAddr;
            data[1] = length;

            if (length >= 2)
            {
                data[2] = (byte)(value >> 0);
                data[3] = (byte)(value >> 8);
            }
            else
                data[2] = (byte)(value);

            EncodeAndEnqueuePacket(serialPort, pID, (byte)HerkulexDescription.CommandSet.EEP_WRITE, data);
        }

        private void EEP_WRITE(ServoId pID, HerkulexDescription.EEP_ADDR startAddr, byte length, UInt16 value)
        {
            EEP_WRITE(pID, (byte)startAddr, length, value);
        }

        /// <summary>
        /// Sets the minimum allowed absolute position (0 to 1023)
        /// </summary>
        /// <param name="pID">Servo ID</param>
        /// <param name="minPosition">Minimum position</param>
        /// <param name="keepAfterReboot">Weither to keep the change after a reboot</param>
        private void _SetMinAbsolutePosition(ServoId pID, ushort minPosition, bool keepAfterReboot)
        {
            RAM_WRITE(pID, HerkulexDescription.RAM_ADDR.Min_Position, 2, minPosition);

            if (keepAfterReboot)
                EEP_WRITE(pID, HerkulexDescription.EEP_ADDR.Min_Position, 2, minPosition);
        }

        /// <summary>
        /// Sets the maximum allowed absolute position (0 to 1023)
        /// </summary>
        /// <param name="pID">Servo ID</param>
        /// <param name="maxPosition">Maximum position</param>
        /// <param name="keepAfterReboot">Weither to keep the changes after a reboot</param>
        private void _SetMaxAbsolutePosition(ServoId pID, ushort maxPosition, bool keepAfterReboot)
        {
            RAM_WRITE(pID, HerkulexDescription.RAM_ADDR.Max_Position, 2, maxPosition);

            if (keepAfterReboot)
                EEP_WRITE(pID, HerkulexDescription.EEP_ADDR.Max_Position, 2, maxPosition);
        }

        private void I_JOG(Servo servo)
        {
            byte[] dataToSend = new byte[5];
            dataToSend[0] = (byte)(servo.GetTargetAbsolutePosition() >> 0);
            dataToSend[1] = (byte)(servo.GetTargetAbsolutePosition() >> 8);
            dataToSend[2] = servo.GetSETByte();
            dataToSend[3] = (byte)servo.GetID();

            dataToSend[4] = servo.GetPlaytime();

            EncodeAndEnqueuePacket(serialPort, servo.GetID(), (byte)HerkulexDescription.CommandSet.I_JOG, dataToSend);
        }

        private void SetPosition(ServoId id, ushort targetPosition, byte playTime)
        {
            //On clear une éventuelle erreur
            RecoverErrors(id);

            byte[] dataToSend = new byte[5];
            dataToSend[0] = (byte)(targetPosition >> 0);
            dataToSend[1] = (byte)(targetPosition >> 8);
            dataToSend[2] = 0;// servo.GetSETByte();
            dataToSend[3] = (byte)id;

            dataToSend[4] = playTime;

            EncodeAndEnqueuePacket(serialPort, id, (byte)HerkulexDescription.CommandSet.I_JOG, dataToSend);
        }

        /// <summary>
        /// Sets the specified servo led color
        /// </summary>
        /// <param name="pID">Servo ID</param>
        /// <param name="color">Led color (LedColor enum)</param>
        private void _SetLedColor(ServoId pID, HerkulexDescription.LedColor color)
        {
            RAM_WRITE(pID, HerkulexDescription.RAM_ADDR.LED_Control, 1, (ushort)color);
        }

        /// <summary>
        /// Clears all of the servo error statuses
        /// </summary>
        /// <param name="pID">Servo ID</param>
        private void ClearAllErrors(ServoId pID)
        {
            RAM_WRITE(pID, HerkulexDescription.RAM_ADDR.Status_Error, 1, 0x00);
        }

        /// <summary>
        /// Writes to the specified RAM address, up to 2 bytes
        /// </summary>
        /// <param name="port">Serial port to use</param>
        /// <param name="pID">Servo ID</param>
        /// <param name="addr">Start memory address</param>
        /// <param name="length">Length of the data to write</param>
        /// <param name="value">data</param>
        private void RAM_WRITE(ServoId pID, byte addr, byte length, UInt16 value)
        {
            if (length > 2)
                return;

            byte[] data = new byte[2 + length];
            data[0] = (byte)addr;
            data[1] = length;

            if (length >= 2)
            {
                data[2] = (byte)(value >> 0); //little endian, LSB first
                data[3] = (byte)(value >> 8);
            }
            else
                data[2] = (byte)(value);

            EncodeAndEnqueuePacket(serialPort, pID, (byte)HerkulexDescription.CommandSet.RAM_WRITE, data);
        }

        private void RAM_WRITE(ServoId pID, HerkulexDescription.RAM_ADDR addr, byte length, UInt16 value)
        {
            RAM_WRITE(pID, (byte)addr, length, value);
        }

        /// <summary>
        /// Reads the specified number of bytes from RAM
        /// </summary>
        /// <param name="port">Serial port to use</param>
        /// <param name="pID">Servo ID</param>
        /// <param name="startAddr">Address to start from</param>
        /// <param name="length">Number of bytes to read</param>
        private void RAM_READ(ServoId pID, byte startAddr, byte length)
        {
            byte[] data = { (byte)startAddr, length };
            EncodeAndEnqueuePacket(serialPort, pID, (byte)HerkulexDescription.CommandSet.RAM_READ, data);
        }

        private void RAM_READ(ServoId pID, HerkulexDescription.RAM_ADDR startAddr, byte length)
        {
            RAM_READ(pID, (byte)startAddr, length);
        }

        private void EncodeAndEnqueuePacket(SerialPort port, ServoId pID, byte CMD, byte[] dataToSend)
        {
            byte packetSize = (byte)(7 + dataToSend.Length);
            byte[] packet = new byte[packetSize];

            packet[0] = 0xFF;
            packet[1] = 0xFF;
            packet[2] = packetSize;
            packet[3] = (byte)pID;
            packet[4] = CMD;
            packet[5] = CommonMethods.CheckSum1(packet[2], packet[3], packet[4], dataToSend);
            packet[6] = CommonMethods.CheckSum2(packet[5]);

            for (int i = 0; i < dataToSend.Length; i++)
                packet[7 + i] = dataToSend[i];

            messageQueue.Enqueue(packet);
            
            //serialPort.Write(packet, 0, packet.Length);
            //Console.WriteLine("Serial Port Write Finish : " + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
        }


        /// <summary>
        /// Encodes and sends a packet with the Herkulex protocol
        /// </summary>
        /// <param name="port">Serial port to use</param>
        /// <param name="pID">Servo ID</param>
        /// <param name="CMD">Command ID</param>
        private void EncodeAndEnqueuePacket(SerialPort port, byte pID, byte CMD)
        {
            byte[] packet = new byte[7];

            packet[0] = 0xFF;
            packet[1] = 0xFF;
            packet[2] = 7;
            packet[3] = pID;
            packet[4] = CMD;
            packet[5] = CommonMethods.CheckSum1(packet[2], packet[3], packet[4]);
            packet[6] = CommonMethods.CheckSum2(packet[5]);

            serialPort.Write(packet, 0, packet.Length);
            //Console.WriteLine("\nSerial Port Write Finish : " + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
        }
        private void STAT(byte pID)
        {
            EncodeAndEnqueuePacket(serialPort, pID, (byte)HerkulexDescription.CommandSet.STAT);
        }

        public byte[] ScanForServoIDs(int timeout = 1000, int minID = 1, int maxID = 0xFD)
        {
            byte[] ID_Buffer = new byte[0xFD];

            int count = 0;
            bool AckReceived = false;

            for (int ID = minID; ID < maxID + 1; ID++)
            {

                //Console.WriteLine("Envoi du STAT Début : " + sw.ElapsedMilliseconds + " " + DateTime.Now.Second + "." + DateTime.Now.Millisecond);
                STAT((byte)ID);
                //AckReceived = WaitingForAck.WaitOne(timeout);
                if (AckReceived)
                {
                    Console.WriteLine(ID + " Online");
                    ID_Buffer[count] = (byte)ID;
                    count++;
                }
                else
                    Console.WriteLine(ID + " Offline");
            }

            byte[] ID_Return = new byte[count];
            for (int i = 0; i < ID_Return.Length; i++)
                ID_Return[i] = ID_Buffer[i];

            return ID_Return;
        }

        #endregion LowLevelMethods
    }
}
