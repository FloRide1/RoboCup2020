using Hybridizer.Runtime.CUDAImports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GPU_DLL
{
    public class GPU_DLL
    {

        //public void Run(int N, double[] a, double[] b)
        //{
        //    Parallel.For(0, N, i => { a[i] += b[i]; });
        //}

        [EntryPoint("run")]
        public void ParallelCalculateHeatMap(double[,] heatMap, int width, int height, float widthTerrain, float heightTerrain, float destinationX, float destinationY)
        {
            float destXInHeatmap = (float)((float)destinationX / widthTerrain + 0.5) * width;
            float destYInHeatmap = (float)((float)destinationY / heightTerrain + 0.5) * height;

            float normalizer = height;

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    //Calcul de la fonction de cout de stratégie
                    heatMap[y, x] = Math.Max(0, 1 - Math.Sqrt((destXInHeatmap - x) * (destXInHeatmap - x) + (destYInHeatmap - y) * (destYInHeatmap - y)) / normalizer);
                }
            });
        }


        public void GpuGenerateHeatMap(string dllName, double[,] heatMap, int width, int height, float widthTerrain, float heightTerrain, float destinationX, float destinationY)
        {
            cudaDeviceProp prop;
            cuda.GetDeviceProperties(out prop, 0);
            HybRunner runner = HybRunner.Cuda(dllName).SetDistrib(prop.multiProcessorCount * 16, 128);

            // create a wrapper object to call GPU methods instead of C#
            dynamic wrapped = runner.Wrap(this);

            // run the method on GPU
            wrapped.ParallelCalculateHeatMap(heatMap, width, height, widthTerrain, heightTerrain, destinationX, destinationY);

            Console.Out.WriteLine("DONE");
            Thread.Sleep(10000);
        }

        //public void Test(string dllName)
        //{
        //    // 268 MB allocated on device -- should fit in every CUDA compatible GPU
        //    int N = 1024 * 1024 * 16;
        //    double[] acuda = new double[N];
        //    double[] adotnet = new double[N];

        //    double[] b = new double[N];

        //    Random rand = new Random();

        //    //Initialize acuda et adotnet and b by some doubles randoms, acuda and adotnet have same numbers. 
        //    for (int i = 0; i < N; ++i)
        //    {
        //        acuda[i] = rand.NextDouble();
        //        adotnet[i] = acuda[i];
        //        b[i] = rand.NextDouble();
        //    }

        //    cudaDeviceProp prop;
        //    cuda.GetDeviceProperties(out prop, 0);
        //    HybRunner runner = HybRunner.Cuda(dllName).SetDistrib(prop.multiProcessorCount * 16, 128);

        //    // create a wrapper object to call GPU methods instead of C#
        //    dynamic wrapped = runner.Wrap(this);

        //    // run the method on GPU
        //    wrapped.Run(N, acuda, b);

        //    // run .Net method
        //    Run(N, adotnet, b);

        //    // verify the results
        //    for (int k = 0; k < N; ++k)
        //    {
        //        if (acuda[k] != adotnet[k])
        //            Console.Out.WriteLine("ERROR !");
        //    }
        //    Console.Out.WriteLine("DONE");
        //    Thread.Sleep(10000);
        //}
    }
}
