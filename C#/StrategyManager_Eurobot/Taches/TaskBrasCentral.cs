using EventArgsLibrary;
using HerkulexManagerNS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static HerkulexManagerNS.HerkulexEventArgs;

namespace StrategyManager
{
    public class TaskBrasCentral
    {
        Thread TaskThread;
        TaskBrasState state = TaskBrasState.Attente;
        public bool isFineshed = false;
        StrategyManager_Eurobot ParentStrategy;
        Stopwatch sw = new Stopwatch();

        public bool isServosDeplacementFinished
        {
            get
            {
                if(ParentStrategy.HerkulexServos.ContainsKey(ServoId.BrasCentralEpaule) &&
                    ParentStrategy.HerkulexServos.ContainsKey(ServoId.BrasCentralEpaule) &&
                    ParentStrategy.HerkulexServos.ContainsKey(ServoId.BrasCentralEpaule))
                    if (ParentStrategy.HerkulexServos[ServoId.BrasCentralEpaule].isDeplacementFinished &&
                    ParentStrategy.HerkulexServos[ServoId.BrasCentralEpaule].isDeplacementFinished &&
                    ParentStrategy.HerkulexServos[ServoId.BrasCentralEpaule].isDeplacementFinished)
                        return true;
                return false;
            }
            set
            {

            }
        }
        enum TaskBrasState
        {
            Init,
            Attente,
            AttenteGobelet,
            AttenteGobeletEnCours,
            PrehensionGobelet,
            PrehensionGobeletEnCours,
            PreparationRangementSurSupport,
            PreparationRangementSurSupportEnCours,
            StockageSurSupport,
            StockageSurSupportEnCours,
            PreparationStockageEnHauteur,
            PreparationStockageEnHauteurEnCours,
            StockageEnHauteur,
            StockageEnHauteurEnCours,
            StockageEnHauteur2,
            StockageEnHauteurEnCours2,
            Depose,
            DeposeEnCours,
            Finished
        }
        #region Enums position
        enum TaskBrasPositionsInit
        {
            BrasCentralEpaule = 467,
            BrasCentralCoude = 150,
            BrasCentralPoignet = 478,
        }
        enum TaskBrasPositionsAttenteGobelet
        {
            BrasCentralEpaule = 507,
            BrasCentralCoude = 164,
            BrasCentralPoignet = 453,
        }
        enum TaskBrasPositionsPrehensionGobelet
        {
            BrasCentralEpaule = 556,
            BrasCentralCoude = 218,
            BrasCentralPoignet = 455,
        }
        enum TaskBrasPositionsStockageEnHauteur
        {
            BrasCentralEpaule = 891,
            BrasCentralEpaule_1 = 461,
            BrasCentralCoude = 831,
            BrasCentralPoignet = 345,
        }
        enum TaskBrasPositionsDepose
        {
            BrasCentralEpaule = 467,
            BrasCentralCoude = 150,
            BrasCentralPoignet = 478,
        }
        #endregion


        public TaskBrasCentral(StrategyManager_Eurobot parentStrategy)
        {
            ParentStrategy = parentStrategy;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
            sw.Stop();
            sw.Reset();
        }

        Dictionary<ServoId, int> servoPositionsRequested = new Dictionary<ServoId, int>();
        public bool isSupportCentralFull { get; private set; } = false;
        private double ventouseBrasCentralCurrent = -1;
        private bool isRunning = false;

        #region Methode pilotage class
        public void Init()
        {
            state = TaskBrasState.Init;
            OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 0);
            isSupportCentralFull = false;
            isFineshed = false;
            sw.Stop();
            sw.Reset();
        }

        public void StartPrehension()
        {
            state = TaskBrasState.AttenteGobelet;
            isFineshed = false;
            sw.Stop();
            sw.Reset();
        }

