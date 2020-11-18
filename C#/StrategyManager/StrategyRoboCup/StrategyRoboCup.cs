using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrategyManager.StrategyRoboCupNS
{
    class StrategyRoboCup : StrategyInterface
    {
        public void EvaluateStrategy()
        {

        }

        public event EventHandler<ByteEventArgs> OnSetAsservissementModeEvent;
    }
}
