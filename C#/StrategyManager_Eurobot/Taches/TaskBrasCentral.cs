using EventArgsLibrary;
using HerkulexManagerNS;
using System;
using System.Collections.Generic;
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
        enum TaskBrasState
        {
            Init,
            Attente,
            AttenteGobelet,            
            PrehensionGobelet,
            PreparationRangementSurSupport,
            StockageSurSupport,
            StockageEnHauteur,
            Depose,
            Finished
        }

        enum TaskBrasPositionsInit
        {
            BrasCentralEpaule = 467,
            BrasCentralCoude = 150,
            BrasCentralPoignet = 478,
        }
        enum TaskBrasPositionsAttenteGobelet
        {
            BrasCentralEpaule = 467,
            BrasCentralCoude = 150,
            BrasCentralPoignet = 478,
        }
        enum TaskBrasPositionsPrehensionGobelet
        {
            BrasCentralEpaule = 537,
            BrasCentralCoude = 226,
            BrasCentralPoignet = 478,
        }
        enum TaskBrasPositionsPreparationRangementSurSupport
        {
            BrasCentralEpaule = 469,
            BrasCentralCoude = 326,
            BrasCentralPoignet = 887,
        }
        enum TaskBrasPositionsStockageSurSupport
        {
            BrasCentralEpaule = 470,
            BrasCentralCoude = 222,
            BrasCentralPoignet = 789,
        }
        enum TaskBrasPositionsStockageEnHauteur
        {
            BrasCentralEpaule = 891,
            BrasCentralCoude = 831,
            BrasCentralPoignet = 345,
        }
        enum TaskBrasPositionsDepose
        {
            BrasCentralEpaule = 467,
            BrasCentralCoude = 150,
            BrasCentralPoignet = 478,
        }

        public TaskBrasCentral()
        {
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        Dictionary<ServoId, int> servoPositionsRequested = new Dictionary<ServoId, int>();
        public bool isSupportCentralFull { get; private set; } = false;
        private double ventouseBrasCentralCurrent = -1;
        private bool isRunning = false;

        public void Init()
        {
            state = TaskBrasState.Init;
            OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 0);
            isSupportCentralFull = false;
            isFineshed = false;
        }

        public void StartPrehension()
        {
            state = TaskBrasState.AttenteGobelet;
            isFineshed = false;
        }
        public void StartDepose()
        {
            state = TaskBrasState.Depose;
            isFineshed = false;
        }

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
                        //On ne sort pas de cet état sans un forçage extérieur vers un autree état
                        break;
                    case TaskBrasState.AttenteGobelet:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 50);
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsAttenteGobelet.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsAttenteGobelet.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsAttenteGobelet.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        state = TaskBrasState.PrehensionGobelet;
                        break;
                    case TaskBrasState.PrehensionGobelet:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsPrehensionGobelet.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsPrehensionGobelet.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsPrehensionGobelet.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        if(ventouseBrasCentralCurrent > 1.0)//mesuré 0.6A à vide et 1.35A en charge
                        {
                            if(!isSupportCentralFull)
                            {
                                //state = TaskBrasState.PreparationRangementSurSupport;
                                state = TaskBrasState.StockageEnHauteur;
                            }
                            else
                            {
                                //state = TaskBrasState.StockageEnHauteur;
                                state = TaskBrasState.StockageEnHauteur;
                            }
                        }
                        else
                        {
                            state = TaskBrasState.AttenteGobelet;
                        }
                        break;
                    case TaskBrasState.PreparationRangementSurSupport:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsPreparationRangementSurSupport.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsPreparationRangementSurSupport.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsPreparationRangementSurSupport.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        state = TaskBrasState.StockageSurSupport;
                        break;
                    case TaskBrasState.StockageSurSupport:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsStockageSurSupport.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsStockageSurSupport.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsStockageSurSupport.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 0);
                        Thread.Sleep(500);
                        isSupportCentralFull = true;
                        state = TaskBrasState.AttenteGobelet;
                        break;
                    case TaskBrasState.StockageEnHauteur:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsStockageEnHauteur.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsStockageEnHauteur.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsStockageEnHauteur.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        state = TaskBrasState.Finished;
                        break;
                    case TaskBrasState.Depose:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentralEpaule, (int)TaskBrasPositionsDepose.BrasCentralEpaule);
                        servoPositionsRequested.Add(ServoId.BrasCentralCoude, (int)TaskBrasPositionsDepose.BrasCentralCoude);
                        servoPositionsRequested.Add(ServoId.BrasCentralPoignet, (int)TaskBrasPositionsDepose.BrasCentralPoignet);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        state = TaskBrasState.Finished;
                        break;
                    case TaskBrasState.Finished:
                        isFineshed = true;
                        break;
                    default:
                        break;
                }
                Thread.Sleep(100);
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
    }
}
