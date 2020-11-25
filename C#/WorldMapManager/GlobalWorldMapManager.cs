using AdvancedTimers;
using EventArgsLibrary;
using Newtonsoft.Json;
using PerformanceMonitorTools;
using RefereeBoxAdapter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Utilities;
using WorldMap;
using ZeroFormatter;

namespace WorldMapManager
{
    public class GlobalWorldMapManager
    {
        int TeamId;
        string TeamIpAddress = "";
        double freqRafraichissementWorldMap = 20;

        ConcurrentDictionary<int, LocalWorldMap> localWorldMapDictionary = new ConcurrentDictionary<int, LocalWorldMap>();
        GlobalWorldMapStorage globalWorldMapStorage = new GlobalWorldMapStorage();
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();
        //Timer globalWorldMapSendTimer;
        HighFreqTimer globalWorldMapSendTimer;

        GameState currentGameState = GameState.STOPPED;
        StoppedGameAction currentStoppedGameAction = StoppedGameAction.NONE;
        PlayingSide playingSide = PlayingSide.Left;

        bool bypassMulticastUdp = false;
        

        public GlobalWorldMapManager(int teamId, string ipAddress, bool bypassMulticast)
        {
            TeamId = teamId;
            TeamIpAddress = ipAddress;
            bypassMulticastUdp = bypassMulticast;
            globalWorldMapSendTimer = new HighFreqTimer(freqRafraichissementWorldMap);
            globalWorldMapSendTimer.Tick += GlobalWorldMapSendTimer_Tick; 
            globalWorldMapSendTimer.Start();
        }

        private void GlobalWorldMapSendTimer_Tick(object sender, EventArgs e)
        {
            //ATTENTION : Starting point temporel pour beaucoup de processing, car cela envoie la GlobalWorldMap aux robots.
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
                case RefBoxCommand.PLAYLEFT:
                    //currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        playingSide = PlayingSide.Left;
                    else
                        playingSide = PlayingSide.Right;
                    break;
                case RefBoxCommand.PLAYRIGHT:
                    //currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == TeamIpAddress)
                        playingSide = PlayingSide.Right;
                    else
                        playingSide = PlayingSide.Left;
                    break;
            }
        }


        private void AddOrUpdateLocalWorldMap(LocalWorldMap localWorldMap)
        {
            int robotId = localWorldMap.RobotId;
            int teamId = localWorldMap.TeamId;
            //lock (localWorldMapDictionary)
            {
                localWorldMapDictionary.AddOrUpdate(robotId, localWorldMap, (key, value) => localWorldMap);
                //if (localWorldMapDictionary.ContainsKey(robotId))
                //    localWorldMapDictionary[robotId] = localWorldMap;
                //else
                //    localWorldMapDictionary.Add(robotId, localWorldMap);
            }
        }

        DecimalJsonConverter decimalJsonConverter = new DecimalJsonConverter();

