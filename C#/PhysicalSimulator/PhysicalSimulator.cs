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
        double LengthAireDeJeu = 0;
        double WidthAireDeJeu = 0;

        Dictionary<int, PhysicalRobotSimulator> robotList = new Dictionary<int, PhysicalRobotSimulator>();
        Dictionary<int, PhysicalBallSimulator> ballSimulatedList = new Dictionary<int, PhysicalBallSimulator>();
        double fSampling = 50;

        HighFreqTimer highFrequencyTimer;
                
        FiltreOrdre1 filterLowPassVx = new FiltreOrdre1();
        FiltreOrdre1 filterLowPassVy = new FiltreOrdre1();
        FiltreOrdre1 filterLowPassVTheta = new FiltreOrdre1();

        public PhysicalSimulator(double lengthAireDeJeu, double widthAireDeJeu)
        {
            LengthAireDeJeu = lengthAireDeJeu;
            WidthAireDeJeu = widthAireDeJeu;

            ballSimulatedList.Add(0, new PhysicalBallSimulator(0, 0));
            ballSimulatedList.Add(1, new PhysicalBallSimulator(3, 0));
            ballSimulatedList.Add(2, new PhysicalBallSimulator(6, 0));

            filterLowPassVx.LowPassFilterInit(fSampling, 10);
            filterLowPassVy.LowPassFilterInit(fSampling, 10);
            filterLowPassVTheta.LowPassFilterInit(fSampling, 10);

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
                    robot.Value.newXWithoutCollision = robot.Value.X + (robot.Value.VxRefRobot * Math.Cos(robot.Value.Theta) - robot.Value.VyRefRobot * Math.Sin(robot.Value.Theta)) / fSampling;
                    robot.Value.newYWithoutCollision = robot.Value.Y + (robot.Value.VxRefRobot * Math.Sin(robot.Value.Theta) + robot.Value.VyRefRobot * Math.Cos(robot.Value.Theta)) / fSampling;
                    robot.Value.newThetaWithoutCollision = robot.Value.Theta + robot.Value.Vtheta / fSampling;
                }
                               
                //TODO : Gérer les collisions polygoniales en déclenchant l'étude fine à l'aide d'un cercle englobant.
                //TODO : gérer la balle et les rebonds robots poteaux cages
                //TODO : gérer la perte d'énergie de la balle : modèle à trouver... mesure précise :) faite sur le terrain : 1m.s-1 -> arrêt à 10m
                //TODO : gérer le tir (ou passe)
                //TODO : gérer les déplacements balle au pied
                //TODO : gérer les cas de contestation

                //On Initialisae les collisions robots à false
                foreach (var robot in robotList)
                {
                    robot.Value.Collision = false;
                }

                //Pour chacun des robots, on regarde les collisions avec les murs
                foreach (var robot in robotList)
                {
                    //On check les murs 
                    if ((robot.Value.newXWithoutCollision + robot.Value.radius > 13) || (robot.Value.newXWithoutCollision - robot.Value.radius < -13)
                        || (robot.Value.newYWithoutCollision + robot.Value.radius > 9) || (robot.Value.newYWithoutCollision - robot.Value.radius < -9))
                    {
                        robot.Value.Collision = true;
                    }
                }

                //Pour chacun des robots, on regarde les collisions avec les autres robots
                foreach (var robot in robotList)
                {
                    //On check les autres robots
                    foreach (var otherRobot in robotList)
                    {
                        if (otherRobot.Key != robot.Key) //On exclu le test entre robots identiques
                        {
                            if (Toolbox.Distance(robot.Value.newXWithoutCollision, robot.Value.newYWithoutCollision, otherRobot.Value.newXWithoutCollision, otherRobot.Value.newYWithoutCollision) < robot.Value.radius * 2)
                                robot.Value.Collision = true;
                        }
                    }
                }

                //On calcule la nouvelle position théorique de la balle si il n'y a pas de collision
                foreach (var ballSimu in ballSimulatedList)
                {
                    var ballSimulated = ballSimu.Value;
                    ballSimulated.newXWithoutCollision = ballSimulated.X + (ballSimulated.Vx * Math.Cos(ballSimulated.Theta) - ballSimulated.Vy * Math.Sin(ballSimulated.Theta)) / fSampling;
                    ballSimulated.newYWithoutCollision = ballSimulated.Y + (ballSimulated.Vx * Math.Sin(ballSimulated.Theta) + ballSimulated.Vy * Math.Cos(ballSimulated.Theta)) / fSampling;
                }

                //On Initialisae les collisions balles à false
                foreach (var ballSimu in ballSimulatedList)
                {
                    ballSimu.Value.Collision = false;
                }

                //On check les collisions balle-robot
                foreach (var robot in robotList)
                {
                    foreach (var ballSimu in ballSimulatedList)
                    {
                        if (Toolbox.Distance(robot.Value.newXWithoutCollision, robot.Value.newYWithoutCollision, ballSimu.Value.newXWithoutCollision, ballSimu.Value.newYWithoutCollision) < 1 * (robot.Value.radius + ballSimu.Value.radius))
                        {
                            ballSimu.Value.Collision = true;
                            ballSimu.Value.Vx = 1.5 * robot.Value.VxRefRobot;
                            ballSimu.Value.Vy = 1.5 * robot.Value.VyRefRobot;
                        }
                    }
                }

                //On check les collisions balle-murs
                foreach (var ballSimu in ballSimulatedList)
                {
                    //Gestion des collisions balle-murs
                    //On check les murs virtuels
                    //Mur haut ou bas
                    if ((ballSimu.Value.newYWithoutCollision + ballSimu.Value.radius > 7) || (ballSimu.Value.newYWithoutCollision + ballSimu.Value.radius < -7))
                    {
                        ballSimu.Value.Vy = -ballSimu.Value.Vy; //On simule un rebond
                    }
                    //Mur gauche ou droit
                    if ((ballSimu.Value.newXWithoutCollision + ballSimu.Value.radius < -11) || (ballSimu.Value.newXWithoutCollision + ballSimu.Value.radius > 11))
                    {
                        ballSimu.Value.Vx = -ballSimu.Value.Vx; //On simule un rebond
                    }
                }

                //Gestion de la décélération de la balle
                double deceleration = 0.5;

                //Calcul de la nouvelle location des balles
                List<Location> newBallLocationList = new List<Location>();
                foreach (var ballSimu in ballSimulatedList)
                {
                    ballSimu.Value.newXWithoutCollision = ballSimu.Value.X + (ballSimu.Value.Vx * Math.Cos(ballSimu.Value.Theta) - ballSimu.Value.Vy * Math.Sin(ballSimu.Value.Theta)) / fSampling;
                    ballSimu.Value.newYWithoutCollision = ballSimu.Value.Y + (ballSimu.Value.Vx * Math.Sin(ballSimu.Value.Theta) + ballSimu.Value.Vy * Math.Cos(ballSimu.Value.Theta)) / fSampling;
                    ballSimu.Value.X = ballSimu.Value.newXWithoutCollision;
                    ballSimu.Value.Y = ballSimu.Value.newYWithoutCollision;
                    newBallLocationList.Add(new Location(ballSimu.Value.X, ballSimu.Value.Y, ballSimu.Value.Theta, ballSimu.Value.Vx, ballSimu.Value.Vy, ballSimu.Value.Vtheta));
                }
                OnPhysicalBallListPosition(newBallLocationList);

                //Calcul de la nouvelle Location des robots
                foreach (var robot in robotList)
                {
                    if (!robot.Value.Collision)
                    {
                        robot.Value.X = robot.Value.newXWithoutCollision;
                        robot.Value.Y = robot.Value.newYWithoutCollision;
                        robot.Value.Theta = robot.Value.newThetaWithoutCollision;
                    }
                    else
                    {
                        robot.Value.VxRefRobot = 0;
                        robot.Value.VyRefRobot = 0;
                        robot.Value.Vtheta = 0;
                    }
                
                    //Emission d'un event de position physique 
                    Location loc = new Location(robot.Value.X, robot.Value.Y, robot.Value.Theta, robot.Value.VxRefRobot, robot.Value.VyRefRobot, robot.Value.Vtheta);
                    OnPhysicalRobotLocation(robot.Key, loc);
                }                  

                List<Location> objectsLocationList = new List<Location>();
                foreach (var robot in robotList)
                {
                    objectsLocationList.Add(new Location(robot.Value.X, robot.Value.Y, robot.Value.Theta, robot.Value.VxRefRobot, robot.Value.VyRefRobot, robot.Value.Vtheta));
                }
                OnPhysicicalObjectListLocation(objectsLocationList);
            }
        }

        public void SetRobotSpeed(object sender, SpeedArgs e)
        {
            //Attention, les vitesses proviennent de l'odométrie et sont donc dans le référentiel robot
            if (robotList.ContainsKey(e.RobotId))
            {
                robotList[e.RobotId].VxRefRobot = filterLowPassVx.Filter(e.Vx);
                robotList[e.RobotId].VyRefRobot = filterLowPassVy.Filter(e.Vy);
                robotList[e.RobotId].Vtheta = filterLowPassVTheta.Filter(e.Vtheta);
            }
        }

        public void SetRobotPosition(int id, double x, double y, double theta)
        {
            //Attention, les positions sont dans le référentiel terrain
            if (robotList.ContainsKey(id))
            {
                robotList[id].X = x;
                robotList[id].Y = y;
                robotList[id].Theta = theta;
            }
        }

        public void OnCollisionReceived(object sender, EventArgsLibrary.CollisionEventArgs e)
        {
            SetRobotPosition(e.RobotId, e.RobotRealPosition.X, e.RobotRealPosition.Y, e.RobotRealPosition.Theta);
        }

        //Output events
        public event EventHandler<LocationArgs> OnPhysicalRobotLocationEvent;
        public virtual void OnPhysicalRobotLocation(int id, Location location)
        {
            var handler = OnPhysicalRobotLocationEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = location });
            }
        }

        public event EventHandler<LocationListArgs> OnPhysicalBallPositionListEvent;
        public virtual void OnPhysicalBallListPosition(List<Location> locationList)
        {
            var handler = OnPhysicalBallPositionListEvent;
            if (handler != null)
            {
                handler(this, new LocationListArgs { LocationList = locationList });
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

    public double VxRefRobot;
    public double VyRefRobot;
    public double Vtheta;

    public bool Collision;

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

        public bool Collision;

        public PhysicalBallSimulator(double xPos, double yPos)
        {
            X = xPos;
            Y = yPos;
            Vy = 8;
        }
    }
}
