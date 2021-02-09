using Constants;
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

namespace StrategyManagerProjetEtudiantNS
{
    public class TaskBalade
    {
        Thread TaskThread;
        TaskBaladeStates state = TaskBaladeStates.Attente;
        public bool isFinished = false;
        StrategyEurobot2021 parentManager;
        enum TaskBaladeStates
        {
            Init,
            Attente,
            Deplacement1,
            Deplacement1Attente,
            Deplacement2,
            Deplacement2Attente,
            Deplacement3,
            Deplacement3Attente,
            Deplacement4,
            Deplacement4Attente,
            Deplacement5,
            Deplacement5Attente,
            Finished,
        }

        public TaskBalade(StrategyEurobot2021 manager)
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
                        state = TaskBaladeStates.Attente;
                        break;
                    case TaskBaladeStates.Attente:
                        break;
                    case TaskBaladeStates.Deplacement1:
                        if(parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.2, 0.140);
                            parentManager.robotOrientation = 0;
                        }
                        else if(parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.2, 0.140);
                            parentManager.robotOrientation = Math.PI;
                        }
                        state = TaskBaladeStates.Deplacement1Attente;
                        break;
                    case TaskBaladeStates.Deplacement1Attente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskBaladeStates.Deplacement2;
                        break;
                    case TaskBaladeStates.Deplacement2:
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(-0.725, 0.140);
                            parentManager.robotOrientation = 0;
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(0.725, 0.140);
                            parentManager.robotOrientation = Math.PI;
                        }
                        state = TaskBaladeStates.Deplacement2Attente;
                        break;
                    case TaskBaladeStates.Deplacement2Attente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskBaladeStates.Deplacement3;
                        break;
                    case TaskBaladeStates.Deplacement3:
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(-0.725, -0.545);
                            parentManager.robotOrientation = -Math.PI / 2;
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(0.725, -0.545);
                            parentManager.robotOrientation = -Math.PI / 2;
                        }
                        state = TaskBaladeStates.Deplacement3Attente;
                        break;
                    case TaskBaladeStates.Deplacement3Attente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskBaladeStates.Deplacement4;
                        break;
                    case TaskBaladeStates.Deplacement4:
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.2, -0.545);
                            parentManager.robotOrientation = Math.PI;
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.2, -0.545);
                            parentManager.robotOrientation = 0;
                        }
                        state = TaskBaladeStates.Deplacement4Attente;
                        break;
                    case TaskBaladeStates.Deplacement4Attente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskBaladeStates.Deplacement5;
                        break;
                    case TaskBaladeStates.Deplacement5:
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.25, -0.2);
                            parentManager.robotOrientation = Math.PI - Toolbox.DegToRad(45);
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.25, -0.2);
                            parentManager.robotOrientation = Toolbox.DegToRad(45);
                        }
                        state = TaskBaladeStates.Deplacement5Attente;
                        break;
                    case TaskBaladeStates.Deplacement5Attente:
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
