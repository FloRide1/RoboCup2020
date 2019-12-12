using EventArgsLibrary;
using SharpDX.XInput;
using System;
using System.Timers;

namespace XBoxController
{
    public class XBoxController
    {
        string robotName = "";
        Controller controller;
        Gamepad gamepad;
        public bool connected = false;
        public int deadband = 2500;
        public float leftTrigger, rightTrigger;

        Timer timerGamepad = new Timer(100);

        public XBoxController(string name)
        {
            robotName = name;
            controller = new Controller(UserIndex.One);
            connected = controller.IsConnected;

            timerGamepad.Elapsed += TimerGamepad_Elapsed;
            timerGamepad.Start();
        }

        private void TimerGamepad_Elapsed(object sender, ElapsedEventArgs e)
        {
            double Vx;
            double Vy;
            double Vtheta;

            if (controller.IsConnected)
            {
                gamepad = controller.GetState().Gamepad;

                if (Math.Abs((float)gamepad.LeftThumbX) < deadband)
                    Vx = 0;
                else
                    Vx = (float)gamepad.LeftThumbX / short.MinValue * 1;

                if (Math.Abs((float)gamepad.LeftThumbY) < deadband)
                    Vy = 0;
                else
                    Vy = (float)gamepad.LeftThumbY / short.MinValue * -1;

                if (Math.Abs((float)gamepad.RightThumbX) < deadband)
                    Vtheta = 0;
                else
                    Vtheta = (float)gamepad.RightThumbX / short.MinValue * 1;

                OnSpeedConsigneToRobot(robotName, (float)Vy, (float)Vx, (float)Vtheta);
            }
        }

        //Events générés en sortie
        public delegate void SpeedConsigneEventHandler(object sender, SpeedConsigneArgs e);
        public event EventHandler<SpeedConsigneArgs> OnSpeedConsigneEvent;
        public virtual void OnSpeedConsigneToRobot(string name, float vx, float vy, float vtheta)
        {
            var handler = OnSpeedConsigneEvent;
            if (handler != null)
            {
                handler(this, new SpeedConsigneArgs { RobotName = name, Vx = vx, Vy = vy, Vtheta = vtheta });
            }
        }
    }
}
