using HeatMap;
using Newtonsoft.Json;
using System.Collections.Generic;
using Utilities;

namespace WorldMap
{

    public class LocalWorldMap
    {
        public string Type = "LocalWorldMap";
        public int RobotId = 0;
        public int TeamId = 0;
        public Location robotLocation { get; set; }
        public Location robotGhostLocation { get; set; }
        public Location destinationLocation { get; set; }
        public Location waypointLocation { get; set; }
        public List<Location> ballLocationList { get; set; }
        public List<LocationExtended> obstaclesLocationList { get; set; }
        public List<PolarPointListExtended> lidarObjectList { get; set; }

        [JsonIgnore]
        public List<PointD> lidarMap { get; set; }
        [JsonIgnore]
        public Heatmap heatMapStrategy { get; set; }
        public Heatmap heatMapWaypoint { get; set; }

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
        GOTO_0_1,
        GOTO_0_1_OPPONENT,
        GOTO_1_0,
        GOTO_1_0_OPPONENT,
        GOTO_0_M1,
        GOTO_0_M1_OPPONENT,
        GOTO_M1_0,
        GOTO_M1_0_OPPONENT,
        GOTO_0_0,
        GOTO_0_0_OPPONENT,
    }
}
