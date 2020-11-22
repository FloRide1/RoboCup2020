using EventArgsLibrary;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using WorldMap;

namespace StrategyManager.StrategyRoboCupNS
{
    class StrategyRoboCup : StrategyGenerique
    {
        Stopwatch sw = new Stopwatch();

        public PointD robotDestination = new PointD(0, 0);

        RobotRole role = RobotRole.Stopped;
        PlayingSide playingSide = PlayingSide.Left;

        public StrategyRoboCup(int robotId, int teamId) : base(robotId, teamId)
        {
            this.teamId = teamId;
            this.robotId = robotId;
        }

        public override void InitHeatMap()
        {
            positioningHeatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 7)); //Init HeatMap
        }

        public override void DetermineRobotRole()
        {
            /// La détermination des rôles du robot se fait robot par robot, chacun détermine son propre rôle en temps réel. 
            /// Il n'y a pas de centralisation de la détermination dans la Base Station, ce qui permettra ultérieurement de jouer sans base station.
            /// 
            /// Le Gamestate est donné par la BaseStation via la GlobalWorldMap car il intègre les commandes de la Referee Box
            /// 
            /// On détermine la situation de jeu : defense / attaque / arret / placement avant remise en jeu / ...
            /// et on détermine le rôle du robot.
            /// 


            switch (globalWorldMap.gameState)
            {
                case GameState.STOPPED:
                    role = RobotRole.Stopped;
                    break;
                case GameState.PLAYING:
                    {
                        /// On commence par créer une liste de TeamMateRoleClassifier qui va permettre de trier intelligemment 
                        /// 
                        Dictionary<int, TeamMateRoleClassifier> teamRoleClassifier = new Dictionary<int, TeamMateRoleClassifier>();
                        foreach (var teammate in globalWorldMap.teammateLocationList)
                        {
                            TeamMateRoleClassifier player = new TeamMateRoleClassifier(new PointD(teammate.Value.X, teammate.Value.Y), RobotRole.Stopped);
                            teamRoleClassifier.Add(teammate.Key, player);
                        }

                        foreach (var teammate in teamRoleClassifier)
                        {
                            if (teammate.Key % 10 == 0)
                            {
                                teamRoleClassifier[teammate.Key].Role = RobotRole.Gardien;
                            }
                        }

                        /// On détermine le rang du joueur dans l'équipe en fonction de sa distance au ballon                        
                        int rangDistanceBalle = -1;

                        if (globalWorldMap.ballLocationList.Count > 0)
                        {
                            var ballPosition = new PointD(globalWorldMap.ballLocationList[0].X, globalWorldMap.ballLocationList[0].Y);
                            for (int i = 0; i < teamRoleClassifier.Count(); i++)
                            {
                                ///On ajoute à la liste en premier la distance à chacun des coéquipiers
                                teamRoleClassifier.ElementAt(i).Value.DistanceBalle = Toolbox.Distance(teamRoleClassifier.ElementAt(i).Value.Position, ballPosition);
                            }
                        }

                        /// On détermine le rang du joueur dans l'équipe en fonction de sa distance au but 
                        PointD goalPosition;
                        if (playingSide == PlayingSide.Right)
                            goalPosition = new PointD(-11, 0);
                        else
                            goalPosition = new PointD(11, 0);

                        ///On ajoute à la liste en premier la distance au robot considéré                        
                        for (int i = 0; i < globalWorldMap.teammateLocationList.Count(); i++)
                        {
                            ///On ajoute à la liste en premier la distance à chacun des coéquipiers
                            teamRoleClassifier.ElementAt(i).Value.DistanceBut = Toolbox.Distance(teamRoleClassifier.ElementAt(i).Value.Position, goalPosition);
                        }

                        /// A présent, on filtre la liste de l'équipe de manière à trouver le joueur le plus proche de la balle n'étant pas le gardien
                        var teamSansGardienOrdonnee = teamRoleClassifier.Where(x => x.Value.Role != RobotRole.Gardien).OrderBy(elt => elt.Value.DistanceBalle).ToList();
                        teamRoleClassifier[teamSansGardienOrdonnee.ElementAt(0).Key].Role = RobotRole.ContesteurDeBalle;

                        /// A présent, on filtre la liste de l'équipe de manière à trouver le joueur le plus proche de la balle n'étant pas le gardien, ni le contesteur
                        var teamSansGardienNiContesteurOrdonnee = teamRoleClassifier.Where(x => (x.Value.Role != RobotRole.Gardien)&& (x.Value.Role != RobotRole.ContesteurDeBalle)).OrderBy(elt => elt.Value.DistanceBut).ToList();
                        teamRoleClassifier[teamSansGardienNiContesteurOrdonnee.ElementAt(0).Key].Role = RobotRole.AttaquantDemarque;

                        var teamSansGardienNiContesteurNiAttaquantOrdonnee = teamRoleClassifier.Where(x => (x.Value.Role != RobotRole.Gardien) && (x.Value.Role != RobotRole.AttaquantDemarque) && (x.Value.Role != RobotRole.ContesteurDeBalle)).OrderBy(elt => elt.Value.DistanceBut).ToList();
                        teamRoleClassifier[teamSansGardienNiContesteurNiAttaquantOrdonnee.ElementAt(0).Key].Role = RobotRole.DefenseurInterception;
                        teamRoleClassifier[teamSansGardienNiContesteurNiAttaquantOrdonnee.ElementAt(1).Key].Role = RobotRole.DefenseurInterception;

                        role = teamRoleClassifier[robotId].Role;
                        //int rangDistanceBut = -1;

                        ///// En fonction de la distance au but et de la distance à la balle
                        ///// on prend la décision d'attribution des rôles dans l'équipe
                        ///// Le plus proche de la balle est COn

                        //if (rangDistanceBalle == 0)
                        //    role = RobotRole.ContesteurDeBalle;
                        //else
                        //{
                        //    if (rangDistanceBut <= 1)
                        //        role = RobotRole.AttaquantDemarque;
                        //    else
                        //        role = RobotRole.DefenseurInterception;
                        //}

                    }

                    break;
                case GameState.STOPPED_GAME_POSITIONING:
                    role = RobotRole.DefenseurInterception;
                    break;
            }

            OnRole(robotId, role);
            playingSide = globalWorldMap.playingSide;
            //OnPlayingSide(robotId, playingSide);

            /// En fonction du rôle attribué, on définit les zones de préférence, les zones à éviter et les zones d'exclusion
            DefinePlayerZones(role);
        }

        public void DefinePlayerZones(RobotRole role)
        {

            InitPreferedZones();
            InitAvoidanceZones();
            InitForbiddenRectangleList();
            InitStrictlyAllowedRectangleList();
            InitAvoidanceConicalZoneList();
            InitPreferredSegmentZoneList();

            ///On exclut d'emblée les surface de réparation pour tous les joueurs
            if (role != RobotRole.Gardien)
            {
                AddForbiddenRectangle(new RectangleD(-11, -11 + 0.75, -3.9 / 2, 3.9 / 2));
                AddForbiddenRectangle(new RectangleD(11, 11 - 0.75, -3.9 / 2, 3.9 / 2));
            }

            switch (role)
            {
                case RobotRole.Gardien:
                    /// Gestion du cas du gardien
                    /// Exclusion de tout le terrain sauf la surface de réparation
                    /// Ajout d'une zone préférentielle centrée sur le but
                    /// Réglage du cap pour faire toujours face à la balle
                    if (playingSide == PlayingSide.Right)
                    {
                        AddStrictlyAllowedRectangle(new RectangleD(11 - 0.75, 11, -3.9 / 2, 3.9 / 2));
                        AddPreferedZone(new PointD(10.6, 0), 1.5);
                    }
                    else
                    {
                        AddStrictlyAllowedRectangle(new RectangleD(-11, -11 + 0.75, -3.9 / 2, 3.9 / 2));
                        AddPreferedZone(new PointD(-10.6, 0), 1.5);
                    }

                    if (globalWorldMap.ballLocationList.Count > 0)
                        robotOrientation = Math.Atan2(globalWorldMap.ballLocationList[0].Y - robotCurrentLocation.Y, globalWorldMap.ballLocationList[0].X - robotCurrentLocation.X);
                    break;
                case RobotRole.ContesteurDeBalle:
                    if (globalWorldMap.ballLocationList.Count > 0)
                        AddPreferedZone(new PointD(globalWorldMap.ballLocationList[0].X, globalWorldMap.ballLocationList[0].Y), 3, 0.5);
                    break;
                case RobotRole.AttaquantDemarque:
                    /// Gestion du cas de l'attaquant démarqué
                    /// Il doit faire en sorte que la ligne de passe entre lui et le porteur du ballon soit libre
                    ///     Pour cela il faudrait idéalement placer une zone de pénalisation conique de centre le joueur dans l'axe de chaque adversaire
                    /// Il doit également se placer dans un position de tir possible 
                    ///     Pour cela il faudrait idéalement placer une zone de pénalisation conique de centre le but dans l'axe de chaque adversaire
                    
                    if (playingSide == PlayingSide.Left)
                    {
                        AddPreferredSegmentZoneList(new PointD(7, -3), new PointD(7, 3), 3, 1);
                        //AddPreferedZone(new PointD(8, 3), 3, 0.1);
                        //AddPreferedZone(new PointD(8, -3), 3, 0.1);
                    }
                        else
                    {
                        //AddPreferredSegmentZoneList(new PointD(-7, -3), new PointD(-7, 3), 3, 1);
                        //AddPreferedZone(new PointD(-8, 3), 3, 0.1);
                        //AddPreferedZone(new PointD(-8, -3), 3, 0.1);
                    }

                    foreach (var adversaire in globalWorldMap.obstacleLocationList)
                    {
                        AddAvoidanceConicalZoneList(new PointD(robotCurrentLocation.X, robotCurrentLocation.Y), new PointD(adversaire.X, adversaire.Y), 1);                        
                    }
                    break;
                case RobotRole.DefenseurInterception:
                    foreach (var adversaire1 in globalWorldMap.obstacleLocationList)
                    {
                        foreach (var adversaire2 in globalWorldMap.obstacleLocationList)
                        {
                            if(adversaire1!=adversaire2)
                            {
                                AddPreferedZone(new PointD((adversaire1.X + adversaire2.X) / 2, (adversaire1.Y + adversaire2.Y) / 2), 1.4, 0.5);                                
                            }
                        }
                    }
                    break;
            }
        }
        public override void IterateStateMachines()
        {

            //AddPreferedZone(new PointD(5, -3), 1);
            //AddPreferedZone(new PointD(10, 5), 3.5);
            //AddPreferedZone(new PointD(-2, 4), 3.5);
            //AddPreferedZone(new PointD(-7, -4), 2.0);
            //AddPreferedZone(new PointD(-8, 0), 5.0);
            //AddPreferedZone(new PointD(0, 1.5), 3.5);
            //AddPreferedZone(new PointD(3, 5), 1.5);

            //AddAvoidanceZone(new PointD(0, 0), 5.5);

            //Ajout d'une zone préférée autour du robot lui-même de manière à stabiliser son comportement sur des cartes presques plates
            AddPreferedZone(new PointD(robotCurrentLocation.X, robotCurrentLocation.Y), 2, 0.1);

            //AddForbiddenRectangle(new RectangleD(-5, 3, 4, 6));
        }

        //void SetRobotRole()
        //{
        //    //On détermine les distances des joueurs à la balle
        //    Dictionary<int, double> DictDistancePlayerBall = new Dictionary<int, double>();
        //    var ballLocationList = globalWorldMap.ballLocationList;
        //    foreach (var player in globalWorldMap.teammateLocationList)
        //    {
        //        //On exclut le gardien
        //        if (player.Key != (int)TeamId.Team1 + (int)Constants.RobotId.Robot1 && player.Key != (int)TeamId.Team2 + (int)Constants.RobotId.Robot1)
        //        {
        //            DictDistancePlayerBall.Add(player.Key, Toolbox.Distance(new PointD(player.Value.X, player.Value.Y), new PointD(ballLocationList[0].X, ballLocationList[0].Y)));
        //        }
        //    }

        //    var OrderedDictDistancePlayerBall = DictDistancePlayerBall.OrderBy(p => p.Value);
        //    for (int i = 0; i < OrderedDictDistancePlayerBall.Count(); i++)
        //    {
        //        if (OrderedDictDistancePlayerBall.ElementAt(i).Key == robotId)
        //        {
        //            switch (i)
        //            {
        //                case 0:
        //                    robotRole = PlayerRole.AttaquantAvecBalle;
        //                    break;
        //                case 1:
        //                    robotRole = PlayerRole.AttaquantPlace;
        //                    break;
        //                case 2:
        //                    robotRole = PlayerRole.Centre;
        //                    break;
        //                default:
        //                    robotRole = PlayerRole.Centre;
        //                    break;
        //            }
        //        }
        //    }

        //    if (robotId == (int)TeamId.Team1 + (int)Constants.RobotId.Robot1 || robotId == (int)TeamId.Team2 + (int)Constants.RobotId.Robot1)
        //    {
        //        //Cas du gardien
        //        robotRole = PlayerRole.Gardien;
        //    }
        //}

        //public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        //public virtual void OnHeatMap(int id, Heatmap heatMap)
        //{
        //    OnHeatMapEvent?.Invoke(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
        //}

        //public event EventHandler<LocationArgs> OnDestinationEvent;
        //public virtual void OnDestination(int id, Location location)
        //{
        //    OnDestinationEvent?.Invoke(this, new LocationArgs { RobotId = id, Location = location });
        //}

    }

    public class TeamMateRoleClassifier
    {
        public PointD Position;
        public RobotRole Role;
        public double DistanceBalle;
        public double DistanceBut;

        public TeamMateRoleClassifier(PointD position, RobotRole role)
        {
            this.Role = role;
            Position = position;
        }
    }

}
