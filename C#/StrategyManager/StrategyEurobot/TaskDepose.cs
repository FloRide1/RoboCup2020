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

namespace StrategyManagerNS
{
    public class TaskDepose
    {
        Thread TaskThread;
        TaskDeposeStates state = TaskDeposeStates.Attente;
        public bool isFinished = false;
        StrategyEurobot2021 parentManager;
        enum TaskDeposeStates
        {
            Init,
            Attente,
            Deplacement1,
            Deplacement1Attente,
            Deplacement2,
            Deplacement2Attente,
            Finished,
        }

        public TaskDepose(StrategyEurobot2021 manager)
        {
            parentManager = manager;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        private bool isRunning = false;

        public void Init()
        {
            state = TaskDeposeStates.Init;
            isFinished = false;
        }
        public void Start()
        {
            state = TaskDeposeStates.Deplacement1;
            isFinished = false;
        }
        public void Pause()
        {
            state = TaskDeposeStates.Attente;
            isFinished = false;
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskDeposeStates.Init:
                        state = TaskDeposeStates.Attente;
                        break;
                    case TaskDeposeStates.Attente:
                        break;
                    case TaskDeposeStates.Deplacement1:
                        if(parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.2, -0.2);
                            parentManager.robotOrientation = 0;
                        }
                        else if(parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.2, -0.2);
                            parentManager.robotOrientation = Math.PI;
                        }
                        state = TaskDeposeStates.Deplacement1Attente;
                        break;
                    case TaskDeposeStates.Deplacement1Attente:
                        if (parentManager.isDeplacementFinished)
                        {
                            state = TaskDeposeStates.Finished;
                            OnPilotageVentouse(5, -50);
                            OnPilotageVentouse(6, -50);
                            OnPilotageVentouse(7, -50);
                            Thread.Sleep(20);
                        }
                        break;
                    case TaskDeposeStates.Finished:
                        isFinished = true;
                        OnPilotageVentouse(5, 0);
                        OnPilotageVentouse(6, 0);
                        OnPilotageVentouse(7, 0);
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
