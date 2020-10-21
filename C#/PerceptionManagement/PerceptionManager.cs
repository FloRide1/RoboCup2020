using AbsolutePositionEstimatorNS;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using Utilities;
using WorldMap;

namespace PerceptionManagement
{
    public class PerceptionManager
    {
        int robotId = 0;
        
        List<Location> physicalObjectList;
        Perception robotPerception;

        LidarProcessor.LidarProcessor lidarProcessor;
        AbsolutePositionEstimator absolutePositionEstimator;

        GlobalWorldMap globalWorldMap;

        public PerceptionManager(int id)
        {
            robotId = id;
            globalWorldMap = new GlobalWorldMap();

            robotPerception = new Perception();
            physicalObjectList = new List<Location>();

            //Chainage des modules composant le Perception Manager
            absolutePositionEstimator = new AbsolutePositionEstimator(robotId);

            lidarProcessor = new LidarProcessor.LidarProcessor(robotId);
            lidarProcessor.OnLidarBalisesListExtractedEvent += absolutePositionEstimator.OnLidarBalisesListExtractedEvent;
            lidarProcessor.OnLidarObjectProcessedEvent += OnLidarObjectsReceived; 

            absolutePositionEstimator.OnAbsolutePositionCalculatedEvent += OnAbsolutePositionCalculatedEvent;

        }
        
        //private void LidarProcessor_OnLidarProcessedEvent(object sender, RawLidarArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        // Event position absoklue, on le forward vers l'extérieur du perception Manager
        public event EventHandler<PositionArgs> OnAbsolutePositionEvent;
        private void OnAbsolutePositionCalculatedEvent(object sender, PositionArgs e)
        {
            OnAbsolutePositionEvent?.Invoke(this, e);
            robotPerception.robotAbsoluteLocation = new Location(e.X, e.Y, e.Theta, 0, 0, 0);
        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            //On forward les données lidar brutes reçues au lidarProcessor
            lidarProcessor.OnRawLidarDataReceived(sender, e);
        }


        //L'arrivée d'une nouvelle position mesurée (ou simulée) déclenche le recalcul et event de perception
        public void OnPhysicalRobotPositionReceived(object sender, LocationArgs e)
        {
            //On calcule la perception simulée de position d'après le retour du simulateur physique directement
            //On réel on utilisera la triangulation lidar et la caméra
            if (robotId == e.RobotId)
            {
                robotPerception.robotKalmanLocation = e.Location;
                GeneratePerception();
            }
        }


        void GeneratePerception()
        {            
            robotPerception.obstaclesLocationList.Clear();

            //On regarde sur chacun des objets détectés si il appartient ou pas à une équipe.
            lock (physicalObjectList)
            {
                foreach (var obj in physicalObjectList)
                {
                    bool isRobot = false;

                    //On regarde si l'objet physique n'est pas le robot lui même
                    if (obj != null)
                    {
                        //if (Toolbox.Distance(obj.X, obj.Y, robotPerception.robotLocation.X, robotPerception.robotLocation.Y) < 0.4)
                        //{
                        //    isRobot = true;
                        //}

                        if (!isRobot)
                        {
                            robotPerception.obstaclesLocationList.Add(new Location(obj.X, obj.Y, obj.Theta, obj.Vx, obj.Vy, obj.Vtheta));
                        }
                    }
                }
            }

            //Transmission de la perception
            OnPerception(robotPerception);
        }

        public void OnLidarObjectsReceived(object sender, EventArgsLibrary.PolarPointListExtendedListArgs e)
        {
            lock (physicalObjectList)
            {
                physicalObjectList.Clear();
                //On récupère la liste des objets physiques vus par le robot en simulation (y compris lui-même)
                foreach (var obj in e.ObjectList)
                {
                    double angle = obj.polarPointList[0].Angle;
                    double distance = obj.polarPointList[0].Distance;
                    double xRobot = robotPerception.robotKalmanLocation.X;
                    double yRobot = robotPerception.robotKalmanLocation.Y;
                    double angleRobot = robotPerception.robotKalmanLocation.Theta;

                    physicalObjectList.Add(new Location(xRobot + distance * Math.Cos(angle + angleRobot), yRobot + distance * Math.Sin(angle + angleRobot), 0, 0, 0, 0));
                }
            }
        }

        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
        }

        public void OnPhysicalObjectListLocationReceived(object sender, LocationListArgs e)
        {
            //On récupère la liste des objets physiques vus par le robot en simulation (y compris lui-même)
            physicalObjectList = e.LocationList;
        }

        public void OnPhysicalBallPositionListReceived(object sender, LocationListArgs e)
        {
            //On calcule la perception simulée de position balle d'après le retour du simulateur physique directement
            //En réel on utilisera la caméra
            robotPerception.ballLocationList = e.LocationList;            
        }

        public delegate void PerceptionEventHandler(object sender, PerceptionArgs e);
        public event EventHandler<PerceptionArgs> OnPerceptionEvent;
        public virtual void OnPerception(Perception perception)
        {
            var handler = OnPerceptionEvent;
            if (handler != null)
            {
                handler(this, new PerceptionArgs { RobotId=robotId, Perception = perception });
            }
        }
    }  
}

