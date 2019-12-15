using System;
using System.Collections.Generic;
using System.Diagnostics;
using Communications;
using Robotics;

namespace Controls.Interactive.RefereeBox
{
    public class RefereeBox
    {
        public RefereeBox()
        {
            _refereeServer.NewClient += RefereeServer_OnNewClient;
            _refereeServer.Start();
        }

        private void RefereeServer_OnNewClient(object sender, NewClientEventArgs e)
        {
            _baseStationStreams.Add(new MessageStream(e.NewClient));
        }

        #region Properties

        public enum GameParts
        {
            PreFirstHalf,
            FirstHalf,
            PreSecondHalf,
            SecondHalf,
            PreFirstHalfOvertime,
            FirstHalfOvertime,
            PreSecondHalfOvertime,
            SecondHalfOvertime,
            Penalties,
            GameOver
        }

        public GameParts GamePart { get; set; } = GameParts.PreFirstHalf;

        public int CyanScore { get; set; } = 0;
        public int MagentaScore { get; set; } = 0;

        public Stopwatch GameStopwatch { get; } = new Stopwatch();
        private DateTime _initialPartTime = DateTime.Now;

        public TimeSpan PartTime => DateTime.Now - _initialPartTime;

        public Commands LastSentCommand { get; private set; }

        public bool IsStopped { get; private set; } = true;

        public bool IsInitialPartKickOffDone { get; private set; } = false;

        public TeamColors LastTeamToKickOff { get; private set; } = TeamColors.None;

        public bool IsChangingScore { get; private set; } = false;

        private TeamColors _teamScoring = TeamColors.None;
        private TeamColors _nextTeamToKickOff = TeamColors.None;

        private readonly TCPServer _refereeServer = new TCPServer(28097);
        private readonly List<MessageStream> _baseStationStreams = new List<MessageStream>();

        #endregion
        #region RefereeOrders

        public bool Start()
        {
            if (IsStopped)
            {
                // Change game part when starting the initial kickoff
                if (!IsInitialPartKickOffDone)
                {
                    // Update game part and kickoff info
                    IsInitialPartKickOffDone = true;
                    LastTeamToKickOff = _nextTeamToKickOff;
                    _initialPartTime = DateTime.Now;

                    // Send new game part command
                    switch (GamePart)
                    {
                        case GameParts.PreFirstHalf:
                            SendCommand(Commands.FIRST_HALF);
                            GamePart = GameParts.FirstHalf;
                            break;
                        case GameParts.PreSecondHalf:
                            SendCommand(Commands.SECOND_HALF);
                            GamePart = GameParts.SecondHalf;
                            break;
                        case GameParts.PreFirstHalfOvertime:
                            SendCommand(Commands.FIRST_HALF_OVERTIME);
                            GamePart = GameParts.FirstHalfOvertime;
                            break;
                        case GameParts.PreSecondHalfOvertime:
                            SendCommand(Commands.SECOND_HALF_OVERTIME);
                            GamePart = GameParts.SecondHalfOvertime;
                            break;
                    }
                }

                IsChangingScore = false;
                GameStopwatch.Start();
                SendCommand(Commands.START);
            }

            return IsStopped;
        }

        public bool Stop()
        {
            IsStopped = true;
            GameStopwatch.Stop();
            SendCommand(Commands.STOP);

            return true;
        }

