﻿using EventArgsLibrary;
using Newtonsoft.Json.Linq;
using PerformanceMonitorTools;
using System;
using System.Diagnostics;
using System.Text;
using Utilities;
using WorldMap;
using ZeroFormatter;


namespace UdpMulticastInterpreter
{
    public class UDPMulticastInterpreter
    {
        int Id;
        public UDPMulticastInterpreter(int id)
        {
            Id = id;
        }
        
        public void OnMulticastDataReceived(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            lock (e.Data)
            {
                var deserialzation = ZeroFormatterSerializer.Deserialize<WorldMap.WorldMap>(e.Data);

                switch (deserialzation.Type)
                {
                    case WorldMapType.LocalWM:
                        LocalWorldMap lwm = (LocalWorldMap)deserialzation;
                        //if (Id == lwm.RobotId)
                        {
                            OnLocalWorldMap(lwm);
                            UdpMonitor.LWMReceived(e.Data.Length);
                        }
                        break;
                    case WorldMapType.GlobalWM:
                        GlobalWorldMap gwm = (GlobalWorldMap)deserialzation;
                        UdpMonitor.GWMReceived(e.Data.Length);
                        OnGlobalWorldMap(gwm);
                        break;
                    default:
                        break;
                }

                //var compressedData = e.Data;
                //Console.WriteLine(string.Format("Received bytes: {0}", compressedData.Length));
                //string uncompressedTextData = "";// Zip.UnzipText(compressedData);
                //                                 //Console.WriteLine(string.Format("Decompressed bytes: {0}", uncompressedTextData.Length));

                //JObject obj = JObject.Parse(Encoding.Default.GetString(e.Data));
                //try
                //{
                //    switch ((int)obj["Type"])
                //    {
                //        case (int)WorldMapType.LocalWM:
                //            LocalWorldMap lwm = obj.ToObject<LocalWorldMap>();
                //            OnLocalWorldMap(lwm);
                //            break;
                //        case (int)WorldMapType.GlobalWM:
                //            GlobalWorldMap gwm = obj.ToObject<GlobalWorldMap>();
                //            OnGlobalWorldMap(gwm);
                //            break;
                //        default:
                //            break;
                //    }
                //}
                //catch
                //{
                //    Console.WriteLine("Exception Message non décodable en json : UdpMulticastInterpreter");
                //}
            }
        }

        //Output events
        public event EventHandler<LocalWorldMapArgs> OnLocalWorldMapEvent;
        public virtual void OnLocalWorldMap(LocalWorldMap localWorldMap)
        {
            var handler = OnLocalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new LocalWorldMapArgs {LocalWorldMap = localWorldMap });
            }
        }

        //Output events
        public event EventHandler<GlobalWorldMapArgs> OnGlobalWorldMapEvent;
        public virtual void OnGlobalWorldMap(GlobalWorldMap globalWorldMap)
        {
            var handler = OnGlobalWorldMapEvent;
            if (handler != null)
            {
                handler(this, new GlobalWorldMapArgs { GlobalWorldMap = globalWorldMap });
            }
        }
    }
}
