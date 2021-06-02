﻿using Constants;
using EventArgsLibrary;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using WorldMap;

namespace StrategyManagerProjetEtudiantNS.StrategyRoboCupNS
{
    public class StrategyRoboCup : StrategyGenerique
    {
        Stopwatch sw = new Stopwatch();

        RoboCupRobotRole role = RoboCupRobotRole.Stopped;
        public PointD robotDestination = new PointD(0, 0);
        PlayingSide playingSide = PlayingSide.Left;
        public BallHandlingState ballHandlingState = BallHandlingState.NoBall;

        public GameState gameState = GameState.STOPPED;
        public StoppedGameAction stoppedGameAction = StoppedGameAction.NONE;

        public string MessageDisplay = "Debug";

        private double _obstacleAvoidanceDistance = 1.0;
        public override double ObstacleAvoidanceDistance 
        { 
            get { return _obstacleAvoidanceDistance; } 
            set { _obstacleAvoidanceDistance = value; } 
        }
        
        public Location externalRefBoxPosition = new Location();
        TaskBallHandlingManagement taskBallHandlingManagement;

        Timer configTimer;

        public StrategyRoboCup(int robotId, int teamId, string multicastIpAddress) : base(robotId, teamId, multicastIpAddress)
        {
            taskBallHandlingManagement = new TaskBallHandlingManagement(this);
        }

        public override void InitStrategy(int robotId, int teamId)
        {
            //On initialisae le timer de réglage récurrent 
            //Il permet de modifier facilement les paramètre des asservissement durant l'exécution
            configTimer = new System.Timers.Timer(1000);
            configTimer.Elapsed += ConfigTimer_Elapsed;
            configTimer.Start();

            //Obtenus directement à partir du script Matlab
            OnOdometryPointToMeter(4.261590e-06);
            On4WheelsAngleSet(1.256637e+00, 2.513274e+00, 3.769911e+00, 5.026548e+00);
            On4WheelsToPolarSet(-3.804226e-01, -2.351141e-01, 2.351141e-01, 3.804226e-01,
                                4.472136e-01, -4.472136e-01, -4.472136e-01, 4.472136e-01,
                                1.955694e+00, 7.470087e-01, 7.470087e-01, 1.955694e+00);
        }

        private void ConfigTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            double KpIndependant = 2.5;
            double KiIndependant = 300;
            //On envoie périodiquement les réglages du PID de vitesse embarqué
            On4WheelsIndependantSpeedPIDSetup(pM1: KpIndependant, iM1: KiIndependant, 0.0, pM2: KpIndependant, iM2: KiIndependant, 0, pM3: KpIndependant, iM3: KiIndependant, 0, pM4: KpIndependant, iM4: KiIndependant, 0.0,
                pM1Limit: 4, iM1Limit: 4, 0, pM2Limit: 4.0, iM2Limit: 4.0, 0, pM3Limit: 4.0, iM3Limit: 4.0, 0, pM4Limit: 4.0, iM4Limit: 4.0, 0);
            //OnSetRobotSpeedIndependantPID(pM1: 4.0, iM1: 300, 0.0, pM2: 4.0, iM2: 300, 0, pM3: 4.0, iM3: 300, 0, pM4: 4.0, iM4: 300, 0.0,
            //    pM1Limit: 4.0, iM1Limit: 4.0, 0, pM2Limit: 4.0, iM2Limit: 4.0, 0, pM3Limit: 4.0, iM3Limit: 4.0, 0, pM4Limit: 4.0, iM4Limit: 4.0, 0);
            On4WheelsPolarSpeedPIDSetup(px: 4.0, ix: 300, 0.0, py: 4.0, iy: 300, 0, ptheta: 4.0, itheta: 300, 0,
                pxLimit: 4.0, ixLimit: 4.0, 0, pyLimit: 4.0, iyLimit: 4.0, 0, pthetaLimit: 4.0, ithetaLimit: 4.0, 0);

            OnSetAsservissementMode((byte)AsservissementMode.Independant4Wheels);
        }

