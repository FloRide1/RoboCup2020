using Constants;
using EventArgsLibrary;
using RefereeBoxAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Utilities;
using WorldMap;
using WpfWorldMapDisplay;
using ZeroFormatter;

namespace WpfTeamInterfaceNS
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class WpfTeamInterface : Window
    {
        string TeamIpAddress = "224.16.32.79";
        string OpponentTeamIpAddress = "224.16.32.63";
        string typeTerrain;

        DispatcherTimer timerAffichage;

        public WpfTeamInterface(string typeTerrain, string[] team1PlayerNames, string[] team2PlayerNames)
        {
            InitializeComponent();

            this.typeTerrain = typeTerrain;

            InitPlayers(team1PlayerNames, team2PlayerNames);

            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 50);
            timerAffichage.Tick += TimerAffichage_Tick; ;
            timerAffichage.Start();
        }

        private void InitPlayers(string[] team1PlayerNames, string[] team2PlayerNames)
        {
            localWorldMapDisplay1.Init(typeTerrain, LocalWorldMapDisplayType.StrategyMap);
            localWorldMapDisplay2.Init(typeTerrain, LocalWorldMapDisplayType.StrategyMap);
            localWorldMapDisplay3.Init(typeTerrain, LocalWorldMapDisplayType.StrategyMap);
            localWorldMapDisplay4.Init(typeTerrain, LocalWorldMapDisplayType.StrategyMap);
            localWorldMapDisplay5.Init(typeTerrain, LocalWorldMapDisplayType.StrategyMap);
            //localWorldMapDisplay6.Init(typeTerrain, LocalWorldMapDisplayType.StrategyMap);
            
            localWorldMapDisplay1.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, team1PlayerNames[0]);
            localWorldMapDisplay2.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot2, team1PlayerNames[1]);
            localWorldMapDisplay3.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot3, team1PlayerNames[2]);
            localWorldMapDisplay4.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot4, team1PlayerNames[3]);
            localWorldMapDisplay5.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot5, team1PlayerNames[4]);

            localWorldMapDisplay1.OnCtrlClickOnHeatMapEvent += LocalWorldMapDisplay1_OnCtrlClickOnHeatMapEvent;
            localWorldMapDisplay2.OnCtrlClickOnHeatMapEvent += LocalWorldMapDisplay2_OnCtrlClickOnHeatMapEvent;
            localWorldMapDisplay3.OnCtrlClickOnHeatMapEvent += LocalWorldMapDisplay3_OnCtrlClickOnHeatMapEvent;
            localWorldMapDisplay4.OnCtrlClickOnHeatMapEvent += LocalWorldMapDisplay4_OnCtrlClickOnHeatMapEvent;
            localWorldMapDisplay5.OnCtrlClickOnHeatMapEvent += LocalWorldMapDisplay5_OnCtrlClickOnHeatMapEvent;

            //localWorldMapDisplay6.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot6, "RoboCup");

            for (int i = 0; i < 5; i++)
            {
                globalWorldMapDisplayTeam1.InitTeamMate((int)TeamId.Team1 + i, team1PlayerNames[i]);
                globalWorldMapDisplayTeam1.InitOpponent((int)TeamId.Team2 + i, team2PlayerNames[i]);
                globalWorldMapDisplayTeam2.InitTeamMate((int)TeamId.Team2 + i, team2PlayerNames[i]);
                globalWorldMapDisplayTeam2.InitOpponent((int)TeamId.Team1 + i, team1PlayerNames[i]);
            }
        }

        private void LocalWorldMapDisplay1_OnCtrlClickOnHeatMapEvent(object sender, PositionArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot1;
            msg.posX = e.X;
            msg.posY = e.Y;
            msg.posTheta = 0;
            OnRefereeBoxReceivedCommand(msg);
        }
        private void LocalWorldMapDisplay2_OnCtrlClickOnHeatMapEvent(object sender, PositionArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot2;
            msg.posX = e.X;
            msg.posY = e.Y;
            msg.posTheta = 0;
            OnRefereeBoxReceivedCommand(msg);
        }
        private void LocalWorldMapDisplay3_OnCtrlClickOnHeatMapEvent(object sender, PositionArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot3;
            msg.posX = e.X;
            msg.posY = e.Y;
            msg.posTheta = 0;
            OnRefereeBoxReceivedCommand(msg);
        }
        private void LocalWorldMapDisplay4_OnCtrlClickOnHeatMapEvent(object sender, PositionArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot4;
            msg.posX = e.X;
            msg.posY = e.Y;
            msg.posTheta = 0;
            OnRefereeBoxReceivedCommand(msg);
        }
        private void LocalWorldMapDisplay5_OnCtrlClickOnHeatMapEvent(object sender, PositionArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot5;
            msg.posX = e.X;
            msg.posY = e.Y;
            msg.posTheta = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void TimerAffichage_Tick(object sender, EventArgs e)
        {
            globalWorldMapDisplayTeam1.UpdateWorldMapDisplay();
            globalWorldMapDisplayTeam2.UpdateWorldMapDisplay();
            localWorldMapDisplay1.UpdateWorldMapDisplay();
            localWorldMapDisplay2.UpdateWorldMapDisplay();
            localWorldMapDisplay3.UpdateWorldMapDisplay();
            localWorldMapDisplay4.UpdateWorldMapDisplay();
            localWorldMapDisplay5.UpdateWorldMapDisplay();
            //localWorldMapDisplay6.UpdateWorldMapDisplay();
        }

        public void OnLocalWorldMapReceived(object sender, LocalWorldMapArgs e)
        {
            switch (e.LocalWorldMap.RobotId)
            {
                case (int)TeamId.Team1 + (int)RobotId.Robot1:
                    localWorldMapDisplay1.UpdateLocalWorldMap(e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot2:
                    localWorldMapDisplay2.UpdateLocalWorldMap(e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot3:
                    localWorldMapDisplay3.UpdateLocalWorldMap(e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot4:
                    localWorldMapDisplay4.UpdateLocalWorldMap(e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot5:
                    localWorldMapDisplay5.UpdateLocalWorldMap(e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot6:
                    localWorldMapDisplay6.UpdateLocalWorldMap(e.LocalWorldMap);
                    break;
            }
        }
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            if (e.GlobalWorldMap.TeamId == (int)TeamId.Team1)
                globalWorldMapDisplayTeam1.UpdateGlobalWorldMap(e.GlobalWorldMap);
            else if (e.GlobalWorldMap.TeamId == (int)TeamId.Team2)
                globalWorldMapDisplayTeam2.UpdateGlobalWorldMap(e.GlobalWorldMap);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }

            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            this.Top = Properties.Settings.Default.Top;
            this.Left = Properties.Settings.Default.Left;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;
            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }


        double zoomFactor = 5;
        bool isZoomed = false;
        int lastZoomedRow = 0;
        int lastZoomedCol = 0;
        private void GlobalWorldMapDisplay_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //GlobalWorldMapDisplay s = (GlobalWorldMapDisplay)sender;

            //int row=0, column=0;

            //if (s != null)
            //{
            //    row = Grid.GetRow(s);
            //    column = Grid.GetColumn(s);
            //}


            //if (!isZoomed)
            //{
            //    BaseGrid.ColumnDefinitions[column].Width = new GridLength(BaseGrid.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
            //    BaseGrid.RowDefinitions[row].Height = new GridLength(BaseGrid.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
            //    localWorldMapDisplay2.IsExtended = true;
            //    lastZoomedCol = column;
            //    lastZoomedRow = row;
            //    isZoomed = true;
            //}
            //else
            //{
            //    BaseGrid.ColumnDefinitions[lastZoomedCol].Width = new GridLength(BaseGrid.ColumnDefinitions[lastZoomedCol].Width.Value / zoomFactor, GridUnitType.Star);
            //    BaseGrid.RowDefinitions[lastZoomedRow].Height = new GridLength(BaseGrid.RowDefinitions[lastZoomedRow].Height.Value / zoomFactor, GridUnitType.Star);
            //    localWorldMapDisplay2.IsExtended = false;
            //    isZoomed = false;
            //    if(lastZoomedRow!=row || lastZoomedCol!=column)
            //    {
            //        BaseGrid.ColumnDefinitions[column].Width = new GridLength(BaseGrid.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
            //        BaseGrid.RowDefinitions[row].Height = new GridLength(BaseGrid.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
            //        localWorldMapDisplay2.IsExtended = true;
            //        lastZoomedCol = column;
            //        lastZoomedRow = row;
            //        isZoomed = true;
            //    }
            //}
        }

        private void LocalWorldMapDisplay_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LocalWorldMapDisplay s = (LocalWorldMapDisplay)sender;

            int row = 0, column = 0;

            if (s != null)
            {
                row = Grid.GetRow(s);
                column = Grid.GetColumn(s);
            }


            if (!isZoomed)
            {
                BaseGrid.ColumnDefinitions[column].Width = new GridLength(BaseGrid.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                BaseGrid.RowDefinitions[row].Height = new GridLength(BaseGrid.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                localWorldMapDisplay2.IsExtended = true;
                lastZoomedCol = column;
                lastZoomedRow = row;
                isZoomed = true;
            }
            else
            {
                BaseGrid.ColumnDefinitions[lastZoomedCol].Width = new GridLength(BaseGrid.ColumnDefinitions[lastZoomedCol].Width.Value / zoomFactor, GridUnitType.Star);
                BaseGrid.RowDefinitions[lastZoomedRow].Height = new GridLength(BaseGrid.RowDefinitions[lastZoomedRow].Height.Value / zoomFactor, GridUnitType.Star);
                localWorldMapDisplay2.IsExtended = false;
                isZoomed = false;
                if (lastZoomedRow != row || lastZoomedCol != column)
                {
                    BaseGrid.ColumnDefinitions[column].Width = new GridLength(BaseGrid.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                    BaseGrid.RowDefinitions[row].Height = new GridLength(BaseGrid.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                    localWorldMapDisplay2.IsExtended = true;
                    lastZoomedCol = column;
                    lastZoomedRow = row;
                    isZoomed = true;
                }
            }
        }

        private void OnRefereeBoxReceivedCommand(RefBoxMessage rbMsg)
        {
            var msg = ZeroFormatterSerializer.Serialize<ZeroFormatterMsg>(rbMsg);
            OnMulticastSendRefBoxCommand(msg);
        }

        //Output events
        //public event EventHandler<RefBoxMessageArgs> OnRefereeBoxCommandEvent;
        //public virtual void OnRefereeBoxReceivedCommand(RefBoxMessage msg)
        //{
        //    var handler = OnRefereeBoxCommandEvent;
        //    if (handler != null)
        //    {
        //        handler(this, new RefBoxMessageArgs { refBoxMsg = msg });
        //    }
        //}

        public event EventHandler<DataReceivedArgs> OnMulticastSendRefBoxCommandEvent;
        public virtual void OnMulticastSendRefBoxCommand(byte[] data)
        {
            var handler = OnMulticastSendRefBoxCommandEvent;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }
        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.START;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.STOP;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_ThrowInT1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.THROWIN;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_ThrowInT2_Click(object sender, RoutedEventArgs e)
        {

            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.THROWIN;
            msg.targetTeam = OpponentTeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_GoalKcikT1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOALKICK;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_GoalKickT2_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOALKICK;
            msg.targetTeam = OpponentTeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_CornerT1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.CORNER;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_CornerT2_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.CORNER;
            msg.targetTeam = OpponentTeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_KickOffT1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.KICKOFF;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_KickOffT2_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.KICKOFF;
            msg.targetTeam = OpponentTeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_0_0_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 10;
            msg.posX = 0;
            msg.posY = 0;
            msg.posTheta = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_0_1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 10;
            msg.posX = 0;
            msg.posY = 1;
            msg.posTheta = Math.PI/2;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_1_0_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 10;
            msg.posX = 1;
            msg.posY = 0;
            msg.posTheta = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_0_m1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 10;
            msg.posX = 0;
            msg.posY = -1;
            msg.posTheta = -Math.PI / 2;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_m1_0_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 10;
            msg.posX = -1;
            msg.posY = 0;
            msg.posTheta = Math.PI;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_PlayLeftT1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.PLAYLEFT;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_PlayRightT2_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.PLAYRIGHT;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }
    }
}
