using Constants;
using EventArgsLibrary;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using WorldMap;

namespace StrategyManagerProjetEtudiantNS
{
    public class StrategyEurobot : StrategyGenerique
    {
        Stopwatch sw = new Stopwatch();

        public PointD robotDestination = new PointD(0, 0);
        PlayingSide playingSide = PlayingSide.Left;     

        TaskDemoMove taskDemoMove;
        TaskDemoMessage taskDemoMessage;

        Timer configTimer;

        public StrategyEurobot(int robotId, int teamId, string multicastIpAddress) : base(robotId, teamId, multicastIpAddress)
        {
            taskDemoMove = new TaskDemoMove(this);
            taskDemoMessage = new TaskDemoMessage(this);
        }

        public override void InitStrategy()
        {
            //On initialisae le timer de réglage récurrent 
            //Il permet de modifier facilement les paramètre des asservissement durant l'exécution
            configTimer = new System.Timers.Timer(1000);
            configTimer.Elapsed += ConfigTimer_Elapsed; ;
            configTimer.Start();

            ////Obtenus directement à partir du script Matlab
            //OnOdometryPointToMeter(1.178449e-06);
            //On2WheelsAngleSetup(-1.570796e+00, 1.570796e+00);
            //On2WheelsToPolarSetup(5.000000e-01, -5.000000e-01,
            //                    4.166667e+00, 4.166667e+00);

            //double KpIndependant = 1;
            //double KiIndependant = 0;
            ////On envoie périodiquement les réglages du PID de vitesse embarqué
            //On2WheelsIndependantSpeedPIDSetup(pM1: KpIndependant, iM1: KiIndependant, 0.0, pM2: KpIndependant, iM2: KiIndependant, 0, 
            //    pM1Limit: 4, iM1Limit: 4, 0, pM2Limit: 4.0, iM2Limit: 4.0, 0);
            ////On2WheelsPolarSpeedPIDSetup(px: 4.0, ix: 300, 0.0, ptheta: 4.0, itheta: 300, 0,
            ////    pxLimit: 4.0, ixLimit: 4.0, 0, pthetaLimit: 4.0, ithetaLimit: 4.0, 0);

            //OnSetAsservissementMode((byte)AsservissementMode.Independant);
        }

        private void ConfigTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Obtenus directement à partir du script Matlab
            OnOdometryPointToMeter(1.178449e-06);
            On2WheelsAngleSetup(-1.570796e+00, 1.570796e+00);
            On2WheelsToPolarSetup(5.000000e-01, -5.000000e-01,
                                4.166667e+00, 4.166667e+00);

            double KpIndependant = 5;
            double KiIndependant =50;
            //On envoie périodiquement les réglages du PID de vitesse embarqué
            On2WheelsIndependantSpeedPIDSetup(pM1: KpIndependant, iM1: KiIndependant, 0.0, pM2: KpIndependant, iM2: KiIndependant, 0,
                pM1Limit: 4, iM1Limit: 4, 0, pM2Limit: 4.0, iM2Limit: 4.0, 0);

            OnSetAsservissementMode((byte)AsservissementMode.Independant4Wheels);
        }

        public override void IterateStateMachines()
        {
            
        }
        

        /*********************************** Events reçus **********************************************/
        

        /*********************************** Events de sortie **********************************************/
        
    }
       

}
