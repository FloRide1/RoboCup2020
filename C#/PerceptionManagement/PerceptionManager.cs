using AbsolutePositionEstimatorNS;
using EventArgsLibrary;
using LidarProcessor;
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
            lidarProcessor.OnLidarBalisePointListForDebugEvent += OnLidarBalisePointListForDebugReceived;
            lidarProcessor.OnLidarObjectProcessedEvent += OnLidarObjectsReceived; 

            absolutePositionEstimator.OnAbsolutePositionCalculatedEvent += OnAbsolutePositionCalculatedEvent;

        }

        private void OnLidarBalisePointListForDebugReceived(object sender, RawLidarArgs e)
        {
            //On transmet l'event
            OnLidarBalisePointListForDebug(e.RobotId, e.PtList);
        }

        //private void LidarProcessor_OnLidarProcessedEvent(object sender, RawLidarArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        // Event position absolue, on le forward vers l'extérieur du perception Manager
        public event EventHandler<PositionArgs> OnAbsolutePositionEvent;
        private void OnAbsolutePositionCalculatedEvent(object sender, PositionArgs e)
        {
            robotPerception.robotAbsoluteLocation = new Location(e.X, e.Y, e.Theta, 0, 0, 0);
            OnAbsolutePositionEvent?.Invoke(this, e);
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
                //On transmet la location au positionnement absolu pour qu'il puisse vérifier que la nouvelle position absolue est cohérente avec le positionnement Kalman.
                absolutePositionEstimator.OnPhysicalPositionReceived(sender, e);
                //On génère la perception
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
            if (robotPerception.robotKalmanLocation != null)
            {
                lock (physicalObjectList)
                {
                    physicalObjectList.Clear();

                    double xRobot = robotPerception.robotKalmanLocation.X;
                    double yRobot = robotPerception.robotKalmanLocation.Y;
                    double angleRobot = robotPerception.robotKalmanLocation.Theta;

                    //On récupère la liste des objets physiques vus par le robot (y compris lui-même en simulation)
                    foreach (var obj in e.ObjectList)
                    {
                        double angle = obj.polarPointList[0].Angle;
                        double distance = obj.polarPointList[0].Distance;
                        double xObjetRefTerrain = xRobot + distance * Math.Cos(angle + angleRobot);
                        double yObjetRefTerrain = yRobot + distance * Math.Sin(angle + angleRobot);

                        //Code spécifique Eurobot
                        //On s'accupe des obstacles dans le terrain qui sont a priori des robots
                        if (Math.Abs(xObjetRefTerrain) < 1.5 && Math.Abs(yObjetRefTerrain) < 1.0)
                        {
                            double rayon = 0.2;
                            if (distance > 0.2) //On exclut les obstacles trop proches
                            {
                                //On génère une liste de points périmètres des obstacle pour les interdire
                                for (double anglePourtour = 0; anglePourtour < 2 * Math.PI; anglePourtour += 2 * Math.PI / 5)
                                {
                                    physicalObjectList.Add(new Location(xObjetRefTerrain + rayon * Math.Cos(anglePourtour), yObjetRefTerrain + rayon * Math.Sin(anglePourtour), 0, 0, 0, 0));
                                }
                            }
                        }
                    }

                    //On rajoute les bordures du terrain à la main :
                    for (double x = -1.5; x <= 1.5; x += 0.35)
                    {
                        physicalObjectList.Add(new Location(x, -1, 0, 0, 0, 0));
                        physicalObjectList.Add(new Location(x, 1, 0, 0, 0, 0));
                    }
                    for (double y = -0.8; y <= 0.8; y += 0.35)
                    {
                        physicalObjectList.Add(new Location(-1.5, y, 0, 0, 0, 0));
                        physicalObjectList.Add(new Location(1.5, y, 0, 0, 0, 0));
                    }
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
                handler(this, new PerceptionArgs { RobotId = robotId, Perception = perception });
            }
        }
        
        public delegate void OnLidarBalisePointListForDebugEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarBalisePointListForDebugEvent;
        public virtual void OnLidarBalisePointListForDebug(int id, List<PolarPointRssi> ptList)
        {
            var handler = OnLidarBalisePointListForDebugEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, PtList = ptList });
            }
        }
    }  
}

