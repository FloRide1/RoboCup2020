using EventArgsLibrary;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using WorldMap;

namespace StrategyManagerNS
{
    /****************************************************************************/
    /// <summary>
    /// Il y a un Strategy Manager par robot, qui partage la même Global World Map -> les stratégies collaboratives sont possibles
    /// Le Strategy Manager a pour rôle de déterminer les déplacements et les actions du robot auquel il appartient
    /// 
    /// Il implante implante à minima le schéma de fonctionnement suivant
    /// - Récupération asynchrone de la Global World Map décrivant l'état du monde autour du robot
    ///     La Global World Map inclus en particulier l'état du jeu (à voir pour changer cela)
    /// - Sur Timer Strategy : détermination si besoin du rôle du robot :
    ///         - simple si Eurobot car les rôles sont figés
    ///         - complexe dans le cas de la RoboCup car les rôles sont changeant en fonction des positions et du contexte.
    /// - Sur Timer Strategy : Itération des machines à état de jeu définissant les déplacements et actions
    ///         - implante les machines à état de jeu à Eurobot, ainsi que les règles spécifiques 
    ///         de jeu (déplacement max en controlant le ballon par exemple à la RoboCup).
    ///         - implante les règles de mise à jour 
    ///             des zones préférentielles de destination (par exemple la balle pour le joueur qui la conteste à la RoboCup), 
    ///             des zones interdites (par exemple les zones de départ à Eurobot), d
    ///             es zones à éviter (par exemple pour se démarquer à la RoboCup)...
    /// - DONE - Sur Timer Strategy : génération de la HeatMap de positionnement X Y donnant l'indication d'intérêt de chacun des points du terrain
    ///     et détermination de la destination théorique (avant inclusion des masquages waypoint)
    /// - DONE - Sur Timer Strategy : prise en compte de la osition des obstacles pour générer la HeatMap de WayPoint 
    ///     et trouver le WayPoint courant.
    /// - Sur Timer Strategy : gestion des actions du robot en fonction du contexte
    ///     Il est à noter que la gestion de l'orientation du robot (différente du cap en déplacement de celui-ci)
    ///     est considérée comme une action, et non comme un déplacement car celle-ci dépend avant tout du contexte du jeu
    ///     et non pas de la manière d'aller à un point.
    /// </summary>

    /****************************************************************************/
    public abstract class StrategyGenerique
    {
        public int robotId = 0;
        public int teamId = 0;
        public string teamIpAddress = "";
        public string DisplayName;

        public GlobalWorldMap globalWorldMap;
        public Heatmap positioningHeatMap;
        public Location robotCurrentLocation = new Location(0, 0, 0, 0, 0, 0);
        public double robotOrientation;


        Stopwatch sw = new Stopwatch();
        Stopwatch swGlobal = new Stopwatch();
        Timer timerStrategy;

        public StrategyGenerique(int robotId, int teamId, string teamIpAddress)
        {
            this.teamId = teamId;
            this.robotId = robotId;
            this.teamIpAddress = teamIpAddress;

            globalWorldMap = new GlobalWorldMap();

            InitHeatMap();

            timerStrategy = new Timer();
            timerStrategy.Interval = 50;
            timerStrategy.Elapsed += TimerStrategy_Elapsed;
            timerStrategy.Start();
        }

        public abstract void InitHeatMap();

        //************************ Events reçus ************************************************/
        public abstract void OnRefBoxMsgReceived(object sender, WorldMap.RefBoxMessageArgs e);

        //Event de récupération d'une GlobalWorldMap mise à jour
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            //On récupère le gameState avant arrivée de la nouvelle worldMap
            //GameState gameState_1 = globalWorldMap.gameState;

            //On récupère la nouvelle worldMap
            lock (globalWorldMap)
            {
                globalWorldMap = e.GlobalWorldMap;
            }

