﻿using Constants;
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
using WorldMap;
using static HerkulexManagerNS.HerkulexEventArgs;

namespace StrategyManagerProjetEtudiantNS
{
    class TaskStrategy
    {
        Thread TaskThread;
        TaskStrategyState state = TaskStrategyState.Attente;
        StrategyEurobot2021 parentStrategyManager;
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
            Distributeurs,
            DistributeursEnCours,
            Finished
        }
        
        public TaskStrategy( StrategyEurobot2021 strategyManager)
        {
            parentStrategyManager = strategyManager;
            TaskThread = new Thread(TaskThreadProcess);
            TaskThread.IsBackground = true;
            TaskThread.Start();
            isFirstRun = true;
        }

        public void Init()
        {
            state = TaskStrategyState.InitialPositioning;
            isFirstRun = true;
        }

        List<Location> locsPriseBleue = new List<Location>() {
            new Location(0.575, -1.059, -Math.PI/2, 0, 0, 0),
            new Location(0.725, -1.059, -Math.PI/2, 0, 0, 0),
            new Location(1.559, 0.675, 0, 0, 0, 0),
            new Location(1.559, 0.675, 0, 0, 0, 0),
        };

        List<Location> locsPoseBleue = new List<Location>() {
            new Location(1.3, 0.08, 0, 0, 0, 0),
            new Location(1.3, -0.49, 0, 0, 0, 0),
            new Location(1.15, 0.05, 0, 0, 0, 0),
            new Location(1.15, -0.49, 0, 0, 0, 0),
        };


        List<Location> locsPriseJaune = new List<Location>() {
            new Location(-0.575, -1.059, -Math.PI/2, 0, 0, 0),
            new Location(-0.725, -1.059, -Math.PI/2, 0, 0, 0),
            new Location(-1.559, 0.675, Math.PI, 0, 0, 0),
            new Location(-1.559, 0.525, Math.PI, 0, 0, 0),
        };

        List<Location> locsPoseJaune = new List<Location>() {
            new Location(-1.3, 0.08, Math.PI, 0, 0, 0),
            new Location(-1.3, -0.49, Math.PI, 0, 0, 0),
            new Location(-1.15, -0.05, Math.PI, 0, 0, 0),
            new Location(-1.15, 0.49, Math.PI, 0, 0, 0),
        };

        int indexPrise = 0;
        bool isFirstRun = true;
        void TaskThreadProcess()
        {
            while(true)
            {
                if (timeStamp.ElapsedMilliseconds < 99999)
                {
                    switch (state)
                    {
                        case TaskStrategyState.Attente:
                            break;
                        case TaskStrategyState.InitialPositioning:  //Le positionnement initial est manuel de manière à pouvoir coller deux robots très proches sans mouvement parasite
                            parentStrategyManager.OnEnableMotors(false);
                            //Le jack force le retour à cet état
                            //parentStrategyManager.taskDistributeur.Init();
                            RefBoxMessage message = new RefBoxMessage();
                            message.command = RefBoxCommand.STOP;
                            message.targetTeam = "224.16.32.79";
                            message.robotID = 0;
                            parentStrategyManager.OnRefereeBoxReceivedCommand(message);
                            indexPrise = 0;
                            state = TaskStrategyState.InitialPositioningEnCours;
                            break;
                        case TaskStrategyState.InitialPositioningEnCours:
                            if (!Jack)
                            {
                                parentStrategyManager.taskBrasDroit.Init();
                                parentStrategyManager.taskBrasGauche.Init();
                                parentStrategyManager.taskBrasCentre.Init();
                                parentStrategyManager.taskBrasDrapeau.Init();
                                parentStrategyManager.taskPhare.Init();
                                parentStrategyManager.taskWindFlag.Init();
                                parentStrategyManager.taskBalade.Init();
                                parentStrategyManager.taskDepose.Init();
                                parentStrategyManager.taskFinDeMatch.Init();
                                parentStrategyManager.OnEnableMotors(true);
                                message = new RefBoxMessage();
                                message.command = RefBoxCommand.START;
                                message.targetTeam = "224.16.32.79";
                                message.robotID = 0;
                                parentStrategyManager.OnRefereeBoxReceivedCommand(message); 
                                parentStrategyManager.OnCollision(parentStrategyManager.robotId, parentStrategyManager.robotCurrentLocation); //On génère artificellement une collision pour resetter Kalman et le reste autour de la position courante.
                                state = TaskStrategyState.Phare;
                                if(isFirstRun)
                                {
                                    isFirstRun = false;
                                    StartSw();
                                }
                            }
                            break;
                        
                        case TaskStrategyState.PushFlags:
                            parentStrategyManager.taskWindFlag.Start();
                            state = TaskStrategyState.PushFlagsEnCours;
                            break;
                        case TaskStrategyState.PushFlagsEnCours:
                            if(parentStrategyManager.taskWindFlag.isFinished)
                            {
                                state = TaskStrategyState.Distributeurs;
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
                        case TaskStrategyState.Distributeurs:
                            if (parentStrategyManager.Team == Equipe.Jaune)
                            {
                                if (indexPrise < locsPoseJaune.Count && indexPrise < locsPriseJaune.Count)
                                {
                                    parentStrategyManager.taskDistributeur.Start(locsPriseJaune[indexPrise], locsPoseJaune[indexPrise]);
                                    indexPrise++;
                                    state = TaskStrategyState.DistributeursEnCours;
                                }
                                else
                                {
                                    state = TaskStrategyState.Attente;
                                }
                            }
                            else if (parentStrategyManager.Team == Equipe.Bleue)
                            {
                                if (indexPrise < locsPoseBleue.Count && indexPrise < locsPriseBleue.Count)
                                {
                                    parentStrategyManager.taskDistributeur.Start(locsPriseBleue[indexPrise], locsPoseBleue[indexPrise]);
                                    indexPrise++;
                                    state = TaskStrategyState.DistributeursEnCours;
                                }
                                else
                                {
                                    state = TaskStrategyState.InitialPositioningEnCours;
                                }
                            }
                            break;
                        case TaskStrategyState.DistributeursEnCours:
                            if (parentStrategyManager.taskDistributeur.isFinished)
                            {
                                state = TaskStrategyState.Distributeurs;
                            }
                            break;
                        default:
                            state = TaskStrategyState.Phare;
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
                timeStamp.Restart();
                if (state != TaskStrategyState.InitialPositioningEnCours)
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
