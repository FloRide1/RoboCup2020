using AbsolutePositionEstimatorNS;
using EventArgsLibrary;
using PerformanceMonitorTools;
using System;
using System.Collections.Generic;
using Utilities;
using WorldMap;

namespace PerceptionManagement
{
    public class PerceptionManager
    {
        int robotId = 0;
        GameMode competition;
        
        List<LocationExtended> physicalObjectList;
        Perception robotPerception;

        LidarProcessor.LidarProcessor lidarProcessor;
        AbsolutePositionEstimator absolutePositionEstimator;

        GlobalWorldMap globalWorldMap;

        //bool replayModeActivated = false;


        public PerceptionManager(int id, GameMode compet)
        {
            robotId = id;
            competition = compet;
            globalWorldMap = new GlobalWorldMap();

            robotPerception = new Perception();
            physicalObjectList = new List<LocationExtended>();

            //Chainage des modules composant le Perception Manager
            absolutePositionEstimator = new AbsolutePositionEstimator(robotId);

            lidarProcessor = new LidarProcessor.LidarProcessor(robotId, competition);
            lidarProcessor.OnLidarBalisesListExtractedEvent += absolutePositionEstimator.OnLidarBalisesListExtractedEvent;
            lidarProcessor.OnLidarBalisePointListForDebugEvent += OnLidarBalisePointListForDebugReceived;
            lidarProcessor.OnLidarObjectProcessedEvent += OnLidarObjectsReceived;
            lidarProcessor.OnLidarProcessedEvent += OnLidarProcessedData;

            absolutePositionEstimator.OnAbsolutePositionCalculatedEvent += OnAbsolutePositionCalculatedEvent;

            PerceptionMonitor.swPerception.Start();

        }


        //public void OnEnableDisableLogReplayEvent(object sender, BoolEventArgs e)
        //{
        //    replayModeActivated = e.value;
        //}

        private void OnLidarBalisePointListForDebugReceived(object sender, RawLidarArgs e)
        {
            //On transmet l'event
            OnLidarBalisePointListForDebug(e.RobotId, e.PtList);
        }


        Equipe playingTeam = Equipe.Jaune;
        public void OnIOValuesFromRobotEvent(object sender, IOValuesEventArgs e)
        {
            bool config1IsOn = (((e.ioValues >> 1) & 0x01) == 0x01);
            if (config1IsOn)
                playingTeam = Equipe.Jaune;
            else
                playingTeam = Equipe.Bleue;
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
            //On forward si on n'est pas en mode replay
            //if (!replayModeActivated)
            {
                //On forward les données lidar brutes reçues au lidarProcessor
                lidarProcessor.OnRawLidarDataReceived(sender, e);
                //On rémet un event pour les affichages éventuels
                OnLidarRawData(e);


                /////////////////////////////////////////////////////////////////////////////////////////////////
                //TODO : remove that lines - just there for using recording with Lidar only
                //On génère la perception
                GeneratePerception();

                /////////////////////////////////////////////////////////////////////////////////////////////////
            }
        }

        //public void OnRawLidarReplayDataReceived(object sender, RawLidarArgs e)
        //{
        //    //On forward si on est en mode replay
        //    if (replayModeActivated)
        //    {
        //        //On forward les données lidar brutes reçues au lidarProcessor
        //        lidarProcessor.OnRawLidarDataReceived(sender, e);
        //        //On rémet un event pour les affichages éventuels
        //        OnLidarRawData(e);
        //    }
        //}

        //L'arrivée d'une nouvelle position mesurée (ou simulée) déclenche le recalcul et event de perception
        public void OnPhysicalRobotPositionReceived(object sender, LocationArgs e)
        {
            if (robotId == e.RobotId)
            {
                robotPerception.robotKalmanLocation = e.Location;
                //On transmet la location au positionnement absolu pour qu'il puisse vérifier que la nouvelle position absolue est cohérente avec le positionnement Kalman.
                absolutePositionEstimator.OnPhysicalPositionReceived(sender, e);
                //On génère la perception
                GeneratePerception();
            }
        }
        
        public void OnMirrorModeReceived(object sender, BoolEventArgs e)
        {
            //On forward l'event vers le position estimator
            absolutePositionEstimator.OnMirrorModeReceived(sender, e);
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
                            robotPerception.obstaclesLocationList.Add(new LocationExtended(obj.X, obj.Y, obj.Theta, obj.Vx, obj.Vy, obj.Vtheta, obj.Type));
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
                        //On s'occupe des obstacles dans le terrain qui sont a priori des robots
                        //if (Math.Abs(xObjetRefTerrain) < 1.5 && Math.Abs(yObjetRefTerrain) < 1.0)
                        {
                            double rayon = 0.2;
                            if (distance > 0.05) //On exclut les obstacles trop proches
                            {
                                physicalObjectList.Add(new LocationExtended(xObjetRefTerrain, yObjetRefTerrain, 0, 0, 0, 0, ObjectType.Robot));
                                ////On génère une liste de points périmètres des obstacle pour les interdire
                                //for (double anglePourtour = 0; anglePourtour < 2 * Math.PI; anglePourtour += 2 * Math.PI / 5)
                                //{
                                //    physicalObjectList.Add(new LocationExtended(xObjetRefTerrain + rayon * Math.Cos(anglePourtour), yObjetRefTerrain + rayon * Math.Sin(anglePourtour), 0, 0, 0, 0, ObjectType.Robot));
                                //}
                            }
                        }
                    }

