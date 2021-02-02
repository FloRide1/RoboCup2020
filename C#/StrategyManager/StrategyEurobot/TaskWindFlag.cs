using Constants;
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

namespace StrategyManagerNS
{
    public class TaskWindFlag
    {
        Thread TaskThread;
        TaskWindFlagStates state = TaskWindFlagStates.Attente;
        public bool isFinished = false;
        StrategyEurobot2021 parentManager;
        Stopwatch sw = new Stopwatch();
        enum TaskWindFlagStates
        {
            Init,
            Attente,
            DeplacementToFlag,
            DeplacementToFlagAttente,
            PrepareServo,
            PrepareServoAttente,
            PushFirstFlag,
            PushFirstFlagAttente,
            PrepareServo2,
            PrepareServo2Attente,
            PushSecondFlag,
            PushSecondFlagAttente,
            ReplyServo,
            ReplyServoAttente,
            Finished,
        }

        public TaskWindFlag(StrategyEurobot2021 manager)
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
            BrasGauche = 560,
            BrasDroit = 560,
            BrasCentral = 560,
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
            state = TaskWindFlagStates.Init;
            isFinished = false;
            StopSw();
        }
        public void Start()
        {
            state = TaskWindFlagStates.DeplacementToFlag;
            isFinished = false;
            StopSw();
        }
        public void Pause()
        {
            state = TaskWindFlagStates.Attente;
            isFinished = false;
            StopSw();
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                switch (state)
                {
                    case TaskWindFlagStates.Init:
                        state = TaskWindFlagStates.Attente;
                        break;
                    case TaskWindFlagStates.Attente:
                        break;
                        /**********************Deplacement vers drapeau droit**************************************/
                    case TaskWindFlagStates.DeplacementToFlag:
                        if(parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.4, 0.90);
                            parentManager.robotOrientation = Math.PI/2;
                        }
                        else if(parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.4, 0.90);
                            parentManager.robotOrientation = Math.PI/2;
                        }
                        state = TaskWindFlagStates.DeplacementToFlagAttente;
                        StartSw();
                        break;
                    case TaskWindFlagStates.DeplacementToFlagAttente:
                        if (parentManager.isDeplacementFinished || sw.ElapsedMilliseconds > 5000)
                        {
                            state = TaskWindFlagStates.PrepareServo;
                            StopSw();
                        }
                        break;
                    /**********************Préparation des servos**************************************/
                    case TaskWindFlagStates.PrepareServo:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsPush.BrasDroit);
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPush.BrasGauche);
                        }
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        state = TaskWindFlagStates.PrepareServoAttente;
                        StartSw();
                        break;
                    case TaskWindFlagStates.PrepareServoAttente:
                        if(sw.ElapsedMilliseconds > 500)
                        {
                            StopSw();
                            state = TaskWindFlagStates.PushFirstFlag;
                        }
                        break;
                    /**********************Deplacement vers drapeau droit**************************************/
                    case TaskWindFlagStates.PushFirstFlag:
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(1.0, 0.87);
                            parentManager.robotOrientation = Math.PI / 2;
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-1.0, 0.87);
                            parentManager.robotOrientation = Math.PI / 2;
                        }
                        state = TaskWindFlagStates.PushFirstFlagAttente;
                        StartSw();
                        break;
                    case TaskWindFlagStates.PushFirstFlagAttente:
                        if (parentManager.isDeplacementFinished || sw.ElapsedMilliseconds > 5000)
                        {
                            state = TaskWindFlagStates.PrepareServo2;
                            StopSw();
                        }
                        break;
                    /**********************Preparation des servo 2 eme flag**************************************/
                    case TaskWindFlagStates.PrepareServo2:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsInit.BrasDroit);
                            servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsPush.BrasGauche);
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsInit.BrasGauche);
                            servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsPush.BrasDroit);
                        }
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        StartSw();
                        state = TaskWindFlagStates.PrepareServo2Attente;
                        break;
                    case TaskWindFlagStates.PrepareServo2Attente:
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            StopSw();
                            state = TaskWindFlagStates.PushSecondFlag;
                        }
                        break;
                    /**********************Push flag 2**************************************/
                    case TaskWindFlagStates.PushSecondFlag:
                        if (parentManager.Team == Equipe.Bleue)
                        {
                            parentManager.robotDestination = new PointD(0.75, 0.87);
                            parentManager.robotOrientation = Math.PI / 2;
                        }
                        else if (parentManager.Team == Equipe.Jaune)
                        {
                            parentManager.robotDestination = new PointD(-0.75, 0.87);
                            parentManager.robotOrientation = Math.PI / 2;
                        }
                        state = TaskWindFlagStates.PushSecondFlagAttente;
                        StartSw();
                        break;
                    case TaskWindFlagStates.PushSecondFlagAttente:
                        if (parentManager.isDeplacementFinished || sw.ElapsedMilliseconds > 5000)
                        {
                            state = TaskWindFlagStates.ReplyServo;
                            StopSw();
                        }
                        break;
                    /**********************Reply servos**************************************/
                    case TaskWindFlagStates.ReplyServo:
                        servoPositionsRequested = new Dictionary<ServoId, int>();
                        servoPositionsRequested.Add(ServoId.BrasGauche, (int)TaskBrasPositionsInit.BrasGauche);
                        servoPositionsRequested.Add(ServoId.BrasDroit, (int)TaskBrasPositionsInit.BrasDroit);
                        servoPositionsRequested.Add(ServoId.BrasCentral, (int)TaskBrasPositionsInit.BrasCentral);
                        OnHerkulexPositionRequest(servoPositionsRequested);
                        StartSw();
                        state = TaskWindFlagStates.ReplyServoAttente;
                        break;
                    case TaskWindFlagStates.ReplyServoAttente:
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            StopSw();
                            state = TaskWindFlagStates.Finished;
                        }
                        break;
                    /**********************Finished task**************************************/
                    case TaskWindFlagStates.Finished:
                        isFinished = true;
                        state = TaskWindFlagStates.Attente;
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
