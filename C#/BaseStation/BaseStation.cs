using Constants;
using RefereeBoxAdapter;
using RefereeBoxProcessor;
using SciChart.Charting.Visuals;
using StrategyManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamInterface;
using UDPMulticast;
using WorldMapManager;

namespace BaseStation
{
    static class BaseStation
    {
        //static bool usingPhysicalSimulator = true;

        static System.Timers.Timer timerStrategie;
        static GlobalWorldMapManager globalWorldMapManagerTeam1;
        
        //static Dictionary<int, StrategyManager.StrategyManager> strategyManagerDictionary;
        static List<LocalWorldMapManager> localWorldMapManagerList;
        
        static RefereeBoxAdapter.RefereeBoxAdapter refBoxAdapter;
        static RefereeBoxProcessor.RefereeBoxProcessor refereeBoxProcessor;

        static System.Timers.Timer timerTest;
        static UDPMulticastSender MultiCastTeamSender;

        static object ExitLock = new object();

        static int nbPlayersTeam1 = 5;

        [STAThread] //à ajouter au projet initial

        static void Main(string[] args)
        {
            SciChartSurface.SetRuntimeLicenseKey(@"<LicenseContract>
            <Customer>Universite De Toulon</Customer>
            <OrderId>EDUCATIONAL-USE-0128</OrderId>
            <LicenseCount>1</LicenseCount>
            <IsTrialLicense>false</IsTrialLicense>
            <SupportExpires>02/17/2020 00:00:00</SupportExpires>
            <ProductCode>SC-WPF-2D-PRO-SITE</ProductCode>
            <KeyCode>lwAAAQEAAACS9FAFUqnVAXkAQ3VzdG9tZXI9VW5pdmVyc2l0ZSBEZSBUb3Vsb247T3JkZXJJZD1FRFVDQVRJT05BTC1VU0UtMDEyODtTdWJzY3JpcHRpb25WYWxpZFRvPTE3LUZlYi0yMDIwO1Byb2R1Y3RDb2RlPVNDLVdQRi0yRC1QUk8tU0lURYcbnXYui4rna7TqbkEmUz1V7oD1EwrO3FhU179M9GNhkL/nkD/SUjwJ/46hJZ31CQ==</KeyCode>
            </LicenseContract>");
                        
            globalWorldMapManagerTeam1 = new GlobalWorldMapManager((int)TeamId.Team1);

            DefineRoles();

            StartInterfaces();

            refBoxAdapter = new RefereeBoxAdapter.RefereeBoxAdapter();
            refereeBoxProcessor = new RefereeBoxProcessor.RefereeBoxProcessor();
            MultiCastTeamSender = new UDPMulticastSender(0);
            UDPMulticastReceiver receiver1 = new UDPMulticastReceiver(0);
            localWorldMapManagerList = new List<LocalWorldMapManager>();

            refBoxAdapter.OnRefereeBoxCommandEvent += refereeBoxProcessor.OnRefereeBoxCommandReceived;
            refereeBoxProcessor.OnMulticastSendEvent += MultiCastTeamSender.OnMulticastMessageToSendReceived;
            //refBoxAdapter2 = new RefereeBoxAdapter.RefereeBoxAdapter();

            //Timer de stratégie
            timerStrategie = new System.Timers.Timer(20000);
            timerStrategie.Elapsed += TimerStrategie_Tick;
            timerStrategie.Start();

            //Tests à supprimer plus tard
            timerTest = new System.Timers.Timer(1000);
            timerTest.Elapsed += TimerTest_Elapsed;

            //UDPMulticastReceiver receiver2 = new UDPMulticastReceiver(0);
            //UDPMulticastReceiver receiver3 = new UDPMulticastReceiver(0);

            receiver1.OnDataReceivedEvent += Receiver1_OnDataReceivedEvent;
            //receiver2.OnDataReceivedEvent += Receiver2_OnDataReceivedEvent;
            //receiver3.OnDataReceivedEvent += Receiver3_OnDataReceivedEvent;
            timerTest.Start();

            lock (ExitLock)
            {
                // Do whatever setup code you need here
                // once we are done wait
                Monitor.Wait(ExitLock);
            }
        }


        private static void Receiver1_OnDataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            Console.WriteLine("Received on UDP Receiver 1 : " + Encoding.ASCII.GetString(e.Data));
        }

        //private static void Receiver2_OnDataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        //{
        //    Console.WriteLine("Received on UDP Receiver 2 : " + Encoding.ASCII.GetString(e.Data));
        //}

        //private static void Receiver3_OnDataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        //{
        //    Console.WriteLine("Received on UDP Receiver 3 : " + Encoding.ASCII.GetString(e.Data));
        //}

        static int index = 0;
        private static void TimerTest_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //string msg = "Toto " + index.ToString();
            //index++;
            //MultiCastTeamSender.Send(Encoding.ASCII.GetBytes(msg + " X"));
            //sender2.Send(Encoding.ASCII.GetBytes(msg + "  X"));
        }

        static Random randomGenerator = new Random();
        
        private static void TimerStrategie_Tick(object sender, EventArgs e)
        {
            DefineRoles();
        }

        private static void DefineRoles()
        {
            List<int> roleList = new List<int>();

            for (int i = 0; i < nbPlayersTeam1; i++)
                roleList.Add(i + 1);

            Shuffle(roleList);

            //for (int i = 0; i < nbPlayersTeam1; i++)
            //{
            //    //strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team1"].SetRole((StrategyManager.PlayerRole)roleList[i]);
            //    //strategyManagerDictionary["Robot" + (i + 1).ToString() + "Team1"].ProcessStrategy();
            //    strategyManagerDictionary[(int)TeamId.Team1 + i].SetRole((StrategyManager.PlayerRole)roleList[i]);
            //    strategyManagerDictionary[(int)TeamId.Team1 + i].ProcessStrategy();
            //}
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = randomGenerator.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        static void ExitProgram()
        {
            lock (ExitLock)
            {
                Monitor.Pulse(ExitLock);
            }
        }

        static WpfTeamInterface TeamConsole;

        static void StartInterfaces()
        {
            Thread t1 = new Thread(() =>
            {
                //Attention, il est nécessaire d'ajouter PresentationFramework, PresentationCore, WindowBase and your wpf window application aux ressources.
                TeamConsole = new WpfTeamInterface();

                //for (int i = 0; i < nbPlayersTeam1; i++)
                //{
                //    localWorldMapManagerList[i].OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived;
                //}
                globalWorldMapManagerTeam1.OnGlobalWorldMapEvent += TeamConsole.OnGlobalWorldMapReceived;
                TeamConsole.ShowDialog();
            });
            t1.SetApartmentState(ApartmentState.STA);
            t1.Start();
        }

        private static void RefBoxAdapter_DataReceivedEvent(object sender, EventArgsLibrary.DataReceivedArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
