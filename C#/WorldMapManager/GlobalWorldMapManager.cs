using EventArgsLibrary;
using Newtonsoft.Json;
using RefereeBoxAdapter;
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
        string TeamIpAddress = "";
        double freqRafraichissementWorldMap = 30;

        Dictionary<int, LocalWorldMap> localWorldMapDictionary = new Dictionary<int, LocalWorldMap>();
        GlobalWorldMapStorage globalWorldMapStorage = new GlobalWorldMapStorage();
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();
        Timer globalWorldMapSendTimer;

        GameState currentGameState = GameState.STOPPED;
        StoppedGameAction currentStoppedGameAction = StoppedGameAction.NONE;
        

        public GlobalWorldMapManager(int teamId, string ipAddress)
        {
            TeamId = teamId;
            TeamIpAddress = ipAddress;
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
            AddOrUpdateLocalWorldMap(e.LocalWorldMap);
        }

        public void OnRefereeBoxCommandReceived(object sender, RefBoxMessageArgs e)
        {
            var command = e.refBoxMsg.command;
            var robotId = e.refBoxMsg.robotID;
            var targetTeam = e.refBoxMsg.targetTeam;

            switch (command)
            {
                case RefBoxCommand.START:
                    currentGameState = GameState.PLAYING;
                    currentStoppedGameAction = StoppedGameAction.NONE;
                    break;
                case RefBoxCommand.STOP:
                    currentGameState = GameState.STOPPED;
                    break;
                case RefBoxCommand.DROP_BALL:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    currentStoppedGameAction = StoppedGameAction.DROPBALL;
                    break;
                case RefBoxCommand.HALF_TIME:
                    break;
                case RefBoxCommand.END_GAME:
                    break;
                case RefBoxCommand.GAME_OVER:
                    break;
                case RefBoxCommand.PARK:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    currentStoppedGameAction = StoppedGameAction.PARK;
                    break;
                case RefBoxCommand.FIRST_HALF:
                    break;
                case RefBoxCommand.SECOND_HALF:
                    break;
                case RefBoxCommand.FIRST_HALF_OVER_TIME:
                    break;
                case RefBoxCommand.RESET:
                    break;
                case RefBoxCommand.WELCOME:
                    break;
                case RefBoxCommand.KICKOFF:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.KICKOFF;
                    else
                        currentStoppedGameAction = StoppedGameAction.KICKOFF_OPPONENT;
                    break;
                case RefBoxCommand.FREEKICK:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.FREEKICK;
                    else
                        currentStoppedGameAction = StoppedGameAction.FREEKICK_OPPONENT;
                    break;
                case RefBoxCommand.GOALKICK:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.GOALKICK;
                    else
                        currentStoppedGameAction = StoppedGameAction.GOALKICK_OPPONENT;
                    break;
                case RefBoxCommand.THROWIN:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.THROWIN;
                    else
                        currentStoppedGameAction = StoppedGameAction.THROWIN_OPPONENT;
                    break;
                case RefBoxCommand.CORNER:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.CORNER;
                    else
                        currentStoppedGameAction = StoppedGameAction.CORNER_OPPONENT;
                    break;
                case RefBoxCommand.PENALTY:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.PENALTY;
                    else
                        currentStoppedGameAction = StoppedGameAction.PENALTY_OPPONENT;
                    break;
                case RefBoxCommand.GOAL:
                    break;
                case RefBoxCommand.SUBGOAL:
                    break;
                case RefBoxCommand.REPAIR:
                    break;
                case RefBoxCommand.YELLOW_CARD:
                    break;
                case RefBoxCommand.DOUBLE_YELLOW:
                    break;
                case RefBoxCommand.RED_CARD:
                    break;
                case RefBoxCommand.SUBSTITUTION:
                    break;
                case RefBoxCommand.IS_ALIVE:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.KICKOFF;
                    else
                        currentStoppedGameAction = StoppedGameAction.KICKOFF_OPPONENT;
                    break;
                case RefBoxCommand.GOTO_0_0:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.GOTO_0_0;
                    else
                        currentStoppedGameAction = StoppedGameAction.GOTO_0_0_OPPONENT;
                    break;
                case RefBoxCommand.GOTO_0_1:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.GOTO_0_1;
                    else
                        currentStoppedGameAction = StoppedGameAction.GOTO_0_1_OPPONENT;
                    break;
                case RefBoxCommand.GOTO_1_0:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.GOTO_1_0;
                    else
                        currentStoppedGameAction = StoppedGameAction.GOTO_1_0_OPPONENT;
                    break;
                case RefBoxCommand.GOTO_0_M1:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.GOTO_0_M1;
                    else
                        currentStoppedGameAction = StoppedGameAction.GOTO_0_M1_OPPONENT;
                    break;
                case RefBoxCommand.GOTO_M1_0:
                    currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        currentStoppedGameAction = StoppedGameAction.GOTO_M1_0;
                    else
                        currentStoppedGameAction = StoppedGameAction.GOTO_M1_0_OPPONENT;
                    break;
            }
        }

        private void AddOrUpdateLocalWorldMap(LocalWorldMap localWorldMap)
        {
            int robotId = localWorldMap.RobotId;
            int teamId = localWorldMap.TeamId;
            lock (localWorldMapDictionary)
            {
                if (localWorldMapDictionary.ContainsKey(robotId))
                    localWorldMapDictionary[robotId] = localWorldMap;
                else
                    localWorldMapDictionary.Add(robotId, localWorldMap);
            }
        }

        DecimalJsonConverter decimalJsonConverter = new DecimalJsonConverter();
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
                globalWorldMap.teammateGhostLocationList = new Dictionary<int, Location>();
                globalWorldMap.teammateDestinationLocationList = new Dictionary<int, Location>();
                globalWorldMap.teammateWayPointList = new Dictionary<int, Location>();
                globalWorldMap.opponentLocationList = new List<Location>();

                //On place tous les robots de l'équipe dans la global map
                foreach (var localMap in localWorldMapDictionary)
                {
                    //On ajoute la position des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateLocationList.Add(localMap.Key, localMap.Value.robotLocation);
                    //On ajoute le ghost (position théorique) des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateGhostLocationList.Add(localMap.Key, localMap.Value.robotGhostLocation);
                    //On ajoute la destination des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateDestinationLocationList.Add(localMap.Key, localMap.Value.destinationLocation);
                    //On ajoute le waypoint courant des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateWayPointList.Add(localMap.Key, localMap.Value.waypointLocation);
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

                lock (globalWorldMap.opponentLocationList)
                {
                    for (int i = 0; i < AdversairesPotentielsList.Count; i++)
                    {
                        if (AdversairesPotentielsMatchOccurenceList[i] >= 2)
                        {
                            var opponentLocation = AdversairesPotentielsList[i];
                            globalWorldMap.opponentLocationList.Add(opponentLocation);
                        }
                    }
                }
            }

            //On ajoute les informations de stratégie utilisant les commandes de la referee box
            globalWorldMap.gameState = currentGameState;
            globalWorldMap.stoppedGameAction = currentStoppedGameAction;

            string json = JsonConvert.SerializeObject(globalWorldMap, decimalJsonConverter);
            OnGlobalWorldMap(globalWorldMap);
            OnMulticastSendGlobalWorldMap(json.GetBytes());
        }

        void DefineRolesAndGameState()
        {
            
        }
        
        //Output events
        public event EventHandler<DataReceivedArgs> OnMulticastSendGlobalWorldMapEvent;
        public virtual void OnMulticastSendGlobalWorldMap(byte[] data)
        {
            var handler = OnMulticastSendGlobalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }

        public event EventHandler<GlobalWorldMapArgs> OnGlobalWorldMapEvent;
        public virtual void OnGlobalWorldMap(GlobalWorldMap map)
        {
            var handler = OnGlobalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new GlobalWorldMapArgs { GlobalWorldMap = map });
            }
        }
    }
}