            //On regarde si le gamestate a changé
            //if (globalWorldMap.gameState != gameState_1)
            //{
            //    //Le gameState a changé, on envoie un event
            //    OnGameStateChanged(robotId, globalWorldMap.gameState);
            //}
        }
        public void OnPositionRobotReceived(object sender, LocationArgs location)
        {
            robotCurrentLocation.X = location.Location.X;
            robotCurrentLocation.Y = location.Location.Y;
            robotCurrentLocation.Theta = location.Location.Theta;

            robotCurrentLocation.Vx = location.Location.Vx;
            robotCurrentLocation.Vy = location.Location.Vy;
            robotCurrentLocation.Vtheta = location.Location.Vtheta;
        }


        

        bool displayConsole = false;
        private void TimerStrategy_Elapsed(object sender, ElapsedEventArgs e)
        {
            InitRobotRoleDeterminationZones();
            swGlobal.Restart();
            //Le joueur détermine sa stratégie
            sw.Restart();
            DetermineRobotRole();
            if(displayConsole)
                Console.WriteLine("Tps calcul détermination des rôles : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            sw.Restart();
            IterateStateMachines();
            if (displayConsole)
                Console.WriteLine("Tps calcul State machines : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure


            sw.Restart();
            PositioningHeatMapGeneration();
            if (displayConsole)
                Console.WriteLine("Tps calcul Heatmap Destination : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            sw.Restart();
            var optimalPosition = GetOptimalDestination();
            if (displayConsole)
                Console.WriteLine("Tps calcul Get Optimal Destination : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            List<LocationExtended> obstacleList = new List<LocationExtended>();

            double seuilDetectionObstacle = 0.4;

            sw.Restart();
            //Construction de la liste des obstacles en enlevant le robot lui-même
            lock (globalWorldMap)
            {
                if (globalWorldMap.obstacleLocationList != null)
                {
                    foreach (var obstacle in globalWorldMap.obstacleLocationList)
                    {
                        if (Toolbox.Distance(new PointD(obstacle.X, obstacle.Y), new PointD(robotCurrentLocation.X, robotCurrentLocation.Y)) > seuilDetectionObstacle)
                            obstacleList.Add(obstacle);
                    }
                }
                if (globalWorldMap.teammateLocationList != null)
                {
                    foreach (var teammate in globalWorldMap.teammateLocationList)
                    {
                        if (teammate.Key != robotId)
                            obstacleList.Add(new LocationExtended(teammate.Value.X, teammate.Value.Y, 0, 0, 0, 0, ObjectType.Robot));
                    }
                }

            }
            if (displayConsole)
                Console.WriteLine("Tps calcul Génération obstacles : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            //Renvoi de la HeatMap Stratégie
            sw.Restart();
            OnHeatMapStrategy(robotId, positioningHeatMap);
            if (displayConsole)
                Console.WriteLine("Tps envoi strat Heatmap : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            /// Calcul de la HeatMap WayPoint
            sw.Restart();
            positioningHeatMap.ExcludeMaskedZones(new PointD(robotCurrentLocation.X, robotCurrentLocation.Y), obstacleList, 0.2);
            if (displayConsole)
                Console.WriteLine("Tps calcul zones exclusion obstacles : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            sw.Restart();
            OnHeatMapWayPoint(robotId, positioningHeatMap);
            if (displayConsole)
                Console.WriteLine("Tps calcul HeatMap WayPoint : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            sw.Restart();
            var optimalWayPoint = GetOptimalDestination();
            if (displayConsole)
                Console.WriteLine("Tps calcul Get Optimal Waypoint : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure mesure

            //Mise à jour de la destination
            sw.Restart();
            if (optimalPosition==null)
                OnDestination(robotId, new Location((float)robotCurrentLocation.X, (float)robotCurrentLocation.Y, (float)robotOrientation, 0, 0, 0));
            else
                OnDestination(robotId, new Location((float)optimalPosition.X, (float)optimalPosition.Y, (float)robotOrientation, 0, 0, 0));

            if(optimalWayPoint==null)
                OnWaypoint(robotId, new Location((float)robotCurrentLocation.X, (float)robotCurrentLocation.Y, (float)robotOrientation, 0, 0, 0));
            else
                OnWaypoint(robotId, new Location((float)optimalWayPoint.X, (float)optimalWayPoint.Y, (float)robotOrientation, 0, 0, 0));

            if (displayConsole)
                Console.WriteLine("Tps events waypoint et destination : " + sw.Elapsed.TotalMilliseconds.ToString("N4") + " ms"); // Affichage de la mesure

            if (displayConsole)
                Console.WriteLine("Tps calcul Global Stratégie : " + swGlobal.Elapsed.TotalMilliseconds.ToString("N4") + " ms \n\n"); // Affichage de la mesure globale

        }

        private void InitRobotRoleDeterminationZones()
        {
            InitPreferedZones();
            InitAvoidanceZones();
            InitForbiddenRectangleList();
            InitStrictlyAllowedRectangleList();
            InitPreferredRectangleList();
            InitAvoidanceConicalZoneList();
            InitPreferredSegmentZoneList();
        }

        public abstract void DetermineRobotRole(); //A définir dans les classes héritées

        public abstract void IterateStateMachines(); //A définir dans les classes héritées

        private void PositioningHeatMapGeneration()
        {
            //TestGPU.ActionWithClosure();
            sw.Reset();
            sw.Start(); // début de la mesure

            //Génération de la HeatMap
            
            positioningHeatMap.GenerateHeatMap(preferredZonesList, avoidanceZonesList, forbiddenRectangleList, 
                strictlyAllowedRectangleList, preferredRectangleList, avoidanceConicalZoneList, preferredSegmentZoneList);

            sw.Stop();
        }

        public PointD GetOptimalDestination()
        {
            PointD optimalPosition = positioningHeatMap.GetOptimalPosition();

            //TODO à gérer à partir des coordonnées des centres des zones préférées

            //Si la position optimale est très de la cible théorique, on prend la cible théorique
            //double seuilPositionnementFinal = 0.1;
            //if (Toolbox.Distance(new PointD(robotDestination.X, robotDestination.Y), new PointD(OptimalPosition.X, OptimalPosition.Y)) < seuilPositionnementFinal)
            //{
            //    OptimalPosition = robotDestination;
            //}

            //OnDestination(robotId, new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, (float)robotOrientation, 0, 0, 0));
            return optimalPosition;
        }

        //Zones circulaires préférentielles
        List<Zone> preferredZonesList = new List<Zone>();
        public void InitPreferedZones()
        {
            lock (preferredZonesList)
            {
                preferredZonesList = new List<Zone>();
            }
        }
        public void AddPreferedZone(PointD location, double radius, double strength=1)
        {
            lock (preferredZonesList)
            {
                preferredZonesList.Add(new Zone(location, radius, strength));
            }
        }

        //Zones circulaires à éviter
        List<Zone> avoidanceZonesList = new List<Zone>();
        public void InitAvoidanceZones()
        {
            lock (avoidanceZonesList)
            {
                avoidanceZonesList = new List<Zone>();
            }
        }
        public void AddAvoidanceZone(PointD location, double radius, double strength=1)
        {
            lock (avoidanceZonesList)
            {
                avoidanceZonesList.Add(new Zone(location, radius, strength));
            }
        }

        //Zones rectangulaires interdites
        List<RectangleZone> forbiddenRectangleList = new List<RectangleZone>();
        public void InitForbiddenRectangleList()
        {
            lock (forbiddenRectangleList)
            {
                forbiddenRectangleList = new List<RectangleZone>();
            }
        }
        public void AddForbiddenRectangle(RectangleD rect)
        {
            lock (forbiddenRectangleList)
            {
                forbiddenRectangleList.Add(new RectangleZone(rect));
            }
        }

        //Zones coniques déconseillée
        List<ConicalZone> avoidanceConicalZoneList = new List<ConicalZone>();
        public void InitAvoidanceConicalZoneList()
        {
            lock (avoidanceConicalZoneList)
            {
                avoidanceConicalZoneList = new List<ConicalZone>();
            }
        }
        public void AddAvoidanceConicalZoneList(PointD initPt, PointD ciblePt, double radius)
        {
            lock (avoidanceConicalZoneList)
            {
                avoidanceConicalZoneList.Add(new ConicalZone(initPt, ciblePt, radius));
            }
        }

        //Zones Segment préférentielles
        List<SegmentZone> preferredSegmentZoneList = new List<SegmentZone>();
        public void InitPreferredSegmentZoneList()
        {
            lock (preferredSegmentZoneList)
            {
                preferredSegmentZoneList = new List<SegmentZone>();
            }
        }
        public void AddPreferredSegmentZoneList(PointD ptA, PointD ptB, double radius, double strength = 1)
        {
            lock (preferredSegmentZoneList)
            {
                preferredSegmentZoneList.Add(new SegmentZone(ptA, ptB, radius, strength));
            }
        }



        //Zones rectangulaires interdites
        List<RectangleZone> strictlyAllowedRectangleList = new List<RectangleZone>();
        public void InitStrictlyAllowedRectangleList()
        {
            lock (strictlyAllowedRectangleList)
            {
                strictlyAllowedRectangleList = new List<RectangleZone>();
            }
        }
        public void AddStrictlyAllowedRectangle(RectangleD rect)
        {
            lock (strictlyAllowedRectangleList)
            {
                strictlyAllowedRectangleList.Add(new RectangleZone(rect));
            }
        }

        //Zones rectangulaires interdites
        List<RectangleZone> preferredRectangleList = new List<RectangleZone>();
        public void InitPreferredRectangleList()
        {
            lock (preferredRectangleList)
            {
                preferredRectangleList = new List<RectangleZone>();
            }
        }
        public void AddPreferredRectangle(RectangleD rect)
        {
            lock (preferredRectangleList)
            {
                preferredRectangleList.Add(new RectangleZone(rect));
            }
        }


        /****************************************** Events envoyés ***********************************************/

        public event EventHandler<HeatMapArgs> OnHeatMapStrategyEvent;
        public virtual void OnHeatMapStrategy(int id, Heatmap heatMap)
        {
            OnHeatMapStrategyEvent?.Invoke(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
        }

        public event EventHandler<HeatMapArgs> OnHeatMapWayPointEvent;
        public virtual void OnHeatMapWayPoint(int id, Heatmap heatMap)
        {
            var handler = OnHeatMapWayPointEvent;
            if (handler != null)
            {
                handler(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
            }
        }

        public event EventHandler<LocationArgs> OnDestinationEvent;
        public virtual void OnDestination(int id, Location location)
        {
            OnDestinationEvent?.Invoke(this, new LocationArgs { RobotId = id, Location = location });
        }

        public event EventHandler<RoleArgs> OnRoleEvent;
        public virtual void OnRole(int id, RobotRole role)
        {
            OnRoleEvent?.Invoke(this, new RoleArgs { RobotId = id, Role = role });
        }

        public event EventHandler<BallHandlingStateArgs> OnBallHandlingStateEvent;
        public virtual void OnBallHandlingState(int id, BallHandlingState state)
        {
            OnBallHandlingStateEvent?.Invoke(this, new BallHandlingStateArgs { RobotId = id, State = state });
        }

        public event EventHandler<MessageDisplayArgs> OnMessageDisplayEvent;
        public virtual void OnMessageDisplay(int id, string msg)
        {
            OnMessageDisplayEvent?.Invoke(this, new MessageDisplayArgs { RobotId = id, Message = msg});
        }

        //public event EventHandler<PlayingSideArgs> OnPlayingSideEvent;
        //public virtual void OnPlayingSide(int id, PlayingSide playSide)
        //{
        //    OnPlayingSideEvent?.Invoke(this, new  PlayingSideArgs { RobotId = id, PlaySide = playSide});
        //}



        public event EventHandler<LocationArgs> OnWaypointEvent;
        public virtual void OnWaypoint(int id, Location wayPointlocation)
        {
            var handler = OnWaypointEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = wayPointlocation });
            }
        }

        public event EventHandler<GameStateArgs> OnGameStateChangedEvent;
        public virtual void OnGameStateChanged(int robotId, GameState state)
        {
            var handler = OnGameStateChangedEvent;
            if (handler != null)
            {
                handler(this, new GameStateArgs { RobotId = robotId, gameState = state });
            }
        }


    }

    
}
