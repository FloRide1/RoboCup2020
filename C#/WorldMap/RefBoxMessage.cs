using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using ZeroFormatter;

namespace WorldMap
{
    [ZeroFormattable]
    public class RefBoxMessage: ZeroFormatterMsg
    {
        public override ZeroFormatterMsgType Type
        {
            get
            {
                return ZeroFormatterMsgType.RefBoxMsg;
            }
        }
        [Index(0)]
        public virtual RefBoxCommand command { get; set; }
        [Index(1)]
        public virtual string targetTeam { get; set; }
        [Index(2)]
        public virtual int robotID { get; set; }
    }

    public class RefBoxMessageArgs : EventArgs
    {
        public RefBoxMessage refBoxMsg { get; set; }
    }
}
