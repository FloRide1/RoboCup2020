using GPU_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Test_GPU_Caller
{
    class Program
    {

        static GPU_DLL.GPU_DLL gpuDllTest = new GPU_DLL.GPU_DLL();

        static void Main(string[] args)
        {
            Timer tmr = new Timer(100);
            tmr.Elapsed += Tmr_Elapsed;
            tmr.Start();

            while (true) ;
        }
        private static void Tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            gpuDllTest.Test("GPU_DLL_CUDA.dll");
        }

    }
}
