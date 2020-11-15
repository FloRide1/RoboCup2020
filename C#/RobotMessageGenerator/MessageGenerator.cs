using System;
using EventArgsLibrary;
using Utilities;
using Constants;
using System.Linq;
using HerkulexManagerNS;

namespace RobotMessageGenerator
{
    public class MsgGenerator
    {
        //Input events
        public void GenerateMessageSetSpeedConsigneToRobot(object sender, PolarSpeedArgs e)
        {
            byte[] payload = new byte[12];
            payload.SetValueRange(((float)e.Vx).GetBytes(), 0);
            payload.SetValueRange(((float)e.Vy).GetBytes(), 4);
            payload.SetValueRange(((float)e.Vtheta).GetBytes(), 8);
            OnMessageToRobot((Int16)Commands.SetSpeedConsigne, 12, payload);
            OnSetSpeedConsigneToRobotReceived(e);
        }

        public event EventHandler<PolarSpeedArgs> OnSetSpeedConsigneToRobotReceivedEvent;
        public virtual void OnSetSpeedConsigneToRobotReceived(PolarSpeedArgs args)
        {
            OnSetSpeedConsigneToRobotReceivedEvent?.Invoke(this, args);
        }

        public void GenerateMessageSetIOPollingFrequencyToRobot(object sender, ByteEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0]= e.Value;
            OnMessageToRobot((Int16)Commands.SetIOPollingFrequency, 1, payload);
        }

        public void GenerateMessageSetSpeedConsigneToMotor(object sender, SpeedConsigneToMotorArgs e)
        {
            byte[] payload = new byte[5];
            payload.SetValueRange(((float)e.V).GetBytes(), 0);
            payload[4] = (byte)e.MotorNumber;
            OnMessageToRobot((Int16)Commands.SetMotorSpeedConsigne, 5, payload);
        }
        public void GenerateMessageTir(object sender, TirEventArgs e)
        {
            byte[] payload = new byte[4];
            payload.SetValueRange(((float)e.Puissance).GetBytes(), 0);
            OnMessageToRobot((Int16)Commands.TirCommand, 4, payload);
        }
        public void GenerateMessageMoveTirUp(object sender, EventArgs e)
        {
            OnMessageToRobot((Int16)Commands.MoveTirUp, 0, null);
        }
        
        public void GenerateMessageMoveTirDown(object sender, EventArgs e)
        {
            OnMessageToRobot((Int16)Commands.MoveTirDown, 0, null);
        }

