﻿using AdvancedTimers;
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

namespace RefereeBoxAdapter
{
    public class RefereeBoxAdapter
    {
        TCPAdapter.TCPAdapter tcpAdapter;

        public RefereeBoxAdapter()
        {
            tcpAdapter = new TCPAdapter.TCPAdapter("172.16.1.2", 28097, "Referee Box Adapter");
            tcpAdapter.OnDataReceivedEvent += TcpAdapter_OnDataReceivedEvent;
        }

        private void TcpAdapter_OnDataReceivedEvent(object sender, DataReceivedArgs e)
        {
            //On deserialize le message JSON en provenance de la Referee Box
            string s = Encoding.ASCII.GetString(e.Data);
            var json = JsonConvert.DeserializeObject<RefBoxMessage>(s);
            OnRefereeBoxReceivedCommand(json.command);
        }

        //Output events
        public event EventHandler<StringArgs> OnRefereeBoxCommandEvent;
        public virtual void OnRefereeBoxReceivedCommand(string data)
        {
            var handler = OnRefereeBoxCommandEvent;
            if (handler != null)
            {
                handler(this, new StringArgs { Value = data });
            }
        }
    }

    public class RefBoxMessage
    {
        public string command { get; set; }
        public string targetTeam { get; set; }
        public int robotID { get; set; }
    }

}
