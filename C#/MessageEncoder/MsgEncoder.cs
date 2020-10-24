using EventArgsLibrary;
using System;

namespace MessageEncoder
{
    public class MsgEncoder
    {
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

        private void UartEncodeAndSendMessage(int msgFunction,
                int msgPayloadLength, byte[] msgPayload)
        {
            byte[] message = new byte[msgPayloadLength + 6];
            int pos = 0;
            message[pos++] = 0xFE;
            message[pos++] = (byte)(msgFunction >> 8);
            message[pos++] = (byte)(msgFunction >> 0);
            message[pos++] = (byte)(msgPayloadLength >> 8);
            message[pos++] = (byte)(msgPayloadLength >> 0);
            for (int i = 0; i < msgPayloadLength; i++)
            {
                message[pos++] = msgPayload[i];
            }
            message[pos++] = CalculateChecksum(msgFunction, msgPayloadLength, msgPayload);
            OnMessageEncoded(message);
        }
        
        public void EncodeMessageToRobot(object sender, EventArgsLibrary.MessageToRobotArgs e)
        {
            byte[] message = new byte[e.MsgPayloadLength + 6];
            int pos = 0;
            message[pos++] = 0xFE;
            message[pos++] = (byte)(e.MsgFunction >> 8);
            message[pos++] = (byte)(e.MsgFunction >> 0);
            message[pos++] = (byte)(e.MsgPayloadLength >> 8);
            message[pos++] = (byte)(e.MsgPayloadLength >> 0);
            for (int i = 0; i < e.MsgPayloadLength; i++)
            {
                message[pos++] = e.MsgPayload[i];
            }
            message[pos++] = CalculateChecksum(e.MsgFunction, e.MsgPayloadLength, e.MsgPayload);
            OnMessageEncoded(message);
        }

        //public delegate void MessageEncodedEventHandler(object sender, MessageEncodedArgs e);
        public event EventHandler<MessageEncodedArgs> OnMessageEncodedEvent;
        public virtual void OnMessageEncoded( byte[] msg)
        {
            var handler = OnMessageEncodedEvent;
            if (handler != null)
            {
                handler(this, new MessageEncodedArgs { Msg = msg });
            }
        }
    }
}
