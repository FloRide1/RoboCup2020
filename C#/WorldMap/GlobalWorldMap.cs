using System.Collections.Generic;
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

        public PlayingSide playingSide = PlayingSide.Left;

        public List<Location> ballLocationList { get; set; }
        public Dictionary<int, Location> teammateLocationList { get; set; }
        public Dictionary<int, Location> teammateGhostLocationList { get; set; }
        public Dictionary<int, Location> teammateDestinationLocationList { get; set; }
        public Dictionary<int, Location> teammateWayPointList { get; set; }
        public List<Location> opponentLocationList { get; set; }
        public List<LocationExtended> obstacleLocationList { get; set; }
        public Dictionary<int, RobotRole> teammateRoleList { get; set; }
        public Dictionary<int, PlayingSide> teammatePlayingSideList { get; set; }

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
            foreach (var teamMate in teammateLocationList)
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

            //On prend par défaut la première balle du premier robot
            Ball b = new Ball();
            b.Position = new List<double?>() { ballLocationList[0].X, ballLocationList[0].X, 0 };
            b.Velocity = new List<double?>() { ballLocationList[0].Vx, ballLocationList[0].Vy, 0 };
            b.Confidence = 1;
            wsm.Balls.Add(b);

            foreach (var o in obstacleLocationList)
            {
                Obstacle obstacle = new Obstacle();
                obstacle.Position = new List<double>() { o.X, o.Y };
                obstacle.Velocity = new List<double>() { o.Vx, o.Vy };
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

    public class GlobalWorldMapStorage
    {
        public Dictionary<int, Location> robotLocationDictionary { get; set; }
        public Dictionary<int, Location> destinationLocationDictionary { get; set; }
        public Dictionary<int, Location> waypointLocationDictionary { get; set; }
        public Dictionary<int, List<Location>> ballLocationListDictionary { get; set; }
        public Dictionary<int, List<LocationExtended>> ObstaclesLocationListDictionary { get; set; }
        public Dictionary<int, RobotRole> robotRoleDictionary { get; set; }
        public Dictionary<int, PlayingSide> robotPlayingSideDictionary { get; set; }

        public GlobalWorldMapStorage()
        {
            robotLocationDictionary = new Dictionary<int, Location>();
            destinationLocationDictionary = new Dictionary<int, Location>();
            waypointLocationDictionary = new Dictionary<int, Location>();
            ballLocationListDictionary = new Dictionary<int, List<Location>>();
            ObstaclesLocationListDictionary = new Dictionary<int, List<LocationExtended>>();
            robotRoleDictionary = new Dictionary<int, RobotRole>();
            robotPlayingSideDictionary = new Dictionary<int, PlayingSide>();
        }

        public void AddOrUpdateRobotLocation(int id, Location loc)
        {
            lock (robotLocationDictionary)
            {
                if (robotLocationDictionary.ContainsKey(id))
                    robotLocationDictionary[id] = loc;
                else
                    robotLocationDictionary.Add(id, loc);
            }
        }

        public void AddOrUpdateRobotDestination(int id, Location loc)
        {
            lock (destinationLocationDictionary)
            {
                if (destinationLocationDictionary.ContainsKey(id))
                    destinationLocationDictionary[id] = loc;
                else
                    destinationLocationDictionary.Add(id, loc);
            }
        }

        public void AddOrUpdateRobotWayPoint(int id, Location loc)
        {
            lock (waypointLocationDictionary)
            {
                if (waypointLocationDictionary.ContainsKey(id))
                    waypointLocationDictionary[id] = loc;
                else
                    waypointLocationDictionary.Add(id, loc);
            }
        }
        public void AddOrUpdateRobotRole(int id, RobotRole role)
        {
            lock (robotRoleDictionary)
            {
                if (robotRoleDictionary.ContainsKey(id))
                    robotRoleDictionary[id] = role;
                else
                    robotRoleDictionary.Add(id, role);
            }
        }

        public void AddOrUpdateRobotPlayingSide(int id, PlayingSide playSide)
        {
            lock (robotRoleDictionary)
            {
                if (robotPlayingSideDictionary.ContainsKey(id))
                    robotPlayingSideDictionary[id] = playSide;
                else
                    robotPlayingSideDictionary.Add(id, playSide);
            }
        }

        public void AddOrUpdateBallLocationList(int id, List<Location> ballLocationList)
        {
            lock (ballLocationListDictionary)
            {
                if (ballLocationListDictionary.ContainsKey(id))
                    ballLocationListDictionary[id] = ballLocationList;
                else
                    ballLocationListDictionary.Add(id, ballLocationList);
            }
        }

        public void AddOrUpdateObstaclesList(int id, List<LocationExtended> locList)
        {
            lock (ObstaclesLocationListDictionary)
            {
                if (ObstaclesLocationListDictionary.ContainsKey(id))
                    ObstaclesLocationListDictionary[id] = locList;
                else
                    ObstaclesLocationListDictionary.Add(id, locList);
            }
        }
    }
}
