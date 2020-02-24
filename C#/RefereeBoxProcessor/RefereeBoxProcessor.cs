using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefereeBoxProcessor
{
    public class RefereeBoxProcessor
    {
        public void OnRefereeBoxCommandReceived(object sender, StringArgs e)
        {
            OnMulticastSendCommand(Encoding.ASCII.GetBytes(e.Value));
        }

        //Output events
        public event EventHandler<DataReceivedArgs> OnMulticastSendEvent;
        public virtual void OnMulticastSendCommand(byte[] data)
        {
            var handler = OnMulticastSendEvent;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }
    }
}
