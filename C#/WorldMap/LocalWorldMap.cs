using HeatMap;
using Newtonsoft.Json;
using PerceptionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace WorldMap
{
    public class GlobalWorldMap
    {
        public string Type = "GlobalWorldMap";
        public int TeamId;
        public int timeStampMs;
        public GameState gameState = GameState.STOPPED;
        public StoppedGameAction stoppedGameAction = StoppedGameAction.NONE;
        public Location ballLocation { get; set; }
        public Dictionary<int, Location> teammateLocationList { get; set; }
        public Dictionary<int, Location> teammateDestinationLocationList { get; set; }
        public List<Location> opponentLocationList { get; set; }
        public List<Location> obstacleLocationList { get; set; }

        public GlobalWorldMap()
        {
        }
        public GlobalWorldMap(int teamId)
        {
            TeamId = teamId;
        }

        public WorldStateMessage ConvertToWorldStateMessage()
        {
            WorldStateMessage wsm = new WorldStateMessage();
            foreach(var teamMate in teammateLocationList)
            {
                Robot r = new Robot();
                r.Id = teamMate.Key;
                r.Pose = new List<double>() { teamMate.Value.X, teamMate.Value.Y, teamMate.Value.Theta };
                r.TargetPose = new List<double>() { 0, 0, 0 };
                r.Velocity = new List<double>() { teamMate.Value.Vx, teamMate.Value.Vy, teamMate.Value.Vtheta };
                r.Intention = "";
                r.BatteryLevel = 100;
                r.BallEngaged = 0;
                wsm.Robots.Add(r);
            }

            Ball b = new Ball();
            b.Position = new List<double?>() { ballLocation.X, ballLocation.X, 0};
            b.Velocity = new List<double?>() { ballLocation.Vx, ballLocation.Vy, 0 };
            b.Confidence = 1;
            wsm.Balls.Add(b);

            foreach (var o in obstacleLocationList)
            {
                Obstacle obstacle = new Obstacle();
                obstacle.Position = new List<double>() { o.X, o.Y};
                obstacle.Velocity = new List<double>() { o.Vx, o.Vy};
                obstacle.Radius = 0.5;
                obstacle.Confidence = 1;
                wsm.Obstacles.Add(obstacle);
            }

            wsm.Intention = "Win";
            wsm.AgeMs = timeStampMs;
            wsm.TeamName = "RCT";
            wsm.Type = "worldstate";
            return wsm;
        }


    }
    public class LocalWorldMap
    {
        public string Type = "LocalWorldMap";
        public int RobotId = 0;
        public int TeamId = 0;
        public Location robotLocation { get; set; }
        public Location ballLocation { get; set; }
        public Location destinationLocation { get; set; }
        public Location waypointLocation { get; set; }
        public List<Location> obstaclesLocationList { get; set; }
        public List<PolarPointListExtended> lidarObjectList { get; set; }

        [JsonIgnore] 
        public List<PointD> lidarMap { get; set; }
        [JsonIgnore] 
        public Heatmap heatMap { get; set; }

        public LocalWorldMap()
        {
        }        
    }



    public enum GameState
    {
        STOPPED,
        STOPPED_GAME_POSITIONING,
        PLAYING,
    }

    public enum StoppedGameAction
    {
        NONE,
        KICKOFF,
        KICKOFF_OPPONENT,
        FREEKICK,
        FREEKICK_OPPONENT,
        GOALKICK,
        GOALKICK_OPPONENT,
        THROWIN,
        THROWIN_OPPONENT,
        CORNER,
        CORNER_OPPONENT,
        PENALTY,
        PENALTY_OPPONENT,
        PARK,
        DROPBALL,
    }
}