        public void GenerateMessageEnablePowerMonitoring(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnablePowerMonitoring, 1, payload);
        }
        public void GenerateMessageEnableIOPolling(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableIOPolling, 1, payload);
        }

        public void GenerateMessageEnableDisableMotors(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableDisableMotors, 1, payload);
        }

        public void GenerateMessageForwardHerkulex(object sender, DataReceivedArgs e)
        {
            OnMessageToRobot((Int16)Commands.ForwardHerkulex, (short)e.Data.Length, e.Data);
        }

        public void GenerateMessageEnableDisableTir(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableDisableTir, 1, payload);
        }

        public void GenerateMessageSetAsservissementMode(object sender, ByteEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.Value);
            OnMessageToRobot((Int16)Commands.SetAsservissementMode, 1, payload);
        }

        public void GenerateMessageEnableAsservissementDebugData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableAsservissementDebugData, 1, payload);
        }

        public void GenerateMessageEnableSpeedPidCorrectionData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.SpeedPidEnableCorrectionData, 1, payload);
        }

        public void GenerateMessageEnableEncoderRawData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableEncoderRawData, 1, payload);
        }

        public void GenerateMessageEnableMotorCurrentData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableMotorCurrent, 1, payload);
        }

        public void GenerateMessageEnableMotorPositionData(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnablePositionData, 1, payload);
        }

        public void GenerateMessageEnableMotorSpeedConsigne(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);
            OnMessageToRobot((Int16)Commands.EnableMotorSpeedConsigne, 1, payload);
        }

        public void GenerateMessageSTOP(object sender, BoolEventArgs e)
        {
            byte[] payload = new byte[1];
            payload[0] = Convert.ToByte(e.value);

            OnMessageToRobot((Int16)Commands.EmergencySTOP, 1, payload);
        }

        public void GenerateMessageSetupSpeedPolarPIDToRobot(object sender, PolarPIDSetupArgs e)
        {
            byte[] payload = new byte[72];
            payload.SetValueRange(((float)(e.P_x)).GetBytes(), 0);
            payload.SetValueRange(((float)(e.I_x)).GetBytes(), 4);
            payload.SetValueRange(((float)(e.D_x)).GetBytes(), 8);
            payload.SetValueRange(((float)(e.P_y)).GetBytes(), 12);
            payload.SetValueRange(((float)(e.I_y)).GetBytes(), 16);
            payload.SetValueRange(((float)(e.D_y)).GetBytes(), 20);
            payload.SetValueRange(((float)(e.P_theta)).GetBytes(), 24);
            payload.SetValueRange(((float)(e.I_theta)).GetBytes(), 28);
            payload.SetValueRange(((float)(e.D_theta)).GetBytes(), 32);
            payload.SetValueRange(((float)(e.P_x_Limit)).GetBytes(), 36);
            payload.SetValueRange(((float)(e.I_x_Limit)).GetBytes(), 40);
            payload.SetValueRange(((float)(e.D_x_Limit)).GetBytes(), 44);
            payload.SetValueRange(((float)(e.P_y_Limit)).GetBytes(), 48);
            payload.SetValueRange(((float)(e.I_y_Limit)).GetBytes(), 52);
            payload.SetValueRange(((float)(e.D_y_Limit)).GetBytes(), 56);
            payload.SetValueRange(((float)(e.P_theta_Limit)).GetBytes(), 60);
            payload.SetValueRange(((float)(e.I_theta_Limit)).GetBytes(), 64);
            payload.SetValueRange(((float)(e.D_theta_Limit)).GetBytes(), 68);
            OnMessageToRobot((Int16)Commands.SetSpeedPolarPIDValues, 72, payload);
            OnMessageToDisplaySpeedPolarPidSetup(e);
        }

        public void GenerateMessageSetupSpeedIndependantPIDToRobot(object sender, IndependantPIDSetupArgs e)
        {
            byte[] payload = new byte[96];
            payload.SetValueRange(((float)(e.P_M1)).GetBytes(), 0);
            payload.SetValueRange(((float)(e.I_M1)).GetBytes(), 4);
            payload.SetValueRange(((float)(e.D_M1)).GetBytes(), 8);
            payload.SetValueRange(((float)(e.P_M2)).GetBytes(), 12);
            payload.SetValueRange(((float)(e.I_M2)).GetBytes(), 16);
            payload.SetValueRange(((float)(e.D_M2)).GetBytes(), 20);
            payload.SetValueRange(((float)(e.P_M3)).GetBytes(), 24);
            payload.SetValueRange(((float)(e.I_M3)).GetBytes(), 28);
            payload.SetValueRange(((float)(e.D_M3)).GetBytes(), 32);
            payload.SetValueRange(((float)(e.P_M4)).GetBytes(), 36);
            payload.SetValueRange(((float)(e.I_M4)).GetBytes(), 40);
            payload.SetValueRange(((float)(e.D_M4)).GetBytes(), 44);
            payload.SetValueRange(((float)(e.P_M1_Limit)).GetBytes(), 48);
            payload.SetValueRange(((float)(e.I_M1_Limit)).GetBytes(), 52);
            payload.SetValueRange(((float)(e.D_M1_Limit)).GetBytes(), 56);
            payload.SetValueRange(((float)(e.P_M2_Limit)).GetBytes(), 60);
            payload.SetValueRange(((float)(e.I_M2_Limit)).GetBytes(), 64);
            payload.SetValueRange(((float)(e.D_M2_Limit)).GetBytes(), 68);
            payload.SetValueRange(((float)(e.P_M3_Limit)).GetBytes(), 72);
            payload.SetValueRange(((float)(e.I_M3_Limit)).GetBytes(), 76);
            payload.SetValueRange(((float)(e.D_M3_Limit)).GetBytes(), 80);
            payload.SetValueRange(((float)(e.P_M4_Limit)).GetBytes(), 84);
            payload.SetValueRange(((float)(e.I_M4_Limit)).GetBytes(), 88);
            payload.SetValueRange(((float)(e.D_M4_Limit)).GetBytes(), 92);
            OnMessageToRobot((Int16)Commands.SetSpeedIndependantPIDValues, 96, payload);
            OnMessageToDisplaySpeedIndependantPidSetup(e);
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
        public event EventHandler<MessageToRobotArgs> OnMessageToRobotGeneratedEvent;
        public virtual void OnMessageToRobot(Int16 msgFunction, Int16 msgPayloadLength, byte[] msgPayload)
        {
            OnMessageToRobotGeneratedEvent?.Invoke(this, new MessageToRobotArgs { MsgFunction = msgFunction, MsgPayloadLength = msgPayloadLength, MsgPayload = msgPayload });
        }

        public event EventHandler<PolarPIDSetupArgs> OnMessageToDisplaySpeedPolarPidSetupEvent;
        public virtual void OnMessageToDisplaySpeedPolarPidSetup(PolarPIDSetupArgs setup)
        {
            OnMessageToDisplaySpeedPolarPidSetupEvent?.Invoke(this, setup);
        }

        public event EventHandler<IndependantPIDSetupArgs> OnMessageToDisplaySpeedIndependantPidSetupEvent;
        public virtual void OnMessageToDisplaySpeedIndependantPidSetup(IndependantPIDSetupArgs setup)
        {
            OnMessageToDisplaySpeedIndependantPidSetupEvent?.Invoke(this, setup);
        }

        //public event EventHandler<MessageToRobotArgs> OnMessageToRobotGeneratedEvent;
        //public virtual void OnMessageToRobot(Int16 msgFunction, Int16 msgPayloadLength, byte[] msgPayload)
        //{
        //    OnMessageToRobotGeneratedEvent?.Invoke(this, new MessageToRobotArgs { MsgFunction = msgFunction, MsgPayloadLength = msgPayloadLength, MsgPayload = msgPayload });
        //}
    }
}
