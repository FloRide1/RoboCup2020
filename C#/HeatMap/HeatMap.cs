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
        double FieldLength;
        double FieldHeight;
        
        double HalfFieldLength;
        double HalfFieldHeight;

        public double[,] BaseHeatMapData;

        public int nbIterations;

        public List<double> SubSamplingRateList;
        public List<double> SubSamplingCellSizeList;
        public List<double> nbCellInSubSampledHeatMapHeightList;
        public List<double> nbCellInSubSampledHeatMapWidthList;

        ////public double[,] SubSampledHeatMapData1;
        //public double SubSamplingRate1;
        //public double SubSampledCellSize1;
        //public int nbCellInSubSampledHeatMapHeight1;
        //public int nbCellInSubSampledHeatMapWidth1;

        ////public double[,] SubSampledHeatMapData2;
        //public double SubSamplingRate2;
        //public double SubSampledCellSize2;
        //public int nbCellInSubSampledHeatMapHeight2;
        //public int nbCellInSubSampledHeatMapWidth2;

        public double BaseCellSize;
        public int nbCellInBaseHeatMapHeight;
        public int nbCellInBaseHeatMapWidth;

        public Heatmap(double length, double height, double baseCellSize, int iterations)
        {
            BaseCellSize = baseCellSize;
            FieldLength = length;
            FieldHeight = height;
            HalfFieldLength = FieldLength / 2;
            HalfFieldHeight = FieldHeight / 2;

            nbIterations = iterations;

            SubSamplingRateList = new List<double>();
            SubSamplingCellSizeList = new List<double>();
            nbCellInSubSampledHeatMapHeightList = new List<double>();
            nbCellInSubSampledHeatMapWidthList = new List<double>();

            for (int i = 0; i < nbIterations; i++)
            {
                double subSamplingRate = Math.Pow(length / baseCellSize, (nbIterations-(i+1.0)) / nbIterations);
                SubSamplingRateList.Add(subSamplingRate);
                SubSamplingCellSizeList.Add((double)(BaseCellSize * subSamplingRate));
                nbCellInSubSampledHeatMapHeightList.Add((double)(FieldHeight / BaseCellSize / subSamplingRate));
                nbCellInSubSampledHeatMapWidthList.Add((double)(FieldLength / BaseCellSize /subSamplingRate));
            }
            
            //subSamplingRate1 = Math.Round(Math.Pow(length / baseCellSize, 1 / 3.0));
            //subSamplingRate2 = Math.Round(Math.Pow(length / baseCellSize, 2 / 3.0));

            BaseCellSize = baseCellSize;
            nbCellInBaseHeatMapHeight = (int)(FieldHeight / BaseCellSize);
            nbCellInBaseHeatMapWidth = (int)(FieldLength / BaseCellSize);
            BaseHeatMapData = new double[nbCellInBaseHeatMapHeight, nbCellInBaseHeatMapWidth];

            //SubSamplingRate1 = subSamplingRate1;
            //SubSampledCellSize1 = BaseCellSize * subSamplingRate1;
            //nbCellInSubSampledHeatMapHeight1 = (int)(FieldHeight / BaseCellSize / subSamplingRate1);
            //nbCellInSubSampledHeatMapWidth1 = (int)(FieldLength / BaseCellSize / subSamplingRate1);
            //SubSampledHeatMapData1 = new double[nbCellInSubSampledHeatMapHeight1, nbCellInSubSampledHeatMapWidth1];

            //SubSamplingRate2 = subSamplingRate2;
            //SubSampledCellSize2 = BaseCellSize * subSamplingRate2;
            //nbCellInSubSampledHeatMapHeight2 = (int)(FieldHeight / BaseCellSize / subSamplingRate2);
            //nbCellInSubSampledHeatMapWidth2 = (int)(FieldLength / BaseCellSize / subSamplingRate2);
            //SubSampledHeatMapData2 = new double[nbCellInSubSampledHeatMapHeight2, nbCellInSubSampledHeatMapWidth2];
        }

        public PointD GetFieldPosFromBaseHeatMapCoordinates(double x, double y)
        {
            return new PointD(-HalfFieldLength + x * BaseCellSize, -HalfFieldHeight + y * BaseCellSize);
        }
        public PointD GetBaseHeatMapPosFromFieldCoordinates(double x, double y)
        {
            return new PointD((x + HalfFieldLength) / BaseCellSize, (y + HalfFieldHeight) / BaseCellSize);
            //return new PointD(-HalfFieldLength + x * BaseCellSize, -HalfFieldHeight + y * BaseCellSize);
        }

        public PointD GetFieldPosFromSubSampledHeatMapCoordinates(double x, double y, int n)
        {
            return new PointD(-HalfFieldLength + x * SubSamplingCellSizeList[n], -HalfFieldHeight + y * SubSamplingCellSizeList[n]);
        }
        //public PointD GetFieldPosFromSubSampledHeatMapCoordinates1(int x, int y)
        //{
        //    return new PointD(-HalfFieldLength + x * SubSampledCellSize1, -HalfFieldHeight + y * SubSampledCellSize1);
        //}

        //public PointD GetFieldPosFromSubSampledHeatMapCoordinates2(int x, int y)
        //{
        //    return new PointD(-HalfFieldLength + x * SubSampledCellSize2, -HalfFieldHeight + y * SubSampledCellSize2);
        //}

        double max = double.NegativeInfinity;
        int maxPosX = 0;
        int maxPosY = 0;

        public PointD GetMaxPositionInBaseHeatMap()
        {
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

        public Heatmap Copy()
        {
            Heatmap copyHeatmap = new Heatmap(FieldLength, FieldHeight, BaseCellSize, nbIterations);            
            copyHeatmap.BaseHeatMapData = this.BaseHeatMapData;
            return copyHeatmap;
        }

    }
}
