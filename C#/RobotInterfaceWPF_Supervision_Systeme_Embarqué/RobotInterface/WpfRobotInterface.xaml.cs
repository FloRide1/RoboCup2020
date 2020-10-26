using System;
using System.Windows;
using System.IO.Ports;
using System.Windows.Threading;
using EventArgsLibrary;
using System.Configuration; 
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Constants;
using WpfOscilloscopeControl;
using WpfWorldMapDisplay;
using SciChart.Charting.Visuals;
using System.Globalization;
using System.Threading;
using System.Windows.Markup;
using System.Windows.Input;
using System.Linq;
using RefereeBoxAdapter;
using Utilities;
using SciChart.Charting.Visuals.Axes;
using HerkulexManagerNS;

namespace RobotInterface
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class WpfRobotInterface : Window
    {
        DispatcherTimer timerAffichage = new DispatcherTimer();

        public WpfRobotInterface()
        {
            InitializeComponent();
            
            //Among other settings, this code may be used
            CultureInfo ci = CultureInfo.CurrentUICulture;

            try
            {
                //Override the default culture with something from app settings
                ci = new CultureInfo("Fr");
            }
            catch { }
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            //Here is the important part for databinding default converters
            FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(
                        XmlLanguage.GetLanguage(ci.IetfLanguageTag)));

            //Among other code
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".")
            {
                //Handler attach - will not be done if not needed
                PreviewKeyDown += new KeyEventHandler(MainWindow_PreviewKeyDown);
            }
            
            worldMapDisplayStrategy.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, "Eurobot");
            worldMapDisplayStrategy.Init("Eurobot", LocalWorldMapDisplayType.StrategyMap);

            worldMapDisplayWaypoint.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, "Eurobot");
            worldMapDisplayWaypoint.Init("Eurobot", LocalWorldMapDisplayType.WayPointMap);

            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }
                                   
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 50);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();

            oscilloM1.SetTitle("Moteur 1");
            oscilloM1.AddOrUpdateLine(0, 100, "Vitesse M1");
            //oscilloM1.AddOrUpdateLine(1, 100, "Courant M1");
            //oscilloM1.AddOrUpdateLine(2, 100, "Position M1");
            //oscilloM1.ChangeLineColor("Courant M1", Colors.Red);
            oscilloM2.SetTitle("Moteur 2");
            oscilloM2.AddOrUpdateLine(0, 100, "Vitesse M2");
            //oscilloM2.AddOrUpdateLine(1, 100, "Courant M2");
            //oscilloM2.AddOrUpdateLine(2, 100, "Position M2");
            //oscilloM2.ChangeLineColor("Courant M2", Colors.Red);
            oscilloM3.SetTitle("Moteur 3");
            oscilloM3.AddOrUpdateLine(0, 100, "Vitesse M3");
            //oscilloM3.AddOrUpdateLine(1, 100, "Courant M3");
            //oscilloM3.AddOrUpdateLine(2, 100, "Position M3");
            //oscilloM3.ChangeLineColor("Courant M3", Colors.Red);
            oscilloM4.SetTitle("Moteur 4");
            oscilloM4.AddOrUpdateLine(0, 100, "Vitesse M4");
            //oscilloM4.AddOrUpdateLine(1, 100, "Courant M4");
            //oscilloM4.AddOrUpdateLine(2, 100, "Position M4");
            //oscilloM4.ChangeLineColor("Courant M4", Colors.Red);

            oscilloX.SetTitle("Vx");
            oscilloX.AddOrUpdateLine(0, 100, "Vitesse X Consigne");
            oscilloX.AddOrUpdateLine(1, 100, "Vitesse X");
            oscilloX.AddOrUpdateLine(2, 100, "Accel X");
            oscilloX.ChangeLineColor("Vitesse X", Colors.Red);
            oscilloX.ChangeLineColor("Vitesse X Consigne", Colors.Blue);
            oscilloY.SetTitle("Vy");
            oscilloY.AddOrUpdateLine(0, 100, "Vitesse Y Consigne");
            oscilloY.AddOrUpdateLine(1, 100, "Vitesse Y");
            oscilloY.AddOrUpdateLine(2, 100, "Accel Y");
            oscilloY.ChangeLineColor("Vitesse Y", Colors.Red);
            oscilloY.ChangeLineColor("Vitesse Y Consigne", Colors.Blue);

            oscilloTheta.SetTitle("Vtheta");
            oscilloTheta.AddOrUpdateLine(0, 100, "Vitesse Theta Consigne");
            oscilloTheta.AddOrUpdateLine(1, 100, "Vitesse Theta");
            oscilloTheta.AddOrUpdateLine(2, 100, "Gyr Z");
            oscilloTheta.ChangeLineColor(1, Colors.Red);
            oscilloTheta.ChangeLineColor(0, Colors.Blue);
            
            oscilloLidar.AddOrUpdateLine(0, 20000, "Lidar RSSI", false);
            oscilloLidar.AddOrUpdateLine(1, 20000, "Lidar Distance");
            oscilloLidar.AddOrUpdateLine(2, 20000, "Balise Points");
            oscilloLidar.ChangeLineColor(0, Colors.SeaGreen);
            oscilloLidar.ChangeLineColor(1, Colors.IndianRed);
            oscilloLidar.ChangeLineColor(2, Colors.LightGoldenrodYellow);

            //oscilloLidar.DrawOnlyPoints(0);
        }

        void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Decimal)
            {
                e.Handled = true;

                if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.Length > 0)
                {
                    Keyboard.FocusedElement.RaiseEvent(
                        new TextCompositionEventArgs(
                            InputManager.Current.PrimaryKeyboardDevice,
                            new TextComposition(InputManager.Current,
                                Keyboard.FocusedElement,
                                CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                            )
                        { RoutedEvent = TextCompositionManager.TextInputEvent });
                }
            }
        }

        int nbMsgSent = 0;

        int nbMsgReceived = 0;
        public void DisplayMessageDecoded(object sender, MessageDecodedArgs e)
        {
            nbMsgReceived += 1;
        }
        
        int nbMsgReceivedErrors = 0;
        public void DisplayMessageDecodedError(object sender, MessageDecodedArgs e)
        {
            nbMsgReceivedErrors += 1;
        }

        double currentTime = 0;
        private void TimerAffichage_Tick(object sender, EventArgs e)
        {
            lock(HerkulexServos)
            {
                string output = "";
                for(int i = 0; i<HerkulexServos.Count; i++)
                {
                    output += "Servo " + HerkulexServos.Keys.ElementAt(i) + " : " + HerkulexServos.Values.ElementAt(i) + "\n";
                }
                textBoxConsole.Text = output;
            }
            //currentTime += 0.050;
            //double value = Math.Sin(0.5 * currentTime);
            //oscilloX.AddPointToLine(0, currentTime, value);
            //textBoxReception.Text = "Nb Message Sent : " + nbMsgSent + " Nb Message Received : " + nbMsgReceived + " Nb Message Received Errors : " + nbMsgReceivedErrors;
        }

        public void OnLocalWorldMapStrategyEvent(object sender, EventArgsLibrary.LocalWorldMapArgs e)
        {
            //throw new NotImplementedException();
            worldMapDisplayStrategy.UpdateLocalWorldMap(e.LocalWorldMap);
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                worldMapDisplayStrategy.UpdateWorldMapDisplay();
            }));
        }
        public void OnLocalWorldMapWayPointEvent(object sender, EventArgsLibrary.LocalWorldMapArgs e)
        {
            //throw new NotImplementedException();
            worldMapDisplayWaypoint.UpdateLocalWorldMap(e.LocalWorldMap);
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                worldMapDisplayWaypoint.UpdateWorldMapDisplay();
            }));
        }

        public void OnRawLidarDataReceived(object sender, EventArgsLibrary.RawLidarArgs e)
        {
            List<Point> ptList = new List<Point>();
            ptList = e.PtList.Select(p => new Point(p.Angle, p.Rssi)).ToList();
            oscilloLidar.UpdatePointListOfLine(0, ptList);
            List<Point> ptList2 = new List<Point>();
            ptList2 = e.PtList.Select(p => new Point(p.Angle, p.Distance)).ToList();
            oscilloLidar.UpdatePointListOfLine(1, ptList2);
        }

        public void OnRawLidarBalisePointsReceived(object sender, EventArgsLibrary.RawLidarArgs e)
        {
            List<Point> ptList2 = new List<Point>();
            ptList2 = e.PtList.Select(p => new Point(p.Angle, p.Distance)).ToList();
            oscilloLidar.UpdatePointListOfLine(2, ptList2);
        }

        Dictionary<ServoId, Servo> HerkulexServos = new Dictionary<ServoId, Servo>();
        int counterServo = 0;
        public void OnHerkulexServoInformationReceived(object sender, HerkulexEventArgs.HerkulexServoInformationArgs e)
        {
            lock (HerkulexServos)
            {
                if (HerkulexServos.ContainsKey(e.Servo.GetID()))
                {
                    HerkulexServos[e.Servo.GetID()] = e.Servo;
                }
                else
                {
                    HerkulexServos.Add(e.Servo.GetID(), e.Servo);
                }
            }

            if (counterServo++ % HerkulexServos.Count == 0) //On n'affiche qu'une fois tous les n évènements, n étant égal au nombre de servos
            {
                textBoxConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    string output = "";
                    lock (HerkulexServos)
                    {
                        for (int i = 0; i < HerkulexServos.Count; i++)
                        {
                            //output += "Servo " + HerkulexServos.Keys.ElementAt(i) + " Abs Pos : " + HerkulexServos.Values.ElementAt(i).AbsolutePosition + " Cal Pos : "+ HerkulexServos.Values.ElementAt(i).CalibratedPosition + "\n";
                            output += "Servo " + HerkulexServos.Keys.ElementAt(i) + " Cal Pos : " + HerkulexServos.Values.ElementAt(i).CalibratedPosition + "\n";
                        }
                    }
                    textBoxConsole.Text = output;
                }));
            }
        }

        public void ResetInterfaceState()
        {
            oscilloX.ResetGraph();
            oscilloY.ResetGraph();
            oscilloTheta.ResetGraph();
            oscilloM1.ResetGraph();
            oscilloM2.ResetGraph();
            oscilloM3.ResetGraph();
            oscilloM4.ResetGraph();
        }

         public void UpdateSpeedDataOnGraph(object sender, SpeedDataEventArgs e)
        {
            //oscilloX.AddPointToLine(1, e.EmbeddedTimeStampInMs / 1000.0, (e.Vx - Vx_T_1) * 50);
            //Vx_T_1 = e.Vx;
            //oscilloY.AddPointToLine(1, e.EmbeddedTimeStampInMs / 1000.0, (e.Vy - Vy_T_1) * 50);
            //Vy_T_1 = e.Vy;
            //oscilloTheta.AddPointToLine(1, e.EmbeddedTimeStampInMs / 1000.0, e.Vtheta);
            //Vtheta_T_1 = e.Vtheta;
            oscilloX.AddPointToLine(1, e.EmbeddedTimeStampInMs / 1000.0, e.Vx);
            oscilloY.AddPointToLine(1, e.EmbeddedTimeStampInMs / 1000.0, e.Vy);
            oscilloTheta.AddPointToLine(1, e.EmbeddedTimeStampInMs / 1000.0, e.Vtheta);
            currentTime = e.EmbeddedTimeStampInMs / 1000.0;
        }
        public void ActualizeAccelDataOnGraph(object sender, AccelEventArgs e)
        {
            oscilloX.AddPointToLine(2, e.timeStampMS, e.accelX);
            oscilloY.AddPointToLine(2, e.timeStampMS, e.accelY);
            
        }

        public void UpdateImuDataOnGraph(object sender, IMUDataEventArgs e)
        {
            oscilloX.AddPointToLine(2, e.EmbeddedTimeStampInMs/1000.0, e.accelX);
            oscilloY.AddPointToLine(2, e.EmbeddedTimeStampInMs/1000.0, e.accelY);
            oscilloTheta.AddPointToLine(2, e.EmbeddedTimeStampInMs / 1000.0, e.gyroZ);
            currentTime = e.EmbeddedTimeStampInMs/1000.0;
        }

        public void UpdateSpeedConsigneOnGraph(object sender, SpeedArgs e)
        {
            oscilloX.AddPointToLine(0, currentTime, e.Vx);
            oscilloY.AddPointToLine(0, currentTime, e.Vy);
            oscilloTheta.AddPointToLine(0, currentTime, e.Vtheta);

        }

        double VM1_1 = 0;
        public void UpdateMotorSpeedConsigneOnGraph(object sender, MotorsVitesseDataEventArgs e)
        {
            oscilloM1.AddPointToLine(4, e.timeStampMS / 1000.0, e.vitesseMotor1);
            oscilloM2.AddPointToLine(4, e.timeStampMS / 1000.0, e.vitesseMotor2);
            oscilloM3.AddPointToLine(4, e.timeStampMS / 1000.0, e.vitesseMotor3);
            oscilloM4.AddPointToLine(4, e.timeStampMS / 1000.0, e.vitesseMotor4);
        }

        public void UpdateMotorsCurrentsOnGraph(object sender, MotorsCurrentsEventArgs e)
        {
            oscilloM1.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor1);
            oscilloM2.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor2);
            oscilloM3.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor3);
            oscilloM4.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor4);
        }

        public void UpdateMotorsSpeedsOnGraph(object sender, MotorsVitesseDataEventArgs e)
        {
            oscilloM1.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor1);
            oscilloM2.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor2);
            oscilloM3.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor3);
            oscilloM4.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor4);
        }

        public void UpdateMotorsPositionOnGraph(object sender, MotorsPositionDataEventArgs e)
        {
            oscilloM1.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor1);
            oscilloM2.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor2);
            oscilloM3.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor3);
            oscilloM4.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor4);
        }

        public void UpdateMotorsEncRawDataOnGraph(object sender, EncodersRawDataEventArgs e)
        {
            oscilloM1.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor1);
            oscilloM2.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor2);
            oscilloM3.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor3);
            oscilloM4.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor4);
        }

        public void UpdatePIDDebugDataOnGraph(object sender, PIDDebugDataArgs e)
        {
            oscilloX.AddPointToLine(3, e.timeStampMS / 1000.0, e.xErreur);
            oscilloX.AddPointToLine(4, e.timeStampMS / 1000.0, e.xCorrection);

            oscilloY.AddPointToLine(3, e.timeStampMS / 1000.0, e.yErreur);
            oscilloY.AddPointToLine(4, e.timeStampMS / 1000.0, e.yCorrection);

            oscilloTheta.AddPointToLine(3, e.timeStampMS / 1000.0, e.thetaErreur);
            oscilloTheta.AddPointToLine(4, e.timeStampMS / 1000.0, e.thetaCorrection);

            oscilloX.AddPointToLine(5, e.timeStampMS / 1000.0, e.xConsigneFromRobot);
            oscilloY.AddPointToLine(5, e.timeStampMS / 1000.0, e.yConsigneFromRobot);
            oscilloTheta.AddPointToLine(5, e.timeStampMS / 1000.0, e.thetaConsigneFromRobot);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
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
            catch { }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            try
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
            catch {; }
        }

        bool motorsDisabled = false;
        private void ButtonDisableMotors_Click(object sender, RoutedEventArgs e)
        {
            if (!motorsDisabled)
            {
                motorsDisabled = true;
                OnEnableDisableMotorsFromInterface(false);
            }
            else
            {
                motorsDisabled = false;
                OnEnableDisableMotorsFromInterface(true);
            }
            ResetInterfaceState();
        }
       

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void ActualizeEnableDisableMotorsButton(object sender, BoolEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            //Utilisation ici d'une methode anonyme
            ButtonDisableMotors.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (!e.value)
                {
                    ButtonDisableMotors.Content = "Enable Motors";
                    motorsDisabled = true;
                }
                else
                {
                    ButtonDisableMotors.Content = "Disable Motors";
                    motorsDisabled = false;
                }
            }));
        }

        private void ButtonEnableDisableTir_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonEnableDisableTir.Content == "Enable Tir")
                OnEnableDisableTirFromInterface(true);
            else
                OnEnableDisableTirFromInterface(false);
        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void ActualizeEnableDisableTirButton(object sender, BoolEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            ButtonEnableDisableTir.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (!e.value)
                    ButtonEnableDisableTir.Content = "Enable Tir";
                else
                    ButtonEnableDisableTir.Content = "Disable Tir";
            }));
        }



        private void ButtonEnableDisableServos_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonEnableDisableServos.Content == "Servos Torque Off")
            {
                OnEnableDisableServosFromInterface(false);
                ButtonEnableDisableServos.Content = "Servos Torque On";
            }
            else
            {
                OnEnableDisableServosFromInterface(true);
                ButtonEnableDisableServos.Content = "Servos Torque Off";
            }
        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void ActualizeEnableDisableServosButton(object sender, BoolEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            ButtonEnableDisableServos.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (!e.value)
                    ButtonEnableDisableServos.Content = "Servos Torque On";
                else
                    ButtonEnableDisableServos.Content = "Servos Torque Off";
            }));
        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void ActualizeEnableAsservissementButton(object sender, BoolEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            ButtonEnableAsservissement.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (!e.value)
                {
                    ButtonEnableAsservissement.Content = "Enable Asservissement";
                    asservissementEnable = false;
                }
                else
                {
                    ButtonEnableAsservissement.Content = "Disable Asservissement";
                    asservissementEnable = true;
                }
            }));
        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void AppendConsole(object sender, StringEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            textBoxConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                textBoxConsole.Text += e.value+'\n';
                if (textBoxConsole.Text.Length >= 2000)
                {
                    textBoxConsole.Text = textBoxConsole.Text.Remove(0, 2000);
                }
                scrollViewerTextBoxConsole.ScrollToEnd();
            }));
        }


        public void MessageCounterReceived(object sender, MsgCounterArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                LabelNbSpeedOdometryDataPerSec.Content = "Nb odometry data / sec : " + e.nbMessageOdometry;
                LabelNbIMUDataPerSec.Content = "Nb IMU data / sec : " + e.nbMessageIMU;
            }));
        }

        double zoomFactor = 5;
        bool isZoomed = false;
        int lastZoomedRow = 0;
        int lastZoomedCol = 0;
        private void ZoomOnGraph_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int row = 0, column = 0;
            if (sender.GetType() == typeof(WpfOscilloscope))
            {
                WpfOscilloscope s = (WpfOscilloscope)sender;
                if (s != null)
                {
                    row = Grid.GetRow(s);
                    column = Grid.GetColumn(s);
                }
            }
            else if(sender.GetType()== typeof(GroupBox))
            {
                GroupBox s = (GroupBox)sender;
                if (s != null)
                {
                    row = Grid.GetRow(s);
                    column = Grid.GetColumn(s);
                }
            }

            if (!isZoomed)
            {
                GridApplication.ColumnDefinitions[column].Width = new GridLength(GridApplication.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                GridApplication.RowDefinitions[row].Height = new GridLength(GridApplication.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                lastZoomedCol = column;
                lastZoomedRow = row;
                isZoomed = true;
            }
            else
            {
                GridApplication.ColumnDefinitions[lastZoomedCol].Width = new GridLength(GridApplication.ColumnDefinitions[lastZoomedCol].Width.Value / zoomFactor, GridUnitType.Star);
                GridApplication.RowDefinitions[lastZoomedRow].Height = new GridLength(GridApplication.RowDefinitions[lastZoomedRow].Height.Value / zoomFactor, GridUnitType.Star);
                isZoomed = false;
                if (lastZoomedRow != row || lastZoomedCol != column)
                {
                    GridApplication.ColumnDefinitions[column].Width = new GridLength(GridApplication.ColumnDefinitions[column].Width.Value * zoomFactor, GridUnitType.Star);
                    GridApplication.RowDefinitions[row].Height = new GridLength(GridApplication.RowDefinitions[row].Height.Value * zoomFactor, GridUnitType.Star);
                    lastZoomedCol = column;
                    lastZoomedRow = row;
                    isZoomed = true;
                }
            }
        }
#region OUTPUT EVENT
        //OUTPUT EVENT
        public delegate void EnableDisableMotorsEventHandler(object sender, BoolEventArgs e);
        public event EnableDisableMotorsEventHandler OnEnableDisableMotorsFromInterfaceGeneratedEvent;
        public virtual void OnEnableDisableMotorsFromInterface(bool val)
        {
            var handler = OnEnableDisableMotorsFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val } );
            }
        }

        //public delegate void EnableDisableTirEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableDisableTirFromInterfaceGeneratedEvent;
        public virtual void OnEnableDisableTirFromInterface(bool val)
        {
            var handler = OnEnableDisableTirFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        //public delegate void EnableDisableTirEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableDisableServosFromInterfaceGeneratedEvent;
        public virtual void OnEnableDisableServosFromInterface(bool val)
        {
            var handler = OnEnableDisableServosFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }
        //public delegate void EnableDisableTirEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableAsservissementFromInterfaceGeneratedEvent;
        public virtual void OnEnableAsservissementFromInterface(bool val)
        {
            var handler = OnEnableAsservissementFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }
        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableDisableControlManetteFromInterfaceGeneratedEvent;
        public virtual void OnEnableDisableControlManetteFromInterface(bool val)
        {
            var handler = OnEnableDisableControlManetteFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableMotorCurrentDataFromInterfaceGeneratedEvent;
        public virtual void OnEnableMotorCurrentDataFromInterface(bool val)
        {
            var handler = OnEnableMotorCurrentDataFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableEncodersDataFromInterfaceGeneratedEvent;
        public virtual void OnEnableEncodersDataFromInterface(bool val)
        {
            var handler = OnEnableEncodersDataFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableEncodersRawDataFromInterfaceGeneratedEvent;
        public virtual void OnEnableEncodersRawDataFromInterface(bool val)
        {
            var handler = OnEnableEncodersRawDataFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableMotorsSpeedConsigneDataFromInterfaceGeneratedEvent;
        public virtual void OnEnableMotorSpeedConsigneDataFromInterface(bool val)
        {
            var handler = OnEnableMotorsSpeedConsigneDataFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnablePIDDebugDataFromInterfaceGeneratedEvent;
        public virtual void OnEnablePIDDebugDataFromInterface(bool val)
        {
            var handler = OnEnablePIDDebugDataFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<PIDDataArgs> OnSetRobotPIDFromInterfaceGeneratedEvent;
        public virtual void OnSetRobotPIDFromInterface(double px, double ix, double dx, double py, double iy, double dy, double ptheta, double itheta, double dtheta)
        {
            var handler = OnSetRobotPIDFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new PIDDataArgs { P_x = px, I_x=ix, D_x=dx, P_y=py, I_y=iy, D_y=dy, P_theta=ptheta, I_theta=itheta, D_theta=dtheta });
            }
        }


        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<EventArgs> OnCalibrateGyroFromInterfaceGeneratedEvent;
        public virtual void OnCalibrateGyroFromInterface()
        {
            var handler = OnCalibrateGyroFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        #endregion
        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if(CheckBoxControlManette.IsChecked ?? false)
            {
                OnEnableDisableControlManetteFromInterface(true);
            }
            else
            {
                OnEnableDisableControlManetteFromInterface(false);
            }
        }


        bool isWorldMapZoomed = false;
        double worldMapZoomFactor = 5;
        
        private void worldMapDisplay_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LocalWorldMapDisplay s = (LocalWorldMapDisplay)sender;

            int row = 0, column = 0;

            if (s != null)
            {
                row = Grid.GetRow(s);
                column = Grid.GetColumn(s);
            }

            if (!isWorldMapZoomed)
            {
                GridApplication.ColumnDefinitions[column].Width = new GridLength(GridApplication.ColumnDefinitions[column].Width.Value * worldMapZoomFactor, GridUnitType.Star);
                GridApplication.RowDefinitions[row].Height = new GridLength(GridApplication.RowDefinitions[row].Height.Value * worldMapZoomFactor, GridUnitType.Star);
                isWorldMapZoomed = true;
            }
            else
            {
                GridApplication.ColumnDefinitions[column].Width = new GridLength(GridApplication.ColumnDefinitions[column].Width.Value / worldMapZoomFactor, GridUnitType.Star);
                GridApplication.RowDefinitions[row].Height = new GridLength(GridApplication.RowDefinitions[row].Height.Value / worldMapZoomFactor, GridUnitType.Star);
                isWorldMapZoomed = false;
            }
        }

        bool asservissementEnable = false;
        private void ButtonEnableAsservissement_Click(object sender, RoutedEventArgs e)
        {
            if (asservissementEnable)
            {
                OnEnableAsservissementFromInterface(false);
                
            }
            else
                OnEnableAsservissementFromInterface(true);
        }

        //private void ButtonSetPID_Click(object sender, RoutedEventArgs e)
        //{
        //    double Px = Convert.ToDouble(textBoxPx.Text);
        //    double Ix = Convert.ToDouble(textBoxIx.Text);
        //    double Dx = Convert.ToDouble(textBoxDx.Text);
        //    double Py = Convert.ToDouble(textBoxPy.Text);
        //    double Iy = Convert.ToDouble(textBoxIy.Text);
        //    double Dy = Convert.ToDouble(textBoxDy.Text);
        //    double Ptheta = Convert.ToDouble(textBoxPtheta.Text);
        //    double Itheta = Convert.ToDouble(textBoxItheta.Text);
        //    double Dtheta = Convert.ToDouble(textBoxDtheta.Text);
        //    OnSetRobotPIDFromInterface(Px,Ix, Dx, Py, Iy, Dy, Ptheta, Itheta, Dtheta);

        //}

        private void CheckBoxEnableMotorCurrentData_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxEnableMotorCurrentData.IsChecked ?? false)
            {
                OnEnableMotorCurrentDataFromInterface(true);
                oscilloM1.AddOrUpdateLine(1, 100, "Courant M1");
                oscilloM1.ChangeLineColor(1, Colors.Red);
                oscilloM2.AddOrUpdateLine(1, 100, "Courant M2");
                oscilloM2.ChangeLineColor(1, Colors.Red);
                oscilloM3.AddOrUpdateLine(1, 100, "Courant M3");
                oscilloM3.ChangeLineColor(1, Colors.Red);
                oscilloM4.AddOrUpdateLine(1, 100, "Courant M4");
                oscilloM4.ChangeLineColor(1, Colors.Red);
            }
            else
            {
                OnEnableMotorCurrentDataFromInterface(false);
                if (oscilloM1.LineExist(1))
                {
                    oscilloM1.RemoveLine(1);
                }
                if (oscilloM2.LineExist(1))
                {
                    oscilloM2.RemoveLine(1);
                }
                if (oscilloM3.LineExist(1))
                {
                    oscilloM3.RemoveLine(1);
                }
                if (oscilloM4.LineExist(1))
                {
                    oscilloM4.RemoveLine(1);
                }
            }
        }

        private void CheckBoxEnablePositionData_Checked(object sender, RoutedEventArgs e)
        {
            if(CheckBoxEnablePositionData.IsChecked ?? false)
            {
                OnEnableEncodersDataFromInterface(true);
                oscilloM1.AddOrUpdateLine(2, 100, "Position M1");
                oscilloM1.ChangeLineColor(2, Colors.Blue);
                oscilloM2.AddOrUpdateLine(2, 100, "Position M2");
                oscilloM2.ChangeLineColor(2, Colors.Blue);
                oscilloM3.AddOrUpdateLine(2, 100, "Position M3");
                oscilloM3.ChangeLineColor(2, Colors.Blue);
                oscilloM4.AddOrUpdateLine(2, 100, "Position M4");
                oscilloM4.ChangeLineColor(2, Colors.Blue);
            }
            else
            {
                OnEnableEncodersDataFromInterface(false);
                if (oscilloM1.LineExist(2))
                {
                    oscilloM1.RemoveLine(2);
                }
                if (oscilloM2.LineExist(2))
                {
                    oscilloM2.RemoveLine(2);
                }
                if (oscilloM3.LineExist(2))
                {
                    oscilloM3.RemoveLine(2);
                }
                if (oscilloM4.LineExist(2))
                {
                    oscilloM4.RemoveLine(2);
                }
            }
        }

        private void CheckBoxEnableEncRawData_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxEnableEncRawData.IsChecked ?? false)
            {
                OnEnableEncodersRawDataFromInterface(true);
                oscilloM1.AddOrUpdateLine(3, 100, "RAW Val M1");
                oscilloM1.ChangeLineColor(3, Colors.GreenYellow);
                oscilloM2.AddOrUpdateLine(3, 100, "RAW Val M2");
                oscilloM2.ChangeLineColor(3, Colors.GreenYellow);
                oscilloM3.AddOrUpdateLine(3, 100, "RAW Val M3");
                oscilloM3.ChangeLineColor(3, Colors.GreenYellow);
                oscilloM4.AddOrUpdateLine(3, 100, "RAW Val M4");
                oscilloM4.ChangeLineColor(3, Colors.GreenYellow);
            }
            else
            {
                OnEnableEncodersRawDataFromInterface(false);
                if (oscilloM1.LineExist(3))
                {
                    oscilloM1.RemoveLine(3);
                }
                if (oscilloM2.LineExist(3))
                {
                    oscilloM2.RemoveLine(3);
                }
                if (oscilloM3.LineExist(3))
                {
                    oscilloM3.RemoveLine(3);
                }
                if (oscilloM4.LineExist(3))
                {
                    oscilloM4.RemoveLine(3);
                }
            }
        }

        private void CheckBoxEnableMotorSpeedConsigneData_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxEnableMotorSpeedConsigneData.IsChecked ?? false)
            {
                OnEnableMotorSpeedConsigneDataFromInterface(true);
                oscilloM1.AddOrUpdateLine(4, 100, "PWM M1");
                oscilloM1.ChangeLineColor(4, Colors.GreenYellow);
                oscilloM2.AddOrUpdateLine(4, 100, "PWM M2");
                oscilloM2.ChangeLineColor(4, Colors.GreenYellow);
                oscilloM3.AddOrUpdateLine(4, 100, "PWM M3");
                oscilloM3.ChangeLineColor(4, Colors.GreenYellow);
                oscilloM4.AddOrUpdateLine(4, 100, "PWM M4");
                oscilloM4.ChangeLineColor(4, Colors.GreenYellow);
            }
            else
            {
                OnEnableMotorSpeedConsigneDataFromInterface(false);
                if (oscilloM1.LineExist(4))
                {
                    oscilloM1.RemoveLine(4);
                }
                if (oscilloM2.LineExist(4))
                {
                    oscilloM2.RemoveLine(4);
                }
                if (oscilloM3.LineExist(4))
                {
                    oscilloM3.RemoveLine(4);
                }
                if (oscilloM4.LineExist(4))
                {
                    oscilloM4.RemoveLine(4);
                }
            }
        }

        private void CheckBoxEnablePIDDebugData_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxEnablePIDDebugData.IsChecked ?? false)
            {
                OnEnablePIDDebugDataFromInterface(true);
                oscilloX.AddOrUpdateLine(3, 100, "xErreur");
                oscilloX.ChangeLineColor(3, Colors.GreenYellow);
                oscilloY.AddOrUpdateLine(3, 100, "yErreur");
                oscilloY.ChangeLineColor(3, Colors.GreenYellow);
                oscilloTheta.AddOrUpdateLine(3, 100, "thetaErreur");
                oscilloTheta.ChangeLineColor(3, Colors.GreenYellow);

                oscilloX.AddOrUpdateLine(4, 100, "xCorrection %");
                oscilloX.ChangeLineColor(4, Colors.ForestGreen);
                oscilloY.AddOrUpdateLine(4, 100, "yCorrection %");
                oscilloY.ChangeLineColor(4, Colors.ForestGreen);
                oscilloTheta.AddOrUpdateLine(4, 100, "thetaCorrection %");
                oscilloTheta.ChangeLineColor(4, Colors.ForestGreen);

                oscilloX.AddOrUpdateLine(5, 100, "xConsigne robot");
                oscilloX.ChangeLineColor(5, Colors.Yellow);
                oscilloY.AddOrUpdateLine(5, 100, "xConsigne robot");
                oscilloY.ChangeLineColor(5, Colors.Yellow);
                oscilloTheta.AddOrUpdateLine(5, 100, "thetaConsigne robot");
                oscilloTheta.ChangeLineColor(5, Colors.Yellow);
            }
            else
            {
                OnEnablePIDDebugDataFromInterface(false);
                if (oscilloX.LineExist(3))
                {
                    oscilloX.RemoveLine(3);
                }
                if (oscilloX.LineExist(4))
                {
                    oscilloX.RemoveLine(4);
                }
                if (oscilloX.LineExist(5))
                {
                    oscilloX.RemoveLine(5);
                }

                if (oscilloY.LineExist(3))
                {
                    oscilloY.RemoveLine(3);
                }
                if (oscilloY.LineExist(4))
                {
                    oscilloY.RemoveLine(4);
                }
                if (oscilloY.LineExist(5))
                {
                    oscilloY.RemoveLine(5);
                }

                if (oscilloTheta.LineExist(3))
                {
                    oscilloTheta.RemoveLine(3);
                }
                if (oscilloTheta.LineExist(4))
                {
                    oscilloTheta.RemoveLine(4);
                }
                if (oscilloTheta.LineExist(5))
                {
                    oscilloTheta.RemoveLine(5);
                }
            }
        }

        private void ButtonCalibrateGyro_Click(object sender, RoutedEventArgs e)
        {
            OnCalibrateGyroFromInterface();
        }

        string TeamIpAddress = "0.0.0.0";
        private void Button_0_0_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO_0_0;
            msg.targetTeam = TeamIpAddress; //Ici on est en local, pas de transmission, on remplis pour rien.
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_0_1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO_0_1;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_1_0_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO_1_0;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_0_m1_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO_0_M1;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_m1_0_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO_M1_0;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = 0;
            OnRefereeBoxReceivedCommand(msg);
        }

        //Output events
        public event EventHandler<RefBoxMessageArgs> OnRefereeBoxCommandEvent;
        public virtual void OnRefereeBoxReceivedCommand(RefBoxMessage msg)
        {
            var handler = OnRefereeBoxCommandEvent;
            if (handler != null)
            {
                handler(this, new RefBoxMessageArgs { refBoxMsg = msg });
            }
        }
    }
}
