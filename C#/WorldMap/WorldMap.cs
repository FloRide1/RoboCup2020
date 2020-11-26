using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroFormatter;

namespace WorldMap
{
    public enum WorldMapType
    {
        LocalWM, GlobalWM
    }

    // UnionAttribute abstract/interface type becomes Union, arguments is union subtypes.
    // It needs single UnionKey to discriminate
    [Union(typeof(LocalWorldMap), typeof(GlobalWorldMap))]
    public abstract class WorldMap
    {
        [UnionKey]
        public abstract WorldMapType Type { get; }
    }
}
