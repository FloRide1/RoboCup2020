using AdvancedTimers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EthernetTeamNetwork
{
    public class EthernetTeamNetworkAdapter
    {
        private Thread connectionThread;
        NetworkStream networkStream;
        TcpClient refereeBoxClient;
        HighFreqTimer timerPing = new HighFreqTimer(10);
        private IPAddress localIp;

        private bool isRobot = false;
        private bool isCoach = false;
                
        UDPSocket s = new UDPSocket();

        public EthernetTeamNetworkAdapter()
        {
            string strHostName = Dns.GetHostName();
            Console.WriteLine("Local Machine's Host Name: " + strHostName);
            // Then using host name, get the IP address list..
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;

            //TeamDiscovery();
            

            timerPing.Tick += TimerPing_Tick;
            timerPing.Start();
        }

        NetworkMachine Coach = new NetworkMachine();
        NetworkMachine Robot1 = new NetworkMachine();
        NetworkMachine Robot2 = new NetworkMachine();
        NetworkMachine Robot3 = new NetworkMachine();
        Role role = Role.NONE;

        public void TeamDiscovery()
        {
            //Découvre l'ensemble des PC connecté sur le résau et les identifie par adresse MAC
            List<string> Robot1MacList = new List<string> { "2C4D54992E91" };
            List<string> CoachMacList = new List<string> { "D0C637CA253D", "0A0027000012", "D8EB9720C10A" };

            var listMacIp =  IPMacFinder.GetIPsAndMacList();

            //var tot = LocalIpMacScanner.GetMacAddress();
            //var tot2 = LocalIpMacScanner.GetIPAddress();
            //var all = LocalIpMacScanner.GetAllDevicesOnLAN();


            //foreach (var mac in Robot1MacList)
            //{
            //    Console.WriteLine(IPMacFinder.FindIPFromMacAddress(mac));
            //}

            //foreach (var mac in CoachMacList)
            //{
            //    Console.WriteLine(IPMacFinder.FindIPFromMacAddress(mac));
            //}

            foreach (var elt in listMacIp)
            {
                Console.WriteLine("IP : " + elt.IP + " - MAC : " + elt.MAC);
            }
            
            foreach (var elt in listMacIp)
            {
                foreach (var mac in Robot1MacList)
                {
                    if (elt.MAC == mac.ToLower())
                    {
                        Console.WriteLine("Robot 1 trouvé : " + elt.IP + " - MAC :" + elt.MAC);
                        Robot1.IP = elt.IP;
                        Robot1.MAC = elt.MAC;                        
                        if (Robot1.MAC == LocalIpMacScanner.GetMacAddress().ToString())
                            role = Role.ROBOT1;
                    }
                }

                foreach (var mac in CoachMacList)
                {

                    if (elt.MAC == mac)
                    {
                        Console.WriteLine("Coach trouvé : " + elt.IP + " - MAC :" + elt.MAC);
                        Coach.IP = elt.IP;
                        Coach.MAC = elt.MAC;
                        if (Coach.MAC == LocalIpMacScanner.GetMacAddress().ToString())
                            role = Role.COACH;
                    }
                }
            }

            switch (role)
            {
                case Role.ROBOT1:
                    s.Client(Coach.IP.ToString(), 27000);
                    break;

                case Role.COACH:
                    s.Server("127.0.0.1", 27000);
                    break;
            }

            //    //On démarre un serveur UDP                
            //    s.Server("192.168.0.101", 27000);
            //    s.Send("Transmit Coach to Robot");
            //}
            //else if (isRobot)
            //{
            //    s.Client("192.168.0.107", 27000);
            //    s.Send("Transmit Robot to Coach");
            //}
            //{
            //    //On démarre un serveur UDP                
            //    s.Server("192.168.0.101", 27000);
            //    s.Send("Transmit Coach to Robot");
            //}
            //else if (isRobot)
            //{
            //    s.Client("192.168.0.107", 27000);
            //    s.Send("Transmit Robot to Coach");
            //}
        }

        private void TimerPing_Tick(object sender, EventArgs e)
        {
            switch (role)
            {
                case Role.ROBOT1:
                    s.Send("Transmit Robot to Coach");
                    break;

                case Role.COACH:
                    s.Send("Transmit Coach to Robot");
                    break;
            }
        }

        public bool IsIpInRange(string startIpAddr, string endIpAddr, string address)
        {
            long ipStart = BitConverter.ToInt32(IPAddress.Parse(startIpAddr).GetAddressBytes().Reverse().ToArray(), 0);

            long ipEnd = BitConverter.ToInt32(IPAddress.Parse(endIpAddr).GetAddressBytes().Reverse().ToArray(), 0);

            long ip = BitConverter.ToInt32(IPAddress.Parse(address).GetAddressBytes().Reverse().ToArray(), 0);

            return ip >= ipStart && ip <= ipEnd; //edited
        }
    }

    class NetworkMachine
    {
        public string IP;
        public string MAC;
        public Role Role;
    }

    enum Role
    {
        NONE,
        COACH,
        ROBOT1,
        ROBOT2,
        ROBOT3,
        ROBOT4,
        ROBOT5,
        ROBOT6
    }

    public class UDPSocket
    {
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Server(string address, int port)
        {
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            Receive();
        }

        public void Client(string address, int port)
        {
            _socket.Connect(IPAddress.Parse(address), port);
            Receive();
        }

        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndSend(ar);
                Console.WriteLine("SEND: {0}, {1}", bytes, text);
            }, state);
        }

        private void Receive()
        {
            _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes));
            }, state);
        }
    }


    public static class IPMacFinder
    {
        private static List<IPAndMac> list;

        private static StreamReader ExecuteCommandLine(String file, String arguments = "")
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = file;
            startInfo.Arguments = arguments;

            Process process = Process.Start(startInfo);

            return process.StandardOutput;
        }

        public static List<IPAndMac> GetIPsAndMacList()
        {
            List<string> result = new List<string>();
            var arpStream = ExecuteCommandLine("arp", "-a");
            while (!arpStream.EndOfStream)
            {
                var line = arpStream.ReadLine().Trim();
                result.Add(line);
            }

            string patternMAC = "(([0-9A-Fa-f]{2}[:-]{1}){5}[0-9A-Fa-f]{2})";
            string patternIP = "(([0-9A-Fa-f]{1,3}[.]{1}){3}[0-9A-Fa-f]{1,3})";
            Regex regexMAC = new Regex(patternMAC, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Regex regexIP = new Regex(patternIP, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            List<IPAndMac> listMacIp = new List<IPAndMac>();

            foreach (var entry in result)
            {
                Match mMAC = regexMAC.Match(entry);
                Match mIP = regexIP.Match(entry);
                if (mMAC.Success && mIP.Success)
                {
                    var mac = mMAC.Groups[1].ToString();
                    var ip = mIP.Groups[1].ToString();
                    listMacIp.Add(new IPAndMac { IP = ip, MAC = mac.Replace("-","").ToLower()});
                }
            }
            return listMacIp;
        }

        public static string FindIPFromMacAddress(string macAddress)
        {
            GetIPsAndMacList();
            IPAndMac item = list.SingleOrDefault(x => x.MAC == macAddress);
            if (item == null)
                return null;
            return item.IP;
        }

        public static string FindMacFromIPAddress(string ip)
        {
            GetIPsAndMacList();
            IPAndMac item = list.SingleOrDefault(x => x.IP == ip);
            if (item == null)
                return null;
            return item.MAC;
        }

    }
    
    public class IPAndMac
    {
        public string IP { get; set; }
        public string MAC { get; set; }
    }

    static class LocalIpMacScanner
    {

        /// <summary>
        /// MIB_IPNETROW structure returned by GetIpNetTable
        /// DO NOT MODIFY THIS STRUCTURE.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct MIB_IPNETROW
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwIndex;
            [MarshalAs(UnmanagedType.U4)]
            public int dwPhysAddrLen;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac0;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac1;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac2;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac3;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac4;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac5;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac6;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac7;
            [MarshalAs(UnmanagedType.U4)]
            public int dwAddr;
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
        }

        /// <summary>
        /// GetIpNetTable external method
        /// </summary>
        /// <param name="pIpNetTable"></param>
        /// <param name="pdwSize"></param>
        /// <param name="bOrder"></param>
        /// <returns></returns>
        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        static extern int GetIpNetTable(IntPtr pIpNetTable,
              [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);

        /// <summary>
        /// Error codes GetIpNetTable returns that we recognise
        /// </summary>
        const int ERROR_INSUFFICIENT_BUFFER = 122;
        /// <summary>
        /// Get the IP and MAC addresses of all known devices on the LAN
        /// </summary>
        /// <remarks>
        /// 1) This table is not updated often - it can take some human-scale time 
        ///    to notice that a device has dropped off the network, or a new device
        ///    has connected.
        /// 2) This discards non-local devices if they are found - these are multicast
        ///    and can be discarded by IP address range.
        /// </remarks>
        /// <returns></returns>
        public static Dictionary<IPAddress, PhysicalAddress> GetAllDevicesOnLAN()
        {
            Dictionary<IPAddress, PhysicalAddress> all = new Dictionary<IPAddress, PhysicalAddress>();
            // Add this PC to the list...
            all.Add(GetIPAddress(), GetMacAddress());
            int spaceForNetTable = 0;
            // Get the space needed
            // We do that by requesting the table, but not giving any space at all.
            // The return value will tell us how much we actually need.
            GetIpNetTable(IntPtr.Zero, ref spaceForNetTable, false);
            // Allocate the space
            // We use a try-finally block to ensure release.
            IntPtr rawTable = IntPtr.Zero;
            try
            {
                rawTable = Marshal.AllocCoTaskMem(spaceForNetTable);
                // Get the actual data
                int errorCode = GetIpNetTable(rawTable, ref spaceForNetTable, false);
                if (errorCode != 0)
                {
                    // Failed for some reason - can do no more here.
                    throw new Exception(string.Format(
                      "Unable to retrieve network table. Error code {0}", errorCode));
                }
                // Get the rows count
                int rowsCount = Marshal.ReadInt32(rawTable);
                IntPtr currentBuffer = new IntPtr(rawTable.ToInt64() + Marshal.SizeOf(typeof(Int32)));
                // Convert the raw table to individual entries
                MIB_IPNETROW[] rows = new MIB_IPNETROW[rowsCount];
                for (int index = 0; index < rowsCount; index++)
                {
                    rows[index] = (MIB_IPNETROW)Marshal.PtrToStructure(new IntPtr(currentBuffer.ToInt64() +
                                                (index * Marshal.SizeOf(typeof(MIB_IPNETROW)))
                                               ),
                                                typeof(MIB_IPNETROW));
                }
                // Define the dummy entries list (we can discard these)
                PhysicalAddress virtualMAC = new PhysicalAddress(new byte[] { 0, 0, 0, 0, 0, 0 });
                PhysicalAddress broadcastMAC = new PhysicalAddress(new byte[] { 255, 255, 255, 255, 255, 255 });
                foreach (MIB_IPNETROW row in rows)
                {
                    IPAddress ip = new IPAddress(BitConverter.GetBytes(row.dwAddr));
                    byte[] rawMAC = new byte[] { row.mac0, row.mac1, row.mac2, row.mac3, row.mac4, row.mac5 };
                    PhysicalAddress pa = new PhysicalAddress(rawMAC);
                    if (!pa.Equals(virtualMAC) && !pa.Equals(broadcastMAC) && !IsMulticast(ip))
                    {
                        //Console.WriteLine("IP: {0}\t\tMAC: {1}", ip.ToString(), pa.ToString());
                        if (!all.ContainsKey(ip))
                        {
                            all.Add(ip, pa);
                        }
                    }
                }
            }
            finally
            {
                // Release the memory.
                Marshal.FreeCoTaskMem(rawTable);
            }
            return all;
        }

        /// <summary>
        /// Gets the IP address of the current PC
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetIPAddress()
        {
            String strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            foreach (IPAddress ip in addr)
            {
                if (!ip.IsIPv6LinkLocal)
                {
                    return (ip);
                }
            }
            return addr.Length > 0 ? addr[0] : null;
        }

        /// <summary>
        /// Gets the MAC address of the current PC.
        /// </summary>
        /// <returns></returns>
        public static PhysicalAddress GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only consider Ethernet network interfaces
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.OperationalStatus == OperationalStatus.Up)
                {
                    return nic.GetPhysicalAddress();
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if the specified IP address is a multicast address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static bool IsMulticast(IPAddress ip)
        {
            bool result = true;
            if (!ip.IsIPv6Multicast)
            {
                byte highIP = ip.GetAddressBytes()[0];
                if (highIP < 224 || highIP > 239)
                {
                    result = false;
                }
            }
            return result;
        }
    }
}