                    //double borderAvoidanceZone = 0.05;
                    ////On rajoute les bordures du terrain à la main :
                    //physicalObjectList.Add(new LocationExtended(0, -1+ borderAvoidanceZone, 0, 0, 0, 0, ObjectType.LimiteHorizontaleBasse));
                    //physicalObjectList.Add(new LocationExtended(0, 1- borderAvoidanceZone, 0, 0, 0, 0, ObjectType.LimiteHorizontaleHaute));
                    //physicalObjectList.Add(new LocationExtended(-1.5+ borderAvoidanceZone, 0, 0, 0, 0, 0, ObjectType.LimiteVerticaleGauche));
                    //physicalObjectList.Add(new LocationExtended(1.5- borderAvoidanceZone, 0, 0, 0, 0, 0, ObjectType.LimiteVerticaleDroite));

                    //if (playingTeam == Equipe.Jaune)
                    //{
                    //    physicalObjectList.Add(new LocationExtended(1.05, 0.08, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(1.05, -0.49, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(1.3, 0.08, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(1.3, -0.49, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(1.05, (0.08 - 0.49) / 2.0, 0, 0, 0, 0, ObjectType.Obstacle));
                    //}
                    //else if (playingTeam == Equipe.Bleue)
                    //{
                    //    physicalObjectList.Add(new LocationExtended(-1.05, 0.08, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(-1.3, 0.08, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(-1.05, -0.49, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(-1.3, -0.49, 0, 0, 0, 0, ObjectType.Obstacle));
                    //    physicalObjectList.Add(new LocationExtended(-1.05, (0.08-0.49)/2.0, 0, 0, 0, 0, ObjectType.Obstacle));
                    //}
                    //physicalObjectList.Add(new LocationExtended(0, 0.7, 0, 0, 0, 0, ObjectType.Obstacle));
                    //physicalObjectList.Add(new LocationExtended(-0.611, 0.85, 0, 0, 0, 0, ObjectType.Obstacle));
                    //physicalObjectList.Add(new LocationExtended(0.611, 0.85, 0, 0, 0, 0, ObjectType.Obstacle));
                    ////for (double x = -1.5; x <= 1.5; x += 0.35)
                    ////{
                    ////    physicalObjectList.Add(new LocationExtended(x, -1, 0, 0, 0, 0, ObjectType.Obstacle));
                    ////    physicalObjectList.Add(new LocationExtended(x, 1, 0, 0, 0, 0, ObjectType.Obstacle));
                    ////}
                    ////for (double y = -0.8; y <= 0.8; y += 0.35)
                    ////{
                    ////    physicalObjectList.Add(new LocationExtended(-1.5, y, 0, 0, 0, 0, ObjectType.Obstacle));
                    ////    physicalObjectList.Add(new LocationExtended(1.5, y, 0, 0, 0, 0, ObjectType.Obstacle));
                    ////}
                }
            }
        }

        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
        }

        public void OnPhysicalObjectListLocationReceived(object sender, LocationExtendedListArgs e)
        {
            //On récupère la liste des objets physiques vus par le robot en simulation (y compris lui-même)
            physicalObjectList = e.LocationExtendedList;
        }

        public void OnPhysicalBallPositionListReceived(object sender, LocationListArgs e)
        {
            //On calcule la perception simulée de position balle d'après le retour du simulateur physique directement
            //En réel on utilisera la caméra
            robotPerception.ballLocationList = e.LocationList;            
        }

        public event EventHandler<RawLidarArgs> OnLidarRawDataEvent;
        public virtual void OnLidarRawData(RawLidarArgs e)
        {
            var handler = OnLidarRawDataEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<RawLidarArgs> OnLidarProcessedDataEvent;
        public virtual void OnLidarProcessedData(object sender, RawLidarArgs e)
        {
            var handler = OnLidarProcessedDataEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<PerceptionArgs> OnPerceptionEvent;
        public virtual void OnPerception(Perception perception)
        {
            var handler = OnPerceptionEvent;
            if (handler != null)
            {
                handler(this, new PerceptionArgs { RobotId = robotId, Perception = perception });
            }
        }
        
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

