using EventArgsLibrary;
using System;
using System.Windows;
using System.Windows.Threading;

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
            localWorldMapDisplay1.InitRobot("Robot1Team1");
            localWorldMapDisplay2.InitRobot("Robot2Team1");
            localWorldMapDisplay3.InitRobot("Robot3Team1");
            localWorldMapDisplay4.InitRobot("Robot4Team1");
            localWorldMapDisplay5.InitRobot("Robot5Team1");
            localWorldMapDisplay6.InitRobot("Robot6Team1");

            for (int i = 0; i < 6; i++)
            {
                globalWorldMapDisplay.InitRobot("Robot" + i.ToString() + "Team1");
            }

            for (int i = 0; i < 6; i++)
            {
                globalWorldMapDisplay.InitRobot("Robot" + i.ToString() + "Team2");
            }
        }

        public void OnLocalWorldMapReceived(object sender, LocalWorldMapArgs e)
        {
            switch(e.RobotName)
            {
                case "Robot1Team1":
                    localWorldMapDisplay1.UpdateLocalWorldMap(e.RobotName, e.LocalWorldMap);
                    break;
                case "Robot2Team1":
                    localWorldMapDisplay2.UpdateLocalWorldMap(e.RobotName, e.LocalWorldMap);
                    break;
                case "Robot3Team1":
                    localWorldMapDisplay3.UpdateLocalWorldMap(e.RobotName, e.LocalWorldMap);
                    break;
                case "Robot4Team1":
                    localWorldMapDisplay4.UpdateLocalWorldMap(e.RobotName, e.LocalWorldMap);
                    break;
                case "Robot5Team1":
                    localWorldMapDisplay5.UpdateLocalWorldMap(e.RobotName, e.LocalWorldMap);
                    break;
                case "Robot6Team1":
                    localWorldMapDisplay6.UpdateLocalWorldMap(e.RobotName, e.LocalWorldMap);
                    break;
            }
        }
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMapDisplay.UpdateGlobalWorldMap(e.GlobalWorldMap);
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
    }
}
