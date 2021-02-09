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
using WorldMap;
using ZeroFormatter;
using System.IO;

namespace RobotInterface
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class WpfRobot4RouesHolonomeInterface : Window
    {
        GameMode gameMode;
        DispatcherTimer timerAffichage = new DispatcherTimer();

        public WpfRobot4RouesHolonomeInterface(GameMode gamemode)
        {
            gameMode = gamemode;
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
            
            var currentDir = Directory.GetCurrentDirectory();
            var racineProjets = Directory.GetParent(currentDir);
            var imagePath = racineProjets.Parent.Parent.FullName.ToString() + "\\Images\\";
            if (gameMode == GameMode.Eurobot)
            {
                worldMapDisplayStrategy.Init(gameMode, LocalWorldMapDisplayType.StrategyMap);
                worldMapDisplayStrategy.SetFieldImageBackGround(imagePath + "Eurobot2020.png");
                worldMapDisplayWaypoint.Init(gameMode, LocalWorldMapDisplayType.WayPointMap);
                worldMapDisplayWaypoint.SetFieldImageBackGround(imagePath + "Eurobot2020.png");
            }
            else if (gameMode == GameMode.RoboCup)
            {
                //worldMapDisplayStrategy.Init(gameMode, LocalWorldMapDisplayType.StrategyMap, imagePath + "RoboCup.png");
                //worldMapDisplayWaypoint.Init(gameMode, LocalWorldMapDisplayType.WayPointMap, imagePath + "RoboCup.png");
            }

            worldMapDisplayStrategy.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, GameMode.Eurobot,  "Wally");
            worldMapDisplayWaypoint.InitTeamMate((int)TeamId.Team1 + (int)RobotId.Robot1, GameMode.Eurobot,  "Wally");

            worldMapDisplayStrategy.OnCtrlClickOnHeatMapEvent += WorldMapDisplay_OnCtrlClickOnHeatMapEvent;
            worldMapDisplayWaypoint.OnCtrlClickOnHeatMapEvent += WorldMapDisplay_OnCtrlClickOnHeatMapEvent;


            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }
                                   
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 50);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();

            //oscilloM1.SetTitle("Moteur 1");
            //oscilloM1.AddOrUpdateLine(0, 100, "Vitesse M1");
            //oscilloM1.AddOrUpdateLine(1, 100, "Courant M1");
            //oscilloM2.SetTitle("Moteur 2");
            //oscilloM2.AddOrUpdateLine(0, 100, "Vitesse M2");
            //oscilloM2.AddOrUpdateLine(1, 100, "Courant M2");
            //oscilloM3.SetTitle("Moteur 3");
            //oscilloM3.AddOrUpdateLine(0, 100, "Vitesse M3");
            //oscilloM3.AddOrUpdateLine(1, 100, "Courant M3");
            //oscilloM4.SetTitle("Moteur 4");
            //oscilloM4.AddOrUpdateLine(0, 100, "Vitesse M4");
            //oscilloM4.AddOrUpdateLine(1, 100, "Courant M4");

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

            asservPositionDisplay.SetTitle("Asservissement Position");
            asservSpeedDisplay.SetTitle("Asservissement Vitesse");
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
                //textBoxConsole.Text = output;
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
            //Dispatcher.BeginInvoke(new Action(delegate ()
            //{
            //    worldMapDisplayStrategy.UpdateWorldMapDisplay();
            //}));
        }
        public void OnLocalWorldMapWayPointEvent(object sender, EventArgsLibrary.LocalWorldMapArgs e)
        {
            //throw new NotImplementedException();
            worldMapDisplayWaypoint.UpdateLocalWorldMap(e.LocalWorldMap);
            //Dispatcher.BeginInvoke(new Action(delegate ()
            //{
            //    worldMapDisplayWaypoint.UpdateWorldMapDisplay();
            //}));
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
            List<Point> ptListBalises = new List<Point>();
            ptListBalises = e.PtList.Select(p => new Point(p.Angle, p.Distance)).ToList();
            oscilloLidar.UpdatePointListOfLine(2, ptListBalises);
        }

        public void OnMessageToDisplayPolarSpeedPidSetupReceived(object sender, PolarPIDSetupArgs e)
        {
            asservSpeedDisplay.UpdatePolarSpeedCorrectionGains(e.P_x, e.P_y, e.P_theta, e.I_x, e.I_y, e.I_theta, e.D_x, e.D_y, e.D_theta);
            asservSpeedDisplay.UpdatePolarSpeedCorrectionLimits(e.P_x_Limit, e.P_y_Limit, e.P_theta_Limit, e.I_x_Limit, e.I_y_Limit, e.I_theta_Limit, e.D_x_Limit, e.D_y_Limit, e.D_theta_Limit);
        }

        public void OnMessageToDisplayIndependantSpeedPidSetupReceived(object sender, IndependantPIDSetupArgs e)
        {
            asservSpeedDisplay.UpdateIndependantSpeedCorrectionGains(e.P_M1, e.P_M2, e.P_M3, e.P_M4, e.I_M1, e.I_M2, e.I_M3, e.I_M4, e.D_M1, e.D_M2, e.D_M3, e.D_M4);
            asservSpeedDisplay.UpdateIndependantSpeedCorrectionLimits(e.P_M1_Limit, e.P_M2_Limit, e.P_M3_Limit, e.P_M4_Limit, e.I_M1_Limit, e.I_M2_Limit, e.I_M3_Limit, e.I_M4_Limit, e.D_M1_Limit, e.D_M2_Limit, e.D_M3_Limit, e.D_M4_Limit);
        }

        public void OnMessageToDisplayPositionPidSetupReceived(object sender, PolarPIDSetupArgs e)
        {
            asservPositionDisplay.UpdatePolarSpeedCorrectionGains(e.P_x, e.P_y, e.P_theta, e.I_x, e.I_y, e.I_theta, e.D_x, e.D_y, e.D_theta);
            asservPositionDisplay.UpdatePolarSpeedCorrectionLimits(e.P_x_Limit, e.P_y_Limit, e.P_theta_Limit, e.I_x_Limit, e.I_y_Limit, e.I_theta_Limit, e.D_x_Limit, e.D_y_Limit, e.D_theta_Limit);
        }

        public void OnMessageToDisplayPositionPidCorrectionReceived(object sender, PolarPidCorrectionArgs e)
        {
            asservPositionDisplay.UpdatePolarSpeedCorrectionValues(e.CorrPx, e.CorrPy, e.CorrPTheta, e.CorrIx, e.CorrIy, e.CorrITheta, e.CorrDx, e.CorrDy, e.CorrDTheta);
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
                //textBoxConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                //{
                //    string output = "";
                //    lock (HerkulexServos)
                //    {
                //        for (int i = 0; i < HerkulexServos.Count; i++)
                //        {
                //            //output += "Servo " + HerkulexServos.Keys.ElementAt(i) + " Abs Pos : " + HerkulexServos.Values.ElementAt(i).AbsolutePosition + " Cal Pos : "+ HerkulexServos.Values.ElementAt(i).CalibratedPosition + "\n";
                //            output += "Servo " + HerkulexServos.Keys.ElementAt(i) + " Cal Pos : " + HerkulexServos.Values.ElementAt(i).CalibratedPosition + "\n";
                //        }
                //    }
                //    textBoxConsole.Text = output;
                //}));
            }
        }

        public void ResetInterfaceState()
        {
            oscilloX.ResetGraph();
            oscilloY.ResetGraph();
            oscilloTheta.ResetGraph();
            //oscilloM1.ResetGraph();
            //oscilloM2.ResetGraph();
            //oscilloM3.ResetGraph();
            //oscilloM4.ResetGraph();
        }

         public void UpdateSpeedPolarOdometryOnInterface(object sender, PolarSpeedEventArgs e)
        {
            oscilloX.AddPointToLine(1, e.timeStampMs / 1000.0, e.Vx);
            oscilloY.AddPointToLine(1, e.timeStampMs / 1000.0, e.Vy);
            oscilloTheta.AddPointToLine(1, e.timeStampMs / 1000.0, e.Vtheta);
            currentTime = e.timeStampMs / 1000.0;

            asservSpeedDisplay.UpdatePolarOdometrySpeed(e.Vx, e.Vy, e.Vtheta);
        }
        public void UpdateSpeedIndependantOdometryOnInterface(object sender, IndependantSpeedEventArgs e)
        {
            asservSpeedDisplay.UpdateIndependantOdometrySpeed(e.VitesseMoteur1, e.VitesseMoteur2, e.VitesseMoteur3, e.VitesseMoteur4);
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

        public void UpdatePolarSpeedConsigneOnGraph(object sender, PolarSpeedArgs e)
        {
            oscilloX.AddPointToLine(0, currentTime, e.Vx);
            oscilloY.AddPointToLine(0, currentTime, e.Vy);
            oscilloTheta.AddPointToLine(0, currentTime, e.Vtheta);

            //asservSpeedDisplay.UpdateConsigneValues(e.Vx, e.Vy, e.Vtheta);
        }

        public void UpdateIndependantSpeedConsigneOnGraph(object sender, IndependantSpeedEventArgs e)
        {
            //oscilloM1.AddPointToLine(4, e.timeStampMs / 1000.0, e.VitesseMoteur1);
            //oscilloM2.AddPointToLine(4, e.timeStampMs / 1000.0, e.VitesseMoteur2);
            //oscilloM3.AddPointToLine(4, e.timeStampMs / 1000.0, e.VitesseMoteur3);
            //oscilloM4.AddPointToLine(4, e.timeStampMs / 1000.0, e.VitesseMoteur4);
        }

        //public void UpdateAuxiliarySpeedConsigneOnGraph(object sender, AuxiliaryMotorsVitesseDataEventArgs e)
        //{
        //    oscilloM5.AddPointToLine(4, e.timeStampMS / 1000.0, e.vitesseMotor5);
        //    oscilloM6.AddPointToLine(4, e.timeStampMS / 1000.0, e.vitesseMotor6);
        //    oscilloM7.AddPointToLine(4, e.timeStampMS / 1000.0, e.vitesseMotor7);
        //}

        public void UpdateMotorsCurrentsOnGraph(object sender, MotorsCurrentsEventArgs e)
        {
            //oscilloM1.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor1);
            //oscilloM2.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor2);
            //oscilloM3.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor3);
            //oscilloM4.AddPointToLine(1, e.timeStampMS / 1000.0, e.motor4);
        }

        //public void UpdateMotorsSpeedsOnGraph(object sender, MotorsVitesseDataEventArgs e)
        //{
        //    oscilloM1.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor1);
        //    oscilloM2.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor2);
        //    oscilloM3.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor3);
        //    oscilloM4.AddPointToLine(0, e.timeStampMS / 1000.0, e.vitesseMotor4);
        //}

        public void UpdateMotorsPositionOnGraph(object sender, MotorsPositionDataEventArgs e)
        {
            //oscilloM1.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor1);
            //oscilloM2.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor2);
            //oscilloM3.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor3);
            //oscilloM4.AddPointToLine(2, e.timeStampMS / 1000.0, e.motor4);
        }

        public void UpdateMotorsEncRawDataOnGraph(object sender, EncodersRawDataEventArgs e)
        {
            //oscilloM1.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor1);
            //oscilloM2.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor2);
            //oscilloM3.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor3);
            //oscilloM4.AddPointToLine(3, e.timeStampMS / 1000.0, e.motor4);
        }

        public void UpdateSpeedPolarPidErrorCorrectionConsigneDataOnGraph(object sender, Polar4WheelsPidErrorCorrectionConsigneDataArgs e)
        {
            asservSpeedDisplay.UpdatePolarSpeedErrorValues(e.xErreur, e.yErreur, e.thetaErreur);
            asservSpeedDisplay.UpdatePolarSpeedCommandValues(e.xCorrection, e.yCorrection, e.thetaCorrection);
            asservSpeedDisplay.UpdatePolarSpeedConsigneValues(e.xConsigneFromRobot, e.yConsigneFromRobot, e.thetaConsigneFromRobot);

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
        public void UpdateSpeedIndependantPidErrorCorrectionConsigneDataOnGraph(object sender, Independant4WheelsPidErrorCorrectionConsigneDataArgs e)
        {
            asservSpeedDisplay.UpdateIndependantSpeedErrorValues(e.M1Erreur, e.M2Erreur, e.M3Erreur, e.M4Erreur);
            asservSpeedDisplay.UpdateIndependantSpeedCommandValues(e.M1Correction, e.M2Correction, e.M3Correction, e.M4Correction);
            asservSpeedDisplay.UpdateIndependantSpeedConsigneValues(e.M1ConsigneFromRobot, e.M2ConsigneFromRobot, e.M3ConsigneFromRobot, e.M4ConsigneFromRobot);
        }

        public void UpdateSpeedPolarPidCorrectionData(object sender, PolarPidCorrectionArgs e)
        {
            asservSpeedDisplay.UpdatePolarSpeedCorrectionValues(e.CorrPx, e.CorrPy, e.CorrPTheta,
                e.CorrIx, e.CorrIy, e.CorrITheta,
                e.CorrDx, e.CorrDy, e.CorrDTheta);
        }

        public void UpdateSpeedIndependantPidCorrectionData(object sender, IndependantPidCorrectionArgs e)
        {
            asservSpeedDisplay.UpdateIndependantSpeedCorrectionValues(e.CorrPM1, e.CorrPM2, e.CorrPM3, e.CorrPM4,
                e.CorrIM1, e.CorrIM2, e.CorrIM3, e.CorrIM4,
                e.CorrDM1, e.CorrDM2, e.CorrDM3, e.CorrDM4);
        }
        public void UpdatePowerMonitoringValues(object sender, PowerMonitoringValuesEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            LabelBattCommandVoltage.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                LabelBattCommandVoltage.Content = "BATT COMMAND Voltage : " + e.battCMDVoltage.ToString("F2") + "V" + "  Current : " + e.battCMDCurrent.ToString("F2") + "A" ;
            }));


            LabelBattPowerVoltage.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                LabelBattPowerVoltage.Content = "BATT POWER Voltage : " + e.battPWRVoltage.ToString("F2") + "V" + "  Current : " + e.battPWRCurrent.ToString("F2") + "A";
            }));

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

        private void ButtonDisableMotors_Click(object sender, RoutedEventArgs e)
        {
            if (currentMotorActivation == true)
            {
                OnEnableDisableMotorsFromInterface(false);
            }
            else
            {                
                OnEnableDisableMotorsFromInterface(true);
            }
            ResetInterfaceState();
        }


        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        bool currentMotorActivation = false;
        public void ActualizeEnableDisableMotorsButton(object sender, BoolEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            //Utilisation ici d'une methode anonyme

            currentMotorActivation = e.value;
            ButtonDisableMotors.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (currentMotorActivation)
                {
                    LabelMotorState.Content = "Motor State : Enabled";
                }
                else
                {
                    LabelMotorState.Content = "Motor State : Disabled";
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
        public void UpdateAsservissementMode(object sender, AsservissementModeEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.

            currentAsservissementMode = e.mode;
            ButtonChangeAsservissementMode.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                switch(currentAsservissementMode)
                {
                    case AsservissementMode.Off4Wheels:
                        LabelAsservMode.Content = "Asserv Mode :  Disabled";
                        asservSpeedDisplay.SetAsservissementMode(currentAsservissementMode);
                        break;
                    case AsservissementMode.Independant4Wheels:
                        LabelAsservMode.Content = "Asserv Mode : 4 Roues independant";
                        asservSpeedDisplay.SetAsservissementMode(currentAsservissementMode);
                        break;
                    case AsservissementMode.Polar4Wheels:
                        LabelAsservMode.Content = "Asserv Mode : 4 roues polaire";
                        asservSpeedDisplay.SetAsservissementMode(currentAsservissementMode);
                        break;
                    case AsservissementMode.Independant2Wheels:
                        LabelAsservMode.Content = "Asserv Mode : 2 Roues independant";
                        asservSpeedDisplay.SetAsservissementMode(currentAsservissementMode);
                        break;
                    case AsservissementMode.Polar2Wheels:
                        LabelAsservMode.Content = "Asserv Mode : 2 roues polaire";
                        asservSpeedDisplay.SetAsservissementMode(currentAsservissementMode);
                        break;
                }
            }));
        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void ActualizeEnableMotorCurrentCheckBox(object sender, BoolEventArgs e)
        {
        }


        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void ActualizeEnableAsservissementDebugDataCheckBox(object sender, BoolEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            CheckBoxEnableAsservissementDebugData.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            {
            }));
        }


        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void ActualizEnablePowerMonitoringCheckBox(object sender, BoolEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.

        }

        //Methode appelée sur evenement (event) provenant du port Serie.
        //Cette methode est donc appelée depuis le thread du port Serie. Ce qui peut poser des problemes d'acces inter-thread
        public void AppendConsole(object sender, StringEventArgs e)
        {
            //La solution consiste a passer par un delegué qui executera l'action a effectuer depuis le thread concerné.
            //Ici, l'action a effectuer est la modification d'un bouton. Ce bouton est un objet UI, et donc l'action doit etre executée depuis un thread UI.
            //Sachant que chaque objet UI (d'interface graphique) dispose d'un dispatcher qui permet d'executer un delegué (une methode) depuis son propre thread.
            //La difference entre un Invoke et un beginInvoke est le fait que le Invoke attend la fin de l'execution de l'action avant de sortir.
            //textBoxConsole.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
            //{
            //    textBoxConsole.Text += e.value+'\n';
            //    if (textBoxConsole.Text.Length >= 2000)
            //    {
            //        textBoxConsole.Text = textBoxConsole.Text.Remove(0, 2000);
            //    }
            //    scrollViewerTextBoxConsole.ScrollToEnd();
            //}));
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
        private void WorldMapDisplay_OnCtrlClickOnHeatMapEvent(object sender, PositionArgs e)
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
        public event EventHandler<ByteEventArgs> OnSetAsservissementModeFromInterfaceGeneratedEvent;
        public virtual void OnSetAsservissementModeFromInterface(byte val)
        {
            var handler = OnSetAsservissementModeFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new ByteEventArgs { Value = val });
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

        public event EventHandler<BoolEventArgs> OnEnableDisableLoggingEvent;
        public virtual void OnEnableDisableLogging(bool val)
        {
            var handler = OnEnableDisableLoggingEvent;
            if (handler != null)
            {
                handler(this, new BoolEventArgs { value = val });
            }
        }

        public event EventHandler<BoolEventArgs> OnEnableDisableLogReplayEvent;
        public virtual void OnEnableDisableLogReplay(bool val)
        {
            var handler = OnEnableDisableLogReplayEvent;
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
        public event EventHandler<BoolEventArgs> OnEnableSpeedPIDEnableDebugInternalFromInterfaceGeneratedEvent;
        public virtual void OnEnableSpeedPIDEnableDebugInternalFromInterface(bool val)
        {
            OnEnableSpeedPIDEnableDebugInternalFromInterfaceGeneratedEvent?.Invoke(this, new BoolEventArgs { value = val });
        }

        public event EventHandler<BoolEventArgs> OnEnablePowerMonitoringDataFromInterfaceGeneratedEvent;
        public virtual void OnEnablePowerMonitoringDataFromInterface(bool val)
        {
            OnEnablePowerMonitoringDataFromInterfaceGeneratedEvent?.Invoke(this, new BoolEventArgs { value = val });
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<BoolEventArgs> OnEnableSpeedPIDEnableDebugErrorCorrectionConsigneFromInterfaceEvent;
        public virtual void OnEnableSpeedPIDEnableDebugErrorCorrectionConsigneFromInterface(bool val)
        {
            OnEnableSpeedPIDEnableDebugErrorCorrectionConsigneFromInterfaceEvent?.Invoke(this, new BoolEventArgs { value = val });
        }

        //public delegate void EnableDisableControlManetteEventHandler(object sender, BoolEventArgs e);
        public event EventHandler<PolarPIDSetupArgs> OnSetRobotPIDFromInterfaceGeneratedEvent;
        public virtual void OnSetRobotPIDFromInterface(double px, double ix, double dx, double py, double iy, double dy, double ptheta, double itheta, double dtheta)
        {
            var handler = OnSetRobotPIDFromInterfaceGeneratedEvent;
            if (handler != null)
            {
                handler(this, new PolarPIDSetupArgs { P_x = px, I_x=ix, D_x=dx, P_y=py, I_y=iy, D_y=dy, P_theta=ptheta, I_theta=itheta, D_theta=dtheta });
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

        AsservissementMode currentAsservissementMode = AsservissementMode.Off4Wheels;
        private void ButtonEnableAsservissement_Click(object sender, RoutedEventArgs e)
        {
            switch(currentAsservissementMode)
            {
                case AsservissementMode.Off4Wheels:
                    OnSetAsservissementModeFromInterface((byte)AsservissementMode.Polar4Wheels);
                    break;
                case AsservissementMode.Polar4Wheels:
                    OnSetAsservissementModeFromInterface((byte)AsservissementMode.Independant4Wheels);
                    break;
                case AsservissementMode.Independant4Wheels:
                    OnSetAsservissementModeFromInterface((byte)AsservissementMode.Off4Wheels);
                    break;
            }
        }
        private void CheckBoxEnableAsservissementDebugData_Checked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxEnableAsservissementDebugData.IsChecked ?? false)
            {
                OnEnableSpeedPIDEnableDebugInternalFromInterface(true);
                OnEnableSpeedPIDEnableDebugErrorCorrectionConsigneFromInterface(true);
            }
            else
            {
                OnEnableSpeedPIDEnableDebugInternalFromInterface(false);
                OnEnableSpeedPIDEnableDebugErrorCorrectionConsigneFromInterface(false);
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
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress; //Ici on est en local, pas de transmission, on remplis pour rien.
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot1;
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
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot1;
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
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot1;
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
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot1;
            msg.posX = 0;
            msg.posY = -1;
            msg.posTheta = -Math.PI/2;
            OnRefereeBoxReceivedCommand(msg);
        }

        private void Button_m1_0_Click(object sender, RoutedEventArgs e)
        {
            RefBoxMessage msg = new RefBoxMessage();
            msg.command = RefBoxCommand.GOTO;
            msg.targetTeam = TeamIpAddress;
            msg.robotID = (int)TeamId.Team1 + (int)RobotId.Robot1;
            msg.posX = -1;
            msg.posY = 0;
            msg.posTheta = Math.PI;
            OnRefereeBoxReceivedCommand(msg);
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
        
        bool currentXBoxActivation = false;
        private void ButtonXBoxController_Click(object sender, RoutedEventArgs e)
        {
            currentXBoxActivation = !currentXBoxActivation;
            if (currentXBoxActivation)
            {
                OnEnableDisableControlManetteFromInterface(true);
                LabelXBoxControllerMode.Content = "XBox Pad : Enabled";
            }
            else
            {
                OnEnableDisableControlManetteFromInterface(false);
                LabelXBoxControllerMode.Content = "XBox Pad : Disabled";
            }
        }

        bool currentLoggingActivationState = false;
        private void ButtonLogging_Click(object sender, RoutedEventArgs e)
        {
            currentLoggingActivationState = !currentLoggingActivationState;            
            if(currentLoggingActivationState)
            {
                OnEnableDisableLogging(true);
                LabelLoggingState.Content = "Logging Started";
            }
            else
            {
                OnEnableDisableLogging(false);
                LabelLoggingState.Content = "Logging Stopped";
            }
        }

        bool logReplayActivationState = false;
        private void ButtonLogReplay_Click(object sender, RoutedEventArgs e)
        {
            logReplayActivationState = !logReplayActivationState;
            if (logReplayActivationState)
            {
                OnEnableDisableLogReplay(true);
                LabelLogReplayState.Content = "Replay";
            }
            else
            {
                OnEnableDisableLogReplay(false);
                LabelLogReplayState.Content = "Real Data";
            }
        }

        //private void CheckBoxEnablePowerMonitoringData_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (CheckBoxEnablePowerMonitoringData.IsChecked ?? false)
        //    {
        //        OnEnablePowerMonitoringDataFromInterface(true);
        //    }
        //    else
        //    {
        //        OnEnablePowerMonitoringDataFromInterface(false);
        //    }
        //}
    }
}
