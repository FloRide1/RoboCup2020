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

        public float preferedDestinationX;
        public float preferedDestinationY;

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

        public void SetPreferedDestination(float destinationX, float destinationY)
        {
            preferedDestinationX = destinationX;
            preferedDestinationY = destinationY;
        }

        public void GenerateHeatMap(double[,] heatMap, int width, int height, float widthTerrain, float heightTerrain)
        {
            float destXInHeatmap = (float)(preferedDestinationX / widthTerrain + 0.5) * (width - 1);  //-1 car on a augmenté la taille de 1 pour avoir une figure symétrique
            float destYInHeatmap = (float)(preferedDestinationY / heightTerrain + 0.5) * (height - 1);  //-1 car on a augmenté la taille de 1 pour avoir une figure symétrique

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

        public PointD GetOptimalPosition()
        {

            //Détermination
            double[] tabMax = new double[nbCellInBaseHeatMapHeight];
            int[] tabIndexMax = new int[nbCellInBaseHeatMapHeight];
            Parallel.For(0, nbCellInBaseHeatMapHeight, i =>
            {
                tabMax[i] = 0;
                tabIndexMax[i] = 0;
                for (int j = 0; j < nbCellInBaseHeatMapWidth; j++)
                {
                    if (BaseHeatMapData[i, j] > tabMax[i])
                    {
                        tabMax[i] = BaseHeatMapData[i, j];
                        tabIndexMax[i] = j;
                    }
                }
            });

            //Recherche du maximum
            double max = 0;
            int indexMax = 0;
            for (int i = 0; i < nbCellInBaseHeatMapHeight; i++)
            {
                if (tabMax[i] > max)
                {
                    max = tabMax[i];
                    indexMax = i;
                }
            }

            int maxYpos = indexMax;// indexMax % heatMap.nbCellInBaseHeatMapWidth;
            int maxXpos = tabIndexMax[indexMax];// indexMax / heatMap.nbCellInBaseHeatMapWidth;

            //On a le point dans le référentiel de la heatMap, on la passe en référentiel Terrain
            return GetFieldPosFromBaseHeatMapCoordinates(maxXpos, maxYpos);
            //return new PointD(maxXpos, maxYpos);
        }

        //public Heatmap Copy()
        //{
        //    Heatmap copyHeatmap = new Heatmap(FieldLength, FieldHeight, BaseCellSize, nbIterations);            
        //    copyHeatmap.BaseHeatMapData = this.BaseHeatMapData;
        //    return copyHeatmap;
        //}
    }
}
