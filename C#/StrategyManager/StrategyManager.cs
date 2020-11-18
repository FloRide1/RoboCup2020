using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Utilities;
using WorldMap;
using HeatMap;
using System.Diagnostics;
using PerceptionManagement;
using System.Timers;
using Constants;
using HerkulexManagerNS;
using StrategyManager.StrategyRoboCupNS;
using StrategyManager.StrategyEurobotNS;

namespace StrategyManager
{

    public class StrategyManager
    {
        GameMode strategyMode = GameMode.RoboCup;

        int robotId = 0;
        int teamId = 0;
        
        
        PlayerRole robotRole = PlayerRole.Stop;
        PointD robotDestination = new PointD(0, 0);
        double robotOrientation = 0;
        

        public StrategyGenerique strategy;

        public StrategyManager(int robotId, int teamId, GameMode stratMode)
        {
            strategyMode = stratMode;
            this.teamId = teamId;
            this.robotId = robotId;

            switch(strategyMode)
            {
                case GameMode.RoboCup:
                    //heatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 8), 1);
                    strategy = new StrategyRoboCup(robotId, teamId);
                    break;
                case GameMode.Eurobot:
                    //heatMap = new Heatmap(3, 2, (int)Math.Pow(2, 5), 1);
                    strategy = new StrategyEurobot(robotId, teamId);
                    break;
                case GameMode.Demo:
                    //heatMap = new Heatmap(3, 2, (int)Math.Pow(2, 5), 1);
                    break;
            }
        }


        

        //************************ Event envoyés par le gestionnaire de strategie ***********************/

        public event EventHandler<ByteEventArgs> OnSetAsservissementModeEvent;
        public virtual void OnSetAsservissementMode(byte val)
        {
            OnSetAsservissementModeEvent?.Invoke(this, new ByteEventArgs { Value = val });
        }

        public event EventHandler<BoolEventArgs> OnEnableMotorsEvent;
        public virtual void OnEnableMotors(bool val)
        {
            OnEnableMotorsEvent?.Invoke(this, new BoolEventArgs { value = val });
        }

        public event EventHandler<BoolEventArgs> OnMirrorModeForwardEvent;
        public virtual void OnMirrorMode(object sender, BoolEventArgs val)
        {
            OnMirrorModeForwardEvent?.Invoke(sender, val);
        }

        public event EventHandler<LocationArgs> OnDestinationEvent;
        public virtual void OnDestination(int id, Location location)
        {
            OnDestinationEvent?.Invoke(this, new LocationArgs { RobotId = id, Location = location });
        }

        //public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        //public virtual void OnHeatMap(int id, Heatmap heatMap)
        //{
        //    OnHeatMapEvent?.Invoke(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
        //}

        public event EventHandler<GameStateArgs> OnGameStateChangedEvent;
        public virtual void OnGameStateChanged(int robotId, GameState state)
        {
            OnGameStateChangedEvent?.Invoke(this, new GameStateArgs { RobotId = robotId, gameState = state });
        }

        
        

