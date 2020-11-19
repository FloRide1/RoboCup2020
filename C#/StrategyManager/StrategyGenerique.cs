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

namespace StrategyManager
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
    /// - Sur Timer Strategy : Itération des machines à état de jeu définissant les déplacements et actions en fonction du temps
    ///         - implante les machines à état de jeu à Eurobot, ainsi que les règles spécifiques 
    ///         de jeu (déplacement max en controlant le ballon par exemple à la RoboCup).
    ///         - met à jour la destination théorique de déplacement (par exemple la balle pour le joueur qui la conteste à la RoboCup), 
    ///         les zones interdites (par exemple les zones de départ à Eurobot), 
    ///         les zones préférées (par exemple pour se démarquer à la RoboCup)...
    /// - Sur Timer Strategy : génération de la HeatMap de positionnement X Y donnant l'indication d'intérêt de chacun des points du terrain
    ///     et détermination de la destination théorique (avant inclusion des masquages waypoint)
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

        public GlobalWorldMap globalWorldMap;
        public Heatmap positioningHeatMap;
        public Location robotCurrentLocation = new Location(0, 0, 0, 0, 0, 0);

        Stopwatch sw = new Stopwatch();
        Timer timerStrategy;

        public StrategyGenerique(int robotId, int teamId)
        {
            this.teamId = teamId;
            this.robotId = robotId;

            globalWorldMap = new GlobalWorldMap();

            InitHeatMap();

            timerStrategy = new Timer();
            timerStrategy.Interval = 50;
            timerStrategy.Elapsed += TimerStrategy_Elapsed;
            timerStrategy.Start();
        }

        public abstract void InitHeatMap();

        //************************ Events reçus ************************************************/

        //Event de récupération d'une GlobalWorldMap mise à jour
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            //On récupère le gameState avant arrivée de la nouvelle worldMap
            GameState gameState_1 = globalWorldMap.gameState;

            //On récupère la nouvelle worldMap
            globalWorldMap = e.GlobalWorldMap;

            ////On regarde si le gamestate a changé
            //if (globalWorldMap.gameState != gameState_1)
            //{
            //    //Le gameState a changé, on envoie un event
            //    OnGameStateChanged(robotId, globalWorldMap.gameState);
            //}
        }

        private void TimerStrategy_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Le joueur détermine sa stratégie
            DetermineRobotRole();
            IterateStateMachines();
            PositioningHeatMapGeneration();
            var optimalPosition = GetOptimalDestination();

            List<LocationExtended> obstacleList = new List<LocationExtended>();
            obstacleList.Add(new LocationExtended(5, 0, 0, 0, 0, 0, ObjectType.Obstacle));
            obstacleList.Add(new LocationExtended(-5, 0, 0, 0, 0, 0, ObjectType.Obstacle));
            obstacleList.Add(new LocationExtended(-2, 2, 0, 0, 0, 0, ObjectType.Obstacle));
            obstacleList.Add(new LocationExtended(-3, 0, 0, 0, 0, 0, ObjectType.Obstacle));
            obstacleList.Add(new LocationExtended(-0, -4, 0, 0, 0, 0, ObjectType.Obstacle));
            obstacleList.Add(new LocationExtended(-0, 4, 0, 0, 0, 0, ObjectType.Obstacle));
            positioningHeatMap.ExcludeMaskedZones(new PointD(robotCurrentLocation.X, robotCurrentLocation.Y), obstacleList, 1.0);

            //Renvoi de la HeatMap Stratégie
            OnHeatMap(robotId, positioningHeatMap);
            //Mise à jour de la destination
            double robotOrientation = 0;
            OnDestination(robotId, new Location((float)optimalPosition.X, (float)optimalPosition.Y, (float)robotOrientation, 0, 0, 0));
        }

        public abstract void DetermineRobotRole(); //A définir dans les classes héritées

        public abstract void IterateStateMachines(); //A définir dans les classes héritées

        private void PositioningHeatMapGeneration()
        {
            //TestGPU.ActionWithClosure();
            sw.Reset();
            sw.Start(); // début de la mesure

            //Génération de la HeatMap
            positioningHeatMap.InitHeatMapData();
            positioningHeatMap.GenerateHeatMap(preferredZonesList, avoidanceZonesList, forbiddenRectangleList);

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


        /****************************************** Events envoyés ***********************************************/

        public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        public virtual void OnHeatMap(int id, Heatmap heatMap)
        {
            OnHeatMapEvent?.Invoke(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
        }

        public event EventHandler<LocationArgs> OnDestinationEvent;
        public virtual void OnDestination(int id, Location location)
        {
            OnDestinationEvent?.Invoke(this, new LocationArgs { RobotId = id, Location = location });
        }
    }

    
}
