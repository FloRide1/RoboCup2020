using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Utilities;
using WorldMap;

namespace WorldMapManager
{
    public class GlobalWorldMapManager
    {
        int TeamId;
        double freqRafraichissementWorldMap = 30;

        Dictionary<int, LocalWorldMap> localWorldMapDictionary = new Dictionary<int, LocalWorldMap>();
        GlobalWorldMapStorage globalWorldMapStorage = new GlobalWorldMapStorage();
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();
        Timer globalWorldMapSendTimer;


        public GlobalWorldMapManager(int teamId)
        {
            TeamId = teamId;
            globalWorldMapSendTimer = new Timer(1000/freqRafraichissementWorldMap);
            globalWorldMapSendTimer.Elapsed += GlobalWorldMapSendTimer_Elapsed;
            globalWorldMapSendTimer.Start();
        }

        private void GlobalWorldMapSendTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            MergeLocalWorldMaps();
        }

        public void OnLocalWorldMapReceived(object sender, EventArgsLibrary.LocalWorldMapArgs e)
        {
            AddOrUpdateLocalWorldMap(e.RobotId, e.TeamId, e.LocalWorldMap);
        }

        private void AddOrUpdateLocalWorldMap(int robotId, int teamId, LocalWorldMap localWorldMap)
        {
            lock (localWorldMapDictionary)
            {
                if (localWorldMapDictionary.ContainsKey(robotId))
                    localWorldMapDictionary[robotId] = localWorldMap;
                else
                    localWorldMapDictionary.Add(robotId, localWorldMap);
            }
        }

        private void MergeLocalWorldMaps()
        {
            //Fusion des World Map locales pour construire la world map globale
            lock (localWorldMapDictionary)
            {
                //On rassemble les infos issues des cartes locales de chacun des robots
                foreach (var localMap in localWorldMapDictionary)
                {
                    globalWorldMapStorage.AddOrUpdateRobotLocation(localMap.Key, localMap.Value.robotLocation);
                    globalWorldMapStorage.AddOrUpdateBallLocation(localMap.Key, localMap.Value.ballLocation);
                    globalWorldMapStorage.AddOrUpdateRobotDestination(localMap.Key, localMap.Value.destinationLocation);
                    globalWorldMapStorage.AddOrUpdateRobotWayPoint(localMap.Key, localMap.Value.waypointLocation);
                    globalWorldMapStorage.AddOrUpdateObstaclesList(localMap.Key, localMap.Value.obstaclesLocationList);
                }

                //Génération de la carte fusionnée à partir des perceptions des robots de l'équipe
                //La fusion porte avant tout sur la balle et sur les adversaires.

                //TODO : faire un algo de fusion robuste pour la balle
                globalWorldMap = new WorldMap.GlobalWorldMap(TeamId);

                //Pour l'instant on prend la position de balle vue par le robot 1 comme vérité, mais c'est à améliorer !
                if (localWorldMapDictionary.Count > 0)
                    globalWorldMap.ballLocation = localWorldMapDictionary.First().Value.ballLocation;
                globalWorldMap.teammateLocationList = new Dictionary<int, Location>();
                globalWorldMap.opponentLocationList = new List<Location>();

                //On place tous les robots de l'équipe dans la global map
                foreach (var localMap in localWorldMapDictionary)
                {
                    //On ajoute la position des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateLocationList.Add(localMap.Key, localMap.Value.robotLocation);
                }

                //On établit une liste des emplacements d'adversaires potentiels afin de les fusionner si possible
                List<Location> AdversairesPotentielsList = new List<Location>();
                List<int> AdversairesPotentielsMatchOccurenceList = new List<int>();
                foreach (var localMap in localWorldMapDictionary)
                {
                    //On tente de transformer les objets vus et ne correspondant pas à des robots alliés en des adversaires
                    List<Location> obstacleLocationList = new List<Location>();
                    try
                    {
                         obstacleLocationList = localMap.Value.obstaclesLocationList.ToList();
                    }
                    catch { }

                    foreach (var obstacleLocation in obstacleLocationList)
                    {
                        bool isTeamMate = false;
                        bool isAlreadyPresentInOpponentList = false;

                        //On regarde si l'obstacle est un coéquipier ou pas
                        foreach (var robotTeamLocation in globalWorldMap.teammateLocationList.Values)
                        {
                            if (obstacleLocation != null && robotTeamLocation != null)
                            {
                                if (Toolbox.Distance(obstacleLocation.X, obstacleLocation.Y, robotTeamLocation.X, robotTeamLocation.Y) < 0.4)
                                    isTeamMate = true;
                            }
                        }

                        //On regarde si l'obstacle existe dans la liste des adversaires potentiels ou pas
                        foreach (var opponentLocation in AdversairesPotentielsList)
                        {
                            if (obstacleLocation != null && opponentLocation != null)
                            {
                                if (Toolbox.Distance(obstacleLocation.X, obstacleLocation.Y, opponentLocation.X, opponentLocation.Y) < 0.4)
                                {
                                    isAlreadyPresentInOpponentList = true;
                                    var index = AdversairesPotentielsList.IndexOf(opponentLocation);
                                    AdversairesPotentielsMatchOccurenceList[index]++;
                                }
                            }
                        }

                        //Si un obstacle n'est ni un coéquipier, ni un adversaire potentiel déjà trouvé, c'est un nouvel adversaire potentiel
                        if (!isTeamMate && !isAlreadyPresentInOpponentList)
                        {
                            AdversairesPotentielsList.Add(obstacleLocation);
                            AdversairesPotentielsMatchOccurenceList.Add(1);
                        }
                    }
                }

                //On valide les adversaires potentiels si ils ont été perçus plus d'une fois par les robots
                for(int i=0; i< AdversairesPotentielsList.Count; i++)
                {
                    if (AdversairesPotentielsMatchOccurenceList[i] >= 2)
                    {
                        var opponentLocation = AdversairesPotentielsList[i];
                        globalWorldMap.opponentLocationList.Add(opponentLocation);
                    }
                }
            }
            OnGlobalWorldMap(globalWorldMap);
        }

        public delegate void GlobalWorldMapEventHandler(object sender, GlobalWorldMapArgs e);
        public event EventHandler<GlobalWorldMapArgs> OnGlobalWorldMapEvent;
        public virtual void OnGlobalWorldMap(GlobalWorldMap globalWorldMap)
        {
            var handler = OnGlobalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new GlobalWorldMapArgs {GlobalWorldMap = globalWorldMap});
            }
        }
    }
}
