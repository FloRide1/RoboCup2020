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
    public class TaskBrasGauche
    {
        Thread TaskThread;
        TaskBrasState state = TaskBrasState.Attente;
        enum TaskBrasState
        {
            Init,
            Attente,
            PrehensionGobelet,
            StockageSurSupport,
            StockageEnHauteur,
            Rangement,
            Finished
        }

        enum TaskBrasPositionsInit
        {
            BrasGauche = 772,
        }
        enum TaskBrasPositionsPrehensionGobelet
        {
            BrasGauche = 538,
        }
        enum TaskBrasPositionsStockageSurSupport
        {
            BrasGauche = 918,
        }
        enum TaskBrasPositionsStockageEnHauteur
        {
            BrasGauche = 697,
        }
        enum TaskBrasPositionsRangement
        {
            BrasGauchce = 512,
        }

        public TaskBrasGauche()
        {
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        Dictionary<ServoId, int> servoPositionsRequested = new Dictionary<ServoId, int>();
        public bool isSupportGauchceFull { get; private set; } = false;
        private double ventouseBrasGauchceCurrent = -1;
        private bool isRunning = false;

        public void Init()
        {
            state = TaskBrasState.Init;
            OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 0);
            isSupportGauchceFull = false;
        }

        public void StartPrehension()
        {
            state = TaskBrasState.PrehensionGobelet;
        }

        void TaskThreadProcess()
        {
            while (true)
            {
                switch (state)
                {
                    case TaskBrasState.Init:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsInit.BrasGauche);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.Attente;
                        break;
                    case TaskBrasState.Attente:
                        //On ne sort pas de cet état sans un forçage extérieur vers un autree état
                        break;
                    case TaskBrasState.PrehensionGobelet:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        OnPilotageVentouse((byte)PilotageVentouse.BrasGauche, 50);
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPrehensionGobelet.BrasGauche);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        if (ventouseBrasGauchceCurrent > 1.0)//mesuré 0.6A à vide et 1.35A en charge
                        {
                            if (!isSupportGauchceFull)
                            {
                                state = TaskBrasState.StockageSurSupport;
                            }
                            else
                            {
                                state = TaskBrasState.StockageEnHauteur;
                            }
                        }
                        else
                        {
                            state = TaskBrasState.PrehensionGobelet;
                        }
                        break;
                    case TaskBrasState.StockageSurSupport:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsStockageSurSupport.BrasGauche);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        OnPilotageVentouse((byte)PilotageVentouse.BrasGauche, 0);
                        Thread.Sleep(500);
                        isSupportGauchceFull = true;
                        state = TaskBrasState.PrehensionGobelet;
                        break;
                    case TaskBrasState.StockageEnHauteur:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsStockageEnHauteur.BrasGauche);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        Thread.Sleep(500);
                        state = TaskBrasState.Finished;
                        break;
                    case TaskBrasState.Rangement:
                        break;
                    case TaskBrasState.Finished:
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
            ventouseBrasGauchceCurrent = e.motor7;
        }

        public void OnStartPrehension(object sender, BoolEventArgs e)
        {
            Init();
            state = TaskBrasState.PrehensionGobelet;
        }
        public void OnStopPrehension(object sender, BoolEventArgs e)
        {
            state = TaskBrasState.Attente;
        }
    }
}
