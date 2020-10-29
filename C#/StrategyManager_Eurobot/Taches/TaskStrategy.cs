using EventArgsLibrary;
using HerkulexManagerNS;
using RefereeBoxAdapter;
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
    class TaskStrategy
    {
        Thread TaskThread;
        TaskStrategyState state = TaskStrategyState.Attente;
        StrategyManager_Eurobot parentStrategyManager;
        public Equipe playingTeam = Equipe.Jaune;
        bool Jack = true;
        Stopwatch timeStamp = new Stopwatch();
        private void StopSw()
        {
            timeStamp.Stop();
            timeStamp.Reset();
        }
        private void StartSw()
        {
            timeStamp.Reset();
            timeStamp.Start();
        }

        enum TaskStrategyState
        {
            InitialPositioning,
            InitialPositioningEnCours,
            Attente,   
            Ballade,
            InitPrehension,
            BalladeEnCours,
            PushFlags,
            PushFlagsEnCours,
            InitCaptureDistributeur,
            CaptureDistributeur1,
            CaptureDistributeur2,
            Phare,
            PhareEnCours,
            Finished
        }
        
        public TaskStrategy( StrategyManager_Eurobot strategyManager)
        {
            parentStrategyManager = strategyManager;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
        }

        public void Init()
        {
            state = TaskStrategyState.InitialPositioning;
        }

        void TaskThreadProcess()
        {
            while(true)
            {
                if (timeStamp.ElapsedMilliseconds < 99990)
                {
                    switch (state)
                    {
                        case TaskStrategyState.Attente:
                            break;
                        case TaskStrategyState.InitialPositioning:
                            parentStrategyManager.OnEnableMotors(true);
                            //Le jack force le retour à cet état
                            parentStrategyManager.taskBrasDroit.Init();
                            parentStrategyManager.taskBrasGauche.Init();
                            parentStrategyManager.taskBalade.Init();
                            parentStrategyManager.taskDepose.Init();
                            parentStrategyManager.taskFinDeMatch.Init();
                            RefBoxMessage message = new RefBoxMessage();
                            message.command = RefBoxCommand.START;
                            message.targetTeam = "224.16.32.79";
                            message.robotID = 0;
                            parentStrategyManager.OnRefereeBoxReceivedCommand(message);

                            if (playingTeam == Equipe.Bleue)
                            {
                                parentStrategyManager.robotDestination = new PointD(1.32, -0.2);
                                parentStrategyManager.robotOrientation = Math.PI;
                                //parentStrategyManager.SetDestination(new Location(1.25, -0.2, Math.PI, 0, 0, 0));
                            }
                            else if (playingTeam == Equipe.Jaune)
                            {
                                parentStrategyManager.robotDestination = new PointD(-1.32, -0.2);
                                parentStrategyManager.robotOrientation = 0;
                                //parentStrategyManager.SetDestination(new Location(0, 0, 0, 0, 0, 0));
                            }
                            state = TaskStrategyState.InitialPositioningEnCours;
                            break;
                        case TaskStrategyState.InitialPositioningEnCours:
                            if (!Jack)
                            {
                                state = TaskStrategyState.Phare;
                                StartSw();
                            }
                            break;
                        case TaskStrategyState.InitCaptureDistributeur:
                            //parentStrategyManager.taskBrasCentral.StartPrehension();
                            //parentStrategyManager.taskBrasDroit.StartPrehension();
                            //parentStrategyManager.taskBrasGauche.StartPrehension();
                            //parentStrategyManager.robotDestination = new PointD(-0.7, -1.067+0.21);
                            //parentStrategyManager.robotOrientation = -Math.PI/2;
                            ////Thread.Sleep(5000);
                            //parentStrategyManager.robotDestination = new PointD(-0.7+0.075, -1.067 + 0.21);
                            //parentStrategyManager.robotOrientation = -Math.PI / 2;
                            state = TaskStrategyState.Attente;
                            break;
                        case TaskStrategyState.InitPrehension:
                            //parentStrategyManager.taskBrasCentral.StartPrehension();
                            //parentStrategyManager.taskBrasDroit.StartPrehension();
                            //parentStrategyManager.taskBrasGauche.StartPrehension();
                            state = TaskStrategyState.Ballade;
                            break;
                        case TaskStrategyState.Ballade:
                            parentStrategyManager.taskBalade.Start();
                            state = TaskStrategyState.BalladeEnCours;
                            break;
                        case TaskStrategyState.BalladeEnCours:
                            //if (parentStrategyManager.taskBrasCentral.isFineshed ||
                            //    parentStrategyManager.taskBrasGauche.isFineshed ||
                            //    parentStrategyManager.taskBrasDroit.isFineshed)
                            //{
                            //    parentStrategyManager.taskBalade.Pause();
                            //    parentStrategyManager.taskDepose.Start();
                            //    if(parentStrategyManager.taskDepose.isFinished)
                            //    {
                            //        state = TaskStrategyState.InitialPositioning;
                            //    }
                            //    //Il faut faire une depose !!! et couper la balade
                            //}
                            //else if
                            if (parentStrategyManager.taskBalade.isFinished)
                            {
                                state = TaskStrategyState.Attente; //Equal stop
                                parentStrategyManager.OnEnableDisableMotorCurrentData(false);
                            }
                            break;
                        case TaskStrategyState.PushFlags:
                            parentStrategyManager.taskWindFlag.Start();
                            state = TaskStrategyState.PushFlagsEnCours;
                            break;
                        case TaskStrategyState.PushFlagsEnCours:
                            if(parentStrategyManager.taskWindFlag.isFinished)
                            {
                                state = TaskStrategyState.Attente;
                            }
                            break;
                        case TaskStrategyState.Phare:
                            parentStrategyManager.taskPhare.Start();
                            state = TaskStrategyState.PhareEnCours;
                            break;
                        case TaskStrategyState.PhareEnCours:
                            if(parentStrategyManager.taskPhare.isFinished)
                            {
                                state = TaskStrategyState.PushFlags;
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Init();
                    if (!isStoped)
                    {
                        parentStrategyManager.taskFinDeMatch.Start();
                        timeStamp.Stop();
                    }
                    isStoped = true;
                }
                Thread.Sleep(1);
            }
        }
        bool isStoped = false;

        //Events
        public void OnIOValuesFromRobotEvent(object sender, IOValuesEventArgs e)
        {
            bool jackIsPresent = (((e.ioValues >> 0) & 0x01) == 0x00);
            Jack = jackIsPresent;
            bool config1IsOn = (((e.ioValues >> 1) & 0x01) == 0x01);
            if(jackIsPresent)
            {
                if(state != TaskStrategyState.InitialPositioningEnCours)
                    state = TaskStrategyState.InitialPositioning;

                if (config1IsOn)
                {
                    playingTeam = Equipe.Jaune;
                    OnMirrorMode(false);
                }
                else
                {
                    playingTeam = Equipe.Bleue;
                    //On transmet au Perception Manager le fait que l'on soit en mode miroir
                    OnMirrorMode(true);
                }
            }            
            else
            {
                ;
            }
            bool config2 = (((e.ioValues >> 2) & 0x01) == 0x01);
            bool config3 = (((e.ioValues >> 3) & 0x01) == 0x01);
            bool config4 = (((e.ioValues >> 4) & 0x01) == 0x01);
        }

        //Events de sortie
        public event EventHandler<BoolEventArgs> OnMirrorModeEvent;
        public virtual void OnMirrorMode(bool isMirroredMode)
        {
            OnMirrorModeEvent?.Invoke(this, new BoolEventArgs { value = isMirroredMode });
        }
    }
}
