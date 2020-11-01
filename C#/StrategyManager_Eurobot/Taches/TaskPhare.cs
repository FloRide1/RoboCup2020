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
    public class TaskPhare
    {
        Thread TaskThread;
        TaskPhareStates state = TaskPhareStates.Attente;
        public bool isFinished = false;
        StrategyManager_Eurobot parentManager;
        Stopwatch sw = new Stopwatch();
        enum TaskPhareStates
        {
            Init,
            Attente,
            DeplacementToPhare,
            DeplacementToPhareAttente,
            PrepareServo,
            PrepareServoAttente,
            ActivatePhare,
            ActivatePhareAttente,
            ReplyServo,
            ReplyServoAttente,
            Finished,
        }

        public TaskPhare(StrategyManager_Eurobot manager)
        {
            parentManager = manager;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        private bool isRunning = false;
        private Dictionary<ServoId, int> servoPositionsRequested;

        #region ENUMS
        enum TaskBrasPositionsInit
        {
            BrasGauche = 772,
            BrasDroit = 772,
            BrasCentral = 772,
        }
        enum TaskBrasPositionsPush
        {
            BrasGauche = 580,
            BrasDroit = 580,
            BrasCentral = 580,
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
            state = TaskPhareStates.Init;
            isFinished = false;
            StopSw();
        }
        public void Start()
        {
            state = TaskPhareStates.DeplacementToPhare;
            isFinished = false;
            StopSw();
        }
        public void Pause()
        {
            state = TaskPhareStates.Attente;
            isFinished = false;
            StopSw();
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskPhareStates.Init:
                        state = TaskPhareStates.Attente;
                        break;
                    case TaskPhareStates.Attente:
                        break;
                        /**********************Deplacement vers phare**************************************/
                    case TaskPhareStates.DeplacementToPhare:
                        if(parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.2, -0.85);
                            parentManager.robotOrientation = -Math.PI/2;
                        }
                        else if(parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.2, -0.85);
                            parentManager.robotOrientation = -Math.PI/2;
                        }
                        state = TaskPhareStates.DeplacementToPhareAttente;
                        break;
                    case TaskPhareStates.DeplacementToPhareAttente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskPhareStates.PrepareServo;
                        break;
                    /**********************Préparation des servos**************************************/
                    case TaskPhareStates.PrepareServo:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPush.BrasGauche);
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsPush.BrasDroit);
                        }
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskPhareStates.PrepareServoAttente;
                        StartSw();
                        break;
                    case TaskPhareStates.PrepareServoAttente:
                        if(sw.ElapsedMilliseconds > 500)
                        {
                            StopSw();
                            state = TaskPhareStates.ActivatePhare;
                        }
                        break;
                    /**********************Deplacement activation phare**************************************/
                    case TaskPhareStates.ActivatePhare:
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.3, -0.85);
                            parentManager.robotOrientation = -Math.PI / 2;
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.3, -0.85);
                            parentManager.robotOrientation = -Math.PI / 2;
                        }
                        state = TaskPhareStates.ActivatePhareAttente;
                        break;
                    case TaskPhareStates.ActivatePhareAttente:
                        if (parentManager.isDeplacementFinished)
                            state = TaskPhareStates.ReplyServo;
                        break;
                    /**********************Reply servos**************************************/
                    case TaskPhareStates.ReplyServo:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsInit.BrasGauche);
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsInit.BrasDroit);
                        }
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        StartSw();
                        state = TaskPhareStates.ReplyServoAttente;
                        break;
                    case TaskPhareStates.ReplyServoAttente:
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            StopSw();
                            state = TaskPhareStates.Finished;
                        }
                        break;
                    /**********************Finished task**************************************/
                    case TaskPhareStates.Finished:
                        isFinished = true;
                        state = TaskPhareStates.Attente;
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
