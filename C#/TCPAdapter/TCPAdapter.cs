using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TCPAdapter
{
    public class TCPAdapter
    {
        private string name;
        private int port;
        private string ipAddress;

        private System.Net.Sockets.TcpClient tcpClient;
        public TCPAdapter(string ipAddress, int port, string name)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.name = name;

            //connectionManagementTimer = new Timer(5000);
            //connectionManagementTimer.Elapsed += ConnectionManagementTimer_Elapsed;
            //connectionManagementTimer.Start();


            Initialize(ipAddress, port);
            BeginRead();
        }

        //private TcpClient tcpClient;

        public void Initialize(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient(ip, port);

                if (tcpClient.Connected)
                    Console.WriteLine("Connected to: {0}:{1}", ip, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Initialize(ip, port);
            }
        }

        public void BeginRead()
        {
            var buffer = new byte[4096];
            var ns = tcpClient.GetStream();
            ns.BeginRead(buffer, 0, buffer.Length, EndRead, buffer);
        }

        public void EndRead(IAsyncResult result)
        {
            var buffer = (byte[])result.AsyncState;
            var ns = tcpClient.GetStream();
            var bytesAvailable = ns.EndRead(result);
            byte[] data = new byte[bytesAvailable];
            Buffer.BlockCopy(buffer, 0, data, 0, bytesAvailable);
            OnDataReceived(data);
            //Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, bytesAvailable));
            BeginRead();
        }

        public void BeginSend(string xml)
        {
            var bytes = Encoding.ASCII.GetBytes(xml);
            var ns = tcpClient.GetStream();
            ns.BeginWrite(bytes, 0, bytes.Length, EndSend, bytes);
        }

        public void EndSend(IAsyncResult result)
        {
            var bytes = (byte[])result.AsyncState;
            Console.WriteLine("Sent  {0} bytes to server.", bytes.Length);
            Console.WriteLine("Sent: {0}", Encoding.ASCII.GetString(bytes));
        }



        //private void ConnectionManagementTimer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    if (!isConnected)
        //    {
        //        //Si on n'est pas connecté, on essaie de se connecter.
        //        connectionManagementTimer.Interval = 5000;
        //        try
        //        {
        //            Console.WriteLine(name+" : Trying to Connect");
        //            Connect();
        //            Console.WriteLine(name + " : Connection successful");
        //        }
        //        catch
        //        {
        //            Console.WriteLine(name + " : Connection failed");
        //        }
        //    }
        //    else
        //    {
        //        //On regarde si la connexion est toujours active
        //        connectionManagementTimer.Interval = 5000;
        //        if (!IsConnected())
        //        {
        //            Console.WriteLine(name + " : Connection lost");
        //            isConnected = false;
        //        }
        //        else
        //            Console.WriteLine(name + " : Connection alive");
        //    }
        //}

        //private void Connect()
        //{
        //    tcpClient = new System.Net.Sockets.TcpClient(ipAddress, port);
        //    tcpClient.NoDelay = true;
        //    //Si tout se passe bien, on récupère le flux de données.
        //    clientStream = tcpClient.GetStream();
        //    //On lance les acquisitions
        //    isConnected = true;
        //    Listening();
        //}

        //private bool IsConnected()
        //{
        //    //Procédure pour tester si la liaison TCP/IP est encore valide
        //    if (tcpClient != null)
        //    {
        //        if (tcpClient.Client.Poll(0, SelectMode.SelectRead))
        //        {
        //            byte[] buff = new byte[1];
        //            try
        //            {
        //                if (tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
        //                {
        //                    // Client disconnected
        //                    return false;
        //                }
        //                else
        //                    return true;
        //            }
        //            catch
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    return true;
        //}

        //private async void Listening()
        //{
        //    // Declare the callback.  Need to do that so that
        //    // the closure captures it.
        //    AsyncCallback callback = null;

        //    byte[] buffer = new byte[65536 * 2];
        //    int offset = 0;
        //    //int count = 1024;

        //    // Assign the callback.
        //    callback = ar => {
        //        //try
        //        {
        //            // Call EndRead.
        //            int bytesRead = clientStream.EndRead(ar);

        //            // Process the bytes here.
        //            byte[] dst = new byte[bytesRead];
        //            Buffer.BlockCopy(buffer, 0, dst, 0, bytesRead);
        //            OnDataReceived(dst);
        //            //Console.WriteLine(System.Text.Encoding.UTF8.GetString(dst));

        //            // Determine if you want to read again.  If not, return.
        //            if (!isConnected) return;

        //            // Read again.  This callback will be called again.
        //            clientStream.BeginRead(buffer, offset, buffer.Length, callback, null);
        //        }
        //        //catch
        //        //{
        //        //    isConnected = false;
        //        //    return;
        //        //}
        //    };

        //    // Trigger the initial read.
        //    clientStream.BeginRead(buffer, offset, buffer.Length, callback, null);
        //}

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
}
