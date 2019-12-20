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
        PhysicalBallSimulator ballSimulated = new PhysicalBallSimulator(0,0);
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

            //Calcul des déplacements théoriques des robots avec gestion des collisions
            lock (robotList)
            {
                //On calcule les nouvelles positions théoriques de tous les robots si il n'y a pas collision
                foreach (var robot in robotList)
                {
                    robot.Value.newXWithoutCollision = robot.Value.X + (robot.Value.Vx * Math.Cos(robot.Value.Theta) - robot.Value.Vy * Math.Sin(robot.Value.Theta)) / fSampling;
                    robot.Value.newYWithoutCollision = robot.Value.Y + (robot.Value.Vx * Math.Sin(robot.Value.Theta) + robot.Value.Vy * Math.Cos(robot.Value.Theta)) / fSampling;
                    robot.Value.newThetaWithoutCollision = robot.Value.Theta + robot.Value.Vtheta / fSampling;
                }

                //On calcule la nouelle position théorique de la balle si il n'y a pas de collision
                ballSimulated.newXWithoutCollision = ballSimulated.X + (ballSimulated.Vx * Math.Cos(ballSimulated.Theta) - ballSimulated.Vy * Math.Sin(ballSimulated.Theta)) / fSampling;
                ballSimulated.newYWithoutCollision = ballSimulated.Y + (ballSimulated.Vx * Math.Sin(ballSimulated.Theta) + ballSimulated.Vy * Math.Cos(ballSimulated.Theta)) / fSampling;

                //TODO : Gérer les collisions polygoniales en déclenchant l'étude fine à l'aide d'un cercle englobant.
                //TODO : gérer la balle et les rebonds robots poteaux cages
                //TODO : gérer la perte d'énergie de la balle : modèle à trouver... mesure précise :) faite sur le terrain : 1m.s-1 -> arrêt à 10m
                //TODO : gérer le tir (ou passe)
                //TODO : gérer les déplacements balle au pied
                //TODO : gérer les cas de contestation

                //Pour chacun des robots, on regarde les collisions possibles 
                foreach (var robot in robotList)
                {
                    bool collisionRobotMur = false;
                    bool collisionRobotRobot = false;
                    bool collisionRobotBalle = false;

                    //On check les murs 
                    if ((robot.Value.newXWithoutCollision + robot.Value.radius > 13) || (robot.Value.newXWithoutCollision - robot.Value.radius < -13)
                        || (robot.Value.newYWithoutCollision + robot.Value.radius > 9) || (robot.Value.newYWithoutCollision - robot.Value.radius < -9))
                    {
                        collisionRobotMur = true;
                    }

                    //On check les autres robots
                    foreach (var otherRobot in robotList)
                    {
                        if (otherRobot.Key != robot.Key) //On exclu le test entre robots identiques
                        {
                            if (Toolbox.Distance(robot.Value.newXWithoutCollision, robot.Value.newYWithoutCollision, otherRobot.Value.newXWithoutCollision, otherRobot.Value.newYWithoutCollision) < robot.Value.radius * 2)
                                collisionRobotRobot = true;
                        }
                    }

                    //On check les collisions avec la balle
                    if(Toolbox.Distance(robot.Value.newXWithoutCollision, robot.Value.newYWithoutCollision, ballSimulated.newXWithoutCollision, ballSimulated.newYWithoutCollision) < 1*(robot.Value.radius +ballSimulated.radius))
                    {
                        collisionRobotBalle = true;
                    }

                    //Validation des déplacements robot
                    if (!collisionRobotRobot && !collisionRobotMur)
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

                    if(collisionRobotBalle)
                    {
                        ballSimulated.Vx = 1.5 * robot.Value.Vx;
                        ballSimulated.Vy = 1.5 * robot.Value.Vy;
                        ballSimulated.newXWithoutCollision = ballSimulated.X + (ballSimulated.Vx * Math.Cos(ballSimulated.Theta) - ballSimulated.Vy * Math.Sin(ballSimulated.Theta)) / fSampling;
                        ballSimulated.newYWithoutCollision = ballSimulated.Y + (ballSimulated.Vx * Math.Sin(ballSimulated.Theta) + ballSimulated.Vy * Math.Cos(ballSimulated.Theta)) / fSampling;
                    }

                    //Emission d'un event de position physique 
                    Location loc = new Location(robot.Value.X, robot.Value.Y, robot.Value.Theta, robot.Value.Vx, robot.Value.Vy, robot.Value.Vtheta);
                    OnPhysicalRobotPosition(robot.Key, loc);
                }

                //Gestion des collisions balle-murs
                //On check les murs virtuels
                //Mur haut ou bas
                if ((ballSimulated.newYWithoutCollision + ballSimulated.radius > 7) || (ballSimulated.newYWithoutCollision + ballSimulated.radius < -7))
                {
                    ballSimulated.Vy = -ballSimulated.Vy; //On simule un rebond
                    ballSimulated.newXWithoutCollision = ballSimulated.X + (ballSimulated.Vx * Math.Cos(ballSimulated.Theta) - ballSimulated.Vy * Math.Sin(ballSimulated.Theta)) / fSampling;
                    ballSimulated.newYWithoutCollision = ballSimulated.Y + (ballSimulated.Vx * Math.Sin(ballSimulated.Theta) + ballSimulated.Vy * Math.Cos(ballSimulated.Theta)) / fSampling;
                }
                //Mur gauche ou droit
                if ((ballSimulated.newXWithoutCollision + ballSimulated.radius < -11) || (ballSimulated.newXWithoutCollision + ballSimulated.radius > 11))
                {
                    ballSimulated.Vx = -ballSimulated.Vx; //On simule un rebond
                    ballSimulated.newXWithoutCollision = ballSimulated.X + (ballSimulated.Vx * Math.Cos(ballSimulated.Theta) - ballSimulated.Vy * Math.Sin(ballSimulated.Theta)) / fSampling;
                    ballSimulated.newYWithoutCollision = ballSimulated.Y + (ballSimulated.Vx * Math.Sin(ballSimulated.Theta) + ballSimulated.Vy * Math.Cos(ballSimulated.Theta)) / fSampling;
                }

                //Gestion de la décélération de la balle
                double deceleration = 0.5;
                ballSimulated.X = ballSimulated.newXWithoutCollision;
                ballSimulated.Y = ballSimulated.newYWithoutCollision;
                //ballSimulated.Vx = Math.Max(0, ballSimulated.Vx - deceleration / fSampling);
                //ballSimulated.Vy = Math.Max(0, ballSimulated.Vy - deceleration / fSampling);
                
                OnPhysicalBallPosition(new Location(ballSimulated.X, ballSimulated.Y, ballSimulated.Theta, ballSimulated.Vx, ballSimulated.Vy, ballSimulated.Vtheta));

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
        public delegate void PhysicalRobotPositionEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnPhysicalRobotPositionEvent;
        public virtual void OnPhysicalRobotPosition(int id, Location location)
        {
            var handler = OnPhysicalRobotPositionEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = location });
            }
        }

        public delegate void PhysicalBallPositionEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnPhysicalBallPositionEvent;
        public virtual void OnPhysicalBallPosition(Location location)
        {
            var handler = OnPhysicalBallPositionEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { Location = location });
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
    public class PhysicalBallSimulator
    {
        public double radius = 0.115;
        public double X;
        public double Y;
        public double Z;
        public double Theta;

        public double newXWithoutCollision;
        public double newYWithoutCollision;
        public double newThetaWithoutCollision;

        public double Vx;
        public double Vy;
        public double Vtheta;

        public PhysicalBallSimulator(double xPos, double yPos)
        {
            X = xPos;
            Y = yPos;
            Vy = 8;
        }
    }
}
