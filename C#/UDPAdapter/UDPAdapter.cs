using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace UDPAdapter
{
    public class UDPReceiver
    {
        int Id;
        private string udpIpAddress;
        private int udpEndPointPort;
        Socket s;
        StateObject so2 = new StateObject();
        
        public UDPReceiver(string ipAddress, int port)
        {
            udpIpAddress = ipAddress;
            udpEndPointPort = port;
            s = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, udpEndPointPort);

            s.Bind(ipep);
            IPAddress ip = IPAddress.Parse(udpIpAddress);
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
                s.Close();
            }

        }
        public void Send(byte[] buffer)
        {
            s.Send(buffer, buffer.Length, SocketFlags.None);
        }

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
        public const int BUFFER_SIZE = 32768;
        public byte[] buffer = new byte[BUFFER_SIZE];
    }
}
