using AdvancedTimers;
using Constants;
using EventArgsLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCPAdapter;
using TCPClient;
using UDPAdapter;
using Utilities;
using Timer = System.Timers.Timer;

namespace LidarOMD60M
{
    public class Lidar_OMD60M_UDP
    {
        int robotId = 0;

        private static readonly HttpClient HttpClient = new HttpClient();
        IPAddress LidarIpAddress = new IPAddress(new byte[] { 169, 254, 235, 44 });
        IPAddress LocalIp = null;// new IPAddress(new byte[] { 172, 16, 79, 101 });
        //IPAddress LidarUdpAddress = new IPAddress(new byte[] { 192, 168, 10, 20 });        

        int TCPPort = 555;
        int UDPPort = 54321;
        string Handle = "";

        TCPAdapter.TCPAdapter TcpLidarAdapter;
        UDPReceiver UdpLidarAdapter;

        Timer watchDogFeedTimer;
        Timer LidarDisplayTimer;

        //Dernière données acquises
        List<PolarPoint> lastLidarPtList = new List<PolarPoint>();
        int lastScanNumber;
        bool newLidarDataAvailable = false;


        static EventWaitHandle _waitHandle = new AutoResetEvent(false);

        public Lidar_OMD60M_UDP(int id)
        {
            robotId = id;
            LocalIp = GetComputerIp();

            //On lance un tmer de fedd watchdog pour éviter la déconnexion du LIDAR
            watchDogFeedTimer = new Timer(2000);
            watchDogFeedTimer.Elapsed += WatchDogFeedTimer_Elapsed;
            LidarDisplayTimer = new Timer(80);
            LidarDisplayTimer.Elapsed += LidarDisplayTimer_Elapsed;

            //On récupère le handle de connection UDP
            LidarUDPInit();

            //On configure les options du LIDAR
            LidarSetApplicationBitmapMode();
            LidarSetImage(0);
            //LidarSetBarSetText("RCT forever !", "GOOAAL !", "Passe la balle !", "Tire tout de suite !");
            LidarSetRotationFrequency(20);
            LidarSetScanResolution(10080);


            UdpLidarAdapter = new UDPReceiver(LidarIpAddress.ToString(), UDPPort);
            UdpLidarAdapter.OnDataReceivedEvent += UdpLidarAdapter_OnDataReceivedEvent;
            //TcpLidarAdapter = new TCPAdapter.TCPAdapter(LidarIpAddress.ToString(), TCPPort, "Lidar TCP Adapter");
            //TcpLidarAdapter.OnDataReceivedEvent += TcpLidarAdapter_OnDataReceivedEvent;

            watchDogFeedTimer.Start();
            LidarDisplayTimer.Start();

            ////On démarre le scan
            LidarStartScan();
            //swLidar.Start();

            new Thread(SendLidarDecodedFrameEvents).Start();
        }

        private void UdpLidarAdapter_OnDataReceivedEvent(object sender, DataReceivedArgs e)
        {
            DecodeLidarScanData(e.Data, e.Data.Length);
        }

        private void TcpLidarAdapter_OnDataReceivedEvent(object sender, DataReceivedArgs e)
        {
            DecodeLidarScanData(e.Data, e.Data.Length);
        }

        private void SendLidarDecodedFrameEvents()
        {
            while (true)
            {
                if(newLidarDataAvailable)
                {
                    //lock(lastLidarPtList)
                    {
                        OnLidarDecodedFrame((int)TeamId.Team1, lastLidarPtList, lastScanNumber); //TODO on creuse le bug de lag
                        Console.WriteLine(lastScanNumber + " Envoi de la trame lidar sur event");
                        newLidarDataAvailable = false;
                    }
                }
                else
                {
                    Thread.Sleep(5);
                }
                //Console.WriteLine("Waiting...");
                //_waitHandle.WaitOne(); // Wait for notification
                //Console.WriteLine("Notified");
            }
        }

        private void LidarSendMapTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (lastLidarPtList)
            {
                
            }
        }

