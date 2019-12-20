using Constants;
using EventArgsLibrary;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfControlLibrary;

namespace TeamInterface
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class WpfTeamInterface : Window
    {
        public WpfTeamInterface()
        {
            InitializeComponent();
            localWorldMapDisplay1.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1);
            localWorldMapDisplay2.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot2);
            localWorldMapDisplay3.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot3);
            localWorldMapDisplay4.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot4);
            localWorldMapDisplay5.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot5);
            localWorldMapDisplay6.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot6);

            for (int i = 0; i < 5; i++)
            {
                globalWorldMapDisplayTeam1.InitTeamMate((int)TeamId.Team1 + i);
                globalWorldMapDisplayTeam1.InitOpponent((int)TeamId.Team2 + i);
                globalWorldMapDisplayTeam2.InitTeamMate((int)TeamId.Team2 + i);
                globalWorldMapDisplayTeam2.InitOpponent((int)TeamId.Team1 + i);
            }
        }

        public void OnLocalWorldMapReceived(object sender, LocalWorldMapArgs e)
        {
            switch(e.RobotId)
            {
                case (int)TeamId.Team1+ (int)RobotId.Robot1:
                    localWorldMapDisplay1.UpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot2:
                    localWorldMapDisplay2.UpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot3:
                    localWorldMapDisplay3.UpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot4:
                    localWorldMapDisplay4.UpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot5:
                    localWorldMapDisplay5.UpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
                    break;
                case (int)TeamId.Team1 + (int)RobotId.Robot6:
                    localWorldMapDisplay6.UpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
                    break;
            }
        }
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            if(e.GlobalWorldMap.TeamId == (int)TeamId.Team1)
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
        private void WorldMapDisplay_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WorldMapDisplay s = (WorldMapDisplay)sender;

            int row=0, column=0;

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
                if(lastZoomedRow!=row || lastZoomedCol!=column)
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
    }
}