        double distanceMaxFusionObstacle = 0.2;
        double distanceMaxFusionTeamMate = 0.2;
        private void MergeLocalWorldMaps()
        {
            //Fusion des World Map locales pour construire la world map globale
            //lock (localWorldMapDictionary)
            {
                //On rassemble les infos issues des cartes locales de chacun des robots
                foreach (var localMap in localWorldMapDictionary)
                {
                    globalWorldMapStorage.AddOrUpdateRobotLocation(localMap.Key, localMap.Value.robotLocation);
                    globalWorldMapStorage.AddOrUpdateGhostLocation(localMap.Key, localMap.Value.robotGhostLocation);
                    globalWorldMapStorage.AddOrUpdateRobotDestination(localMap.Key, localMap.Value.destinationLocation);
                    globalWorldMapStorage.AddOrUpdateRobotWayPoint(localMap.Key, localMap.Value.waypointLocation);
                    globalWorldMapStorage.AddOrUpdateBallLocationList(localMap.Key, localMap.Value.ballLocationList);
                    globalWorldMapStorage.AddOrUpdateObstaclesList(localMap.Key, localMap.Value.obstaclesLocationList);
                    globalWorldMapStorage.AddOrUpdateRobotRole(localMap.Key, localMap.Value.robotRole);
                    globalWorldMapStorage.AddOrUpdateMessageDisplay(localMap.Key, localMap.Value.messageDisplay);
                    globalWorldMapStorage.AddOrUpdateRobotPlayingSide(localMap.Key, localMap.Value.playingSide);
                }

                //Génération de la carte fusionnée à partir des perceptions des robots de l'équipe
                //La fusion porte avant tout sur la balle et sur les adversaires.

                //TODO : faire un algo de fusion robuste pour la balle
                globalWorldMap = new WorldMap.GlobalWorldMap(TeamId);

                //Pour l'instant on prend la position de balle vue par le robot 1 comme vérité, mais c'est à améliorer !
                if (localWorldMapDictionary.Count > 0)
                    globalWorldMap.ballLocationList = localWorldMapDictionary.First().Value.ballLocationList;
                globalWorldMap.teammateLocationList = new Dictionary<int, Location>();
                globalWorldMap.teammateGhostLocationList = new Dictionary<int, Location>();
                globalWorldMap.teammateDestinationLocationList = new Dictionary<int, Location>();
                globalWorldMap.teammateWayPointList = new Dictionary<int, Location>();
                globalWorldMap.opponentLocationList = new List<Location>();
                globalWorldMap.obstacleLocationList = new List<LocationExtended>();
                globalWorldMap.teammateRoleList = new Dictionary<int, RobotRole>();
                globalWorldMap.teammateDisplayMessageList = new Dictionary<int, string>();
                globalWorldMap.teammatePlayingSideList = new Dictionary<int, PlayingSide>();

                //On place tous les robots de l'équipe dans la global map
                foreach (var localMap in localWorldMapDictionary)
                {
                    //On ajoute la position des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateLocationList.Add(localMap.Key, localMap.Value.robotLocation);
                    //On ajoute le rôle des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateRoleList.Add(localMap.Key, localMap.Value.robotRole);
                    //On ajoute le message à afficher des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateDisplayMessageList.Add(localMap.Key, localMap.Value.messageDisplay);
                    //On ajoute le playing Side des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammatePlayingSideList.Add(localMap.Key, localMap.Value.playingSide);
                    //On ajoute le ghost (position théorique) des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateGhostLocationList.Add(localMap.Key, localMap.Value.robotGhostLocation);
                    //On ajoute la destination des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateDestinationLocationList.Add(localMap.Key, localMap.Value.destinationLocation);
                    //On ajoute le waypoint courant des robots de l'équipe dans la WorldMap
                    globalWorldMap.teammateWayPointList.Add(localMap.Key, localMap.Value.waypointLocation);
                }

                try
                {
                    //TODO : Fusion des obstacles vus par chacun des robots
                    foreach (var localMap in localWorldMapDictionary)
                    {
                        foreach (var obstacle in localMap.Value.obstaclesLocationList)
                        {
                            bool skipNext = false;
                            /// On itère sur chacun des obstacles perçus par chacun des robots
                            /// On commence regarde pour chaque obstacle perçu si il ne correspond pas à une position de robot de l'équipe
                            ///     Si c'est le cas, on abandonne cet obstacle
                            ///     Si ce n'est pas le cas, on regarde si il ne correspond pas à un obstacle déjà présent dans la liste des obstacles
                            ///         Si ce n'est pas le cas, on l'ajoute à la liste des obtacles
                            ///         Si c'est le cas, on le fusionne en moyennant ses coordonnées de manière pondérée 
                            ///             et on renforce le poids de cet obstacle
                            foreach (var teamMateRobot in globalWorldMap.teammateLocationList)
                            {
                                if (Toolbox.Distance(new PointD(obstacle.X, obstacle.Y), new PointD(teamMateRobot.Value.X, teamMateRobot.Value.Y)) < distanceMaxFusionTeamMate)
                                {
                                    /// L'obstacle est un robot, on abandonne
                                    skipNext = true;
                                    break;
                                }
                            }
                            if (skipNext == false)
                            {
                                /// Si on arrive ici c'est que l'obstacle n'est pas un robot de l'équipe
                                foreach (var obstacleConnu in globalWorldMap.obstacleLocationList)
                                {
                                    if (Toolbox.Distance(new PointD(obstacle.X, obstacle.Y), new PointD(obstacleConnu.X, obstacleConnu.Y)) < distanceMaxFusionObstacle)
                                    {
                                        //L'obstacle est déjà connu, on le fusionne /TODO : améliorer la fusion avec pondération
                                        obstacleConnu.X = (obstacleConnu.X + obstacle.X) / 2;
                                        obstacleConnu.Y = (obstacleConnu.Y + obstacle.Y) / 2;
                                        skipNext = true;
                                        break;
                                    }
                                }
                            }
                            if (skipNext == false)
                            {
                                /// Si on arrive ici, c'est que l'obstacle n'était pas connu, on l'ajoute
                                globalWorldMap.obstacleLocationList.Add(obstacle);
                            }
                        }
                    }
                }
                catch { }
            }

            /// On ajoute les informations issues des commandes de la referee box
            globalWorldMap.gameState = currentGameState;
            globalWorldMap.stoppedGameAction = currentStoppedGameAction;
            globalWorldMap.playingSide = playingSide;

            if (bypassMulticastUdp)
            {
                //ATTENTION : on bypass l'envoi en Multicast UDP : non utilisable à la ROBOCUP
                OnGlobalWorldMapBypass(globalWorldMap);
            }
            else
            {
                var s = ZeroFormatterSerializer.Serialize<WorldMap.WorldMap>(globalWorldMap);

                //var deserialzation = ZeroFormatterSerializer.Deserialize<WorldMap.WorldMap>(s);

                //switch(deserialzation.Type)
                //{
                //    case WorldMapType.GlobalWM:
                //        globalWorldMap = (GlobalWorldMap)deserialzation;
                //        break;
                //    default:
                //        break;
                //}

                //string json = JsonConvert.SerializeObject(globalWorldMap, decimalJsonConverter);
                //OnMulticastSendGlobalWorldMap(json.GetBytes());

                OnMulticastSendGlobalWorldMap(s);
                GWMEmiseMonitoring.GWMEmiseMonitor(s.Length);
            }
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

        ////Output event for Multicast Bypass : NO USE at RoboCup !
        public event EventHandler<GlobalWorldMapArgs> OnGlobalWorldMapBypassEvent;
        public virtual void OnGlobalWorldMapBypass(GlobalWorldMap map)
        {
            var handler = OnGlobalWorldMapBypassEvent;
            if (handler != null)
            {
                handler(this, new GlobalWorldMapArgs { GlobalWorldMap = map });
            }
        }
    }
}

