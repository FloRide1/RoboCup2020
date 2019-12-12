using AdvancedTimers;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using Utilities;
using WorldMap;

namespace PhysicalGameSimulator
{
    public class PhysicalSimulator
    {
        Dictionary<string, PhysicalRobotSimulator> robotList = new Dictionary<string, PhysicalRobotSimulator>();
        double fSampling = 50;

        HighFreqTimer highFrequencyTimer;

        public PhysicalSimulator()
        {
            highFrequencyTimer = new HighFreqTimer(fSampling);
            highFrequencyTimer.Tick += HighFrequencyTimer_Tick;
            highFrequencyTimer.Start();
        }

        public void RegisterRobot(string name, double xpos, double yPos)
        {
            robotList.Add(name, new PhysicalRobotSimulator(xpos, yPos));
        }

        private void HighFrequencyTimer_Tick(object sender, EventArgs e)
        {
            double newTheoricalX;
            double newTheoricalY;
            double newTheoricalTheta;
            //Calcul des déplacements théoriques des robots
            foreach (var robot in robotList)
            {
                newTheoricalX = robot.Value.X + (robot.Value.Vx * Math.Cos(robot.Value.Theta) - robot.Value.Vy * Math.Sin(robot.Value.Theta)) / fSampling;
                newTheoricalY = robot.Value.Y + (robot.Value.Vx * Math.Sin(robot.Value.Theta) + robot.Value.Vy * Math.Cos(robot.Value.Theta)) / fSampling;
                newTheoricalTheta = robot.Value.Theta + robot.Value.Vtheta / fSampling;

                bool collision = false;
                //Vérification d'éventuelles collisions.
                //On check les murs 
                if ((newTheoricalX + robot.Value.radius > 13) || (newTheoricalX - robot.Value.radius < -13)
                    || (newTheoricalY + robot.Value.radius > 9) || (newTheoricalY - robot.Value.radius < -9))
                {
                    collision = true;
                }

                //On check les autres robots
                foreach(var otherRobot in robotList)
                {
                    if (otherRobot.Key != robot.Key) //On exclu le test entre robots identiques
                    {
                        double newTheoricalXotherRobot = otherRobot.Value.X + (otherRobot.Value.Vx * Math.Cos(otherRobot.Value.Theta) - otherRobot.Value.Vy * Math.Sin(otherRobot.Value.Theta)) / fSampling;
                        double newTheoricalYotherRobot = otherRobot.Value.Y + (otherRobot.Value.Vx * Math.Sin(otherRobot.Value.Theta) + otherRobot.Value.Vy * Math.Cos(otherRobot.Value.Theta)) / fSampling;

                        if (Toolbox.Distance(newTheoricalX, newTheoricalY, newTheoricalXotherRobot, newTheoricalYotherRobot) < robot.Value.radius * 2)
                            collision = true;
                    }
                }

                //Validation des déplacements
                if (!collision)
                {
                    robot.Value.X = newTheoricalX;
                    robot.Value.Y = newTheoricalY;
                    robot.Value.Theta = newTheoricalTheta;
                }
                else
                {
                    robot.Value.Vx = 0;
                    robot.Value.Vy = 0;
                    robot.Value.Vtheta = 0;
                }

                //Emission d'un event de position physique 
                Location loc = new Location((float)robot.Value.X, (float)robot.Value.Y, (float)robot.Value.Theta, (float)robot.Value.Vx, (float)robot.Value.Vy, (float)robot.Value.Vtheta);
                OnPhysicalPosition(robot.Key, loc);
            }
        }

        public void SetRobotSpeed(object sender, SpeedConsigneArgs e)
        {
            if (robotList.ContainsKey(e.RobotName))
            {
                robotList[e.RobotName].Vx = e.Vx;
                robotList[e.RobotName].Vy = e.Vy;
                robotList[e.RobotName].Vtheta = e.Vtheta;
            }
        }

        //Output events
        public delegate void PhysicalPositionEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnPhysicalPositionEvent;
        public virtual void OnPhysicalPosition(string name, Location location)
        {
            var handler = OnPhysicalPositionEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotName = name, Location = location});
            }
        }
    }

    public class PhysicalRobotSimulator
    {
        public double radius = 0.25;
        public double X;
        public double Y;
        public double Theta;

        public double Vx;
        public double Vy;
        public double Vtheta;

        public PhysicalRobotSimulator(double xPos, double yPos)
        {
            X = xPos;
            Y = yPos;
        }
    }
}