        public override void InitHeatMap()
        {
            strategyHeatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 6)); //Init HeatMap
            WayPointHeatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 6)); //Init HeatMap
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


            switch (gameState)
            {
                case GameState.STOPPED:
                    role = RoboCupRobotRole.Stopped;
                    break;
                case GameState.PLAYING:
                    {
                        /// On commence par créer une liste de TeamMateRoleClassifier qui va permettre de trier intelligemment                         /// 
                        /// 
                        /// On détermine le rang du joueur dans l'équipe en fonction de sa distance au ballon   
                        /// On détermine le rang du joueur dans l'équipe en fonction de sa distance au but   
                        /// 
                        /// On regarde si une des équipe a la balle
                        /// 
                        
                        Dictionary<int, TeamMateRoleClassifier> teamRoleClassifier = new Dictionary<int, TeamMateRoleClassifier>();
                        foreach (var teammate in globalWorldMap.teammateLocationList)
                        {
                            TeamMateRoleClassifier player = new TeamMateRoleClassifier(new PointD(teammate.Value.X, teammate.Value.Y), RoboCupRobotRole.Unassigned);
                            teamRoleClassifier.Add(teammate.Key, player);
                        }

                        foreach (var teammate in teamRoleClassifier)
                        {
                            if (teammate.Key % 10 == 0)
                            {
                                teamRoleClassifier[teammate.Key].Role = RoboCupRobotRole.Gardien;
                            }
                        }

                        if (playingSide == PlayingSide.Right)
                        {
                            foreach (var teammate in teamRoleClassifier)
                            {
                                if (teammate.Key % 10 != 0)
                                {
                                    teamRoleClassifier[teammate.Key].Role = RoboCupRobotRole.Stone;
                                }
                            }
                        }
                        else
                        {
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
                            /// Pour cela il faut d'abord définir la position du but.
                            PointD offensiveGoalPosition;
                            PointD defensiveGoalPosition;
                            if (playingSide == PlayingSide.Right)
                            {
                                offensiveGoalPosition = new PointD(-11, 0);
                                defensiveGoalPosition = new PointD(11, 0);
                            }
                            else
                            {
                                defensiveGoalPosition = new PointD(-11, 0);
                                offensiveGoalPosition = new PointD(11, 0);
                            }

                            for (int i = 0; i < globalWorldMap.teammateLocationList.Count(); i++)
                            {
                                teamRoleClassifier.ElementAt(i).Value.DistanceButOffensif = Toolbox.Distance(teamRoleClassifier.ElementAt(i).Value.Position, offensiveGoalPosition);
                            }

                            ///On détermine à présent si l'équipe à la balle et quel joueur la possède
                            ///
                            var teamBallHandlingState = BallHandlingState.NoBall;
                            int IdplayerHandlingBall = -1;
                            foreach (var teammate in globalWorldMap.teammateBallHandlingStateList)
                            {
                                if (teammate.Value != BallHandlingState.NoBall)
                                {
                                    teamBallHandlingState = teammate.Value;
                                    IdplayerHandlingBall = teammate.Key;
                                }
                            }

                            /// Les indicateurs principaux nécessaire à la stratégie ont été déterminés : 
                            /// Possession de balle, distance au but de chacun des coéquipiers et
                            /// distance à la balle de chacun des équipiers.
                            /// Il est donc posssible de prendre des décisions stratégiques de jeu
                            /// On commence par choisir le mode défense ou attaque selon que l'on a la balle ou pas
                            /// 

                            if (teamBallHandlingState == BallHandlingState.NoBall)
                            {
                                /// L'équipe n'a pas la balle, elle se place en mode défense
                                /// On veut deux joueurs en défense placée qui marquent les deux joueurs les plus en avant de l'équipe adverse
                                /// On veut un joueur en contestation de balle
                                /// On veut un joueur en défense d'interception qui coupe les lignes de passe adverses

                                /// A présent, on filtre la liste de l'équipe de manière à trouver le joueur 
                                /// le plus proches de la balle n'étant pas le gardien
                                var teamFiltered1 = teamRoleClassifier.Where(x => x.Value.Role == RoboCupRobotRole.Unassigned).OrderBy(elt => elt.Value.DistanceBalle).ToList();

                                if (teamFiltered1.Count > 0)
                                {
                                    teamRoleClassifier[teamFiltered1.ElementAt(0).Key].Role = RoboCupRobotRole.DefenseurContesteur;
                                }

                                /// A présent, on filtre la liste de l'équipe de manière à trouver le joueur le plus proche du but à défendre
                                /// n'étant pas gardien, ni défenseur au marquage
                                /// Il devient contesteur de balle
                                var teamFiltered2 = teamRoleClassifier.Where(x => (x.Value.Role == RoboCupRobotRole.Unassigned))
                                                                      .OrderBy(elt => elt.Value.DistanceButDefensif).ToList();
                                if (teamFiltered2.Count > 1)
                                {
                                    teamRoleClassifier[teamFiltered2.ElementAt(1).Key].Role = RoboCupRobotRole.DefenseurMarquage;
                                    teamRoleClassifier[teamFiltered2.ElementAt(2).Key].Role = RoboCupRobotRole.DefenseurMarquage;
                                }

                                /// A présent, on filtre la liste de l'équipe de manière à trouver le joueur le plus proche du but à défendre
                                /// n'étant pas gardien, ni défenseur au marquage, ni contesteur
                                /// Il devient défenseur intercepteur
                                var teamFiltered3 = teamRoleClassifier.Where(x => (x.Value.Role == RoboCupRobotRole.Unassigned))
                                                                      .OrderBy(elt => elt.Value.DistanceButDefensif).ToList();
                                if (teamFiltered3.Count > 0)
                                {
                                    teamRoleClassifier[teamFiltered3.ElementAt(0).Key].Role = RoboCupRobotRole.DefenseurIntercepteur;
                                }
                            }

                            else
                            {
                                /// L'équipe a la balle, elle se place en mode attaque
                                /// On a un joueur ayant le ballon qui est l'attaquant avec balle
                                /// On veut deux joueurs attaquants démarqués avec lignes de passes ouvertes
                                /// On veut un attaquant placé entre un défenseur et l'attaquant ayant la balle
                                /// 
                                teamRoleClassifier[IdplayerHandlingBall].Role = RoboCupRobotRole.AttaquantAvecBalle;

                                var teamFiltered1 = teamRoleClassifier.Where(x => (x.Value.Role == RoboCupRobotRole.Unassigned))
                                                                      .OrderBy(elt => elt.Value.DistanceButOffensif).ToList();

                                if (teamFiltered1.Count > 1)
                                {
                                    teamRoleClassifier[teamFiltered1.ElementAt(0).Key].Role = RoboCupRobotRole.AttaquantDemarque;
                                    teamRoleClassifier[teamFiltered1.ElementAt(1).Key].Role = RoboCupRobotRole.AttaquantDemarque;
                                }

                                /// A présent, on filtre la liste de l'équipe de manière à trouver le joueur le plus proche du but en attaque
                                /// n'étant pas gardien, ni attaquant avec le ballon ni attaquant Démarqué 
                                /// Il devient attaquant intercepteur
                                var teamFiltered2 = teamRoleClassifier.Where(x => (x.Value.Role == RoboCupRobotRole.Unassigned))
                                                                      .OrderBy(elt => elt.Value.DistanceButOffensif).ToList();
                                if (teamFiltered2.Count > 0)
                                {
                                    teamRoleClassifier[teamFiltered2.ElementAt(0).Key].Role = RoboCupRobotRole.AttaquantIntercepteur;
                                }
                            }
                        }
                        role = teamRoleClassifier[robotId].Role;
                    }
                    break;
                case GameState.STOPPED_GAME_POSITIONING:
                    role = RoboCupRobotRole.Positioning;
                    break;
            }

            OnRole(robotId, role);
            OnBallHandlingState(robotId, BallHandlingState.NoBall);
            OnMessageDisplay(robotId, MessageDisplay);
            
            /// En fonction du rôle attribué, on définit les zones de préférence, les zones à éviter et les zones d'exclusion
            DefinePlayerZones(role);
        }

        public void DefinePlayerZones(RoboCupRobotRole role)
        {
            ///On exclut d'emblée les surface de réparation pour tous les joueurs
            if (role != RoboCupRobotRole.Gardien)
            {
                AddForbiddenRectangle(new RectangleD(-11, -11 + 0.75 + 0.2, -3.9 / 2 - 0.2, 3.9 / 2 + 0.2));
                AddForbiddenRectangle(new RectangleD(+11 - 0.75 + 0.2, +11, -3.9 / 2 - 0.2, 3.9 / 2 + 0.2));
            }

            /// On a besoin du rang des adversaires en fonction de leur distance au but 
            /// Pour cela il faut d'abord définir la position du but.
            PointD offensiveGoalPosition;
            PointD defensiveGoalPosition;
            if (playingSide == PlayingSide.Right)
            {
                offensiveGoalPosition = new PointD(-11, 0);
                defensiveGoalPosition = new PointD(11, 0);
            }
            else
            {
                defensiveGoalPosition = new PointD(-11, 0);
                offensiveGoalPosition = new PointD(11, 0);
            }

            switch (role)
            {
                case RoboCupRobotRole.Gardien:
                    /// Gestion du cas du gardien
                    /// Exclusion de tout le terrain sauf la surface de réparation
                    /// Ajout d'une zone préférentielle centrée sur le but
                    /// Réglage du cap pour faire toujours face à la balle
                    if (playingSide == PlayingSide.Right)
                    {
                        AddPreferredRectangle(new RectangleD(11 - 0.75, 11, -3.9 / 2, 3.9 / 2));
                        AddPreferedZone(new PointD(10.6, 0), 4.5, 0.2);
                    }
                    else
                    {
                        AddPreferredRectangle(new RectangleD(-11, -11 + 0.75, -3.9 / 2, 3.9 / 2));
                        AddPreferedZone(new PointD(-10.6, 0), 4.5, 0.2);
                    }

                    if (globalWorldMap.ballLocationList.Count > 0)
                        robotOrientation = Math.Atan2(globalWorldMap.ballLocationList[0].Y - robotCurrentLocation.Y, globalWorldMap.ballLocationList[0].X - robotCurrentLocation.X);
                    break;
                case RoboCupRobotRole.Stone:
                    AddPreferedZone(new PointD(-5, 4), 2.5);
                    AddPreferedZone(new PointD(-5, -4), 2.5);
                    AddPreferedZone(new PointD(5, -4), 2.5);
                    AddPreferedZone(new PointD(5, 4), 2.5);
                    break;

                case RoboCupRobotRole.Positioning:
                    AddPreferedZone(new PointD(externalRefBoxPosition.X, externalRefBoxPosition.Y), 5);
                    robotOrientation = 0;
                    break;

                case RoboCupRobotRole.DefenseurContesteur:
                    if (globalWorldMap.ballLocationList.Count > 0)
                        AddPreferedZone(new PointD(globalWorldMap.ballLocationList[0].X, globalWorldMap.ballLocationList[0].Y), 3, 0.5);
                    if (globalWorldMap.ballLocationList.Count > 0)
                        robotOrientation = Math.Atan2(globalWorldMap.ballLocationList[0].Y - robotCurrentLocation.Y, globalWorldMap.ballLocationList[0].X - robotCurrentLocation.X);

                    break;
                case RoboCupRobotRole.DefenseurMarquage:
                    {
                        /// On va placer un défenseur à une distance définie de l'attaquant, sur la ligne attaquant but
                        /// Les zones d'intérêt sont devant les deux attaquants les plus en pointe
                        /// Il faut donc commencer par les trouver
                        Dictionary<int, TeamMateRoleClassifier> adversaireClassifier = new Dictionary<int, TeamMateRoleClassifier>();
                        int i = 0;
                        foreach (var adversaire in globalWorldMap.obstacleLocationList)
                        {
                            var adv = new TeamMateRoleClassifier(new PointD(adversaire.X, adversaire.Y), RoboCupRobotRole.Adversaire);
                            adv.DistanceButDefensif = Toolbox.Distance(new PointD(adversaire.X, adversaire.Y), defensiveGoalPosition);
                            adversaireClassifier.Add(i++, adv);
                        }

                        /// A présent, on filtre la liste des adversaires de manière à trouver les joueurs les plus proches du but en défense
                        /// 
                        var teamFiltered1 = adversaireClassifier.OrderBy(elt => elt.Value.DistanceButDefensif).ToList();

                        if (teamFiltered1.Count > 1)
                        {
                            if (playingSide == PlayingSide.Right)
                            {
                                AddPreferedZone(new PointD(teamFiltered1[0].Value.Position.X + 2, teamFiltered1[0].Value.Position.Y), 1.5);
                                AddPreferedZone(new PointD(teamFiltered1[1].Value.Position.X + 2, teamFiltered1[1].Value.Position.Y), 1.5);
                            }
                            else
                            {
                                AddPreferedZone(new PointD(teamFiltered1[0].Value.Position.X - 2, teamFiltered1[0].Value.Position.Y), 1.5);
                                AddPreferedZone(new PointD(teamFiltered1[1].Value.Position.X - 2, teamFiltered1[1].Value.Position.Y), 1.5);
                            }
                        }

                        if (globalWorldMap.ballLocationList.Count > 0)
                            robotOrientation = Math.Atan2(globalWorldMap.ballLocationList[0].Y - robotCurrentLocation.Y, globalWorldMap.ballLocationList[0].X - robotCurrentLocation.X);

                    }
                    break;

                case RoboCupRobotRole.DefenseurIntercepteur:
                    {
                        /// On va placer un défenseur à une distance définie de l'attaquant, sur la ligne attaquant but
                        /// Les zones d'intérêt sont devant les deux attaquants les plus en pointe
                        /// Il faut donc commencer par les trouver
                        Dictionary<int, TeamMateRoleClassifier> adversaireClassifier = new Dictionary<int, TeamMateRoleClassifier>();
                        int i = 0;
                        foreach (var adversaire in globalWorldMap.obstacleLocationList)
                        {
                            var adv = new TeamMateRoleClassifier(new PointD(adversaire.X, adversaire.Y), RoboCupRobotRole.Adversaire);
                            adv.DistanceButDefensif = Toolbox.Distance(new PointD(adversaire.X, adversaire.Y), defensiveGoalPosition);
                            adversaireClassifier.Add(i++, adv);
                        }

                        /// A présent, on filtre la liste des adversaires de manière à trouver les joueurs les plus proches du but en défense
                        /// 
                        var teamFiltered1 = adversaireClassifier.OrderBy(elt => elt.Value.DistanceButDefensif).ToList();

                        if (teamFiltered1.Count > 1)
                        {
                            var adversaire1 = teamFiltered1[0].Value.Position;
                            var adversaire2 = teamFiltered1[1].Value.Position;
                            AddPreferredSegmentZoneList(new PointD(adversaire1.X, adversaire1.Y), new PointD(adversaire2.X, adversaire2.Y), 0.4, 0.1);
                            AddPreferedZone(new PointD((adversaire1.X + adversaire2.X) / 2, (adversaire1.Y + adversaire2.Y) / 2), 0.4, 0.3);
                            AddAvoidanceZone(new PointD(adversaire1.X, adversaire1.Y), 2, 0.5);

                            //AddPreferedZone(new PointD((teamFiltered1[0].Value.Position.X + teamFiltered1[1].Value.Position.X) / 2, (teamFiltered1[0].Value.Position.Y + teamFiltered1[0].Value.Position.Y) / 2), 2.5);
                        }

                        if (globalWorldMap.ballLocationList.Count > 0)
                            robotOrientation = Math.Atan2(globalWorldMap.ballLocationList[0].Y - robotCurrentLocation.Y, globalWorldMap.ballLocationList[0].X - robotCurrentLocation.X);

                    }
                    break;
                case RoboCupRobotRole.AttaquantDemarque:
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
                    if (globalWorldMap.ballLocationList.Count > 0)
                        robotOrientation = Math.Atan2(globalWorldMap.ballLocationList[0].Y - robotCurrentLocation.Y, globalWorldMap.ballLocationList[0].X - robotCurrentLocation.X);

                    break;

                case RoboCupRobotRole.AttaquantAvecBalle:
                    {
                        /// L'attaquant avec balle doit aller vers le but si il a de l'espace
                        /// Il peut faire des passes à ses coéquipiers démarqués si il est dans une zone un peu dense
                        /// 
                        /// On va placer un défenseur à une distance définie de l'attaquant, sur la ligne attaquant but
                        /// Les zones d'intérêt sont devant les deux attaquants les plus en pointe
                        /// Il faut donc commencer par les trouver
                        
                        Dictionary<int, TeamMateRoleClassifier> adversaireClassifier = new Dictionary<int, TeamMateRoleClassifier>();
                        int i = 0;
                        foreach (var adversaire in globalWorldMap.obstacleLocationList)
                        {
                            var adv = new TeamMateRoleClassifier(new PointD(adversaire.X, adversaire.Y), RoboCupRobotRole.Adversaire);
                            adv.DistanceRobotConsidere = Toolbox.Distance(new PointD(adversaire.X, adversaire.Y), new PointD(robotCurrentLocation.X, robotCurrentLocation.Y));
                            adversaireClassifier.Add(i++, adv);
                        }

                        /// A présent, on filtre la liste des adversaires de manière à trouver les joueurs les plus proches du robot considéré
                        /// 
                        var teamFiltered1 = adversaireClassifier.OrderBy(elt => elt.Value.DistanceRobotConsidere).ToList();

                        if (teamFiltered1.Count > 0)
                        {
                            var adversaireLePlusProche = teamFiltered1[0].Value.Position;
                            if (teamFiltered1[0].Value.DistanceRobotConsidere > 2)
                            {
                                ///On a au moins 2m devant nous, on va vers le but
                                ///TODO : raffiner pour ne prendre en compte que les robots entre le but et nous...
                                AddPreferedZone(offensiveGoalPosition, 5);
                                robotOrientation = Math.Atan2(offensiveGoalPosition.Y - robotCurrentLocation.Y, offensiveGoalPosition.X - robotCurrentLocation.X);
                                /// Si on est suffisament proche du but, on tire
                                if(Toolbox.Distance(new PointD(robotCurrentLocation.X, robotCurrentLocation.Y), offensiveGoalPosition) < 4)
                                    {
                                    ///On tire !
                                    OnShootRequest(robotId, 6);
                                }
                            }
                            else
                            {
                                ///Il y a du monde en face, on prépare une passe
                                Dictionary<int, TeamMateRoleClassifier> teamMateClassifier = new Dictionary<int, TeamMateRoleClassifier>();
                                int j = 0;
                                //foreach (var teamMateLoc in globalWorldMap.teammateLocationList)
                                //{
                                //    var adv = new TeamMateRoleClassifier(new PointD(adversaire.X, adversaire.Y), RobotRole.Adversaire);
                                //    adv.DistanceRobotConsidere = Toolbox.Distance(new PointD(adversaire.X, adversaire.Y), new PointD(robotCurrentLocation.X, robotCurrentLocation.Y));
                                //    adversaireClassifier.Add(i++, adv);
                                //}
                                AddPreferedZone(offensiveGoalPosition, 2);


                            }
                            //AddPreferredSegmentZoneList(new PointD(adversaire1.X, adversaire1.Y), new PointD(adversaire2.X, adversaire2.Y), 0.4, 0.1);
                            //AddAvoidanceZone(new PointD(adversaire1.X, adversaire1.Y), 2, 0.5);

                            //AddPreferedZone(new PointD((teamFiltered1[0].Value.Position.X + teamFiltered1[1].Value.Position.X) / 2, (teamFiltered1[0].Value.Position.Y + teamFiltered1[0].Value.Position.Y) / 2), 2.5);
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
        

        /*********************************** Events reçus **********************************************/
        public void OnBallHandlingSensorInfoReceived(object sender, BallHandlingSensorArgs e)
        {
            if (e.RobotId == robotId)
            {
                if (e.IsHandlingBall && taskBallHandlingManagement.state != TaskBallHandlingManagementState.PossessionBalleEnCours)
                    //Force l'état balle prise dans la machine à état de gestion de la prise tir de 
                    taskBallHandlingManagement.SetTaskState(TaskBallHandlingManagementState.PossessionBalle);

            }
            else
                Console.WriteLine("Probleme d'ID robot");
        }

        public override void OnRefBoxMsgReceived(object sender, WorldMap.RefBoxMessageArgs e)
        {
            var command = e.refBoxMsg.command;
            //var robotId = e.refBoxMsg.robotID;
            var targetTeam = e.refBoxMsg.targetTeam;


            switch (command)
            {
                case RefBoxCommand.START:
                    gameState = GameState.PLAYING;
                    stoppedGameAction = StoppedGameAction.NONE;
                    break;
                case RefBoxCommand.STOP:
                    gameState = GameState.STOPPED;
                    break;
                case RefBoxCommand.DROP_BALL:
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    stoppedGameAction = StoppedGameAction.DROPBALL;
                    break;
                case RefBoxCommand.HALF_TIME:
                    break;
                case RefBoxCommand.END_GAME:
                    break;
                case RefBoxCommand.GAME_OVER:
                    break;
                case RefBoxCommand.PARK:
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    stoppedGameAction = StoppedGameAction.PARK;
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
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        stoppedGameAction = StoppedGameAction.KICKOFF;
                    else
                        stoppedGameAction = StoppedGameAction.KICKOFF_OPPONENT;
                    break;
                case RefBoxCommand.FREEKICK:
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        stoppedGameAction = StoppedGameAction.FREEKICK;
                    else
                        stoppedGameAction = StoppedGameAction.FREEKICK_OPPONENT;
                    break;
                case RefBoxCommand.GOALKICK:
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        stoppedGameAction = StoppedGameAction.GOALKICK;
                    else
                        stoppedGameAction = StoppedGameAction.GOALKICK_OPPONENT;
                    break;
                case RefBoxCommand.THROWIN:
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        stoppedGameAction = StoppedGameAction.THROWIN;
                    else
                        stoppedGameAction = StoppedGameAction.THROWIN_OPPONENT;
                    break;
                case RefBoxCommand.CORNER:
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        stoppedGameAction = StoppedGameAction.CORNER;
                    else
                        stoppedGameAction = StoppedGameAction.CORNER_OPPONENT;
                    break;
                case RefBoxCommand.PENALTY:
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        stoppedGameAction = StoppedGameAction.PENALTY;
                    else
                        stoppedGameAction = StoppedGameAction.PENALTY_OPPONENT;
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
                    gameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        stoppedGameAction = StoppedGameAction.KICKOFF;
                    else
                        stoppedGameAction = StoppedGameAction.KICKOFF_OPPONENT;
                    break;
                case RefBoxCommand.GOTO:
                    if (e.refBoxMsg.robotID == robotId)
                    {
                        gameState = GameState.STOPPED_GAME_POSITIONING;
                        externalRefBoxPosition = new Location(e.refBoxMsg.posX, e.refBoxMsg.posY, e.refBoxMsg.posTheta, 0, 0, 0);
                        if (targetTeam == teamIpAddress)
                            stoppedGameAction = StoppedGameAction.GOTO;
                        else
                            stoppedGameAction = StoppedGameAction.GOTO_OPPONENT;
                    }
                    else
                    {

                    }
                    break;
                case RefBoxCommand.PLAYLEFT:
                    //currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        playingSide = PlayingSide.Left;
                    else
                        playingSide = PlayingSide.Right;
                    break;
                case RefBoxCommand.PLAYRIGHT:
                    //currentGameState = GameState.STOPPED_GAME_POSITIONING;
                    if (targetTeam == teamIpAddress)
                        playingSide = PlayingSide.Right;
                    else
                        playingSide = PlayingSide.Left;
                    break;
            }
            
        }

        /*********************************** Events de sortie **********************************************/
        public event EventHandler<ShootEventArgs> OnShootRequestEvent;
        public virtual void OnShootRequest(int id, double speed)
        {
            var handler = OnShootRequestEvent;
            if (handler != null)
            {
                handler(this, new ShootEventArgs { RobotId = id, shootingSpeed = speed });
            }
        }
    }

    public class TeamMateRoleClassifier
    {
        public PointD Position;
        public RoboCupRobotRole Role;
        public double DistanceBalle;
        public double DistanceRobotConsidere;
        public double DistanceButOffensif;
        public double DistanceButDefensif;

        public TeamMateRoleClassifier(PointD position, RoboCupRobotRole role)
        {
            this.Role = role;
            Position = position;
        }
    }

}
