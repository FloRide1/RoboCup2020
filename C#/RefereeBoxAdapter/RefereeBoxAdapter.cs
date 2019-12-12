using AdvancedTimers;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace RefereeBoxAdapter
{
    public class RefereeBoxAdapter
    {
        private Thread connectionThread;
        NetworkStream networkStream;
        TcpClient refereeBoxClient;
        HighFreqTimer timerPing = new HighFreqTimer(10);
        private bool IsRefereeBoxConnected = false;


        public RefereeBoxAdapter()
        {
            timerPing.Tick += TimerPing_Tick;
            InitConnexionThread();
            connectionThread.Start();
            timerPing.Start();
        }

        private void TimerPing_Tick(object sender, EventArgs e)
        {
            //Procédure pour tester si la liaison TCP/IP est encore valide
            if (refereeBoxClient != null)
            {
                if (refereeBoxClient.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (refereeBoxClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        IsRefereeBoxConnected = false;
                        StartTryingToConnect();
                    }
                }
            }
        }

        private void InitConnexionThread()
        {
            //On crée un Thread de connexion
            connectionThread = new Thread(() =>
            {
                //Le Thread est infini mais il sera suspendu quand le port série sera trouvé et ouvert
                while (true)
                {
                    try
                    {
                        //On créée un connexion TCP/IP
                        string localIp = "127.0.0.1";
                        refereeBoxClient = new TcpClient(localIp, 28097);
                        //Si tout se passe bien, on récupère le flux de données.
                        networkStream = refereeBoxClient.GetStream();
                        //On lance les acquisitions
                        ContinuousRead();
                        //On suspend le Thread de connexion
                        StopTryingToConnect();

                        IsRefereeBoxConnected = true;
                        Console.WriteLine("Connection to Referee Box successful.");
                    }
                    catch
                    {
                        IsRefereeBoxConnected = false;
                        Console.WriteLine("Connection to RefereeBox failed.");
                    }
                    Thread.Sleep(500);
                }
            });
        }

        private void StartTryingToConnect()
        {
            //Reprise du Thread de Connexion
            if (connectionThread.ThreadState == ThreadState.Suspended)
            {
                connectionThread.Resume();
            }
        }

        private void StopTryingToConnect()
        {
            //Suspension du Thread de Connexion
            connectionThread.Suspend();
        }

        public IPAddress GetLocalIPv4()
        {
            IPHostEntry ipEntry = Dns.GetHostEntry("");
            IPAddress address = ipEntry.AddressList.Last();

            return address;
        }

        private void ContinuousRead()
        {
            byte[] buffer = new byte[4096];
            Action kickoffRead = null;
            kickoffRead = (Action)(() => networkStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
            {
                try
                {
                    int count = networkStream.EndRead(ar);
                    byte[] dst = new byte[count];
                    Buffer.BlockCopy(buffer, 0, dst, 0, count);
                    if (count > 0)
                    {
                        foreach(var b in dst)
                        {
                            switch ((char)b)
                            {
                                case 'S':
                                    OnRefereeBoxReceivedCommand("STOP");
                                    break;
                                case 's':
                                    OnRefereeBoxReceivedCommand("START");
                                    break;
                                case 'W':
                                    OnRefereeBoxReceivedCommand("WELCOME");
                                    break;
                                case 'w':
                                    OnRefereeBoxReceivedCommand("WORLD_STATE");
                                    break;
                                case 'Z':
                                    OnRefereeBoxReceivedCommand("RESET");
                                    break;
                                case 'U':
                                    OnRefereeBoxReceivedCommand("TESTMODE_ON");
                                    break;
                                case 'u':
                                    OnRefereeBoxReceivedCommand("TESTMODE_OFF");
                                    break;
                                case 'y':
                                    OnRefereeBoxReceivedCommand("YELLOW_CARD_MAGENTA");
                                    break;
                                case 'Y':
                                    OnRefereeBoxReceivedCommand("YELLOW_CARD_CYAN");
                                    break;
                                case 'r':
                                    OnRefereeBoxReceivedCommand("RED_CARD_MAGENTA");
                                    break;
                                case 'R':
                                    OnRefereeBoxReceivedCommand("RED_CARD_CYAN");
                                    break;
                                case 'b':
                                    OnRefereeBoxReceivedCommand("DOUBLE_YELLOW_IN_MAGENTA");
                                    break;
                                case 'B':
                                    OnRefereeBoxReceivedCommand("DOUBLE_YELLOW_IN_CYAN");
                                    break;
                                case '1':
                                    OnRefereeBoxReceivedCommand("FIRST_HALF");
                                    break;
                                case '2':
                                    OnRefereeBoxReceivedCommand("SECOND_HALF");
                                    break;
                                case '3':
                                    OnRefereeBoxReceivedCommand("FIRST_HALF_OVERTIME");
                                    break;
                                case '4':
                                    OnRefereeBoxReceivedCommand("SECOND_HALF_OVERTIME");
                                    break;
                                case 'h':
                                    OnRefereeBoxReceivedCommand("HALF_TIME");
                                    break;
                                case 'e':
                                    OnRefereeBoxReceivedCommand("END_GAME");
                                    break;
                                case 'z':
                                    OnRefereeBoxReceivedCommand("GAMEOVER");
                                    break;
                                case 'L':
                                    OnRefereeBoxReceivedCommand("PARKING");
                                    break;
                                case 'a':
                                    OnRefereeBoxReceivedCommand("GOAL_MAGENTA");
                                    break;
                                case 'A':
                                    OnRefereeBoxReceivedCommand("GOAL_CYAN");
                                    break;
                                case 'd':
                                    OnRefereeBoxReceivedCommand("SUBGOAL_MAGENTA");
                                    break;
                                case 'D':
                                    OnRefereeBoxReceivedCommand("SUBGOAL_CYAN");
                                    break;
                                case 'k':
                                    OnRefereeBoxReceivedCommand("KICKOFF_MAGENTA");
                                    break;
                                case 'K':
                                    OnRefereeBoxReceivedCommand("KICKOFF_CYAN");
                                    break;
                                case 'f':
                                    OnRefereeBoxReceivedCommand("FREEKICK_MAGENTA");
                                    break;
                                case 'F':
                                    OnRefereeBoxReceivedCommand("FREEKICK_CYAN");
                                    break;
                                case 'g':
                                    OnRefereeBoxReceivedCommand("GOALKICK_MAGENTA");
                                    break;
                                case 'G':
                                    OnRefereeBoxReceivedCommand("GOALKICK_CYAN");
                                    break;
                                case 't':
                                    OnRefereeBoxReceivedCommand("THROWIN_MAGENTA");
                                    break;
                                case 'T':
                                    OnRefereeBoxReceivedCommand("THROWIN_CYAN");
                                    break;
                                case 'c':
                                    OnRefereeBoxReceivedCommand("CORNER_MAGENTA");
                                    break;
                                case 'C':
                                    OnRefereeBoxReceivedCommand("CORNER_CYAN");
                                    break;
                                case 'p':
                                    OnRefereeBoxReceivedCommand("PENALTY_MAGENTA");
                                    break;
                                case 'P':
                                    OnRefereeBoxReceivedCommand("PENALTY_CYAN");
                                    break;
                                case 'N':
                                    OnRefereeBoxReceivedCommand("DROPPED_BALL");
                                    break;
                                case 'o':
                                    OnRefereeBoxReceivedCommand("REPAIR_OUT_MAGENTA");
                                    break;
                                case 'O':
                                    OnRefereeBoxReceivedCommand("REPAIR_OUT_CYAN");
                                    break;
                                case 'i':
                                    OnRefereeBoxReceivedCommand("REPAIR_IN_MAGENTA");
                                    break;
                                case 'I':
                                    OnRefereeBoxReceivedCommand("REPAIR_IN_CYAN");
                                    break;
                                default:
                                    OnRefereeBoxReceivedCommand(b.ToString());
                                    break;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Referee Box exception !");
                }
                kickoffRead();
            }, null));

            kickoffRead();
        }

        //Output events
        public delegate void StringEventHandler(object sender, StringArgs e);
        public event EventHandler<StringArgs> OnRefereeBoxReceivedCommandEvent;
        public virtual void OnRefereeBoxReceivedCommand(string data)
        {
            var handler = OnRefereeBoxReceivedCommandEvent;
            if (handler != null)
            {
                handler(this, new StringArgs { Value = data });
            }
        }
    }
}
