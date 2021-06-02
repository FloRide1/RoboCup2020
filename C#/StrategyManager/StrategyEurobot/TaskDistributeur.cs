using Constants;
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

namespace StrategyManagerProjetEtudiantNS
{
    public class TaskDistributeur
    {
        Thread TaskThread;
        public TaskDistributeurStates state = TaskDistributeurStates.Attente;
        public bool isFinished = false;
        StrategyEurobot2021 parentManager;
        Stopwatch sw = new Stopwatch();
        public enum TaskDistributeurStates
        {
            Init,
            Attente,
            DeplacementCentral,
            DeplacementCentralEnCours,
            PositionnementAvantPrise,
            PositionnementAvantPriseEnCours,
            Prise,
            PriseEnCours,
            DeplacementRecentrage,
            DeplacementRecentrageEnCours,
            Depose, 
            DeposeEnCours,
            Finished,
        }

        public TaskDistributeur(StrategyEurobot2021 manager)
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

        double rayonPrise = 0.12;

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
            BrasGauche = 540,
            BrasDroit = 540,
            BrasCentral = 540,
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
            state = TaskDistributeurStates.Init;
            isFinished = false;
            StopSw();
        }
        public void Start(Location locPrise, Location locPose)
        {
            ptPrise = new PointD(locPrise.X, locPrise.Y);
            anglePrise = locPrise.Theta;
            ptPose = new PointD(locPose.X, locPose.Y);
            anglePose = locPose.Theta;
            state = TaskDistributeurStates.DeplacementCentral;
            isFinished = false;
            StopSw();
        }
        public void Pause()
        {
            state = TaskDistributeurStates.Attente;
            isFinished = false;
            StopSw();
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskDistributeurStates.Init:
                        state = TaskDistributeurStates.Attente;
                        break;
                    case TaskDistributeurStates.Attente:
                        break;
                    /**********************Deplacement prise 1**************************************/
                    case TaskDistributeurStates.DeplacementCentral:
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
                        state = TaskDistributeurStates.DeplacementCentralEnCours;
                        StartSw();
                        break;
                    case TaskDistributeurStates.DeplacementCentralEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                        {
                                state = TaskDistributeurStates.PositionnementAvantPrise;
                                StopSw();
                        }
                        break;
                    /**********************Préparation des servos**************************************/
                    /**********************Deplacement prise 1**************************************/
                    case TaskDistributeurStates.PositionnementAvantPrise:
                        //Servo stuff
                        OnPilotageVentouse(5, 50);
                        OnPilotageVentouse(6, 50);
                        OnPilotageVentouse(7, 50);
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPrehensionGobelet.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsPrehensionGobelet.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsPrehensionGobelet.BrasCentral);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        //Deplacement
                        parentManager.robotDestination = new PointD(ptPrise.X - 0.4 * Math.Cos(anglePrise), ptPrise.Y - 0.4 * Math.Sin(anglePrise));
                        parentManager.robotOrientation = anglePrise;
                        
                        
                        state = TaskDistributeurStates.PositionnementAvantPriseEnCours;
                        StartSw();
                        break;
                    case TaskDistributeurStates.PositionnementAvantPriseEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                        {
                            state = TaskDistributeurStates.Prise;
                            StopSw();
                        }
                        break;
                    /**********************Préparation des servos**************************************/

                    case TaskDistributeurStates.Prise:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPrehensionGobelet.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsPrehensionGobelet.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsPrehensionGobelet.BrasCentral);
                        OnPilotageVentouse(5, 100);
                        OnPilotageVentouse(6, 100);
                        OnPilotageVentouse(7, 100);

                        OnHerkulexPositionRequest(servoPositionsRequested);
                        //Deplacement
                        parentManager.robotDestination = new PointD(ptPrise.X-rayonPrise*Math.Cos(anglePrise), ptPrise.Y - rayonPrise * Math.Sin(anglePrise));
                        parentManager.robotOrientation = anglePrise;
                        state = TaskDistributeurStates.PriseEnCours;
                        StartSw();
                        break;
                    case TaskDistributeurStates.PriseEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                        {
                            StopSw();
                            state = TaskDistributeurStates.DeplacementRecentrage;
                        }
                        break;

                    /**********************Deplacement prise 1**************************************/
                    case TaskDistributeurStates.DeplacementRecentrage:
                        servoPositionsRequested = new Dictionary< ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsStockageEnHauteur.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsStockageEnHauteur.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsStockageEnHauteur.BrasCentral);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        //Deplacement
                        if(parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-0.2, 0);
                            parentManager.robotOrientation = Math.PI;
                        }
                        else if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(0.2, 0);
                            parentManager.robotOrientation = 0;
                        }
                        state = TaskDistributeurStates.DeplacementRecentrageEnCours;
                        StartSw();
                        break;

                    case TaskDistributeurStates.DeplacementRecentrageEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                        {
                            state = TaskDistributeurStates.Depose;
                            StopSw();
                        }
                        break;
                    /************************************************************/
                    case TaskDistributeurStates.Depose:
                        parentManager.robotDestination = new PointD(ptPose.X - rayonPrise * Math.Cos(anglePose), ptPose.Y - rayonPrise * Math.Sin(anglePose)); ;
                        parentManager.robotOrientation = anglePose;
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPrehensionGobelet.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsPrehensionGobelet.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsPrehensionGobelet.BrasCentral);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskDistributeurStates.DeposeEnCours;
                        StartSw();
                        break;
                    case TaskDistributeurStates.DeposeEnCours:
                        if (sw.ElapsedMilliseconds > 5000 || parentManager.isDeplacementFinished)
                            if (parentManager.isDeplacementFinished)
                            {
                                OnPilotageVentouse(5, 20);
                                OnPilotageVentouse(6, 20);
                                OnPilotageVentouse(7, 20);
                                StopSw();
                                state = TaskDistributeurStates.Finished;
                            }
                        break;
                    case TaskDistributeurStates.Finished:
                        isFinished = true;
                        state = TaskDistributeurStates.Attente;
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