        public bool EndPart()
        {
            if (IsStopped)
            {
                // Update part to go to the next one
                _initialPartTime = DateTime.Now;
                IsChangingScore = false;
                IsInitialPartKickOffDone = false;
                GameStopwatch.Reset();

                string command;
                // Send new game part commands
                switch (GamePart)
                {
                    case GameParts.FirstHalf:
                        command = "HALF_TIME";
                        GamePart = GameParts.PreSecondHalf;
                        break;
                    case GameParts.SecondHalf:
                        command = "END_GAME";
                        GamePart = GameParts.PreFirstHalfOvertime;
                        break;
                    case GameParts.FirstHalfOvertime:
                        command = "HALF_TIME";
                        GamePart = GameParts.PreSecondHalfOvertime;
                        break;
                    case GameParts.SecondHalfOvertime:
                        command = "END_GAME";
                        GamePart = GameParts.Penalties;
                        break;
                    case GameParts.Penalties:
                        command = "HALF_TIME";
                        GamePart = GameParts.GameOver;
                        break;
                    default:
                        return false;
                }

                SendCommand(command);
            }

            return IsStopped;
        }

        public bool Park()
        {
            if (IsStopped)
                SendCommand(Commands.PARKING);

            return IsStopped;
        }

        public bool Reset()
        {
            if (IsStopped)
            {
                GamePart = GameParts.PreFirstHalf;
                _initialPartTime = DateTime.Now;

                IsInitialPartKickOffDone = false;
                LastTeamToKickOff = TeamColors.None;
                GameStopwatch.Reset();

                CyanScore = 0;
                MagentaScore = 0;

                SendCommand(Commands.RESET);
            }

            return IsStopped;
        }

        public bool DroppedBall()
        {
            if (IsStopped)
                SendCommand(Commands.DROPPED_BALL);

            return IsStopped;
        }

        public bool Goal(TeamColors teamColor)
        {
            if (IsStopped)
            {
                IsChangingScore = true;

                switch (teamColor)
                {
                    case TeamColors.Cyan:
                        CyanScore++;
                        break;
                    case TeamColors.Magenta:
                        MagentaScore++;
                        break;
                }

                _teamScoring = teamColor;
                SendCommand("GOAL_" + teamColor.ToString().ToUpper());
            }

            return IsStopped;
        }

        public bool UnGoal()
        {
            if (IsStopped && IsChangingScore)
            {
                switch (_teamScoring)
                {
                    case TeamColors.Cyan:
                        CyanScore--;
                        SendCommand(Commands.SUBGOAL_CYAN);
                        break;
                    case TeamColors.Magenta:
                        MagentaScore--;
                        SendCommand(Commands.SUBGOAL_MAGENTA);
                        break;
                }

                return true;
            }

            return false;
        }

        public bool KickOff(TeamColors teamColor)
        {
            if (IsStopped)
            {
                SendCommand("KICKOFF_" + teamColor.ToString().ToUpper());
                _nextTeamToKickOff = teamColor;
            }

            return IsStopped;
        }

        public bool FreeKick(TeamColors teamColor)
        {
            if (IsStopped)
                SendCommand("FREEKICK_" + teamColor.ToString().ToUpper());

            return IsStopped;
        }

        public bool GoalKick(TeamColors teamColor)
        {
            if (IsStopped)
                SendCommand("GOALKICK_" + teamColor.ToString().ToUpper());

            return IsStopped;
        }

        public bool ThrowIn(TeamColors teamColor)
        {
            if (IsStopped)
                SendCommand("THROWIN_" + teamColor.ToString().ToUpper());

            return IsStopped;
        }

        public bool Corner(TeamColors teamColor)
        {
            if (IsStopped)
                SendCommand("CORNER_" + teamColor.ToString().ToUpper());

            return IsStopped;
        }

        public bool Penalty(TeamColors teamColor)
        {
            if (IsStopped)
                SendCommand("PENALTY_" + teamColor.ToString().ToUpper());

            return IsStopped;
        }

        #endregion

        public void Close()
        {
            _refereeServer.Close();
        }

        public void SendCommand(string command)
            => SendCommand((Commands)Enum.Parse(typeof(Commands), command));

        public void SendCommand(Commands command)
        {
            LastSentCommand = command;

            // Send it on each base station stream
            foreach (MessageStream stream in _baseStationStreams)
                stream.SendMessage(new Message(command));
        }
    }
}