        public void StartDepose()
        {
            state = TaskBrasState.Depose;
            isFineshed = false;
            sw.Stop();
            sw.Reset();
        }
        #endregion

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskBrasState.Init:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsInit.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsInit.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsInit.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.Attente;
                        break;
                    case TaskBrasState.Attente:
                        //On attends a l'infini que le strategy manager nous sortes de cet etat
                        break;
                    case TaskBrasState.AttenteGobelet:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 50);
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsAttenteGobelet.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsAttenteGobelet.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsAttenteGobelet.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.AttenteGobeletEnCours;
                        sw.Reset();
                        sw.Start();
                        break;
                    case TaskBrasState.AttenteGobeletEnCours:
                        if (isServosDeplacementFinished)
                        {
                            if (sw.ElapsedMilliseconds > 500)
                            {
                                state = TaskBrasState.PrehensionGobelet;
                                sw.Stop();
                                sw.Reset();
                            }
                        }
                        break;
                    case TaskBrasState.PrehensionGobelet:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsPrehensionGobelet.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsPrehensionGobelet.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsPrehensionGobelet.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.PrehensionGobeletEnCours;
                        sw.Reset();
                        sw.Start();
                        break;
                    case TaskBrasState.PrehensionGobeletEnCours:
                        if (isServosDeplacementFinished)
                        {
                            if (ventouseBrasCentralCurrent > 0.6)//mesuré 0.6A à vide et 1.35A en charge
                            {
                                if (sw.ElapsedMilliseconds > 500)
                                {
                                    state = TaskBrasState.PreparationStockageEnHauteur;
                                    sw.Stop();
                                    sw.Reset();
                                }
                            }
                            else
                            {
                                //Si on a rien on retournes dans la position d'attente d'un gobelet
                                state = TaskBrasState.AttenteGobelet;
                            }
                        }
                        break;
                    case TaskBrasState.PreparationStockageEnHauteur:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsStockageEnHauteur.BrasCentralEpaule_1);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.PreparationStockageEnHauteurEnCours;
                        sw.Reset();
                        sw.Start();
                        break;
                    case TaskBrasState.PreparationStockageEnHauteurEnCours:
                        if (isServosDeplacementFinished)
                        {
                            if (sw.ElapsedMilliseconds > 150)
                            {
                                state = TaskBrasState.StockageEnHauteur;
                                sw.Stop();
                                sw.Reset();
                            }
                        }
                        break;
                    case TaskBrasState.StockageEnHauteur:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        sw.Stop();
                        sw.Reset();
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsStockageEnHauteur.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsStockageEnHauteur.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        sw.Reset();
                        sw.Start();
                        state = TaskBrasState.StockageEnHauteurEnCours;
                        break;
                    case TaskBrasState.StockageEnHauteurEnCours:
                        if (isServosDeplacementFinished)
                        {
                            if (sw.ElapsedMilliseconds > 150)
                            {
                                sw.Stop();
                                sw.Reset();
                                state = TaskBrasState.StockageEnHauteur2;
                            }
                        }
                        break;
                    case TaskBrasState.StockageEnHauteur2:
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsStockageEnHauteur.BrasCentralEpaule);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.StockageEnHauteurEnCours2;
                        sw.Reset();
                        sw.Start();
                        break;
                    case TaskBrasState.StockageEnHauteurEnCours2:
                        if (isServosDeplacementFinished)
                        {
                            if (sw.ElapsedMilliseconds > 150)
                            {
                                sw.Stop();
                                sw.Reset();
                                state = TaskBrasState.Finished;
                            }
                        }
                        break;
                    case TaskBrasState.Depose:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsDepose.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsDepose.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsDepose.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.DeposeEnCours;
                        break;
                    case TaskBrasState.DeposeEnCours:
                        if (isServosDeplacementFinished)
                            state = TaskBrasState.Finished;
                        break;
                    case TaskBrasState.Finished:
                        isFineshed = true;
                        state = TaskBrasState.Attente;
                        break;
                    default:
                        break;
                }
                Thread.Sleep(10);
            }
        }

        public event EventHandler<HerkulexPositionsArgs> OnHerkulexPositionRequestEvent;
        public virtual void OnHerkulexPositionRequest(Dictionary<ServoId, int> positionDictionary)
        {
            OnHerkulexPositionRequestEvent?.Invoke(this, new HerkulexPositionsArgs { servoPositions = positionDictionary });
        }

        public event EventHandler<SpeedConsigneToMotorArgs> OnPilotageVentouseEvent;
        public virtual void OnPilotageVentouse(byte motorNumber, double vitesse)
        {
            OnPilotageVentouseEvent?.Invoke(this, new SpeedConsigneToMotorArgs { MotorNumber = motorNumber, V = vitesse });
        }

        public void OnMotorCurrentReceive(object sender, MotorsCurrentsEventArgs e)
        {
            //Motor 7 is bras central pump
            ventouseBrasCentralCurrent = e.motor7;
        }

        public void OnServoPositionReceived(object sender, HerkulexServoInformationArgs e)
        {
        }
    }
}
