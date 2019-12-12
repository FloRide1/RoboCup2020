using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageDecoder
{
    public class MsgDecoder
    {
        public MsgDecoder()
        {

        }
                       
        byte CalculateChecksum(int msgFunction,
                int msgPayloadLength, byte[] msgPayload)
        {
            byte checksum = 0;
            checksum ^= (byte)(msgFunction >> 8);
            checksum ^= (byte)(msgFunction >> 0);
            checksum ^= (byte)(msgPayloadLength >> 8);
            checksum ^= (byte)(msgPayloadLength >> 0);
            for (int i = 0; i < msgPayloadLength; i++)
            {
                checksum ^= msgPayload[i];
            }
            return checksum;
        }

        public enum StateReception
        {
            Waiting,
            FunctionMSB,
            FunctionLSB,
            PayloadLengthMSB,
            PayloadLengthLSB,
            Payload,
            CheckSum
        }

        StateReception rcvState = StateReception.Waiting;
        int msgDecodedFunction = 0;
        int msgDecodedPayloadLength = 0;
        byte[] msgDecodedPayload;
        int msgDecodedPayloadIndex = 0;

        private void DecodeMessage(byte c)
        {
            switch (rcvState)
            {
                case StateReception.Waiting:
                    if (c == 0xFE)
                        rcvState = StateReception.FunctionMSB;
                    break;
                case StateReception.FunctionMSB:
                    msgDecodedFunction = (Int16)(c << 8);
                    rcvState = StateReception.FunctionLSB;
                    break;
                case StateReception.FunctionLSB:
                    msgDecodedFunction += (Int16)(c << 0);
                    rcvState = StateReception.PayloadLengthMSB;
                    break;
                case StateReception.PayloadLengthMSB:
                    msgDecodedPayloadLength = (Int16)(c << 8);
                    rcvState = StateReception.PayloadLengthLSB;
                    break;
                case StateReception.PayloadLengthLSB:
                    msgDecodedPayloadLength += (Int16)(c << 0);
                    if (msgDecodedPayloadLength > 0)
                    {
                        if (msgDecodedPayloadLength < 1024)
                        {
                            msgDecodedPayloadIndex = 0;
                            msgDecodedPayload = new byte[msgDecodedPayloadLength];
                            rcvState = StateReception.Payload;
                        }
                        else
                        {
                            rcvState = StateReception.Waiting;
                        }
                    }
                    else
                        rcvState = StateReception.CheckSum;
                    break;
                case StateReception.Payload:
                    msgDecodedPayload[msgDecodedPayloadIndex++] = c;
                    if (msgDecodedPayloadIndex >= msgDecodedPayloadLength)
                    {
                        rcvState = StateReception.CheckSum;
                    }
                    break;
                case StateReception.CheckSum:
                    byte calculatedChecksum = CalculateChecksum(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
                    byte receivedChecksum = c;
                    if (calculatedChecksum == receivedChecksum)
                    {
                        //Lance l'event de fin de decodage
                        OnMessageDecoded(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
                    }
                    else
                    {
                        OnMessageDecodedError(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
                    }
                    rcvState = StateReception.Waiting;
                    break;
                default:
                    rcvState = StateReception.Waiting;
                    break;
            }
        }


        //Input CallBack        
        public void DecodeMsgReceived(object sender, DataReceivedArgs e)
        {
            foreach (var b in e.Data)
            {
                DecodeMessage(b);
            }
        }

        //Output Events
        public delegate void MessageDecodedEventHandler(object sender, MessageDecodedArgs e);
        public event EventHandler<MessageDecodedArgs> OnMessageDecodedEvent;
        public virtual void OnMessageDecoded(int msgFunction, int msgPayloadLength, byte[] msgPayload)
        {
            var handler = OnMessageDecodedEvent;
            if (handler != null)
            {
                handler(this, new MessageDecodedArgs { MsgFunction = msgFunction, MsgPayloadLength = msgPayloadLength, MsgPayload = msgPayload});
            }
        }


        public delegate void MessageDecodedErrorEventHandler(object sender, MessageDecodedArgs e);
        public event EventHandler<MessageDecodedArgs> OnMessageDecodedErrorEvent;
        public virtual void OnMessageDecodedError(int msgFunction, int msgPayloadLength, byte[] msgPayload)
        {
            var handler = OnMessageDecodedErrorEvent;
            if (handler != null)
            {
                handler(this, new MessageDecodedArgs { MsgFunction = msgFunction, MsgPayloadLength = msgPayloadLength, MsgPayload = msgPayload });
            }
        }
    }
}
