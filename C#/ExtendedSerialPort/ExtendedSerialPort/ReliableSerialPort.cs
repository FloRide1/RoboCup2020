using EventArgsLibrary;
using System;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;

namespace ExtendedSerialPort
{
    public class ReliableSerialPort : SerialPort
    {

        private Thread connectionThread;
        private bool IsSerialPortConnected = false;
        private String PortType;

        public ReliableSerialPort(string portType, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            PortType = portType;
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
            Handshake = Handshake.None;
            DtrEnable = true;
            NewLine = Environment.NewLine;
            ReceivedBytesThreshold = 1024;
            InitConnexionThread();
            connectionThread.Start();
        }

        private void InitConnexionThread()
        {
            //On créée un Thread de connexion
            connectionThread = new Thread(() =>
            {
                //Le Thread est infini mais il sera suspendu quand le port série sera trouvé et ouvert
                while (true)
                {
                    string PortNameFound = "COM1";//SearchPortName(PortType); TOTO: remettre en etat sinon moi bobo
                    if (!string.IsNullOrWhiteSpace(PortNameFound))
                    {
                        //Si on trouve un port série de type voulu
                        PortName = PortNameFound;
                        try
                        {
                            base.Open();
                            IsSerialPortConnected = true;
                            Console.WriteLine("Connection to serial port successful.");
                            //On lance les acquisitions
                            ContinuousRead();
                            //On suspend le Thread de connexion
                            StopTryingToConnect();
                        }
                        catch
                        {
                            IsSerialPortConnected = false;
                            Console.WriteLine("Connection to serial port failed.");
                        }
                    }
                    else
                    {
                        IsSerialPortConnected = false;
                        Console.WriteLine("Serial port not found.");
                    }
                    Thread.Sleep(2000);
                }
            });
        }

        private void StartTryingToConnect()
        {
            //Reprise du Thread de Connexion
            connectionThread.Resume();
        }
        
        private void StopTryingToConnect()
        {
            //Suspension du Thread de Connexion
            connectionThread.Suspend();
        }

        new public void Open()
        {
            //On ne fait rien, c'est volontaire !
        }



        private string SearchPortName(string vendorName)
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2",
                                                                                 "SELECT * FROM Win32_PnPEntity");
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    // Recherche du fabriquant dans le DeviceID
                    if (queryObj["DeviceID"] != null && queryObj["DeviceID"].ToString().Contains(vendorName))
                        if (queryObj["Caption"] != null)
                        {
                            // Recherche du port COM dans le nom
                            string textToSearch = queryObj["Caption"].ToString();
                            string pattern = @"\((COM[0-9]+?)\)"; // Regex pour la recherche des FTDI 232
                            Regex r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                            Match m = r.Match(textToSearch);
                            if (m.Success)
                                return m.Groups[1].ToString();
                        }
                }

                return "";
            }
            catch { return ""; }
        }

        private void ContinuousRead()
        {
            byte[] buffer = new byte[4096];

            //On lance une action asynchrone de lecture sur le port série
            Action kickoffRead = null;
            kickoffRead = (Action)(() => BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
            {
                try
                {
                    //On récupère le buffer avec les datas dispo
                    int count = BaseStream.EndRead(ar);
                    byte[] dst = new byte[count];
                    Buffer.BlockCopy(buffer, 0, dst, 0, count);
                    //On lance un évènement OnDatReceived
                    OnDataReceived(dst);
                }
                catch (Exception exception)
                {
                    //SI le port ne répond pas
                    Console.WriteLine("OptimizedSerialPort exception !");
                    IsSerialPortConnected = false;
                }
                if (IsSerialPortConnected)
                {
                    //Si on est connecté, on relance l'acquisition en boucle
                    kickoffRead();
                }
            }, null));

            kickoffRead();
        }

        //Input events
        public void SendMessage(object sender, EventArgsLibrary.MessageEncodedArgs e)
        {
            if (IsSerialPortConnected)
            {
                try
                {
                    //Quand on reçoit un message à envoyer, on le fait partir
                    Write(e.Msg, 0, e.Msg.Length);
                }
                catch
                {
                    //Si pb de connexion
                    IsSerialPortConnected = false;
                    //On relance la procédure de connexion
                    StartTryingToConnect();
                }
            }
        }

        //********************************************** Output events **********************************************************************************//
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
