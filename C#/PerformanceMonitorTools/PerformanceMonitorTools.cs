using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerformanceMonitorTools
{
    public static class KalmanMonitor
    {
        public static Stopwatch swKalman = new Stopwatch();
        public static int nbKalmanReceived = 0;
        static Object lockDiagKalman = new object();

        public static void KalmanReceived()
        {
            lock (lockDiagKalman)
            {
                if (!swKalman.IsRunning)
                    swKalman.Start();
                nbKalmanReceived++;
                if (swKalman.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbKalmanReceived + " Kalman calculs en 1 s");
                    swKalman.Restart();
                    nbKalmanReceived = 0;
                }
            }
        }
    }

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
    public static class TrajectoryGeneratorMonitor
    {
        public static Stopwatch swTrajectoryGenerator = new Stopwatch();
        public static int nbTrajectoryGeneratorReceived = 0;
        static Object lockDiagTrajectoryGenerator = new object();

        public static void TrajectoryGeneratorReceived()
        {
            lock (lockDiagTrajectoryGenerator)
            {
                if (!swTrajectoryGenerator.IsRunning)
                    swTrajectoryGenerator.Start();
                nbTrajectoryGeneratorReceived++;
                if (swTrajectoryGenerator.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbTrajectoryGeneratorReceived + " Trajectory Ghost Updates générées en 1 s");
                    swTrajectoryGenerator.Restart();
                    nbTrajectoryGeneratorReceived = 0;
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
                    Console.WriteLine(nbMessageGWMReceived + " GWM reçues en 1 s : " + nbByteGWMReceived + " octets\n");
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

    public static class LWMEmiseMonitoring
    {
        public static Stopwatch swLWMEmise = new Stopwatch();
        public static int nbLWMEmiseReceived = 0;
        public static int nbByteLWMEmis = 0;
        static Object lockDiagLWMEmise = new object();

        public static void LWMEmiseMonitor(int size)
        {
            lock (lockDiagLWMEmise)
            {
                if (!swLWMEmise.IsRunning)
                    swLWMEmise.Start();
                nbLWMEmiseReceived++;
                nbByteLWMEmis += size;
                if (swLWMEmise.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbLWMEmiseReceived + " LWM Emises en 1 s : " + nbByteLWMEmis + " octets");
                    swLWMEmise.Restart();
                    nbLWMEmiseReceived = 0;
                    nbByteLWMEmis = 0;
                }
            }
        }
    }
    public static class GWMEmiseMonitoring
    {
        public static Stopwatch swGWMEmise = new Stopwatch();
        public static int nbGWMEmiseReceived = 0;
        public static int nbByteGWMEmis = 0;
        static Object lockDiagGWMEmise = new object();

        public static void GWMEmiseMonitor(int size)
        {
            lock (lockDiagGWMEmise)
            {
                if (!swGWMEmise.IsRunning)
                    swGWMEmise.Start();
                nbGWMEmiseReceived++;
                nbByteGWMEmis += size;
                if (swGWMEmise.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine(nbGWMEmiseReceived + " GWM Emises en 1 s : " + nbByteGWMEmis + " octets");
                    swGWMEmise.Restart();
                    nbGWMEmiseReceived = 0;
                    nbByteGWMEmis = 0;
                }
            }
        }
    }
    public static class OdometryMonitoring
    {
        public static Stopwatch swOdometryEmise = new Stopwatch();
        public static int nbOdometryEmiseReceived = 0;
        static Object lockDiagOdometryEmise = new object();
        static List<int> listIntervalMonitoring = new List<int>();

        public static void OdometryEmiseMonitor()
        {
            lock (lockDiagOdometryEmise)
            {
                if (!swOdometryEmise.IsRunning)
                    swOdometryEmise.Start();
                nbOdometryEmiseReceived++;

                //if (listIntervalMonitoring.Count > 0)
                listIntervalMonitoring.Add((int)swOdometryEmise.Elapsed.TotalMilliseconds);// - listIntervalMonitoring[listIntervalMonitoring.Count - 1]);
                //else
                //    listIntervalMonitoring.Add((int)swOdometryEmise.Elapsed.TotalMilliseconds);

                if (swOdometryEmise.ElapsedMilliseconds > 1000)
                {
                    string s = nbOdometryEmiseReceived + " Odometry Emises en 1 s - Delta t :";
                    foreach (var interval in listIntervalMonitoring)
                    {
                        s += " " + interval.ToString();
                    }
                    Console.WriteLine(s);

                    swOdometryEmise.Restart();
                    listIntervalMonitoring = new List<int>();
                    nbOdometryEmiseReceived = 0;
                }
            }
        }
    }
    public static class USBMonitoring
    {
        public static Stopwatch swUSBEmise = new Stopwatch();
        public static int nbUSBEmiseReceived = 0;
        static Object lockDiagUSBEmise = new object();
        static List<int> listIntervalMonitoring = new List<int>();
        static List<int> listNbByteRecus = new List<int>();
        public static int nbByteUsbRecus = 0;

        public static void USBRecuMonitor(byte[]buff)
        {
            lock (lockDiagUSBEmise)
            {
                if (!swUSBEmise.IsRunning)
                    swUSBEmise.Start();
                nbUSBEmiseReceived++;
                nbByteUsbRecus += buff.Length;

                listIntervalMonitoring.Add((int)swUSBEmise.Elapsed.TotalMilliseconds);
                listNbByteRecus.Add((int)nbByteUsbRecus);

                if (swUSBEmise.ElapsedMilliseconds > 1000)
                {
                    string s = nbUSBEmiseReceived + " USB Recus en 1 s - Delta t :";
                    foreach (var interval in listIntervalMonitoring)
                        s += " " + interval.ToString();
                    s += " - Nb Bytes : ";
                    foreach (var nbByte in listNbByteRecus)
                        s += " " + nbByte.ToString();

                    Console.WriteLine(s);

                    swUSBEmise.Restart();
                    listIntervalMonitoring = new List<int>();
                    listNbByteRecus = new List<int>();
                    nbUSBEmiseReceived = 0;
                    nbByteUsbRecus = 0;
                }
            }
        }
    }
}
