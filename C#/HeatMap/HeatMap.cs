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
        public double BaseXCellSize;
        public double BaseYCellSize;
        public int nbCellInBaseHeatMapHeight;
        public int nbCellInBaseHeatMapWidth;

        public float preferedDestinationX;
        public float preferedDestinationY;

        public Heatmap(double length, double height, int lengthCellNumber)//, int iterations)
        {
            BaseXCellSize = length / lengthCellNumber;
            BaseYCellSize = height / Math.Floor(height / BaseXCellSize);// BaseXCellSize * height / length;

            FieldLength = length;
            FieldHeight = height;
            HalfFieldLength = FieldLength / 2;
            HalfFieldHeight = FieldHeight / 2;
            
            nbCellInBaseHeatMapHeight = (int)(FieldHeight / BaseYCellSize) +1;
            nbCellInBaseHeatMapWidth = (int)(FieldLength / BaseXCellSize) +1;
            BaseHeatMapData = new double[nbCellInBaseHeatMapHeight, nbCellInBaseHeatMapWidth];
        }

        public void InitHeatMapData()
        {
            BaseHeatMapData = new double[nbCellInBaseHeatMapHeight, nbCellInBaseHeatMapWidth];
        }
               
        public PointD GetFieldPosFromBaseHeatMapCoordinates(double xHeatMap, double yHeatMap)
        {
            //return new PointD(-HalfFieldLength + x * BaseCellSize, -HalfFieldHeight + y * BaseCellSize);
            double xField = (xHeatMap / (nbCellInBaseHeatMapWidth - 1) - 0.5) * FieldLength;
            double yField = (yHeatMap / (nbCellInBaseHeatMapHeight - 1) - 0.5) * FieldHeight;

            return new PointD(xField, yField);

        }
        public PointD GetBaseHeatMapPosFromFieldCoordinates(PointD ptTerrain)
        {
            double xHeatmap = (ptTerrain.X / FieldLength + 0.5) * (nbCellInBaseHeatMapWidth - 1);
            double yHeatmap = (ptTerrain.Y / FieldHeight + 0.5) * (nbCellInBaseHeatMapHeight - 1);

            return new PointD(xHeatmap, yHeatmap);
        }

        public double GetBaseHeatMapXPosFromFieldCoordinates(double posX)
        {
            return (posX / FieldLength + 0.5) * (nbCellInBaseHeatMapWidth - 1);
        }
        public double GetBaseHeatMapYPosFromFieldCoordinates(double posY)
        {
            return (posY / FieldHeight+ 0.5) * (nbCellInBaseHeatMapHeight - 1);
        }

        public double GetBaseHeatMapDistanceFromFieldDistance(double distTerrain)
        {
            //return new PointD((x + HalfFieldLength) / BaseXCellSize, (y + HalfFieldHeight) / BaseYCellSize);

            return distTerrain / FieldLength * (nbCellInBaseHeatMapWidth - 1);
        }

        //public PointD GetFieldPosFromSubSampledHeatMapCoordinates(double x, double y, int n)
        //{
        //    return new PointD(-HalfFieldLength + x * BaseXCellSize, -HalfFieldHeight + y * BaseYCellSize);
        //}

        //double max = double.NegativeInfinity;
        //int maxPosX = 0;
        //int maxPosY = 0;

        ////public PointD GetMaxPositionInBaseHeatMap()
        ////{
        ////    //Fonction couteuse en temps : à éviter !
        ////    max = double.NegativeInfinity;
        ////    for (int y = 0; y < nbCellInBaseHeatMapHeight; y++)
        ////    {
        ////        for (int x = 0; x < nbCellInBaseHeatMapWidth; x++)
        ////        {
        ////            if (BaseHeatMapData[y, x] > max)
        ////            {
        ////                max = BaseHeatMapData[y, x];
        ////                maxPosX = x;
        ////                maxPosY = y;
        ////            }
        ////        }
        ////    }
        ////    return GetFieldPosFromBaseHeatMapCoordinates(maxPosX, maxPosY);
        ////}
        ////public PointD GetMaxPositionInBaseHeatMapCoordinates()
        ////{
        ////    //Fonction couteuse en temps : à éviter
        ////    max = double.NegativeInfinity;
        ////    for (int y = 0; y < nbCellInBaseHeatMapHeight; y++)
        ////    {
        ////        for (int x = 0; x < nbCellInBaseHeatMapWidth; x++)
        ////        {
        ////            if (BaseHeatMapData[y, x] > max)
        ////            {
        ////                max = BaseHeatMapData[y, x];
        ////                maxPosX = x;
        ////                maxPosY = y;
        ////            }
        ////        }
        ////    }
        ////    return new PointD(maxPosX, maxPosY);
        ////}

        public void GenerateHeatMap(List<Zone> preferredZonesList, List<Zone> avoidanceZonesList, List<RectangleZone> forbiddenRectangleList)
        {
            //Gestion des zones interdites
            lock (forbiddenRectangleList)
            {
                foreach (var forbiddenRectangle in forbiddenRectangleList)
                {
                    var xMinHeatMap = GetBaseHeatMapXPosFromFieldCoordinates(forbiddenRectangle.rectangularZone.Xmin);
                    var xMaxHeatMap = GetBaseHeatMapXPosFromFieldCoordinates(forbiddenRectangle.rectangularZone.Xmax);
                    var yMinHeatMap = GetBaseHeatMapYPosFromFieldCoordinates(forbiddenRectangle.rectangularZone.Ymin);
                    var yMaxHeatMap = GetBaseHeatMapYPosFromFieldCoordinates(forbiddenRectangle.rectangularZone.Ymax);

                    for (int y = (int)Math.Max(0, yMinHeatMap); y < (int)(Math.Min(nbCellInBaseHeatMapHeight, yMaxHeatMap)); y++)
                    {
                        for (int x = (int)Math.Max(0, xMinHeatMap); x < (int)(Math.Min(nbCellInBaseHeatMapWidth, xMaxHeatMap)); x++)
                        {
                            BaseHeatMapData[y, x] = -1;
                        }
                    }
                }
            }

            lock (avoidanceZonesList)
            {
                foreach (var avoidanceZone in avoidanceZonesList)
                {
                    var centerRefHeatMap = GetBaseHeatMapPosFromFieldCoordinates(avoidanceZone.center);
                    var radiusRefHeatMap = GetBaseHeatMapDistanceFromFieldDistance(avoidanceZone.radius);
                    var strength = avoidanceZone.strength;

                    for (int y = (int)Math.Max(0, centerRefHeatMap.Y - radiusRefHeatMap); y < (int)(Math.Min(nbCellInBaseHeatMapHeight, centerRefHeatMap.Y + radiusRefHeatMap)); y++)
                    {
                        for (int x = (int)Math.Max(0, centerRefHeatMap.X - radiusRefHeatMap); x < (int)(Math.Min(nbCellInBaseHeatMapWidth, centerRefHeatMap.X + radiusRefHeatMap)); x++)
                        {
                            //if (BaseHeatMapData[y, x] > -1) //On regarde si on n'est pas dans une zone exclue (valeur <= -1)
                            {
                                BaseHeatMapData[y, x] -= strength * Math.Max(0, 1 - Math.Sqrt((centerRefHeatMap.X - x) * (centerRefHeatMap.X - x) + (centerRefHeatMap.Y - y) * (centerRefHeatMap.Y - y)) / radiusRefHeatMap);
                            }
                        }
                    }
                }
            }

            lock (preferredZonesList)
            {
                foreach (var preferredZone in preferredZonesList)
                {
                    var centerRefHeatMap = GetBaseHeatMapPosFromFieldCoordinates(preferredZone.center);
                    var radiusRefHeatMap = GetBaseHeatMapDistanceFromFieldDistance(preferredZone.radius);
                    var strength = preferredZone.strength;

                    for (int y = (int)Math.Max(0, centerRefHeatMap.Y - radiusRefHeatMap); y < (int)(Math.Min(nbCellInBaseHeatMapHeight, centerRefHeatMap.Y + radiusRefHeatMap)); y++)
                    {
                        for (int x = (int)Math.Max(0, centerRefHeatMap.X - radiusRefHeatMap); x < (int)(Math.Min(nbCellInBaseHeatMapWidth, centerRefHeatMap.X + radiusRefHeatMap)); x++)
                        {
                            //if (BaseHeatMapData[y, x] > -1) //On regarde si on n'est pas dans une zone exclue (valeur <= -1)
                            {
                                BaseHeatMapData[y, x] += strength * Math.Max(0, 1 - Math.Sqrt((centerRefHeatMap.X - x) * (centerRefHeatMap.X - x) + (centerRefHeatMap.Y - y) * (centerRefHeatMap.Y - y)) / radiusRefHeatMap);
                            }
                        }
                    }
                }
            }
        }

        //public void GenerateHeatMap(double[,] heatMap, int width, int height, float widthTerrain, float heightTerrain)
        //{
        //    float destXInHeatmap = (float)(preferedDestinationX / widthTerrain + 0.5) * (width - 1);  //-1 car on a augmenté la taille de 1 pour avoir une figure symétrique
        //    float destYInHeatmap = (float)(preferedDestinationY / heightTerrain + 0.5) * (height - 1);  //-1 car on a augmenté la taille de 1 pour avoir une figure symétrique

        //    float normalizer = height;

        //    Parallel.For(0, height, y =>
        //    //for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x++)
        //        {
        //            //Calcul de la fonction de cout de stratégie
        //            heatMap[y, x] = Math.Max(0, 1 - Math.Sqrt((destXInHeatmap - x) * (destXInHeatmap - x) + (destYInHeatmap - y) * (destYInHeatmap - y)) / normalizer);
        //        }
        //    });
        //}

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
    }
}
