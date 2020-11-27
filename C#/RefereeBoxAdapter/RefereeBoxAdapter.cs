using AdvancedTimers;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using TCPAdapter;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Converters;
using System.Globalization;
using Utilities;
using ZeroFormatter;
using WorldMap;

namespace RefereeBoxAdapter
{
    public class RefereeBoxAdapter
    {
        TCPAdapter.TCPAdapter tcpAdapter;

        public RefereeBoxAdapter()
        {
            Thread StartRefBoxAdapterThread = new Thread(StartRefBoxAdapter);
            StartRefBoxAdapterThread.IsBackground = true;
            StartRefBoxAdapterThread.Start();
        }

        private void StartRefBoxAdapter()
        {
            tcpAdapter = new TCPAdapter.TCPAdapter("172.16.1.2", 28097, "Referee Box Adapter");
            tcpAdapter.OnDataReceivedEvent += TcpAdapter_OnDataReceivedEvent;
        }

        private void TcpAdapter_OnDataReceivedEvent(object sender, DataReceivedArgs e)
        {
            //On deserialize le message JSON en provenance de la Referee Box
            string s = Encoding.ASCII.GetString(e.Data);
            var refBoxCommand = JsonConvert.DeserializeObject<RefBoxMessage>(s);

            var msg = ZeroFormatterSerializer.Serialize<ZeroFormatterMsg>(refBoxCommand);
            OnMulticastSendRefBoxCommand(msg);
        }

        //Output events
        public event EventHandler<DataReceivedArgs> OnMulticastSendRefBoxCommandEvent;
        public virtual void OnMulticastSendRefBoxCommand(byte[] data)
        {
            var handler = OnMulticastSendRefBoxCommandEvent;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }
    }

    //public class RefBoxMessage 
    //{
    //    public RefBoxCommand command { get; set; }
    //    public string targetTeam { get; set; }
    //    public int robotID { get; set; }
    //}

    //public class RefBoxMessageArgs : EventArgs
    //{
    //    public RefBoxMessage refBoxMsg { get; set; }
    //}
}
