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
    class StrategyRoboCup : StrategyGenerique, StrategyInterface
    {
        Stopwatch sw = new Stopwatch();

        public PointD robotDestination = new PointD(0, 0);
        public double robotOrientation = 0;

        public StrategyRoboCup(int robotId, int teamId) : base(robotId, teamId)
        {
            this.teamId = teamId;
            this.robotId = robotId;
        }

        public override void InitHeatMap()
        {
            positioningHeatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 8)); //Init HeatMap
        }

        public override void DetermineRobotRole()
        {
        }
        public override void IterateStateMachines()
        {
            InitPreferedZones();
            InitAvoidanceZones();
            InitForbiddenRectangleList();

            AddPreferedZone(new PointD(1, 0), 1);
            AddPreferedZone(new PointD(10, 5), 3.5);

            AddAvoidanceZone(new PointD(-8, -3), 1.5);
            
            AddForbiddenRectangle(new RectangleD(-5, 3, 4, 6));
        }

        public void EvaluateStrategy()
        {
            //CalculateDestination();
        }

        //public void CalculateDestination()
        //{
        //    //TestGPU.ActionWithClosure();
        //    sw.Reset();
        //    sw.Start(); // début de la mesure

        //    //Génération de la HeatMap
        //    heatMap.InitHeatMapData();

        //    double optimizedAreaSize;
        //    PointD OptimalPosition = new PointD(0, 0);
        //    PointD OptimalPosInBaseHeatMapCoordinates = heatMap.GetBaseHeatMapPosFromFieldCoordinates(0, 0);

        //    //Réglage des inputs de la heatmap
        //    //On set la destination souhaitée
        //    heatMap.SetPreferedDestination((float)robotDestination.X, (float)robotDestination.Y);
        //    //Génération de la heatmap
        //    heatMap.GenerateHeatMap(heatMap.BaseHeatMapData, heatMap.nbCellInBaseHeatMapWidth, heatMap.nbCellInBaseHeatMapHeight, (float)heatMap.FieldLength, (float)heatMap.FieldHeight);
        //    OptimalPosition = heatMap.GetOptimalPosition();

        //    //Si la position optimale est très de la cible théorique, on prend la cible théorique
        //    double seuilPositionnementFinal = 0.1;
        //    if (Toolbox.Distance(new PointD(robotDestination.X, robotDestination.Y), new PointD(OptimalPosition.X, OptimalPosition.Y)) < seuilPositionnementFinal)
        //    {
        //        OptimalPosition = robotDestination;
        //    }

        //    OnHeatMap(robotId, heatMap);
        //    OnDestination(robotId, new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, (float)robotOrientation, 0, 0, 0));

        //    sw.Stop();
        //}


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

        public event EventHandler<ByteEventArgs> OnSetAsservissementModeEvent;
    }
}
