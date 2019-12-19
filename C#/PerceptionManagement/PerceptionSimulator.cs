using EventArgsLibrary;
using System;
using System.Collections.Generic;
using Utilities;
using WorldMap;

namespace PerceptionManagement
{
    public class PerceptionSimulator
    {
        int robotId = 0;

        GlobalWorldMap globalWorldMap = new GlobalWorldMap();

        List<Location> physicalObjectList;
        Perception robotPerception;

        public PerceptionSimulator(int id)
        {
            robotPerception = new Perception();
            robotPerception.obstaclesLocationList = new List<Location>();

            physicalObjectList = new List<Location>();
            robotId = id;
        }

        void GeneratePerception()
        {            
            robotPerception.obstaclesLocationList.Clear();

            //On regarde sur chacun des objets détectés si il appartient ou pas à une équipe.
            foreach(var obj in physicalObjectList)
            {
                bool isRobot = false;
                
                //On regarde si l'objet physique n'est pas le robot lui même
                if (Toolbox.Distance(obj.X, obj.Y, robotPerception.robotLocation.X, robotPerception.robotLocation.Y) < 0.4)
                {
                    isRobot = true;
                }

                //    //On regarde dans la liste des robots de l'équipe construite par le globalWorldMapManager de l'équipe
                //    foreach (var r in globalWorldMap.robotLocationDictionary)
                //    {
                //        if (r.Value != null)
                //        {
                //            var robotOfOurTeam = r.Value;
                //            //On regarde si la distance entre l'objet considéré et la position des robots de l'équipe est suffisament petite pour que ce soient les même.
                //            if (Toolbox.Distance(obj.X, obj.Y, robotOfOurTeam.X, robotOfOurTeam.Y) < 0.4)
                //            {
                //                if (robotId != r.Key && !robotPerception.teamLocationList.ContainsKey(r.Key)) //On vérifie que le robot ne s'ajoute pas lui même
                //                    robotPerception.teamLocationList.Add(r.Key, new Location(robotOfOurTeam.X, robotOfOurTeam.Y, robotOfOurTeam.Theta, robotOfOurTeam.Vx, robotOfOurTeam.Vy, robotOfOurTeam.Vtheta));
                                
                //                isRobot = true;
                //            }
                //        }
                //    }
                //}

                if (!isRobot)
                {
                    robotPerception.obstaclesLocationList.Add(new Location(obj.X, obj.Y, obj.Theta, obj.Vx, obj.Vy, obj.Vtheta));
                }
            }

            //Gestion de la balle
            OnPerception(robotPerception);
        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            //Fonctions de traitement
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

        //L'arrivée d'une nouvelle position mesurée (ou simulée) déclenche le recalcul et event de perception
        public void OnPhysicalRobotPositionReceived(object sender, LocationArgs e)
        {
            //On calcule la perception simulée de position d'après le retour du simulateur physique directement
            //On réel on utilisera la triangulation lidar et la caméra
            if (robotId == e.RobotId)
            {
                robotPerception.robotLocation = e.Location;
                GeneratePerception();
            }
        }

        public void OnPhysicalBallPositionReceived(object sender, LocationArgs e)
        {
            //On calcule la perception simulée de position balle d'après le retour du simulateur physique directement
            //On réel on utilisera la caméra
            robotPerception.ballLocation = e.Location;            
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
