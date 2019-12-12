﻿using System;
using EventArgsLibrary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Constants;

namespace RobotMessageGenerator
{
    public class RobotMsgGenerator
    {
        //Input events
        public void GenerateMessageSetSpeedConsigneToRobot(object sender, SpeedConsigneArgs e)
        {
            byte[] payload = new byte[12];
            Int32 Vx = (Int32)(e.Vx*1000);
            Int32 Vy = (Int32)(e.Vy * 1000);
            Int32 Vtheta = (Int32)(e.Vtheta * 1000);

            payload.SetValueRange(Vx.GetBytes(), 0);
            payload.SetValueRange(Vy.GetBytes(), 4);
            payload.SetValueRange(Vtheta.GetBytes(), 8);


            //payload.SetValueRange(Vy.GetBytes(), 4);
            //payload.SetValueRange(Vtheta.GetBytes(), 8);

            OnMessageToRobot((Int16)Commands.SetSpeedConsigne, 12, payload);
        }

        public void GenerateMessageSetSpeedConsigneToMotor(object sender, SpeedConsigneToMotorArgs e)
        {
            byte[] payload = new byte[5];
            payload.SetValueRange(e.V.GetBytes(), 0);
            payload[4] = (byte)e.MotorNumber;
            OnMessageToRobot((Int16)Commands.SetMotorSpeedConsigne, 5, payload);
        }

        //public void GenerateTextMessage(object sender, EventArgsLibrary.SpeedConsigneArgs e)
        //{
        //    byte[] payload = new byte[12];
        //    payload.SetValueRange(e.Vx.GetBytes(), 0);
        //    payload.SetValueRange(e.Vy.GetBytes(), 4);
        //    payload.SetValueRange(e.Vtheta.GetBytes(), 8);
        //    OnMessageToRobot(Commands., 12, payload);
        //}

        //Output events
        public delegate void SpeedConsigneEventHandler(object sender, MessageToRobotArgs e);
        public event EventHandler<MessageToRobotArgs> OnMessageToRobotGeneratedEvent;
        public virtual void OnMessageToRobot(Int16 msgFunction, Int16 msgPayloadLength, byte[] msgPayload)
        {
            var handler = OnMessageToRobotGeneratedEvent;
            if (handler != null)
            {
                handler(this, new MessageToRobotArgs { MsgFunction = msgFunction, MsgPayloadLength = msgPayloadLength, MsgPayload=msgPayload});
            }
        }
    }
}
