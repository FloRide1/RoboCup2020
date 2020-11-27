using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Utilities;
using WorldMap;
using HeatMap;
using System.Diagnostics;
using PerceptionManagement;
using System.Timers;
using Constants;
using HerkulexManagerNS;
using StrategyManagerNS.StrategyRoboCupNS;


namespace StrategyManagerNS
{

    public class StrategyManager
    {
        GameMode strategyMode = GameMode.RoboCup;        

        public StrategyGenerique strategy;

        public StrategyManager(int robotId, int teamId, string multicastIpAddress, GameMode stratMode)
        {
            strategyMode = stratMode;

            switch (strategyMode)
            {
                case GameMode.RoboCup:
                    strategy = new StrategyRoboCup(robotId, teamId, multicastIpAddress);
                    break;
                case GameMode.Eurobot:
                    strategy = new StrategyEurobot2021(robotId, teamId, multicastIpAddress);
                    break;
                case GameMode.Demo:
                    break;
            }
        }        

        //************************ Event envoyés par le gestionnaire de strategie ***********************/

        //public event EventHandler<ByteEventArgs> OnSetAsservissementModeEvent;
        //public virtual void OnSetAsservissementMode(byte val)
        //{
        //    OnSetAsservissementModeEvent?.Invoke(this, new ByteEventArgs { Value = val });
        //}

        //public event EventHandler<BoolEventArgs> OnEnableMotorsEvent;
        //public virtual void OnEnableMotors(bool val)
        //{
        //    OnEnableMotorsEvent?.Invoke(this, new BoolEventArgs { value = val });
        //}

        //public event EventHandler<BoolEventArgs> OnMirrorModeForwardEvent;
        //public virtual void OnMirrorMode(object sender, BoolEventArgs val)
        //{
        //    OnMirrorModeForwardEvent?.Invoke(sender, val);
        //}
    }    
}

