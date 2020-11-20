using EventArgsLibrary;
using HerkulexManagerNS;
using StrategyManagerEurobotNS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static HerkulexManagerNS.HerkulexEventArgs;

namespace StrategyManager.StrategyEurobotNS
{
    public class TaskBrasGauche
    {
        Thread TaskThread;
        TaskBrasState state = TaskBrasState.Attente;
        public bool isFineshed = false;
        Stopwatch sw = new Stopwatch();

        enum TaskBrasState
        {
            Init,
            InitEnCours,
            Attente,
            PrehensionGobelet,
            PrehensionGobeletEnCours,
            StockageSurSupport,
            StockageSurSupportEnCours,
            StockageEnHauteur,
            StockageEnHauteurEnCours,
            Depose,
            DeposeEnCours,
            Finished
        }

        enum TaskBrasPositionsInit
        {
            BrasGauche = 772,
        }
        enum TaskBrasPositionsPrehensionGobelet
        {
            BrasGauche = 520,
        }
        enum TaskBrasPositionsStockageSurSupport
        {
            BrasGauche = 918,
        }
        enum TaskBrasPositionsStockageEnHauteur
        {
            BrasGauche = 697,
        }
        enum TaskBrasPositionsDepose
        {
            BrasGauche = 538,
        }

        public TaskBrasGauche()
        {
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
            sw.Stop();
            sw.Reset();
        }

        Dictionary<ServoId, int> servoPositionsRequested = new Dictionary<ServoId, int>();
        public bool isSupportGaucheFull { get; private set; } = false;
        private double ventouseBrasGaucheCurrent = -1;
        private bool isRunning = false;

        public void Init()
        {
            state = TaskBrasState.Init;
            OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 0);
            isSupportGaucheFull = false;
            isFineshed = false;
            sw.Stop();
            sw.Reset();
        }

        public void StartPrehension()
        {
            state = TaskBrasState.PrehensionGobelet;
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
                        state = TaskBrasState.PrehensionGobeletEnCours;
                        sw.Reset();
                        sw.Start();
                        break;
                    case TaskBrasState.PrehensionGobeletEnCours:
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            if (ventouseBrasGaucheCurrent > 0.8)//mesuré 0.6A à vide et 1.35A en charge
                            {
                                sw.Stop();
                                sw.Reset();
                                state = TaskBrasState.StockageEnHauteur;
                            }
                            else
                            {
                                sw.Stop();
                                state = TaskBrasState.PrehensionGobeletEnCours;
                            }
                        }
                        break;
                    //case TaskBrasState.StockageSurSupport:
                    //    servoPositionsRequested = new Dictionary<ServoId, int>();
                    //    servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsStockageSurSupport.BrasGauche);
                    //    OnHerkulexPositionRequest(servoPositionsRequested);
                    //    Thread.Sleep(500);
                    //    OnPilotageVentouse((byte)PilotageVentouse.BrasGauche, 0);
                    //    Thread.Sleep(500);
                    //    isSupportGaucheFull = true;
                    //    state = TaskBrasState.PrehensionGobelet;
                    //    break;
                    case TaskBrasState.StockageEnHauteur:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsStockageEnHauteur.BrasGauche);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.StockageEnHauteurEnCours;
                        sw.Reset();
                        sw.Start();
                        break;
                    case TaskBrasState.StockageEnHauteurEnCours:
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            sw.Stop();
                            sw.Reset();
                            state = TaskBrasState.Finished;
                        }
                        break;
                    case TaskBrasState.Depose:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsDepose.BrasGauche);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.Finished;
                        sw.Reset();
                        sw.Start();
                        break;
                    case TaskBrasState.DeposeEnCours:
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            sw.Stop();
                            sw.Reset();
                            state = TaskBrasState.Finished;
                        }
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
            ventouseBrasGaucheCurrent = e.motor5;
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
