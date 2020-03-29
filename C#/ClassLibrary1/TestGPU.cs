
using Hybridizer.Runtime.CUDAImports;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestGPU
{
    public class TestGPU
    {
        [EntryPoint("run")]
        public  void ParallelCalculateHeatMap_Cuda(string dllName, double[,] heatMap, int width, int height, float widthTerrain, float heightTerrain, float destinationX, float destinationY)
        {
            cudaDeviceProp prop;
            cuda.GetDeviceProperties(out prop, 0);
            HybRunner runner = HybRunner.Cuda(dllName).SetDistrib(prop.multiProcessorCount * 16, 128);

            // create a wrapper object to call GPU methods instead of C#
            dynamic wrapped = runner.Wrap(this);
            // run the method on GPU
            wrapped.ParallelCalculateHeatMap(heatMap, width, height, widthTerrain, heightTerrain, destinationX, destinationY);        
        }

        public void ParallelCalculateHeatMap(double[,] heatMap, int width, int height, float widthTerrain, float heightTerrain, float destinationX, float destinationY)
        {
            float destXInHeatmap = (float)((float)destinationX / widthTerrain + 0.5) * width;
            float destYInHeatmap = (float)((float)destinationY / heightTerrain + 0.5) * height;

            float normalizer = height;

            Parallel.For(0, height, y =>
            //for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //Calcul de la fonction de cout de stratégie
                    heatMap[y, x] = Math.Max(0, 1 - Math.Sqrt((destXInHeatmap - x) * (destXInHeatmap - x) + (destYInHeatmap - y) * (destYInHeatmap - y)) / normalizer);
                }
            });
        }
    }
}
