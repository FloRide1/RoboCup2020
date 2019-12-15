using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Controls.Annotations;
using Communications;
using Robotics;

namespace Controls.Interactive.RefereeBox
{
    /// <summary>
    /// Interaction logic for RefereeBoxControl.xaml
    /// </summary>
    public sealed partial class RefereeBoxControl : UserControl, INotifyPropertyChanged
    {
        /* TODO - Search alternative for Close()
         * TODO - Manage Unchecked
         * TODO - Add WorldState command
         * TODO - Manage penalties and game over parts
         */

        public RefereeBoxControl()
        {
            InitializeComponent();

            // Enable the good buttons at start
            SetIsEnabledToAllButtons(false);
            SetIsEnabledToButtons(true, CyanKickOffButton, MagentaKickOffButton, StopButton, ResetButton);

            // Enable the clock to display actual time
            _clockTimer.Interval = TimeSpan.FromSeconds(0.25);
            _clockTimer.Tick += ClockTimer_OnTick;
            _clockTimer.Start();
        }

        private void ClockTimer_OnTick(object sender, EventArgs e)
        {
            ClockTextBlock.Text = RefereeBox.PartTime.ToString(@"mm\:ss");
            GamePartRealTimeRun.Text = RefereeBox.GameStopwatch.Elapsed.ToString(@"mm\:ss");

            CyanScoreTextBlock.Text = RefereeBox.CyanScore.ToString();
            MagentaScoreTextBlock.Text = RefereeBox.MagentaScore.ToString();

            GamePartRun.Text = RefereeBox.GamePart.ToString();
        }

        public RefereeBox RefereeBox { get; } = new RefereeBox();

        #region Utilities

        private readonly DispatcherTimer _clockTimer = new DispatcherTimer();

        private void SetIsCheckedToAllButtons(bool value)
        {
            CyanGoalButton.IsChecked = value;
            CyanKickOffButton.IsChecked = value;
            CyanFreeKickButton.IsChecked = value;
            CyanGoalKickButton.IsChecked = value;
            CyanThrowInButton.IsChecked = value;
            CyanCornerButton.IsChecked = value;
            CyanPenaltyButton.IsChecked = value;

            MagentaGoalButton.IsChecked = value;
            MagentaKickOffButton.IsChecked = value;
            MagentaFreeKickButton.IsChecked = value;
            MagentaGoalKickButton.IsChecked = value;
            MagentaThrowInButton.IsChecked = value;
            MagentaCornerButton.IsChecked = value;
            MagentaPenaltyButton.IsChecked = value;
        }

        private void SetIsEnabledToAllButtons(bool value)
        {
            CyanGoalButton.IsEnabled = value;
            CyanKickOffButton.IsEnabled = value;
            CyanFreeKickButton.IsEnabled = value;
            CyanGoalKickButton.IsEnabled = value;
            CyanThrowInButton.IsEnabled = value;
            CyanCornerButton.IsEnabled = value;
            CyanPenaltyButton.IsEnabled = value;

            MagentaGoalButton.IsEnabled = value;
            MagentaKickOffButton.IsEnabled = value;
            MagentaFreeKickButton.IsEnabled = value;
            MagentaGoalKickButton.IsEnabled = value;
            MagentaThrowInButton.IsEnabled = value;
            MagentaCornerButton.IsEnabled = value;
            MagentaPenaltyButton.IsEnabled = value;

            StartButton.IsEnabled = value;
            StopButton.IsEnabled = value;
            DropBallButton.IsEnabled = value;
            ParkButton.IsEnabled = value;
            EndPartButton.IsEnabled = value;
            ResetButton.IsEnabled = value;
        }

        private static void SetIsEnabledToButtons(bool value, params object[] buttons)
        {
            foreach (FrameworkElement button in buttons)
                button.IsEnabled = value;
        }

        #endregion
        #region Logic & Communications

        private void SendCommandButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Check if it's a framework element, get its side and command
            if (!(sender is ButtonBase element))
                return;

            string content = (string) element.Content;
            string side = element.DataContext as string;
            TeamColors color = TeamColors.None;
            if (side != null && side != "SHARED")
                color = (TeamColors) Enum.Parse(typeof(TeamColors), side);

            switch (content)
            {
                case "START":
                    // Update UI and freeze score
                    RefereeBox.Start();
                    SetIsCheckedToAllButtons(false);
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StopButton);
                    break;

                case "STOP":
                    // Does nothing if goal button is checked and no kickoff button is checked
                    if ((CyanGoalButton.IsChecked == true || MagentaGoalButton.IsChecked == true) &&
                        CyanKickOffButton.IsChecked == false && MagentaGoalButton.IsChecked == false &&
                        MagentaKickOffButton.IsChecked == false && CyanGoalButton.IsChecked == false)
                        break;

