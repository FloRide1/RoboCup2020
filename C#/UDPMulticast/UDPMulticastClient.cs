using EventArgsLibrary;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;

namespace UDPMulticast
{
    public class UDPMulticastSender
    {
        int Id;
        //private string multicastIpAddress = "224.16.32.79";
        private string multicastIpAddress;
        private string localInterfaceAddress = "127.0.0.1";
        private int endPointPort = 4567;
        Socket s;

        public UDPMulticastSender(int id, string ipAddress)
        {
            multicastIpAddress = ipAddress;
            Id = id;
            s = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            IPAddress ip = IPAddress.Parse(this.multicastIpAddress);

            MulticastOption mcastOption = new MulticastOption(IPAddress.Parse(this.multicastIpAddress));
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption); 
            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 50);
            IPEndPoint ipep = new IPEndPoint(ip, endPointPort);
            s.Connect(ipep);
        }
        public void Send(byte[] buffer)
        {
            s.Send(buffer, buffer.Length, SocketFlags.None);
            //Console.WriteLine("Multicast Nb of bytes sent : " + buffer.Length);
        }
        
        public void OnMulticastMessageToSendReceived(object sender, DataReceivedArgs e)
        {
            //Console.WriteLine(string.Format("\nUncompressed bytes: {0}", e.Data.Length));
            ////byte[] compressedbyteData = Zip.CompressBytes(e.Data);
            //byte[] compressedData = Zip.ZipText(Encoding.UTF8.GetString(e.Data));
            //Console.WriteLine(string.Format("Compressed bytes: {0}", compressedData.Length));
            //string uncompressedTextData = Zip.UnzipText(compressedData);
            //Console.WriteLine(string.Format("Decompressed bytes: {0}", uncompressedTextData.Length));
            ////Send(compressedData);
            Send(e.Data);
        }
    }

    public class UDPMulticastReceiver
    {
        int Id;
        //private string multicastIpAddress = "224.16.32.79";
        private string multicastIpAddress;
        private int endPointPort = 4567;
        Socket s;
        StateObject so2 = new StateObject();

        public UDPMulticastReceiver(int id, string ipAddress)
        {
            multicastIpAddress = ipAddress;
            Id = id;
            s = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, endPointPort);

            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            s.Bind(ipep);
            IPAddress ip = IPAddress.Parse(multicastIpAddress);
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
        //public StringBuilder sb = new StringBuilder();
    }

    class MulticastEndpoint

    {

        public ArrayList localInterfaceList, multicastJoinList;

        public Socket mcastSocket;

        public IPAddress bindAddress;

        public int bufferSize;

        public int localPort;

        public byte[] dataBuffer;



        /// <summary>

        /// Simple constructor for the

        /// </summary>

        public MulticastEndpoint()

        {

            localInterfaceList = new ArrayList();

            multicastJoinList = new ArrayList();

            bufferSize = 512;

            mcastSocket = null;

        }



        /// <summary>

        /// This method creates the socket, joins it to the given multicast groups, and initializes

        /// the send/receive buffer. Note that the local bind address should be the wildcard address

        /// because it is possible to join multicast groups on one or more local interface and if

        /// a socket is bound to an explicit local interface, it can lead to user confusion (although

        /// this does currently work on Windows OSes).

        /// </summary>

        /// <param name="port">Local port to bind socket to</param>

        /// <param name="bufferLength">Length of the send/recv buffer to create</param>

        public void Create(int port, int bufferLength)

        {

            localPort = port;



            Console.WriteLine("Creating socket, joining multicast group and");

            Console.WriteLine("     initializing the send/receive buffer");

            try

            {

                // If no bind address was specified, pick an appropriate one based on the multicast

                // group being joined.

                if (bindAddress == null)

                {

                    IPAddress tmpAddr = (IPAddress)multicastJoinList[0];



                    if (tmpAddr.AddressFamily == AddressFamily.InterNetwork)

                        bindAddress = IPAddress.Any;

                    else if (tmpAddr.AddressFamily == AddressFamily.InterNetworkV6)

                        bindAddress = IPAddress.IPv6Any;

                }



                // Create the UDP socket

                Console.WriteLine("Creating the UDP socket...");

                mcastSocket = new Socket(

                    bindAddress.AddressFamily,

                    SocketType.Dgram,

                    0

                    );



                Console.WriteLine("{0} multicast socket created", bindAddress.AddressFamily.ToString());



                // Bind the socket to the local endpoint

                Console.WriteLine("Binding the socket to the local endpoint...");

                IPEndPoint bindEndPoint = new IPEndPoint(bindAddress, port);

                mcastSocket.Bind(bindEndPoint);

                Console.WriteLine("Multicast socket bound to: {0}", bindEndPoint.ToString());



                // Join the multicast group

                Console.WriteLine("Joining the multicast group...");

                for (int i = 0; i < multicastJoinList.Count; i++)

                {

                    for (int j = 0; j < localInterfaceList.Count; j++)

                    {

                        // Create the MulticastOption structure which is required to join the

                        //    multicast group

                        if (mcastSocket.AddressFamily == AddressFamily.InterNetwork)

                        {

                            MulticastOption mcastOption = new MulticastOption(

                                (IPAddress)multicastJoinList[i],

                                (IPAddress)localInterfaceList[j]

                                );



                            mcastSocket.SetSocketOption(

                                SocketOptionLevel.IP,

                                SocketOptionName.AddMembership,

                                mcastOption

                                );

                        }

                        else if (mcastSocket.AddressFamily == AddressFamily.InterNetworkV6)

                        {

                            IPv6MulticastOption ipv6McastOption = new IPv6MulticastOption(

                                (IPAddress)multicastJoinList[i],

                                ((IPAddress)localInterfaceList[j]).ScopeId

                                );



                            mcastSocket.SetSocketOption(

                                SocketOptionLevel.IPv6,

                                SocketOptionName.AddMembership,

                                ipv6McastOption

                                );

                        }



                        Console.WriteLine("Joined multicast group {0} on interface {1}",

                            multicastJoinList[i].ToString(),

                            localInterfaceList[j].ToString()

                            );

                    }

                }



                // Allocate the send and receive buffer

                Console.WriteLine("Allocating the send and receive buffer...");

                dataBuffer = new byte[bufferLength];

            }

            catch (SocketException err)

            {

                Console.WriteLine("Exception occurred when creating multicast socket: {0}", err.Message);

                throw;

            }

        }



        /// <summary>

        /// This method drops membership to any joined groups. To do so, you have to

        /// drop the group exactly as you joined it -- that is the local interface

        /// and multicast group must be the same as when it was joined. Also note

        /// that it is not required to drop joined groups before closing a socket.

        /// When a socket is closed all multicast joins are dropped for you -- this

        /// routine just illustrates how to drop a group if you need to in the middle

        /// of the lifetime of a socket.

        /// </summary>

        public void LeaveGroups()
        {
            try
            {
                Console.WriteLine("Dropping membership to any joined groups...");
                for (int i = 0; i < multicastJoinList.Count; i++)
                {
                    for (int j = 0; j < localInterfaceList.Count; j++)
                    {
                        // Create the MulticastOption structure which is required to drop the
                        //    multicast group (the same structure used to join the group is
                        //    required to drop it).
                        if (mcastSocket.AddressFamily == AddressFamily.InterNetwork)
                        {
                            MulticastOption mcastOption = new MulticastOption(
                                (IPAddress)multicastJoinList[i],
                                (IPAddress)localInterfaceList[j]
                                );

                            mcastSocket.SetSocketOption(
                                SocketOptionLevel.IP,
                                SocketOptionName.DropMembership,
                                mcastOption
                                );
                        }

                        else if (mcastSocket.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            IPv6MulticastOption ipv6McastOption = new IPv6MulticastOption(
                                (IPAddress)multicastJoinList[i],
                                ((IPAddress)localInterfaceList[j]).ScopeId
                                );

                            mcastSocket.SetSocketOption(
                                SocketOptionLevel.IPv6,
                                SocketOptionName.DropMembership,
                                ipv6McastOption
                                );
                        }

                        Console.WriteLine("Dropping multicast group {0} on interface {1}",
                            multicastJoinList[i].ToString(),
                            localInterfaceList[j].ToString()
                            );
                    }
                }
            }
            catch
            {   
                Console.WriteLine("LeaveGroups: No multicast groups joined");
            }
        }

        /// <summary>
        /// This method sets the outgoing interface when a socket sends data to a multicast
        /// group. Because multicast addresses are not routable, the network stack simply
        /// /// picks the first interface in the routing table with a multicast route. In order
        /// /// to change this behavior, the MulticastInterface option can be used to set the
        /// /// local interface on which all outgoing multicast traffic is to be sent (for this
        /// socket only). This is done by converting the 4 byte IPv4 address (or 16 byte
        /// IPv6 address) into a byte array.
        /// </summary>
        /// <param name="sendInterface"></param>

        public void SetSendInterface(IPAddress sendInterface)
        {
            // Set the outgoing multicast interface
            try
            {
                Console.WriteLine("Setting the outgoing multicast interface...");
                if (mcastSocket.AddressFamily == AddressFamily.InterNetwork)
                {
                    mcastSocket.SetSocketOption(
                        SocketOptionLevel.IP,
                        SocketOptionName.MulticastInterface,
                        sendInterface.GetAddressBytes()
                        );

                }
                else
                {
                    byte[] interfaceArray = BitConverter.GetBytes((int)sendInterface.ScopeId);
                    mcastSocket.SetSocketOption(
                        SocketOptionLevel.IPv6,
                        SocketOptionName.MulticastInterface,
                        interfaceArray
                        );
                }

                Console.WriteLine("Setting multicast send interface to: " + sendInterface.ToString());
            }

            catch (SocketException err)
            {
                Console.WriteLine("SetSendInterface: Unable to set the multicast interface: {0}", err.Message);
                throw;
            }
        }

        /// <summary>
        /// This method takes a string and repeatedly copies it into the send buffer
        /// to the length of the send buffer.
        /// </summary>
        /// <param name="message">String to copy into send buffer</param>
        public void FormatBuffer(string message)
        {
            byte[] byteMessage = System.Text.Encoding.ASCII.GetBytes(message);
            int index = 0;

            // First convert the string to bytes and then copy into send buffer
            Console.WriteLine("Formatting the send buffer...");
            while (index < dataBuffer.Length)
            {
                for (int j = 0; j < byteMessage.Length; j++)
                {
                    dataBuffer[index] = byteMessage[j];
                    index++;
                    // Make sure we don't go past the send buffer length
                    if (index >= dataBuffer.Length)
                    {
                        break;
                    }
                }
            }
        }
    }
}
