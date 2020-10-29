using EventArgsLibrary;
using HerkulexManagerNS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using static HerkulexManagerNS.HerkulexEventArgs;

namespace StrategyManager
{
    public class TaskDistributeur
    {
        Thread TaskThread;
        TaskPhareStates state = TaskPhareStates.Attente;
        public bool isFinished = false;
        StrategyManager_Eurobot parentManager;
        Stopwatch sw = new Stopwatch();
        enum TaskPhareStates
        {
            Init,
            Attente,
            DeplacementCentral,
            DeplacementCentralEnCours,
            Prise,
            PriseEnCours,
            DeplacementRecentrage,
            DeplacementRecentrageEnCours,
            Depose, 
            DeposeEnCours,
            Finished,
        }

        public TaskDistributeur(StrategyManager_Eurobot manager)
        {
            parentManager = manager;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        private bool isRunning = false;
        private Dictionary<ServoId, int> servoPositionsRequested;
        private PointD ptPrise;
        private PointD ptPose;
        double anglePose;
        double anglePrise;

        private double currentPGauche;
        private double currentPDroit;
        private double currentPCentre;

        double rayonPrise = 0.24;

        #region ENUMS
        enum TaskBrasPositionsInit
        {
            BrasGauche = 772,
            BrasDroit = 772,
            BrasCentral = 772,
        }
        enum TaskBrasPositionsStockageEnHauteur
        {
            BrasGauche = 697,
            BrasDroit = 697,
            BrasCentral = 697,
        }

        enum TaskBrasPositionsDepose
        {
            BrasGauche = 538,
            BrasDroit = 538,
            BrasCentral = 538,
        }

        enum TaskBrasPositionsPrehensionGobelet
        {
            BrasGauche = 520,
            BrasDroit = 520,
            BrasCentral = 520,
        }
        #endregion
        private void StopSw()
        {
            sw.Stop();
            sw.Reset();
        }
        private void StartSw()
        {
            sw.Reset();
            sw.Start();
        }
        public void Init()
        {
            state = TaskPhareStates.Init;
            isFinished = false;
            StopSw();
        }
        public void Start(Location locPrise, Location locPose)
        {
            ptPrise = new PointD(locPrise.X, locPrise.Y);
            anglePrise = locPrise.Theta;
            ptPose = new PointD(locPose.X, locPose.Y);
            anglePose = locPose.Theta;
            state = TaskPhareStates.DeplacementCentral;
            isFinished = false;
            StopSw();
        }
        public void Pause()
        {
            state = TaskPhareStates.Attente;
            isFinished = false;
            StopSw();
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskPhareStates.Init:
                        state = TaskPhareStates.Attente;
                        break;
                    case TaskPhareStates.Attente:
                        break;
                    /**********************Deplacement prise 1**************************************/
                    case TaskPhareStates.DeplacementCentral:
                        //Servo stuff
                        OnPilotageVentouse(5, -20);
                        OnPilotageVentouse(6, -20);
                        OnPilotageVentouse(7, -20);
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsStockageEnHauteur.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsStockageEnHauteur.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsStockageEnHauteur.BrasCentral);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        //Deplacement
                        if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-0.8, 0);
                            parentManager.robotOrientation = Math.PI;
                        }
                        else if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(0.8, 0);
                            parentManager.robotOrientation = 0;
                        }
                        state = TaskPhareStates.DeplacementCentralEnCours;
                        StartSw();
                        break;
                    case TaskPhareStates.DeplacementCentralEnCours:
                        if (parentManager.isDeplacementFinished)
                        {
                                state = TaskPhareStates.Prise;
                                StopSw();
                        }
                        break;
                        /**********************Préparation des servos**************************************/
                    case TaskPhareStates.Prise:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPrehensionGobelet.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsPrehensionGobelet.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsPrehensionGobelet.BrasCentral);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        //Deplacement
                        parentManager.robotDestination = new PointD(ptPrise.X-rayonPrise*Math.Cos(anglePrise), ptPrise.Y - rayonPrise * Math.Sin(anglePrise));
                        parentManager.robotOrientation = anglePrise;
                        state = TaskPhareStates.PriseEnCours;
                        StartSw();
                        break;
                    case TaskPhareStates.PriseEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                        {
                            StopSw();
                            state = TaskPhareStates.DeplacementRecentrage;
                        }
                        break;

                    /**********************Deplacement prise 1**************************************/
                    case TaskPhareStates.DeplacementRecentrage:
                        servoPositionsRequested = new Dictionary< ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsStockageEnHauteur.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsStockageEnHauteur.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsStockageEnHauteur.BrasCentral);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        //Deplacement
                        if(parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-0.8, 0);
                            parentManager.robotOrientation = Math.PI;
                        }
                        else if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(0.8, 0);
                            parentManager.robotOrientation = 0;
                        }
                        state = TaskPhareStates.DeplacementRecentrageEnCours;
                        StartSw();
                        break;

                    case TaskPhareStates.DeplacementRecentrageEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                        {
                            state = TaskPhareStates.Depose;
                            StopSw();
                        }
                        break;
                    /************************************************************/
                    case TaskPhareStates.Depose:
                        parentManager.robotDestination = new PointD(ptPose.X - rayonPrise * Math.Cos(anglePose), ptPose.Y - rayonPrise * Math.Sin(anglePose)); ;
                        parentManager.robotOrientation = anglePose;
                        state = TaskPhareStates.DeposeEnCours;
                        break;
                    case TaskPhareStates.DeposeEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                            if (parentManager.isDeplacementFinished)
                            {
                                OnPilotageVentouse(5, -20);
                                OnPilotageVentouse(6, -20);
                                OnPilotageVentouse(7, -20);

                                state = TaskPhareStates.Finished;
                            }
                        break;
                    case TaskPhareStates.Finished:
                        isFinished = true;
                        state = TaskPhareStates.Attente;
                        break;
                }
                Thread.Sleep(10);
            }
        }
        public event EventHandler<SpeedConsigneToMotorArgs> OnPilotageVentouseEvent;
        public virtual void OnPilotageVentouse(byte motorNumber, double vitesse)
        {
            OnPilotageVentouseEvent?.Invoke(this, new SpeedConsigneToMotorArgs { MotorNumber = motorNumber, V = vitesse });
        }
        public void OnMotorCurrentReceive(object sender, MotorsCurrentsEventArgs e)
        {
            currentPGauche = e.motor5;
            currentPDroit = e.motor7;
            currentPCentre = e.motor6;
        }

        public event EventHandler<HerkulexPositionsArgs> OnHerkulexPositionRequestEvent;
        public virtual void OnHerkulexPositionRequest(Dictionary<ServoId, int> positionDictionary)
        {
            OnHerkulexPositionRequestEvent?.Invoke(this, new HerkulexPositionsArgs { servoPositions = positionDictionary });
        }
    }
}
