using System;
using System.Windows;
using System.IO.Ports;
using System.Windows.Threading;
using EventArgsLibrary;
using System.Configuration; 
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

using Arction.Wpf.Charting;             // LightningChartUltimate and general types.
using Arction.Wpf.Charting.SeriesXY;    // Series for 2D chart.
using Constants;
using WpfOscilloscopeControl;

namespace RobotInterface
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class WpfRobotInterface : Window
    {
        DispatcherTimer timerAffichage;

        public WpfRobotInterface()
        {

            InitializeComponent();
            
            worldMapDisplay.InitRobot((int)TeamId.Team1+(int)RobotId.Robot1);

            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }

            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 50);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();

            oscilloM1.SetTitle("Moteur 1");
            oscilloM1.AddOrUpdateLine(0, 100, "Vitesse M1");
            oscilloM1.AddOrUpdateLine(1, 100, "Courant M1");
            oscilloM1.ChangeLineColor("Courant M1", Colors.Red);
            oscilloM2.SetTitle("Moteur 2");
            oscilloM2.AddOrUpdateLine(0, 100, "Vitesse M2");
            oscilloM2.AddOrUpdateLine(1, 100, "Courant M2");
            oscilloM2.ChangeLineColor("Courant M2", Colors.Red);
            oscilloM3.SetTitle("Moteur 3");
            oscilloM3.AddOrUpdateLine(0, 100, "Vitesse M3");
            oscilloM3.AddOrUpdateLine(1, 100, "Courant M3");
            oscilloM3.ChangeLineColor("Courant M3", Colors.Red);
            oscilloM4.SetTitle("Moteur 4");
            oscilloM4.AddOrUpdateLine(0, 100, "Vitesse M4");
            oscilloM4.AddOrUpdateLine(1, 100, "Courant M4");
            oscilloM4.ChangeLineColor("Courant M4", Colors.Red);

            oscilloX.SetTitle("X");
            oscilloX.AddOrUpdateLine(0, 100, "Vitesse X Consigne");
            oscilloX.AddOrUpdateLine(1, 100, "Vitesse X");
            oscilloX.AddOrUpdateLine(2, 100, "Accel X");
            oscilloX.ChangeLineColor("Vitesse X", Colors.Red);
            oscilloX.ChangeLineColor("Vitesse X Consigne", Colors.Blue);
            oscilloY.SetTitle("Y");
            oscilloY.AddOrUpdateLine(0, 100, "Vitesse Y Consigne");
            oscilloY.AddOrUpdateLine(1, 100, "Vitesse Y");
            oscilloY.AddOrUpdateLine(2, 100, "Accel Y");
            oscilloY.ChangeLineColor("Vitesse Y", Colors.Red);
            oscilloY.ChangeLineColor("Vitesse Y Consigne", Colors.Blue);
        }

        int nbMsgSent = 0;

        int nbMsgReceived = 0;
        public void DisplayMessageDecoded(object sender, MessageDecodedArgs e)
        {
            nbMsgReceived += 1;
        }

        public void RegisterRobot(int id)
        {
            worldMapDisplay.RegisterRobot(id);
        }

        int nbMsgReceivedErrors = 0;
        public void DisplayMessageDecodedError(object sender, MessageDecodedArgs e)
        {
            nbMsgReceivedErrors += 1;
        }

        double currentTime = 0;
        private void TimerAffichage_Tick(object sender, EventArgs e)
        {
            //currentTime += 0.050;
            //double value = Math.Sin(0.5 * currentTime);
            //oscilloX.AddPointToLine(0, currentTime, value);
            //textBoxReception.Text = "Nb Message Sent : " + nbMsgSent + " Nb Message Received : " + nbMsgReceived + " Nb Message Received Errors : " + nbMsgReceivedErrors;
        }

        public void OnLocalWorldMapEvent(object sender, EventArgsLibrary.LocalWorldMapArgs e)
        {
            //throw new NotImplementedException();
            worldMapDisplay.UpdateLocalWorldMap(e.RobotId, e.LocalWorldMap);
        }

        public void ActualizeAccelDataOnGraph(object sender, AccelEventArgs e)
        {
            oscilloX.AddPointToLine(2, e.timeStampMS, e.accelX);
            oscilloY.AddPointToLine(2, e.timeStampMS, e.accelY);
            
        }

        public void UpdateImuDataOnGraph(object sender, IMUDataEventArgs e)
        {
            oscilloX.AddPointToLine(2, e.timeStampMS/1000.0, e.accelX);
            oscilloY.AddPointToLine(2, e.timeStampMS/1000.0, e.accelY);
            currentTime = e.timeStampMS/1000.0;
        }

        public void UpdateSpeedConsigneOnGraph(object sender, SpeedConsigneArgs e)
        {
            oscilloX.AddPointToLine(0, currentTime, e.Vx);
            oscilloY.AddPointToLine(0, currentTime, e.Vy);
        }

        public void UpdateMotorsCurrentsOnGraph(object sender, MotorsCurrentsEventArgs e)
        {
            oscilloM1.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor1);
            oscilloM2.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor2);
            oscilloM3.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor3);
            oscilloM4.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor4);
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

        private void CheckBoxDisableMotors_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        double zoomFactor = 5;
        bool isZoomed = false;
        int lastZoomedRow = 0;
        int lastZoomedCol = 0;
        private void ZoomOnGraph_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WpfOscilloscope s = (WpfOscilloscope)sender;

            int row = 0, column = 0;

            if (s != null)
            {
                row = Grid.GetRow(s);
                column = Grid.GetColumn(s);
            }


            if (!isZoomed)
            {
                AffichageTelemetrieRobot.ColumnDefinitions[column].Width = new GridLength(AffichageTelemetrieRobot.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                AffichageTelemetrieRobot.RowDefinitions[row].Height = new GridLength(AffichageTelemetrieRobot.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                lastZoomedCol = column;
                lastZoomedRow = row;
                isZoomed = true;
            }
            else
            {
                AffichageTelemetrieRobot.ColumnDefinitions[lastZoomedCol].Width = new GridLength(AffichageTelemetrieRobot.ColumnDefinitions[lastZoomedCol].Width.Value / zoomFactor, GridUnitType.Star);
                AffichageTelemetrieRobot.RowDefinitions[lastZoomedRow].Height = new GridLength(AffichageTelemetrieRobot.RowDefinitions[lastZoomedRow].Height.Value / zoomFactor, GridUnitType.Star);
                isZoomed = false;
                if (lastZoomedRow != row || lastZoomedCol != column)
                {
                    AffichageTelemetrieRobot.ColumnDefinitions[column].Width = new GridLength(AffichageTelemetrieRobot.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                    AffichageTelemetrieRobot.RowDefinitions[row].Height = new GridLength(AffichageTelemetrieRobot.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                    lastZoomedCol = column;
                    lastZoomedRow = row;
                    isZoomed = true;
                }
            }
        }
    }
}
