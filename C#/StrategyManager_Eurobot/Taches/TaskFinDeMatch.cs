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
    public class TaskFinDeMatch
    {
        Thread TaskThread;
        TaskFinDeMatchStates state = TaskFinDeMatchStates.Attente;
        public bool isFinished = false;
        StrategyManager_Eurobot parentManager;
        Stopwatch sw = new Stopwatch();
        enum TaskFinDeMatchStates
        {
            Init,
            Attente,
            FinDeMatch,
            FinDeMatchAttente,
            Finished,
        }

        public TaskFinDeMatch(StrategyManager_Eurobot manager)
        {
            parentManager = manager;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        private bool isRunning = false;
        private Dictionary<ServoId, int> servoPositionsRequested;

        #region ENUMS
        enum TaskDrapeauPositionsInit
        {
            Drapeau = 512,
        }
        enum TaskDrapeauPositionsOpen
        {
            Drapeau = 200
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
            state = TaskFinDeMatchStates.Init;
            isFinished = false;
            StopSw();
        }
        public void Start()
        {
            state = TaskFinDeMatchStates.FinDeMatch;
            isFinished = false;
            StopSw();
        }
        public void Pause()
        {
            state = TaskFinDeMatchStates.Attente;
            isFinished = false;
            StopSw();
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskFinDeMatchStates.Init:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.PorteDrapeau, (int)TaskDrapeauPositionsInit.Drapeau);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskFinDeMatchStates.Attente;
                        break;
                    case TaskFinDeMatchStates.Attente:
                        break;
                        /**********************Ouverture drapeau**************************************/
                    case TaskFinDeMatchStates.FinDeMatch:
                        OnPilotageVentouse(5, 0);
                        OnPilotageVentouse(6, 0);
                        OnPilotageVentouse(7, 0);
                        state = TaskFinDeMatchStates.FinDeMatchAttente;
                        parentManager.OnEnableMotors(false);
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.PorteDrapeau, (int)TaskDrapeauPositionsOpen.Drapeau);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        break;
                    case TaskFinDeMatchStates.FinDeMatchAttente:
                        state = TaskFinDeMatchStates.Finished;
                        break;
                    /**********************Finished task**************************************/
                    case TaskFinDeMatchStates.Finished:
                        isFinished = true;
                        state = TaskFinDeMatchStates.Attente;
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
        }

        public event EventHandler<HerkulexPositionsArgs> OnHerkulexPositionRequestEvent;
        public virtual void OnHerkulexPositionRequest(Dictionary<ServoId, int> positionDictionary)
        {
            OnHerkulexPositionRequestEvent?.Invoke(this, new HerkulexPositionsArgs { servoPositions = positionDictionary });
        }
    }
}
