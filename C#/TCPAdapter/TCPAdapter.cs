using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TCPAdapter
{
    public class TCPAdapter
    {
        private int port;
        private string ipAddress;

        private System.Net.Sockets.TcpClient tcpClient;
        private NetworkStream clientStream;
        private bool isConnected = false;

        Timer connectionManagementTimer;
        public TCPAdapter(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;

            connectionManagementTimer = new Timer(5000);
            connectionManagementTimer.Elapsed += ConnectionManagementTimer_Elapsed;
            connectionManagementTimer.Start();
        }

        private void ConnectionManagementTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!isConnected)
            {
                //Si on n'est pas connecté, on essaie de se connecter.
                connectionManagementTimer.Interval = 5000;
                try
                {
                    Console.WriteLine("Trying to Connect");
                    Connect();
                    Console.WriteLine("Connection successful");
                }
                catch
                {
                    Console.WriteLine("Connection failed");
                }
            }
            else
            {
                //On regarde si la connexion est toujours active
                connectionManagementTimer.Interval = 200;
                if (!IsConnected())
                {
                    Console.WriteLine("Connection lost");
                    isConnected = false;
                }
                else
                    Console.WriteLine("Connection alive");
            }
        }

        private void Connect()
        {
            tcpClient = new System.Net.Sockets.TcpClient(ipAddress, port);
            //Si tout se passe bien, on récupère le flux de données.
            clientStream = tcpClient.GetStream();
            //On lance les acquisitions
            isConnected = true;
            Listening();
        }

        private bool IsConnected()
        {
            //Procédure pour tester si la liaison TCP/IP est encore valide
            if (tcpClient != null)
            {
                if (tcpClient.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        return false;
                    }
                    else
                        return true;
                }
                return true;
            }
            return true;
        }

        private async void Listening()
        {
            // Declare the callback.  Need to do that so that
            // the closure captures it.
            AsyncCallback callback = null;

            byte[] buffer = new byte[1024];
            int offset = 0;
            int count = 1024;

            // Assign the callback.
            callback = ar => {
                // Call EndRead.
                int bytesRead = clientStream.EndRead(ar);

                // Process the bytes here.
                byte[] dst = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, dst, 0, bytesRead);
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(dst));

                // Determine if you want to read again.  If not, return.
                if (!isConnected) return;

                // Read again.  This callback will be called again.
                clientStream.BeginRead(buffer, offset, count, callback, null);
            };

            // Trigger the initial read.
            clientStream.BeginRead(buffer, offset, count, callback, null);
        }
    }
}
