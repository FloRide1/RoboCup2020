using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HerkulexManagerNS
{
    public class HerkulexEventArgs
    {
        #region LowLevelEventArgs
        /// <summary>
        /// Herkulex : packetDecoded args
        /// </summary>
        public class HklxPacketDecodedArgs : EventArgs
        {
            public byte PacketSize { get; set; }
            public byte PID { get; set; }
            public byte CMD { get; set; }
            public byte CheckSum1 { get; set; }
            public byte CheckSum2 { get; set; }
            public byte[] PacketData { get; set; }
            public byte StatusError;
            public byte StatusDetail;
        }

        /// <summary>
        /// Herkulex : Checksum error occured at reception
        /// </summary>
        public class HklxCheckSumErrorOccured : EventArgs
        {
            public byte CheckSum1 { get; set; }
            public byte CheckSum2 { get; set; }
            public byte PID;
        }

        /// <summary>
        /// Herkulex : EEPWRITE ack
        /// </summary>
        public class Hklx_EEP_WRITE_Ack_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Herkulex : EEPREAD ack
        /// </summary>
        public class Hklx_EEP_READ_Ack_Args : EventArgs
        {
            public byte[] ReceivedData;
            public byte Address;
            public byte Length;
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Heckulex : RAMWRITE ack
        /// </summary>
        public class Hklx_RAM_WRITE_Ack_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Herkulex : RAMREAD ack
        /// </summary>
        public class Hklx_RAM_READ_Ack_Args : EventArgs
        {
            public byte[] ReceivedData;
            public byte Address;
            public byte Length;
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Herkulex : I_JOG ack
        /// </summary>
        public class Hklx_I_JOG_Ack_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Herkulex : S_JOG ack
        /// </summary>
        public class Hklx_S_JOG_Ack_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Herkulex : STAT ack
        /// </summary>
        public class Hklx_STAT_Ack_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Herkulex : ROLLBACK ack
        /// </summary>
        public class Hklx_ROLLBACK_Ack_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        /// <summary>
        /// Hekulex : REBOOT ack
        /// </summary>
        public class Hklx_REBOOT_Ack_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        public class Hklx_AnyAck_Args : EventArgs
        {
            public List<HerkulexDescription.ErrorStatus> StatusErrors;
            public List<HerkulexDescription.ErrorStatusDetail> StatusDetails;
            public byte PID;
        }

        #endregion LowLevelEventArgs

        #region OutputEventArgs

        public class HerkulexServoInformationArgs : EventArgs
        {
            public Servo Servo;
        }
        
        public class HerkulexPositionsReceivedArgs : EventArgs
        {
            public Dictionary<int, int> servoPositions;

        }

        public class HerkulexErrorArgs : EventArgs
        {
            public Servo Servo;
        }

        #endregion OutputEventArgs
    }
}