        void SetRobotDestination(PlayerRole role)
        {
            //switch (globalWorldMap.gameState)
            //{
            //    case GameState.STOPPED:
            //        if(globalWorldMap.teammateLocationList.ContainsKey(robotId))
            //            robotDestination = new PointD(globalWorldMap.teammateLocationList[robotId].X, globalWorldMap.teammateLocationList[robotId].Y);
            //        break;
            //    case GameState.PLAYING:
            //        //C'est ici qu'il faut calculer les fonctions de cout pour chacun des roles.
            //        switch (role)
            //        {
            //            case PlayerRole.Stop:
            //                robotDestination = new PointD(-8, 3);
            //                break;
            //            case PlayerRole.Gardien:
            //                if(teamId == (int)TeamId.Team1)
            //                    robotDestination = new PointD(10.5, 0);
            //                else
            //                    robotDestination = new PointD(-10.5, 0);
            //                break;
            //            case PlayerRole.DefenseurPlace:
            //                robotDestination = new PointD(-8, 3);
            //                break;
            //            case PlayerRole.DefenseurActif:
            //                robotDestination = new PointD(-8, -3);
            //                break;
            //            case PlayerRole.AttaquantPlace:
            //                robotDestination = new PointD(6, -3);
            //                break;
            //            case PlayerRole.AttaquantAvecBalle:
            //                //if (globalWorldMap.ballLocation != null)
            //                //    robotDestination = new PointD(globalWorldMap.ballLocation.X, globalWorldMap.ballLocation.Y);
            //                //else
            //                //    robotDestination = new PointD(6, 0);
            //                {
            //                    if (globalWorldMap.ballLocationList.Count > 0)
            //                    {
            //                        var ptInterception = GetInterceptionLocation(new Location(globalWorldMap.ballLocationList[0].X, globalWorldMap.ballLocationList[0].Y, 0, globalWorldMap.ballLocationList[0].Vx, globalWorldMap.ballLocationList[0].Vy, 0), new Location(globalWorldMap.teammateLocationList[robotId].X, globalWorldMap.teammateLocationList[robotId].Y, 0, 0, 0, 0), 3);

            //                        if (ptInterception != null)
            //                            robotDestination = ptInterception;
            //                        else
            //                            robotDestination = new PointD(globalWorldMap.ballLocationList[0].X, globalWorldMap.ballLocationList[0].Y);
            //                    }
            //                    else
            //                        robotDestination = new PointD(6, -3);
            //                }
            //                break;
            //            case PlayerRole.Centre:
            //                robotDestination = new PointD(0, 0);
            //                break;
            //            default:
            //                break;
            //        }
            //        break;
            //    case GameState.STOPPED_GAME_POSITIONING:
            //        switch(globalWorldMap.stoppedGameAction)
            //        {
            //            case StoppedGameAction.KICKOFF:        
            //                switch(robotId)
            //                {
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(10, 0);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot2:
            //                        robotDestination = new PointD(-1, 2);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot3:
            //                        robotDestination = new PointD(1, -2);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot4:
            //                        robotDestination = new PointD(6, -3);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot5:
            //                        robotDestination = new PointD(6, 3);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(-10, 0);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot2:
            //                        robotDestination = new PointD(1, 2);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot3:
            //                        robotDestination = new PointD(-1, -2);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot4:
            //                        robotDestination = new PointD(-6, -3);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot5:
            //                        robotDestination = new PointD(-6, 3);
            //                        break;
            //                }
            //                break;

            //            case StoppedGameAction.GOTO_0_1:
            //                switch (robotId)
            //                {
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(0, 1);
            //                        robotOrientation = Math.PI / 2;
            //                        break;
            //                }
            //                break;

            //            case StoppedGameAction.GOTO_1_0:
            //                switch (robotId)
            //                {
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(1, 0);
            //                        robotOrientation = 0;
            //                        break;
            //                }
            //                break;

            //            case StoppedGameAction.GOTO_0_M1:
            //                switch (robotId)
            //                {
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(0, -1); 
            //                        robotOrientation = Math.PI;
            //                        break;
            //                }
            //                break;

            //            case StoppedGameAction.GOTO_M1_0:
            //                switch (robotId)
            //                {
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(-1, 0);
            //                        robotOrientation = 3 * Math.PI / 2;
            //                        break;
            //                }
            //                break;

            //            case StoppedGameAction.GOTO_0_0:
            //                switch (robotId)
            //                {
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(0, 0);
            //                        robotOrientation = 0;
            //                        break;
            //                }
            //                break;

            //            case StoppedGameAction.KICKOFF_OPPONENT:
            //                switch (robotId)
            //                {
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(10, 0);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot2:
            //                        robotDestination = new PointD(1, 2);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot3:
            //                        robotDestination = new PointD(1, -2);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot4:
            //                        robotDestination = new PointD(6, -3);
            //                        break;
            //                    case (int)TeamId.Team1 + (int)Constants.RobotId.Robot5:
            //                        robotDestination = new PointD(6, 3);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot1:
            //                        robotDestination = new PointD(-10, 0);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot2:
            //                        robotDestination = new PointD(-1, 2);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot3:
            //                        robotDestination = new PointD(-1, -2);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot4:
            //                        robotDestination = new PointD(-6, -3);
            //                        break;
            //                    case (int)TeamId.Team2 + (int)Constants.RobotId.Robot5:
            //                        robotDestination = new PointD(-6, 3);
            //                        break;
            //                }
            //                break;
            //        }
            //        break;                
            //}            
        }



        double EvaluateStrategyCostFunction(PointD destination, PointD fieldPos)
        {
            return Math.Max(0, 1 - Toolbox.Distance(destination, fieldPos) / 20.0);
        }

        

        public PointD GetInterceptionLocation(Location target, Location hunter, double huntingSpeed)
        {
            //D'après Al-Kashi, si d est la distance entre le pt target et le pt chasseur, que les vitesses sont constantes 
            //et égales à Vtarget et Vhunter
            //Rappel Al Kashi : A² = B²+C²-2BCcos(alpha) , alpha angle opposé au segment A
            //On a au moment de l'interception à l'instant Tinter: 
            //A = Vh * Tinter
            //B = VT * Tinter
            //C = initialDistance;
            //alpha = Pi - capCible - angleCible

            double targetSpeed = Math.Sqrt(Math.Pow(target.Vx, 2) + Math.Pow(target.Vy, 2));
            double initialDistance = Toolbox.Distance(new PointD(hunter.X, hunter.Y), new PointD(target.X, target.Y));
            double capCible = Math.Atan2(target.Vy, target.Vx);
            double angleCible = Math.Atan2(target.Y- hunter.Y, target.X- hunter.X);
            double angleCapCibleDirectionCibleChasseur = Math.PI - capCible + angleCible;

            //Résolution de ax²+bx+c=0 pour trouver Tinter
            double a = Math.Pow(huntingSpeed, 2) - Math.Pow(targetSpeed, 2);
            double b = 2 * initialDistance * targetSpeed * Math.Cos(angleCapCibleDirectionCibleChasseur);
            double c = -Math.Pow(initialDistance, 2);

            double delta = b * b - 4 * a * c;
            double t1 = (-b - Math.Sqrt(delta)) / (2 * a);
            double t2 = (-b + Math.Sqrt(delta)) / (2 * a);

            if (delta > 0 && t2<10)
            {
                double xInterception = target.X + targetSpeed * Math.Cos(capCible) * t2;
                double yInterception = target.Y + targetSpeed * Math.Sin(capCible) * t2;
                return new PointD(xInterception, yInterception);
            }
            else
                return null;
        }
        public void SetRole(PlayerRole role)
        {
            robotRole = role;
        }
                     
        public void SetDestination(Location location)
        {
            OnDestination(robotId, location);
        }
    }



    public enum PlayerRole
    {
        Stop,
        Gardien,
        DefenseurPlace,
        DefenseurActif,
        AttaquantAvecBalle,
        AttaquantPlace,
        Centre,
    }


    
}

