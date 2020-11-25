using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceMonitorTools
{
    public static class PhysicalSimulatorMonitor
    {
        public static Stopwatch swPhysicalSimulator = new Stopwatch();
        public static int nbPhysicalSimulatorReceived = 0;
        static Object lockDiagPhysicalSimulator = new object();

        public static void PhysicalSimulatorReceived()
        {
            lock (lockDiagPhysicalSimulator)
            {
                if (!swPhysicalSimulator.IsRunning)
                    swPhysicalSimulator.Start();
                nbPhysicalSimulatorReceived++;
                if (swPhysicalSimulator.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbPhysicalSimulatorReceived + " Physical simulations reçues en 1 s");
                    swPhysicalSimulator.Restart();
                    nbPhysicalSimulatorReceived = 0;
                }
            }
        }
    }
    public static class PerceptionMonitor
    {
        public static Stopwatch swPerception = new Stopwatch();
        public static int nbPerceptionReceived = 0;
        static Object lockDiagPerception = new object();

        public static void PerceptionReceived()
        {
            lock (lockDiagPerception)
            {
                if (!swPerception.IsRunning)
                    swPerception.Start();
                nbPerceptionReceived++;
                if (swPerception.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbPerceptionReceived + " perceptions reçues en 1 s");
                    swPerception.Restart();
                    nbPerceptionReceived = 0;
                }
            }
        }
    }

    static public class UdpMonitor
    {
        static public int nbMessageLWMReceived = 0;
        static public int nbMessageGWMReceived = 0;
        static public int nbByteLWMReceived = 0;
        static public int nbByteGWMReceived = 0;
        static public Stopwatch swGWM = new Stopwatch();
        static public Stopwatch swLWM = new Stopwatch();
        static Object lockDiagGWM = new object();
        static Object lockDiagLWM = new object();


        public static void GWMReceived(int size)
        {
            lock (lockDiagGWM)
            {
                if (!swGWM.IsRunning)
                    swGWM.Start();
                nbMessageGWMReceived++;
                nbByteGWMReceived += size;
                if (swGWM.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbMessageGWMReceived + " GWM reçues en 1 s : " + nbByteGWMReceived + " octets");
                    swGWM.Restart();
                    nbMessageGWMReceived = 0;
                    nbByteGWMReceived = 0;
                }
            }
        }
        public static void LWMReceived(int size)
        {
            lock (lockDiagLWM)
            {
                if (!swLWM.IsRunning)
                    swLWM.Start();
                nbMessageLWMReceived++;
                nbByteLWMReceived += size;
                if (swLWM.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbMessageLWMReceived + " LWM reçues en 1 s : " + nbByteLWMReceived + " octets");
                    swLWM.Restart();
                    nbMessageLWMReceived = 0;
                    nbByteLWMReceived = 0;
                }
            }
        }
    }
}
