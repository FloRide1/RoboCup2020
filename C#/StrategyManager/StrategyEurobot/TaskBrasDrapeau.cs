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

namespace StrategyManagerNS
{
    public class TaskBrasDrapeau
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
            Finished
        }

        enum TaskBrasPositionsInit
        {
            BrasDrapeau = 512,
        }
        enum TaskBrasPositionsPrehensionGobelet
        {
            BrasDrapeau = 520,
        }
        enum TaskBrasPositionsStockageSurSupport
        {
            BrasDrapeau = 918,
        }
        enum TaskBrasPositionsStockageEnHauteur
        {
            BrasDrapeau = 697,
        }
        enum TaskBrasPositionsDepose
        {
            BrasDrapeau = 538,
        }

        public TaskBrasDrapeau()
        {
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
            sw.Stop();
            sw.Reset();
        }


        Dictionary<ServoId, int> servoPositionsRequested = new Dictionary<ServoId, int>();
        public bool isSupportDrapeauFull { get; private set; } = false;
        private bool isRunning = false;
        private double ventouseBrasDrapeauCurrent = -1;
        
        public void Init()
        {
            state = TaskBrasState.Init;
            OnPilotageVentouse((byte)PilotageVentouse.BrasCentral, 0);
            isSupportDrapeauFull = false;
            isFineshed = false;
            sw.Stop();
            sw.Reset();
        }
        
        

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskBrasState.Init:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsInit.BrasDrapeau);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskBrasState.Attente;
                        break;
                    case TaskBrasState.Attente:
                        //On ne sort pas de cet état sans un forçage extérieur vers un autree état
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
            ventouseBrasDrapeauCurrent = e.motor6;
        }
    }
}
