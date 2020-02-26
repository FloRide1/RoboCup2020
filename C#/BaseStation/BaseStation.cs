using Constants;
using RefereeBoxAdapter;
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
using UdpMulticastInterpreter;
using WorldMapManager;

namespace BaseStation
{
    static class BaseStation
    {
        static GlobalWorldMapManager globalWorldMapManagerTeam1;

        static RefereeBoxAdapter.RefereeBoxAdapter refBoxAdapter;
        
        static UDPMulticastSender BaseStationUdpMulticastSenderTeam1;
        static UDPMulticastReceiver BaseStationUdpMulticastReceiverTeam1;
        static UDPMulticastInterpreter BaseStationUdpMulticastInterpreterTeam1;
        
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

            globalWorldMapManagerTeam1 = new GlobalWorldMapManager((int)TeamId.Team1, "224.16.32.79");

            //BaseStation RCT
            BaseStationUdpMulticastSenderTeam1 = new UDPMulticastSender(0, "224.16.32.79");
            BaseStationUdpMulticastReceiverTeam1 = new UDPMulticastReceiver(0, "224.16.32.79");
            BaseStationUdpMulticastInterpreterTeam1 = new UDPMulticastInterpreter(0);
            
            StartInterfaces();

            refBoxAdapter = new RefereeBoxAdapter.RefereeBoxAdapter();

            //Event de réception d'une commande de la réferee box
            refBoxAdapter.OnRefereeBoxCommandEvent += globalWorldMapManagerTeam1.OnRefereeBoxCommandReceived;
            //Event de réception de data Multicast sur la base Station Team X
            BaseStationUdpMulticastReceiverTeam1.OnDataReceivedEvent += BaseStationUdpMulticastInterpreterTeam1.OnMulticastDataReceived;
            //Event d'interprétation d'une localWorldMap à sa réception dans la base station
            BaseStationUdpMulticastInterpreterTeam1.OnLocalWorldMapEvent += globalWorldMapManagerTeam1.OnLocalWorldMapReceived;
            //Event d'envoi de la global world map sur le Multicast
            globalWorldMapManagerTeam1.OnMulticastSendGlobalWorldMapEvent += BaseStationUdpMulticastSenderTeam1.OnMulticastMessageToSendReceived;

            lock (ExitLock)
            {
                // Do whatever setup code you need here
                // once we are done wait
                Monitor.Wait(ExitLock);
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
                BaseStationUdpMulticastInterpreterTeam1.OnLocalWorldMapEvent += TeamConsole.OnLocalWorldMapReceived; //->version base station
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
