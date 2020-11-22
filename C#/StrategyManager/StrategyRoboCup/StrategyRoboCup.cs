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
        public double robotOrientation = 0;

        public StrategyRoboCup(int robotId, int teamId) : base(robotId, teamId)
        {
            this.teamId = teamId;
            this.robotId = robotId;
            DisplayName = "T" + teamId + "D" + robotId;
        }

        public override void InitHeatMap()
        {
            positioningHeatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 6)); //Init HeatMap
        }

        public override void DetermineRobotRole()
        {
        }

        public override void IterateStateMachines()
        {
            InitPreferedZones();
            InitAvoidanceZones();
            InitForbiddenRectangleList();

            AddPreferedZone(new PointD(5, -3), 1);
            AddPreferedZone(new PointD(10, 5), 3.5);
            AddPreferedZone(new PointD(-2, 4), 3.5);
            AddPreferedZone(new PointD(-7, -4), 2.0);
            AddPreferedZone(new PointD(-10, 0), 5.0);
            AddPreferedZone(new PointD(0, 1.5), 3.5);
            AddPreferedZone(new PointD(3, 5), 1.5);

            AddAvoidanceZone(new PointD(0, 0), 5.5);

            //Ajout d'une zone préférée autour du robot lui-même de manière à stabiliser son comportement sur des cartes presques plates
            AddPreferedZone(new PointD(robotCurrentLocation.X, robotCurrentLocation.Y), 2, 0.3);

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
}
