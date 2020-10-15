using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace HeatMap
{
    public class Heatmap
    {
        public double FieldLength;
        public double FieldHeight;
        
        double HalfFieldLength;
        double HalfFieldHeight;

        public double[,] BaseHeatMapData;

        public int nbIterations;

        public double[] SubSamplingRateList;
        public double[] SubSamplingCellSizeList;
        public double[] nbCellInSubSampledHeatMapHeightList;
        public double[] nbCellInSubSampledHeatMapWidthList;

        public double BaseXCellSize;
        public double BaseYCellSize;
        public int nbCellInBaseHeatMapHeight;
        public int nbCellInBaseHeatMapWidth;

        public Heatmap(double length, double height, int lengthCellNumber, int iterations)
        {
            BaseXCellSize = length / lengthCellNumber;
            BaseYCellSize = height / Math.Floor(height / BaseXCellSize);// BaseXCellSize * height / length;

            FieldLength = length;
            FieldHeight = height;
            HalfFieldLength = FieldLength / 2;
            HalfFieldHeight = FieldHeight / 2;

            nbIterations = iterations;

            SubSamplingRateList = new double[nbIterations];
            SubSamplingCellSizeList = new double[nbIterations];
            nbCellInSubSampledHeatMapHeightList = new double[nbIterations];
            nbCellInSubSampledHeatMapWidthList = new double[nbIterations];

            for (int i = 0; i < nbIterations; i++)
            {
                double subSamplingRate = Math.Pow(length / BaseXCellSize, (nbIterations-(i+1.0)) / nbIterations);
                SubSamplingRateList[i]=subSamplingRate;
                SubSamplingCellSizeList[i] = (double)(BaseXCellSize * subSamplingRate);
                nbCellInSubSampledHeatMapHeightList[i] = (double)(FieldHeight / BaseXCellSize / subSamplingRate);
                nbCellInSubSampledHeatMapWidthList[i] = (double)(FieldLength / BaseXCellSize / subSamplingRate);
            }            

            //BaseCellSize = baseCellSize;
            nbCellInBaseHeatMapHeight = (int)(FieldHeight / BaseYCellSize) +1;
            nbCellInBaseHeatMapWidth = (int)(FieldLength / BaseXCellSize) +1;
            BaseHeatMapData = new double[nbCellInBaseHeatMapHeight, nbCellInBaseHeatMapWidth];
        }

        public void ReInitHeatMapData()
        {
            BaseHeatMapData = new double[nbCellInBaseHeatMapHeight, nbCellInBaseHeatMapWidth];
        }
               
        public PointD GetFieldPosFromBaseHeatMapCoordinates(double x, double y)
        {
            //return new PointD(-HalfFieldLength + x * BaseCellSize, -HalfFieldHeight + y * BaseCellSize);
            return new PointD(-HalfFieldLength + x * BaseXCellSize, -HalfFieldHeight + y * BaseYCellSize);
        }
        public PointD GetBaseHeatMapPosFromFieldCoordinates(double x, double y)
        {
            return new PointD((x + HalfFieldLength) / BaseXCellSize, (y + HalfFieldHeight) / BaseYCellSize);
        }

        public PointD GetFieldPosFromSubSampledHeatMapCoordinates(double x, double y, int n)
        {
            return new PointD(-HalfFieldLength + x * SubSamplingCellSizeList[n], -HalfFieldHeight + y * SubSamplingCellSizeList[n]);
        }

        double max = double.NegativeInfinity;
        int maxPosX = 0;
        int maxPosY = 0;

        public PointD GetMaxPositionInBaseHeatMap()
        {
            //Fonction couteuse en temps : à éviter !
            max = double.NegativeInfinity;
            for (int y = 0; y < nbCellInBaseHeatMapHeight; y++)
            {
                for (int x = 0; x < nbCellInBaseHeatMapWidth; x++)
                {
                    if (BaseHeatMapData[y, x] > max)
                    {
                        max = BaseHeatMapData[y, x];
                        maxPosX = x;
                        maxPosY = y;
                    }
                }
            }
            return GetFieldPosFromBaseHeatMapCoordinates(maxPosX, maxPosY);
        }
        public PointD GetMaxPositionInBaseHeatMapCoordinates()
        {
            //Fonction couteuse en temps : à éviter
            max = double.NegativeInfinity;
            for (int y = 0; y < nbCellInBaseHeatMapHeight; y++)
            {
                for (int x = 0; x < nbCellInBaseHeatMapWidth; x++)
                {
                    if (BaseHeatMapData[y, x] > max)
                    {
                        max = BaseHeatMapData[y, x];
                        maxPosX = x;
                        maxPosY = y;
                    }
                }
            }
            return new PointD(maxPosX, maxPosY);
        }

        //public Heatmap Copy()
        //{
        //    Heatmap copyHeatmap = new Heatmap(FieldLength, FieldHeight, BaseCellSize, nbIterations);            
        //    copyHeatmap.BaseHeatMapData = this.BaseHeatMapData;
        //    return copyHeatmap;
        //}
    }
}
