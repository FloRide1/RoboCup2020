using GPU_DLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_GPU_Caller
{
    class Program
    {
        static void Main(string[] args)
        {
            GPU_DLL.GPU_DLL gpuDllTest = new GPU_DLL.GPU_DLL();
            gpuDllTest.Test();
        }
    }
}
