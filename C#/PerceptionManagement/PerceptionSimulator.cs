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
            robotPerception.teamLocationList = new Dictionary<int, Location>();
            robotPerception.opponentLocationList = new List<Location>();
            robotPerception.obstacleLocationList = new List<Location>();

            physicalObjectList = new List<Location>();
            robotId = id;
        }

        void GeneratePerception()
        {
            robotPerception.teamLocationList.Clear();
            robotPerception.opponentLocationList.Clear();
            robotPerception.obstacleLocationList.Clear();
            //physicalObjectList.Clear();

            //On regarde sur chacun des objets détectés si il appartient ou pas à une équipe.
            foreach(var obj in physicalObjectList)
            {
                bool isRobot = false;
                
                lock (globalWorldMap.robotLocationDictionary)
                {
                    //On regarde dans la liste des robots de l'équipe construite par le globalWorldMapManager de l'équipe
                    foreach (var r in globalWorldMap.robotLocationDictionary)
                    {
                        if (r.Value != null)
                        {
                            var robotOfOurTeam = r.Value;
                            //On regarde si la distance entre l'objet considéré et la position des robots de l'équipe est suffisament petite pour que ce soient les même.
                            if (Toolbox.Distance(obj.X, obj.Y, robotOfOurTeam.X, robotOfOurTeam.Y) < 0.4)
                            {
                                if (robotId != r.Key && !robotPerception.teamLocationList.ContainsKey(r.Key)) //On vérifie que le robot ne s'ajoute pas lui même
                                    robotPerception.teamLocationList.Add(r.Key, new Location(robotOfOurTeam.X, robotOfOurTeam.Y, robotOfOurTeam.Theta, robotOfOurTeam.Vx, robotOfOurTeam.Vy, robotOfOurTeam.Vtheta));
                                
                                isRobot = true;
                            }
                        }
                    }
                }
                if (!isRobot)
                {
                    robotPerception.opponentLocationList.Add(new Location(obj.X, obj.Y, obj.Theta, obj.Vx, obj.Vy, obj.Vtheta));
                }
            }

            OnPerception(robotPerception);
        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            //Fonctions de traitement
        }

        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
            GeneratePerception();
        }

        public void OnPhysicalObjectListLocationReceived(object sender, LocationListArgs e)
        {
            //On récupère la liste des objets physiques vus par le robot en simulation (y compris lui-même)
            physicalObjectList = e.LocationList;
        }

        public void OnPhysicalPositionReceived(object sender, LocationArgs e)
        {
            //On calcule la perception simulée de position d'après le retour du simulateur physique directement
            //On réel on utilisera la triangulation lidar et la caméra
            //robotPerception.robotLocation = new Location(robotOfOurTeam.X, robotOfOurTeam.Y, robotOfOurTeam.Theta, robotOfOurTeam.Vx, robotOfOurTeam.Vy, robotOfOurTeam.Vtheta);
            if (robotId == e.RobotId)
            {
                robotPerception.robotLocation = e.Location;
                //OnLocalWorldMap(robotName, localWorldMap);
            }
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
