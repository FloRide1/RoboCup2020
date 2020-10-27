using EventArgsLibrary;
using HerkulexManagerNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;
using static HerkulexManagerNS.HerkulexEventArgs;

namespace StrategyManager
{
    public class TaskBalade
    {
        Thread TaskThread;
        TaskBaladeStates state = TaskBaladeStates.Attente;
        public bool isFinished = false;
        StrategyManager_Eurobot parentManager;
        enum TaskBaladeStates
        {
            Init,
            Attente,
            Deplacement1,
            Deplacement1Attente,
            Deplacement2,
            Deplacement2Attente,
            Finished,
        }

        public TaskBalade(StrategyManager_Eurobot manager)
        {
            parentManager = manager;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        private bool isRunning = false;

        public void Init()
        {
            state = TaskBaladeStates.Init;
            isFinished = false;
        }
        public void Start()
        {
            state = TaskBaladeStates.Deplacement1;
            isFinished = false;
        }
        public void Pause()
        {
            state = TaskBaladeStates.Attente;
            isFinished = false;
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskBaladeStates.Init:
                        OnPilotageVentouse(5, 0);
                        state = TaskBaladeStates.Attente;
                        break;
                    case TaskBaladeStates.Attente:
                        break;
                    case TaskBaladeStates.Deplacement1:
                        OnPilotageVentouse(5, 50);
                        parentManager.robotDestination = new PointD(-1.3, 0);
                        parentManager.robotOrientation = Math.PI;
                        state = TaskBaladeStates.Deplacement1Attente;
                        break;
                    case TaskBaladeStates.Deplacement1Attente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskBaladeStates.Deplacement2;
                        break;
                    case TaskBaladeStates.Deplacement2:
                        OnPilotageVentouse(5, 0);
                        parentManager.robotDestination = new PointD(1.3, 0);
                        parentManager.robotOrientation = 0;
                        state = TaskBaladeStates.Deplacement2Attente;
                        break;
                    case TaskBaladeStates.Deplacement2Attente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskBaladeStates.Finished;
                        break;
                    case TaskBaladeStates.Finished:
                        isFinished = true;
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
    }
}
