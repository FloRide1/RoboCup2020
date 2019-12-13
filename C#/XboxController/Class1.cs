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
        public int deadband = 3000;
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
            double vitessePriseBalle;
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

                vitessePriseBalle = (float)(gamepad.RightTrigger) / 2.55;
                if (gamepad.Buttons.HasFlag(GamepadButtonFlags.X))
                {
                    OnTirToRobot(robotName, 50);
                }

                if(gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp))
                {
                    OnMoveTirUpToRobot();
                }
                if (gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown))
                {
                    OnMoveTirDownToRobot();
                }
                OnSpeedConsigneToRobot(robotName, (float)Vy, (float)Vx, (float)Vtheta);
                OnPriseBalleToRobot(5, (float)vitessePriseBalle);
                OnPriseBalleToRobot(6, (float)-vitessePriseBalle);
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

        public delegate void OnTirEventHandler(object sender, TirEventArgs e);
        public event EventHandler<TirEventArgs> OnTirEvent;
        public virtual void OnTirToRobot(string name, float puissance)
        {
            var handler = OnTirEvent;
            if (handler != null)
            {
                handler(this, new TirEventArgs { RobotName = name, Puissance = puissance });
            }
        }

        public delegate void OnMoveTirUpEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> OnMoveTirUpEvent;
        public virtual void OnMoveTirUpToRobot()
        {
            var handler = OnMoveTirUpEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public delegate void OnMoveTirDownEventHandler(object sender, EventArgs e);
        public event EventHandler<EventArgs> OnMoveTirDownEvent;
        public virtual void OnMoveTirDownToRobot()
        {
            var handler = OnMoveTirDownEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        public delegate void OnPriseBalleEventHandler(object sender, SpeedConsigneToMotorArgs e);
        public event EventHandler<SpeedConsigneToMotorArgs> OnPriseBalleEvent;
        public virtual void OnPriseBalleToRobot(byte motorNumber, float vitesse)
        {
            var handler = OnPriseBalleEvent;
            if (handler != null)
            {
                handler(this, new SpeedConsigneToMotorArgs { MotorNumber = motorNumber, V = vitesse });
            }
        }
    }
}
