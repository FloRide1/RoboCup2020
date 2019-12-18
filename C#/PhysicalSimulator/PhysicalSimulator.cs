using AdvancedTimers;
using EventArgsLibrary;
using PerceptionManagement;
using System;
using System.Collections.Generic;
using Utilities;
using WorldMap;

namespace PhysicalSimulator
{
    public class PhysicalSimulator
    {
        Dictionary<int, PhysicalRobotSimulator> robotList = new Dictionary<int, PhysicalRobotSimulator>();
        double fSampling = 50;

        HighFreqTimer highFrequencyTimer;

        public PhysicalSimulator()
        {
            highFrequencyTimer = new HighFreqTimer(fSampling);
            highFrequencyTimer.Tick += HighFrequencyTimer_Tick;
            highFrequencyTimer.Start();
        }

        public void RegisterRobot(int id, double xpos, double yPos)
        {
            lock (robotList)
            {
                robotList.Add(id, new PhysicalRobotSimulator(xpos, yPos));
            }
        }

        private void HighFrequencyTimer_Tick(object sender, EventArgs e)
        {
            double newTheoricalX;
            double newTheoricalY;
            double newTheoricalTheta;
            //Calcul des déplacements théoriques des robots
            lock (robotList)
            {
                //On calcule le nouvelles positions théoriques si il n'y a pas collision
                foreach (var robot in robotList)
                {
                    robot.Value.newXWithoutCollision = robot.Value.X + (robot.Value.Vx * Math.Cos(robot.Value.Theta) - robot.Value.Vy * Math.Sin(robot.Value.Theta)) / fSampling;
                    robot.Value.newYWithoutCollision = robot.Value.Y + (robot.Value.Vx * Math.Sin(robot.Value.Theta) + robot.Value.Vy * Math.Cos(robot.Value.Theta)) / fSampling;
                    robot.Value.newThetaWithoutCollision = robot.Value.Theta + robot.Value.Vtheta / fSampling;
                }

                //TODO : Gérer les collisions polygoniales en déclenchant l'étude fine à l'aide d'un cercle englobant.
                //TODO : gérer la balle et les rebonds robots poteaux cages
                //TODO : gérer la perte d'énergie de la balle : modèle à trouver... mesure précise :) faite sur le terrain : 1m.s-1 -> arrêt à 10m
                //TODO : gérer le tir (ou passe)
                //TODO : gérer les déplacements balle au pied
                //TODO : gérer les cas de contestation

                foreach (var robot in robotList)
                {
                    bool collision = false;

                    //Vérification d'éventuelles collisions.
                    //On check les murs 
                    if ((robot.Value.newXWithoutCollision + robot.Value.radius > 13) || (robot.Value.newXWithoutCollision - robot.Value.radius < -13)
                        || (robot.Value.newYWithoutCollision + robot.Value.radius > 9) || (robot.Value.newYWithoutCollision - robot.Value.radius < -9))
                    {
                        collision = true;
                    }

                    //On check les autres robots
                    foreach (var otherRobot in robotList)
                    {
                        if (otherRobot.Key != robot.Key) //On exclu le test entre robots identiques
                        {
                            if (Toolbox.Distance(robot.Value.newXWithoutCollision, robot.Value.newYWithoutCollision, otherRobot.Value.newXWithoutCollision, otherRobot.Value.newYWithoutCollision) < robot.Value.radius * 2)
                                collision = true;
                        }
                    }

                    //Validation des déplacements
                    if (!collision)
                    {
                        robot.Value.X = robot.Value.newXWithoutCollision;
                        robot.Value.Y = robot.Value.newYWithoutCollision;
                        robot.Value.Theta = robot.Value.newThetaWithoutCollision;
                    }
                    else
                    {
                        robot.Value.Vx = 0;
                        robot.Value.Vy = 0;
                        robot.Value.Vtheta = 0;
                    }

                    //Emission d'un event de position physique 
                    Location loc = new Location(robot.Value.X, robot.Value.Y, robot.Value.Theta, robot.Value.Vx, robot.Value.Vy, robot.Value.Vtheta);
                    OnPhysicalPosition(robot.Key, loc);
                }

                List<Location> objectsLocationList = new List<Location>();
                foreach (var robot in robotList)
                {
                    objectsLocationList.Add(new Location(robot.Value.X, robot.Value.Y, robot.Value.Theta, robot.Value.Vx, robot.Value.Vy, robot.Value.Vtheta));
                }
                OnPhysicicalObjectListLocation(objectsLocationList);
            }
        }

        public void SetRobotSpeed(object sender, SpeedConsigneArgs e)
        {
            if (robotList.ContainsKey(e.RobotId))
            {
                robotList[e.RobotId].Vx = e.Vx;
                robotList[e.RobotId].Vy = e.Vy;
                robotList[e.RobotId].Vtheta = e.Vtheta;
            }
        }

        //Output events
        public delegate void PhysicalPositionEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnPhysicalPositionEvent;
        public virtual void OnPhysicalPosition(int id, Location location)
        {
            var handler = OnPhysicalPositionEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = location });
            }
        }

        public delegate void ObjectsPositionEventHandler(object sender, LocationListArgs e);
        public event EventHandler<LocationListArgs> OnPhysicicalObjectListLocationEvent;
        public virtual void OnPhysicicalObjectListLocation(List<Location> locationList)
        {
            var handler = OnPhysicicalObjectListLocationEvent;
            if (handler != null)
            {
                handler(this, new LocationListArgs { LocationList = locationList });
            }
        }
    }

    public class PhysicalRobotSimulator
    {
        public double radius = 0.25;
        public double X;
        public double Y;
        public double Theta;

        public double newXWithoutCollision;
        public double newYWithoutCollision;
        public double newThetaWithoutCollision;

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