        int horizontalShift = -50;
        private void LidarDisplayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            horizontalShift += 2;
            if (horizontalShift > 130)
                horizontalShift = -100;
            LidarSetImage(horizontalShift);
        }

        private void WatchDogFeedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            LidarFeedWatchdogTCP();
        }

        ///********************************************** Version UDP ******************************************/
        private async Task LidarUDPInit()
        {
            // On demande un handle UDP : cf. page 32 de la doc R2000 Ehternet Protocol
            Task<string[]> udpHandleRequestTask = Task.Run(() => LidarRequestUDPHandle());
            udpHandleRequestTask.Wait();
            Handle = udpHandleRequestTask.Result[0];
            // = Int32.Parse(udpHandleRequestTask.Result[1]);
        }


        /********************************************** Version TCP ******************************************/
        private async Task LidarTCPInit()
        {
            // On demande un handle UDP : cf. page 32 de la doc R2000 Ehternet Protocol
            Task<string[]> tcpHandleRequestTask = Task.Run(() => LidarRequestTCPHandle());
            tcpHandleRequestTask.Wait();
            Handle = tcpHandleRequestTask.Result[0];
            TCPPort = Int32.Parse(tcpHandleRequestTask.Result[1]);
        }

        async Task<string[]> LidarRequestTCPHandle() 
        {
            string[] result = new string[2];
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/request_handle_tcp?packet_type=A&watchdog=on&watchdogtimeout=10000";
            var content = await HttpClient.GetStringAsync(request);
            JObject info = (JObject)JsonConvert.DeserializeObject(content);
            if (info["error_text"].ToString() == "success")
            {
                result[0] = info["handle"].ToString();
                result[1] = info["port"].ToString();
                Console.WriteLine("Lidar Tcp Handle Request Result : \n" + content);
            }
            else
            {
                Console.WriteLine(content);
            }
            return result;
        }

        async Task<string[]> LidarRequestUDPHandle()
        {
            string[] result = new string[2];
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/request_handle_udp?address=" + LocalIp + "&port=" + UDPPort + "&packet_type=A&watchdog=on&watchdogtimeout=10000";
            var content = await HttpClient.GetStringAsync(request);
            JObject info = (JObject)JsonConvert.DeserializeObject(content);
            if (info["error_text"].ToString() == "success")
            {
                result[0] = info["handle"].ToString();
                //result[1] = info["port"].ToString();
                Console.WriteLine("Lidar Udp Handle Request Result : \n"+content);
            }
            else
            {
                Console.WriteLine(content);
            }
            return result;
        }

        async Task LidarStartScan()
        {
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/start_scanoutput?handle=" + Handle;
            var content = await HttpClient.GetStringAsync(request);
            Console.WriteLine("Lidar Start Scan : \n" + content);
        }

        async Task LidarStopTCPScan()
        {
            // On démarre le scan sur le Lidar : cf. page 35 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/stop_scanoutput?handle=" + Handle;
            var content = await HttpClient.GetStringAsync(request);
            //Console.WriteLine(content);
        }

        async Task LidarFeedWatchdogTCP()
        {
            // On lance un feedwatchdog
            string request = @"http://" + LidarIpAddress + "/cmd/feed_watchdog?handle=" + Handle;
            var content = await HttpClient.GetStringAsync(request);
            //Console.WriteLine(content);
        }

        async Task LidarSetRotationFrequency(int freq)
        {
            // On démarre le scan sur le Lidar : cf. page 20 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?scan_frequency=" + freq.ToString();
            var content = await HttpClient.GetStringAsync(request);
            //Console.WriteLine(content);
        }
        async Task LidarSetScanResolution(int nbPtTour)
        {
            // On règle le nb de pts par tour du Lidar : cf. page 21 de la doc R2000 Ehternet Protocol
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?samples_per_scan=" + nbPtTour.ToString();
            var content = await HttpClient.GetStringAsync(request);
            //Console.WriteLine(content);
        }        

        async Task LidarSetBarGraphDistance()
        {
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_display_mode=bargraph_distance";
            var content = await HttpClient.GetStringAsync(request);
            //Console.WriteLine(content);
        }

        async Task LidarSetBarSetText(string text1, string text2, string text3, string text4)
        {
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_display_mode=application_text";
            var content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_static_text_1=" + text1;
            content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_static_text_2=" + text2;
            content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_application_text_1=" + text3;
            content = await HttpClient.GetStringAsync(request);
            request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_application_text_2=" + text4;
            content = await HttpClient.GetStringAsync(request);
            //Console.WriteLine(content);
        }

        async Task LidarSetApplicationBitmapMode()
        {
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_display_mode=application_bitmap";
            var content = await HttpClient.GetStringAsync(request);
            //Console.WriteLine(content);
        }

        async Task LidarSetImage(int horizontalShift)
        {
            Image image = DrawText("Merci Pepperl+Fuchs", new Font("Verdana", 12.0f, FontStyle.Bold), -horizontalShift);

            var array = ConvertBitmapToArray(new Bitmap(image));
            var bitmapString = ConvertToBase64StringForLidar(array);
            
            string request = @"http://" + LidarIpAddress + "/cmd/set_parameter?hmi_application_bitmap="+ bitmapString;
            var content = await HttpClient.GetStringAsync(request);
        }


        List<double> RSSI = new List<double>();
        Stopwatch sw = new Stopwatch();

        /**************************************************** Fonctions d'analyse **************************************************************/        
        Stopwatch swLidar = new Stopwatch();

        List<LidarPacket> scanLidarPacketsList = new List<LidarPacket>();
        List<PolarPoint> lidarPtList = new List<PolarPoint>();
        private void DecodeLidarScanData(byte[] buffer, int bufferSize)
        {         
            int pos = 0;

            //On construit le buffer à décoder comme la fin du précédent non décodée + le buffer arrivant
            //byte[] buffer = new byte[bufferSize + complementOfPreceedingBuffer.Length];
            //complementOfPreceedingBuffer.CopyTo(buffer,0);
            //arrivingBuffer.CopyTo(buffer, complementOfPreceedingBuffer.Length);

            LidarPacket lp = new LidarPacket();

            try
            {
                if(bufferSize==1404)
                {
                    pos = 0;

                    lp.magic = buffer[pos++];
                    lp.magic += (UInt16)(buffer[pos++] << 8);
                    //Console.WriteLine("magic : 0x" + magic.ToString("X4"));
                    if (lp.magic != 0xA25C && lp.magic != 0x3F5C)
                        return;

                    lp.packet_type = buffer[pos++];
                    lp.packet_type += (UInt16)(buffer[pos++] << 8);

                    lp.packet_size = buffer[pos++];
                    lp.packet_size += (UInt32)(buffer[pos++] << 8);
                    lp.packet_size += (UInt32)(buffer[pos++] << 16);
                    lp.packet_size += (UInt32)(buffer[pos++] << 24);

                    lp.header_size = buffer[pos++];
                    lp.header_size += (UInt16)(buffer[pos++] << 8);

                    lp.scan_number = buffer[pos++];
                    lp.scan_number += (UInt16)(buffer[pos++] << 8);

                    lp.packet_number = buffer[pos++];
                    lp.packet_number += (UInt16)(buffer[pos++] << 8);

                    lp.timestamp_raw = buffer[pos++];
                    lp.timestamp_raw += (UInt64)(buffer[pos++] << 8);
                    lp.timestamp_raw += (UInt64)(buffer[pos++] << 16);
                    lp.timestamp_raw += (UInt64)(buffer[pos++] << 24);
                    lp.timestamp_raw += (UInt64)(buffer[pos++] << 32);
                    lp.timestamp_raw += (UInt64)(buffer[pos++] << 40);
                    lp.timestamp_raw += (UInt64)(buffer[pos++] << 48);
                    lp.timestamp_raw += (UInt64)(buffer[pos++] << 56);

                    lp.timestamp_sync = buffer[pos++];
                    lp.timestamp_sync += (UInt64)(buffer[pos++] << 8);
                    lp.timestamp_sync += (UInt64)(buffer[pos++] << 16);
                    lp.timestamp_sync += (UInt64)(buffer[pos++] << 24);
                    lp.timestamp_sync += (UInt64)(buffer[pos++] << 32);
                    lp.timestamp_sync += (UInt64)(buffer[pos++] << 40);
                    lp.timestamp_sync += (UInt64)(buffer[pos++] << 48);
                    lp.timestamp_sync += (UInt64)(buffer[pos++] << 56);

                    lp.status_flags = buffer[pos++];
                    lp.status_flags += (UInt32)(buffer[pos++] << 8);
                    lp.status_flags += (UInt32)(buffer[pos++] << 16);
                    lp.status_flags += (UInt32)(buffer[pos++] << 24);

                    lp.scan_frequency = buffer[pos++];
                    lp.scan_frequency += (UInt32)(buffer[pos++] << 8);
                    lp.scan_frequency += (UInt32)(buffer[pos++] << 16);
                    lp.scan_frequency += (UInt32)(buffer[pos++] << 24);

                    lp.num_points_scan = buffer[pos++];
                    lp.num_points_scan += (UInt16)(buffer[pos++] << 8);

                    lp.num_points_packet = buffer[pos++];
                    lp.num_points_packet += (UInt16)(buffer[pos++] << 8);

                    lp.first_index = buffer[pos++];
                    lp.first_index += (UInt16)(buffer[pos++] << 8);

                    lp.first_angle = buffer[pos++];
                    lp.first_angle += (Int32)(buffer[pos++] << 8);
                    lp.first_angle += (Int32)(buffer[pos++] << 16);
                    lp.first_angle += (Int32)(buffer[pos++] << 24);

                    lp.angular_increment = buffer[pos++];
                    lp.angular_increment += (Int32)(buffer[pos++] << 8);
                    lp.angular_increment += (Int32)(buffer[pos++] << 16);
                    lp.angular_increment += (Int32)(buffer[pos++] << 24);

                    lp.iq_input = buffer[pos++];
                    lp.iq_input += (UInt32)(buffer[pos++] << 8);
                    lp.iq_input += (UInt32)(buffer[pos++] << 16);
                    lp.iq_input += (UInt32)(buffer[pos++] << 24);

                    lp.iq_overload = buffer[pos++];
                    lp.iq_overload += (UInt32)(buffer[pos++] << 8);
                    lp.iq_overload += (UInt32)(buffer[pos++] << 16);
                    lp.iq_overload += (UInt32)(buffer[pos++] << 24);

                    lp.iq_timestamp_raw = buffer[pos++];
                    lp.iq_timestamp_raw += (UInt64)(buffer[pos++] << 8);
                    lp.iq_timestamp_raw += (UInt64)(buffer[pos++] << 16);
                    lp.iq_timestamp_raw += (UInt64)(buffer[pos++] << 24);
                    lp.iq_timestamp_raw += (UInt64)(buffer[pos++] << 32);
                    lp.iq_timestamp_raw += (UInt64)(buffer[pos++] << 40);
                    lp.iq_timestamp_raw += (UInt64)(buffer[pos++] << 48);
                    lp.iq_timestamp_raw += (UInt64)(buffer[pos++] << 56);

                    lp.iq_timestamp_sync = buffer[pos++];
                    lp.iq_timestamp_sync += (UInt64)(buffer[pos++] << 8);
                    lp.iq_timestamp_sync += (UInt64)(buffer[pos++] << 16);
                    lp.iq_timestamp_sync += (UInt64)(buffer[pos++] << 24);
                    lp.iq_timestamp_sync += (UInt64)(buffer[pos++] << 32);
                    lp.iq_timestamp_sync += (UInt64)(buffer[pos++] << 40);
                    lp.iq_timestamp_sync += (UInt64)(buffer[pos++] << 48);
                    lp.iq_timestamp_sync += (UInt64)(buffer[pos++] << 56);

                    //On initialise le pointeur au début de scan data
                    int shift = (4 - pos % 4)%4;
                    pos += shift;
                    pos = lp.header_size;

                    lp.ptList = new List<PolarPoint>();
                    for (int index = 0; index < lp.num_points_packet; index++)
                    {
                        if (lp.packet_type == 'A')
                        {
                            UInt32 dist = buffer[pos++];
                            dist += (UInt32)(buffer[pos++] << 8);
                            dist += (UInt32)(buffer[pos++] << 16);
                            dist += (UInt32)(buffer[pos++] << 24);
                            lp.ptList.Add(new PolarPoint(dist / 1000.0, (lp.first_angle + index * lp.angular_increment) / 10000.0 * Math.PI / 180.0));
                        }
                    }

                    Console.WriteLine("Paquet Lidar - Taille : " + lp.packet_size + " - Taille Header : " + lp.header_size + " - Scan Number : " + lp.scan_number + " - Numero de paquet : " + lp.packet_number + " - Pos : " + pos + " - Buffer Size : " + bufferSize);

                    if (lp.packet_number ==1)
                    {
                        int nbPacketAttendus = lp.num_points_scan / lp.num_points_packet;
                        if (nbPacketAttendus == scanLidarPacketsList.Count)
                        {
                            lidarPtList = new List<PolarPoint>();
                            foreach(var p in scanLidarPacketsList)
                                lidarPtList.AddRange(lp.ptList);
                            lock (lastLidarPtList)
                            {
                                lastLidarPtList = lidarPtList;
                                lastScanNumber = lp.scan_number;
                                newLidarDataAvailable = true;
                            }
                        }
                        else
                            ;
                        //On réinitialise la liste permettant de stocker les packets
                        scanLidarPacketsList = new List<LidarPacket>();                        
                    }

                    scanLidarPacketsList.Add(lp);





                }
                // TODO : A blinder si les packets sont trop petits 
                //while (pos+1404 <= bufferSize)
                //{
                //    //On a encore au moins un packet complet à décoder, on peut y aller
                //    //On stocke la position initiale pour pouvoir la restaurer au moment de la lecture des datas
                //    int initPos = pos;

                //    UInt16 magic = buffer[pos++];
                //    magic += (UInt16)(buffer[pos++] << 8);
                //    //Console.WriteLine("magic : 0x" + magic.ToString("X4"));
                //    if (magic != 0xA25C && magic != 0x3F5C)
                //        return;

                //    UInt16 packet_type = buffer[pos++];
                //    packet_type += (UInt16)(buffer[pos++] << 8);

                //    UInt32 packet_size = buffer[pos++];
                //    packet_size += (UInt32)(buffer[pos++] << 8);
                //    packet_size += (UInt32)(buffer[pos++] << 16);
                //    packet_size += (UInt32)(buffer[pos++] << 24);

                //    UInt16 header_size = buffer[pos++];
                //    header_size += (UInt16)(buffer[pos++] << 8);

                //    UInt16 scan_number = buffer[pos++];
                //    scan_number += (UInt16)(buffer[pos++] << 8);

                //    UInt16 packet_number = buffer[pos++];
                //    packet_number += (UInt16)(buffer[pos++] << 8);
                //    //Console.WriteLine("Packet Number : " + packet_number);


                //    Console.WriteLine("Paquet Lidar - Taille : " + packet_size + " - Taille Header : " + header_size + " - Scan Number : " + scan_number + " - Numero de paquet : " + packet_number + " - Pos : " + pos + " - Buffer Size : " + bufferSize);

                //    if (packet_number == 1)
                //    {
                //        //lock (lastLidarPtList)
                //        {
                //            lastLidarPtList = ptList;
                //            lastScanNumber = scan_number;
                //            newLidarDataAvailable = true;
                //        }
                //        //OnLidarDecodedFrame((int)TeamId.Team1, ptList, scan_number); //TODO on creuse le bug de lag

                //        if (ptList[ptList.Count / 2].Distance > 2)
                //            Console.WriteLine(scan_number + " Distance en face : " + ptList[ptList.Count / 2].Distance + "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
                //        else
                //            Console.WriteLine(scan_number + " Distance en face : " + ptList[ptList.Count / 2].Distance);
                //        //Console.WriteLine(nbDataLidar++ + "Temps event lancé par lidar : " + sw.Elapsed.TotalMilliseconds+ " instant : "+ swLidar.Elapsed.TotalMilliseconds);
                //        ptList = new List<PolarPoint>();
                //        RSSI = new List<double>();
                //    }

                //    //Console.WriteLine("Scan Number : " + scan_number);

                //    UInt64 timestamp_raw = buffer[pos++];
                //    timestamp_raw += (UInt64)(buffer[pos++] << 8);
                //    timestamp_raw += (UInt64)(buffer[pos++] << 16);
                //    timestamp_raw += (UInt64)(buffer[pos++] << 24);
                //    timestamp_raw += (UInt64)(buffer[pos++] << 32);
                //    timestamp_raw += (UInt64)(buffer[pos++] << 40);
                //    timestamp_raw += (UInt64)(buffer[pos++] << 48);
                //    timestamp_raw += (UInt64)(buffer[pos++] << 56);

                //    UInt64 timestamp_sync = buffer[pos++];
                //    timestamp_sync += (UInt64)(buffer[pos++] << 8);
                //    timestamp_sync += (UInt64)(buffer[pos++] << 16);
                //    timestamp_sync += (UInt64)(buffer[pos++] << 24);
                //    timestamp_sync += (UInt64)(buffer[pos++] << 32);
                //    timestamp_sync += (UInt64)(buffer[pos++] << 40);
                //    timestamp_sync += (UInt64)(buffer[pos++] << 48);
                //    timestamp_sync += (UInt64)(buffer[pos++] << 56);

                //    UInt32 status_flags = buffer[pos++];
                //    status_flags += (UInt32)(buffer[pos++] << 8);
                //    status_flags += (UInt32)(buffer[pos++] << 16);
                //    status_flags += (UInt32)(buffer[pos++] << 24);

                //    UInt32 scan_frequency = buffer[pos++];
                //    scan_frequency += (UInt32)(buffer[pos++] << 8);
                //    scan_frequency += (UInt32)(buffer[pos++] << 16);
                //    scan_frequency += (UInt32)(buffer[pos++] << 24);

                //    UInt16 num_points_scan = buffer[pos++];
                //    num_points_scan += (UInt16)(buffer[pos++] << 8);

                //    UInt16 num_points_packet = buffer[pos++];
                //    num_points_packet += (UInt16)(buffer[pos++] << 8);

                //    UInt16 first_index = buffer[pos++];
                //    first_index += (UInt16)(buffer[pos++] << 8);

                //    Int32 first_angle = buffer[pos++];
                //    first_angle += (Int32)(buffer[pos++] << 8);
                //    first_angle += (Int32)(buffer[pos++] << 16);
                //    first_angle += (Int32)(buffer[pos++] << 24);
                //    //Console.WriteLine("First Angle : " + first_angle);

                //    Int32 angular_increment = buffer[pos++];
                //    angular_increment += (Int32)(buffer[pos++] << 8);
                //    angular_increment += (Int32)(buffer[pos++] << 16);
                //    angular_increment += (Int32)(buffer[pos++] << 24);

                //    UInt32 iq_input = buffer[pos++];
                //    iq_input += (UInt32)(buffer[pos++] << 8);
                //    iq_input += (UInt32)(buffer[pos++] << 16);
                //    iq_input += (UInt32)(buffer[pos++] << 24);

                //    UInt32 iq_overload = buffer[pos++];
                //    iq_overload += (UInt32)(buffer[pos++] << 8);
                //    iq_overload += (UInt32)(buffer[pos++] << 16);
                //    iq_overload += (UInt32)(buffer[pos++] << 24);

                //    UInt64 iq_timestamp_raw = buffer[pos++];
                //    iq_timestamp_raw += (UInt64)(buffer[pos++] << 8);
                //    iq_timestamp_raw += (UInt64)(buffer[pos++] << 16);
                //    iq_timestamp_raw += (UInt64)(buffer[pos++] << 24);
                //    iq_timestamp_raw += (UInt64)(buffer[pos++] << 32);
                //    iq_timestamp_raw += (UInt64)(buffer[pos++] << 40);
                //    iq_timestamp_raw += (UInt64)(buffer[pos++] << 48);
                //    iq_timestamp_raw += (UInt64)(buffer[pos++] << 56);

                //    UInt64 iq_timestamp_sync = buffer[pos++];
                //    iq_timestamp_sync += (UInt64)(buffer[pos++] << 8);
                //    iq_timestamp_sync += (UInt64)(buffer[pos++] << 16);
                //    iq_timestamp_sync += (UInt64)(buffer[pos++] << 24);
                //    iq_timestamp_sync += (UInt64)(buffer[pos++] << 32);
                //    iq_timestamp_sync += (UInt64)(buffer[pos++] << 40);
                //    iq_timestamp_sync += (UInt64)(buffer[pos++] << 48);
                //    iq_timestamp_sync += (UInt64)(buffer[pos++] << 56);

                //    //On initialise le pointeur au début de scan data
                //    pos = initPos + header_size;
                //    for (int index = 0; index < num_points_packet; index++)
                //    {
                //        if (packet_type == 'A')
                //        {
                //            UInt32 dist = buffer[pos++];
                //            dist += (UInt32)(buffer[pos++] << 8);
                //            dist += (UInt32)(buffer[pos++] << 16);
                //            dist += (UInt32)(buffer[pos++] << 24);
                //            ptList.Add(new PolarPoint(dist / 1000.0, (first_angle + index * angular_increment) / 10000.0 * Math.PI / 180.0));
                //        }
                //    }
                //}
                ////On a plus un packet complet à décoder
                //complementOfPreceedingBuffer = new byte[buffer.Length - pos];
                //Buffer.BlockCopy(buffer, pos, complementOfPreceedingBuffer, 0, complementOfPreceedingBuffer.Length); //On copie les octets non processés dans le buffer d'attente
            }
            catch
            {
            }
        }

        IPAddress GetComputerIp()
        {
            //TODO : basé sur le fait que la bonne adresse est la première...
            IPAddress localIp = null;
            //Get Local IP Address
            //Console.WriteLine(Dns.GetHostName());
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = addr;
                    return localIp;
                }
            }
            return localIp;
        }

        private Image DrawText(String text, Font font, int horizontalShift)
        {
            Color textColor = Color.Black;
            Color backColor = Color.White;

            int width = 252;
            int height = 24;
            //On créé une image de taille réduite par trois pour avoir le vrai affichage
            var img = new Bitmap(width/3, height);
            Graphics drawing = Graphics.FromImage(img);

            //measure the string to see how big the image needs to be
            //SizeF textSize = drawing.MeasureString(text, font);

            //paint the background
            drawing.Clear(backColor);

            //create a brush for the text
            Brush textBrush = new SolidBrush(textColor);
            drawing.DrawString(text, font, textBrush, horizontalShift, 0);
            drawing.Save();
            textBrush.Dispose();
            drawing.Dispose();

            Bitmap finalImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                g.DrawImage(img, 0, 0, width, height);
            }

            return finalImage;

        }

        public static byte[] ConvertBitmapToArray(Bitmap bmp, int horizontalShift=0)
        {
            var size = bmp.Width * bmp.Height / 8;
            var buffer = new byte[size];

            var i = 0;
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = bmp.Height-1; y >= 0; y--)
                {                
                    var color = bmp.GetPixel(x, y);
                    var intensite = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B; //equation RGB -> YUV

                    if (intensite >= 255 / 2)
                    {
                        int index = i + horizontalShift* bmp.Height;
                        if (index >= bmp.Width* bmp.Height)
                            index -= bmp.Width * bmp.Height;

                        int pos = index/8;                        
                        var bitInByteIndex = index % 8;

                        //On calcule la position décalée

                        buffer[pos] |= (byte)(1 << 7 - bitInByteIndex);
                    }
                    i++;
                }
            }

            return buffer;
        }

        string ConvertToBase64StringForLidar(byte[] bytes)
        {
            string s = System.Convert.ToBase64String(bytes);
            s=s.Replace('+', '-');
            s=s.Replace('/', '_');
            return s;
        }


        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarDecodedFrameEvent;
        public virtual void OnLidarDecodedFrame(int id, List<PolarPoint> ptList, int lidarFrameNumber=0)
        {
            var handler = OnLidarDecodedFrameEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, PtList = ptList, LidarFrameNumber=lidarFrameNumber});
            }
        }
    }

    class LidarPacket
    {
        public UInt16 magic;
        public UInt16 packet_type;
        public UInt32 packet_size;
        public UInt16 header_size;
        public UInt16 scan_number;
        public UInt16 packet_number;
        public UInt64 timestamp_raw;
        public UInt64 timestamp_sync;
        public UInt32 status_flags;
        public UInt32 scan_frequency;
        public UInt16 num_points_scan;
        public UInt16 num_points_packet;
        public UInt16 first_index;
        public Int32 first_angle;
        public Int32 angular_increment;
        public UInt32 iq_input;
        public UInt32 iq_overload;
        public UInt64 iq_timestamp_raw;
        public UInt64 iq_timestamp_sync;
        public List<PolarPoint> ptList = new List<PolarPoint>();
    }
}
