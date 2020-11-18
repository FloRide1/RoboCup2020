using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldMap;

namespace StrategyManager
{
    interface StrategyInterface
    {
        void EvaluateStrategy();

        event EventHandler<ByteEventArgs> OnSetAsservissementModeEvent;

    }
}
