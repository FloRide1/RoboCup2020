using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HerkulexManagerNS
{
    public class Servo
    {
        private byte ID;
        private HerkulexDescription.JOG_MODE Mode;
        private HerkulexDescription.LedColor LEDState;
        private UInt16 TargetAbsolutePosition;
        public bool IsNextOrderSynchronous = false;

        public byte _playtime;

        private byte _SET;

        //values
        public UInt16 AbsolutePosition;
        public UInt16 CalibratedPosition;

        //flags
        public bool IsMoving;
        public bool IsInposition;
        public bool IsMotorOn;

        //error details
        public bool CheckSumError;
        public bool UnknownCommandError;
        public bool ExceedRegRangeError;
        public bool GarbageDetectedError;

        //errors
        public bool Exceed_input_voltage_limit;
        public bool Exceed_allowed_pot_limit;
        public bool Exceed_Temperature_limit;
        public bool Invalid_packet;
        public bool Overload_detected;
        public bool Driver_fault_detected;
        public bool EEP_REG_distorted;

        public Servo(byte pID, HerkulexDescription.JOG_MODE mode)
        {
            ID = pID;
            Mode = mode;
        }

        public byte GetSETByte()
        {
            _SET = 0;
            _SET |= (byte)((byte)Mode << 1);
            _SET |= (byte)((byte)LEDState << 2);
            return _SET;
        }

        public void SetAbsolutePosition(ushort absolutePosition)
        {
            TargetAbsolutePosition = absolutePosition;
        }

        public ushort GetTargetAbsolutePosition()
        {
            return TargetAbsolutePosition;
        }

        public void SetPlayTime(byte playTime)
        {
            _playtime = playTime;
        }

        public byte GetPlaytime()
        {
            return _playtime;
        }

        public void SetLedColor(HerkulexDescription.LedColor color)
        {
            LEDState = color;
        }

        public HerkulexDescription.LedColor GetTargetLedColor()
        {
            return LEDState;
        }

        public byte GetID()
        {
            return ID;
        }
    }
}
