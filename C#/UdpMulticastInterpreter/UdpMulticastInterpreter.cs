using EventArgsLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using Utilities;
using WorldMap;

namespace UdpMulticastInterpreter
{
    public class UDPMulticastInterpreter
    {
        int Id;
        public UDPMulticastInterpreter(int id)
        {
            Id = id;
        }
        
        DecimalJsonConverter decimalJsonConverter = new DecimalJsonConverter();
        public void OnMulticastDataReceived(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            lock (e.Data)
            {
                //var compressedData = e.Data;
                //Console.WriteLine(string.Format("Received bytes: {0}", compressedData.Length));
                //string uncompressedTextData = "";// Zip.UnzipText(compressedData);
                //                                 //Console.WriteLine(string.Format("Decompressed bytes: {0}", uncompressedTextData.Length));
                
                JObject obj = JObject.Parse(Encoding.Default.GetString(e.Data));
                try
                {
                    switch ((string)obj["Type"])
                    {
                        case "LocalWorldMap":
                            LocalWorldMap lwm = obj.ToObject<LocalWorldMap>();
                            OnLocalWorldMap(lwm);
                            break;
                        case "GlobalWorldMap":
                            GlobalWorldMap gwm = obj.ToObject<GlobalWorldMap>();
                            OnGlobalWorldMap(gwm);
                            break;
                        default:
                            break;
                    }
                }
                catch
                {
                    Console.WriteLine("Exception Message non décodable en json : UdpMulticastInterpreter");
                }
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
