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

        public double[,] SubSampledHeatMapData1;
        public double SubSamplingRate1;
        public double SubSampledCellSize1;
        public int nbCellInSubSampledHeatMapHeight1;
        public int nbCellInSubSampledHeatMapWidth1;

        public double[,] SubSampledHeatMapData2;
        public double SubSamplingRate2;
        public double SubSampledCellSize2;
        public int nbCellInSubSampledHeatMapHeight2;
        public int nbCellInSubSampledHeatMapWidth2;

        public double BaseCellSize;
        public int nbCellInBaseHeatMapHeight;
        public int nbCellInBaseHeatMapWidth;

        public Heatmap(double length, double height, double baseCellSize=0.01, double subSamplingRate1 = 10, double subSamplingRate2 = 100)
        {
            FieldLength = length;
            FieldHeight = height;
            HalfFieldLength = FieldLength / 2;
            HalfFieldHeight = FieldHeight / 2;

            BaseCellSize = baseCellSize;
            nbCellInBaseHeatMapHeight = (int)(FieldHeight / BaseCellSize);
            nbCellInBaseHeatMapWidth = (int)(FieldLength / BaseCellSize);
            BaseHeatMapData = new double[nbCellInBaseHeatMapHeight, nbCellInBaseHeatMapWidth];

            SubSamplingRate1 = subSamplingRate1;
            SubSampledCellSize1 = BaseCellSize * subSamplingRate1;
            nbCellInSubSampledHeatMapHeight1 = (int)(FieldHeight / BaseCellSize / subSamplingRate1);
            nbCellInSubSampledHeatMapWidth1 = (int)(FieldLength / BaseCellSize / subSamplingRate1);
            SubSampledHeatMapData1 = new double[nbCellInSubSampledHeatMapHeight1, nbCellInSubSampledHeatMapWidth1];

            SubSamplingRate2 = subSamplingRate2;
            SubSampledCellSize2 = BaseCellSize * subSamplingRate2;
            nbCellInSubSampledHeatMapHeight2 = (int)(FieldHeight / BaseCellSize / subSamplingRate2);
            nbCellInSubSampledHeatMapWidth2 = (int)(FieldLength / BaseCellSize / subSamplingRate2);
            SubSampledHeatMapData2 = new double[nbCellInSubSampledHeatMapHeight2, nbCellInSubSampledHeatMapWidth2];
        }

        public PointD GetFieldPosFromBaseHeatMapCoordinates(int x, int y)
        {
            return new PointD(-HalfFieldLength + x * BaseCellSize, -HalfFieldHeight + y * BaseCellSize);
        }

        public PointD GetFieldPosFromSubSampledHeatMapCoordinates1(int x, int y)
        {
            return new PointD(-HalfFieldLength + x * SubSampledCellSize1, -HalfFieldHeight + y * SubSampledCellSize1);
        }

        public PointD GetFieldPosFromSubSampledHeatMapCoordinates2(int x, int y)
        {
            return new PointD(-HalfFieldLength + x * SubSampledCellSize2, -HalfFieldHeight + y * SubSampledCellSize2);
        }

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
            Heatmap copyHeatmap = new Heatmap(FieldLength, FieldHeight, BaseCellSize, SubSamplingRate1, SubSamplingRate2);            
            copyHeatmap.BaseHeatMapData = this.BaseHeatMapData;
            copyHeatmap.SubSampledHeatMapData1 = this.SubSampledHeatMapData1;
            copyHeatmap.SubSampledHeatMapData2 = this.SubSampledHeatMapData2;
            return copyHeatmap;
        }

    }
}