                    if (CyanGoalButton.IsChecked == true)
                    {
                        // If goal button checked, disable one kickoff
                        MagentaKickOffButton.IsChecked = false;
                        SetIsEnabledToButtons(false, StartButton);
                    }
                    else if (MagentaGoalButton.IsChecked == true)
                    {
                        // If goal button checked, disable one kickoff
                        CyanKickOffButton.IsChecked = false;
                        SetIsEnabledToButtons(false, StartButton);
                    }
                    else if (RefereeBox.IsInitialPartKickOffDone)
                    {
                        // Else if initial takeoff done, enable everything except a few things
                        SetIsCheckedToAllButtons(false);
                        SetIsEnabledToAllButtons(true);
                        SetIsEnabledToButtons(false, StartButton, ParkButton, ResetButton, CyanKickOffButton, MagentaKickOffButton);
                    }
                    else
                    {
                        // Else get to a state without a kickoff button checked
                        SetIsCheckedToAllButtons(false);
                        SetIsEnabledToAllButtons(false);
                        switch (RefereeBox.GamePart)
                        {
                            case RefereeBox.GameParts.PreFirstHalf:
                                SetIsEnabledToButtons(true, StopButton, ResetButton, CyanKickOffButton, MagentaKickOffButton);
                                break;
                            case RefereeBox.GameParts.PreFirstHalfOvertime:
                                SetIsEnabledToButtons(true, StopButton, ResetButton, ParkButton, RefereeBox.LastTeamToKickOff == TeamColors.Cyan ? MagentaKickOffButton : CyanKickOffButton);
                                break;
                            case RefereeBox.GameParts.PreSecondHalf:
                            case RefereeBox.GameParts.PreSecondHalfOvertime:
                                SetIsEnabledToButtons(true, StopButton, ParkButton, RefereeBox.LastTeamToKickOff == TeamColors.Cyan ? MagentaKickOffButton : CyanKickOffButton);
                                break;
                        }
                    }

                    RefereeBox.Stop();
                    break;

                case "Goal":
                    // Update score and update UI to kickoff/end part
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, color == TeamColors.Cyan ? MagentaKickOffButton : CyanKickOffButton, StopButton, EndPartButton, element);
                    RefereeBox.Goal(color);
                    break;

                case "KickOff":
                    // Disable the other takeoff button and enable start/stop
                    SetIsEnabledToButtons(false, color == TeamColors.Cyan ? MagentaKickOffButton : CyanKickOffButton, ResetButton);
                    SetIsEnabledToButtons(true, StartButton, StopButton);
                    RefereeBox.KickOff(color);
                    break;

                case "FreeKick":
                    // Enable start/stop and itself as you can't do another thing
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StartButton, StopButton, element);
                    RefereeBox.FreeKick(color);
                    break;

                case "GoalKick":
                    // Enable start/stop and itself as you can't do another thing
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StartButton, StopButton, element);
                    RefereeBox.GoalKick(color);
                    break;

                case "ThrowIn":
                    // Enable start/stop and itself as you can't do another thing
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StartButton, StopButton, element);
                    RefereeBox.ThrowIn(color);
                    break;

                case "Corner":
                    // Enable start/stop and itself as you can't do another thing
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StartButton, StopButton, element);
                    RefereeBox.Corner(color);
                    break;

                case "Penalty":
                    // Enable start/stop and itself as you can't do another thing
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StartButton, StopButton, element);
                    RefereeBox.Penalty(color);
                    break;

                case "Dropped_Ball":
                    // Enable start/stop and itself as you can't do another thing
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StartButton, StopButton, element);
                    RefereeBox.DroppedBall();
                    break;

                case "Parking":
                    RefereeBox.Park();
                    break;

                case "End Part":
                    // Disable buttons and enable the good ones to restart
                    SetIsCheckedToAllButtons(false);
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StopButton, ParkButton, RefereeBox.LastTeamToKickOff == TeamColors.Cyan ? MagentaKickOffButton : CyanKickOffButton);

                    // Send new game part commands
                    switch (RefereeBox.GamePart)
                    {
                        case RefereeBox.GameParts.SecondHalf:
                        case RefereeBox.GameParts.Penalties:
                            // We can either stop at second half or stop the game at penalties
                            SetIsEnabledToButtons(true, ResetButton);
                            break;
                        case RefereeBox.GameParts.SecondHalfOvertime:
                            // We enter the Penalties game part
                            SetIsEnabledToButtons(true, StartButton, EndPartButton, ResetButton, CyanGoalButton, MagentaGoalButton, CyanPenaltyButton, MagentaPenaltyButton);
                            break;
                    }

                    RefereeBox.EndPart();
                    break;

                case "RESET":
                    // Reset everything and return to initial state
                    SetIsCheckedToAllButtons(false);
                    SetIsEnabledToAllButtons(false);
                    SetIsEnabledToButtons(true, StopButton, ResetButton, CyanKickOffButton, MagentaKickOffButton);

                    RefereeBox.Reset();
                    break;
            }

            LastMessageRun.Text = RefereeBox.LastSentCommand.ToString();
            LastMessageTimeRun.Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            if (LastMessageTextBlock.Visibility == Visibility.Hidden)
                LastMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void GoalButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            // The condition avoid UnGoal when the button goes disabled
            if (RefereeBox.IsChangingScore)
            {
                RefereeBox.UnGoal();

                // Revert score change if it isn't frozen
                SetIsCheckedToAllButtons(false);
                SetIsEnabledToAllButtons(true);
                SetIsEnabledToButtons(false, StartButton, ParkButton, ResetButton, CyanKickOffButton, MagentaKickOffButton);

                LastMessageRun.Text = RefereeBox.LastSentCommand.ToString();
                LastMessageTimeRun.Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            }
        }

        public void Close()
        {
            _clockTimer.Stop();
            RefereeBox.Close();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
