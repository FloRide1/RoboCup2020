using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;


namespace UDPMulticast
{
    public class UDPMulticastSender
    {
        private string multicastRctIpAddress = "224.16.32.79";
        private int endPointPort = 4567;
        Socket s;

        public UDPMulticastSender()
        {
            s = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            IPAddress ip = IPAddress.Parse(multicastRctIpAddress);
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip));
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
            IPEndPoint ipep = new IPEndPoint(ip, endPointPort);
            s.Connect(ipep);
        }

        public void Send(byte[] buffer)
        {
            s.Send(buffer, buffer.Length, SocketFlags.None);
        }
    }

    public class UDPMulticastReceiver
    {
        private string multicastRctIpAddress = "224.16.32.79";
        private int endPointPort = 4567;
        Socket s;
        StateObject so2 = new StateObject();

        public UDPMulticastReceiver(int offset)
        {
            s = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, endPointPort + offset);

            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            s.Bind(ipep);
            IPAddress ip = IPAddress.Parse(multicastRctIpAddress);
            s.SetSocketOption(SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                    new MulticastOption(ip, IPAddress.Any));
            so2.workSocket = s;
            s.BeginReceive(so2.buffer, 0, StateObject.BUFFER_SIZE, 0,
                           new AsyncCallback(ReceiveCallback), so2);
        }
        public void ReceiveCallback(IAsyncResult ar)
        {
            StateObject so = (StateObject)ar.AsyncState;
            Socket s = so.workSocket;

            int read = s.EndReceive(ar);

            if (read > 0)
            {
                string receivedString = Encoding.ASCII.GetString(so.buffer, 0, read);
                //so.sb.Append(Encoding.ASCII.GetString(so.buffer, 0, read));
                s.BeginReceive(so.buffer, 0, StateObject.BUFFER_SIZE, 0,
                                         new AsyncCallback(ReceiveCallback), so);

                OnDataReceived(ASCIIEncoding.ASCII.GetBytes(receivedString));
            }
            else
            {
                //if (so.sb.Length > 1)
                //{
                //    //All of the data has been read, so displays it to the console
                //    string strContent;
                //    strContent = so.sb.ToString();
                //    Console.WriteLine(String.Format("Read {0} byte from socket" +
                //                     "data = {1} ", strContent.Length, strContent));
                //}
                s.Close();
            }

        }
        public void Send(byte[] buffer)
        {
            s.Send(buffer, buffer.Length, SocketFlags.None);
        }

        public delegate void DataReceivedEventHandler(object sender, DataReceivedArgs e);
        public event EventHandler<DataReceivedArgs> OnDataReceivedEvent;
        public virtual void OnDataReceived(byte[] data)
        {
            var handler = OnDataReceivedEvent;
            if (handler != null)
            {
                handler(this, new DataReceivedArgs { Data = data });
            }
        }
    }

    public class StateObject
    {
        public Socket workSocket = null;
        public const int BUFFER_SIZE = 1024;
        public byte[] buffer = new byte[BUFFER_SIZE];
        //public StringBuilder sb = new StringBuilder();
    }
}
