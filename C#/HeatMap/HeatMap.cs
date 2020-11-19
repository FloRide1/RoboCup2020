﻿using System;
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

        public void ExcludeMaskedZones(PointD robotLocation, List<LocationExtended> obstacleLocationList, double exclusionRadius)
        {
            int nbPolygonPoints = 12;
            var exclusionRadiusRefHeatMap = GetBaseHeatMapDistanceFromFieldDistance(exclusionRadius);
            var RobotLocationRefHeatMap = GetBaseHeatMapPosFromFieldCoordinates(new PointD(robotLocation.X, robotLocation.Y));

            /// On calcule la pénalisation sur la liste des obstacles à éviter
            lock (obstacleLocationList)
            {                
                foreach (var obstacle in obstacleLocationList)
                {
                    var centerObstacleRefHeatMap = GetBaseHeatMapPosFromFieldCoordinates(new PointD(obstacle.X, obstacle.Y));
                    double angleCentreObstacle = Math.Atan2(centerObstacleRefHeatMap.Y - RobotLocationRefHeatMap.Y, centerObstacleRefHeatMap.X - RobotLocationRefHeatMap.X);


                    /// Pour chaque obstacle, on détermine les points d'un polygone l'entourant à n cotés pour approximer le cercle 
                    /// d'exclusion du point obstacle considéré.
                    /// Pour chacun de ces points, on détermine l'angle Robot-Point, et on trouve le plus grand et le plus petit qui forment les
                    /// angles max et min du cone d'exclusion.
                    /// 

                    List<PointD> listPtsPolygonExclusion = new List<PointD>();
                    List<double> listAnglePtsPolygonExclusion = new List<double>();
                    for (int i=0; i<nbPolygonPoints; i++)
                    {
                        listPtsPolygonExclusion.Add(new PointD(centerObstacleRefHeatMap.X + exclusionRadiusRefHeatMap * Math.Cos(2 * Math.PI / nbPolygonPoints * i), centerObstacleRefHeatMap.Y + exclusionRadiusRefHeatMap * Math.Sin(2 * Math.PI / nbPolygonPoints * i)));
                        /// On détermine l'angle de chacun des points de la zone d'exclusion du polygon centré autour de l'obstacle
                        double anglePt = Math.Atan2(listPtsPolygonExclusion[i].Y - RobotLocationRefHeatMap.Y, listPtsPolygonExclusion[i].X - RobotLocationRefHeatMap.X);
                        /// On décale cet angle autour de l'angle du centre de l'obstacle pour éviter les discontinuités 0 / 2*PI
                        anglePt= Toolbox.ModuloByAngle(angleCentreObstacle, anglePt);
                        listAnglePtsPolygonExclusion.Add(anglePt);
                    }

                    double minAngle = listAnglePtsPolygonExclusion.Min();
                    int indexAngleMin = listAnglePtsPolygonExclusion.IndexOf(minAngle);
                    PointD ptAngleMin = listPtsPolygonExclusion[indexAngleMin];

                    double maxAngle = listAnglePtsPolygonExclusion.Max();
                    int indexAngleMax = listAnglePtsPolygonExclusion.IndexOf(maxAngle);
                    PointD ptAngleMax = listPtsPolygonExclusion[indexAngleMax];

                    /// On connait les deux points de tangence de la zone d'exclusion ptMinAngle et ptMaxAngle
                    /// On va éliminer les pts masqué par balayage en X uniquement, on veut donc un pt de périmètre 
                    /// pour chaque valeur de X entre ptAngleMin.X et nbCellInBaseHeatMapWidth si Angle Min est compris entre -PI/2 et PI/2
                    /// On peut s'arrêter avant si on a atteint le bord supérieur ou inférieur de la HeatMap
                    /// 
                    /// pour chaque valeur de X entre ptAngleMin.X et 0 si Angle Min n'est pas compris entre -PI/2 et PI/2
                    /// On peut s'arrêter avant si on a atteint le bord supérieur ou inférieur de la HeatMap
                    List<PointD> listPtsPerimetreExclusion = new List<PointD>();

                    int[] listAbscissesZoneExclusionInferieure = new int[nbCellInBaseHeatMapWidth];
                    int[] listAbscissesZoneExclusionSuperieure = new int[nbCellInBaseHeatMapWidth];


                    /// Attention : algo un peu compliqué... 
                    /// Si les angles min et max sont dans le même demi-plan vertical par rapport au robot 
                    /// alors on est dans un cas ou il y a une partie supérieure et une partie inférieure au cone d'exclusion
                    /// Si ce n'est pas le cas, on est de par et d'autre de la verticale : il n'y a donc qu'une partie supérieure OU une partie inférieure
                    /// 

                    if (((Math.Abs(minAngle) < Math.PI / 2) && (Math.Abs(maxAngle) < Math.PI / 2))
                        || ((Math.Abs(minAngle) >= Math.PI / 2) && (Math.Abs(maxAngle) >= Math.PI / 2)))
                    {
                        ///On est dans la situation classique avec une partie sup et une partie inférieure

                        /// Partie inférieure du périmètre d'exclusion
                        if (Math.Abs(minAngle) < Math.PI / 2)
                        {
                            /// On commence par la portion linéaire du cone
                            for (int x = (int)ptAngleMin.X; x < nbCellInBaseHeatMapWidth; x++)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMin.Y + (x - ptAngleMin.X) * Math.Tan(minAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionInferieure[x] = (int)perimetrePt.Y;
                            }
                            /// On gère la partie circulaire
                            for (int x = (int)(centerObstacleRefHeatMap.X - exclusionRadiusRefHeatMap); x <= ptAngleMin.X; x++)
                            {
                                int y = (int)(centerObstacleRefHeatMap.Y - Math.Sqrt(Math.Max(0, exclusionRadiusRefHeatMap * exclusionRadiusRefHeatMap - (x - centerObstacleRefHeatMap.X) * (x - centerObstacleRefHeatMap.X))));

                                listPtsPerimetreExclusion.Add(new PointD(x, y));
                                listAbscissesZoneExclusionInferieure[x] = y;
                            }
                        }
                        else
                        {
                            /// Si on est dans la partie abs(angle)>Pi/2, le minAngle est en haut, et le maxAngle est en bas ! Attention !!!
                            for (int x = (int)ptAngleMax.X; x >= 0; x--)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMax.Y + (x - ptAngleMax.X) * Math.Tan(maxAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionInferieure[x] = (int)perimetrePt.Y;
                            }

                            /// On gère la partie circulaire
                            for (int x = (int)ptAngleMax.X; x <= (int)(centerObstacleRefHeatMap.X + exclusionRadiusRefHeatMap); x++)
                            {
                                int y = (int)(centerObstacleRefHeatMap.Y - Math.Sqrt(Math.Max(0, exclusionRadiusRefHeatMap * exclusionRadiusRefHeatMap - (x - centerObstacleRefHeatMap.X) * (x - centerObstacleRefHeatMap.X))));

                                listPtsPerimetreExclusion.Add(new PointD(x, y));
                                listAbscissesZoneExclusionInferieure[x] = y;
                            }
                        }

                        /// Partie supérieure du périmètre d'exclusion
                        if (Math.Abs(maxAngle) < Math.PI / 2)
                        {
                            for (int x = (int)ptAngleMax.X; x < nbCellInBaseHeatMapWidth; x++)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMax.Y + (x - ptAngleMax.X) * Math.Tan(maxAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionSuperieure[x] = (int)perimetrePt.Y;
                            }
                            /// On gère la partie circulaire
                            for (int x = (int)(centerObstacleRefHeatMap.X - exclusionRadiusRefHeatMap); x <= ptAngleMin.X; x++)
                            {
                                int y = (int)(centerObstacleRefHeatMap.Y + Math.Sqrt(Math.Max(0, exclusionRadiusRefHeatMap * exclusionRadiusRefHeatMap - (x - centerObstacleRefHeatMap.X) * (x - centerObstacleRefHeatMap.X))));

                                listPtsPerimetreExclusion.Add(new PointD(x, y));
                                listAbscissesZoneExclusionSuperieure[x] = y;
                            }
                        }
                        else
                        {
                            /// Si on est dans la partie abs(angle)>Pi/2, le minAngle est en haut, et le maxAngle est en bas ! Attention !!!
                            for (int x = (int)ptAngleMin.X; x >= 0; x--)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMin.Y + (x - ptAngleMin.X) * Math.Tan(minAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionSuperieure[x] = (int)perimetrePt.Y;
                            }

                            ///// On gère la partie circulaire
                            for (int x = (int)ptAngleMin.X; x <= (int)(centerObstacleRefHeatMap.X + exclusionRadiusRefHeatMap); x++)
                            {
                                int y = (int)(centerObstacleRefHeatMap.Y + Math.Sqrt(Math.Max(0, exclusionRadiusRefHeatMap * exclusionRadiusRefHeatMap - (x - centerObstacleRefHeatMap.X) * (x - centerObstacleRefHeatMap.X))));

                                listPtsPerimetreExclusion.Add(new PointD(x, y));
                                listAbscissesZoneExclusionSuperieure[x] = y;
                            }

                        }
                    }
                    else
                    {
                        //On gère les cas où le minAngle et le maxAngle sont dans des cadrans différents
                        /// Partie inférieure du périmètre d'exclusion
                        if (Toolbox.Modulo2PiAngleRad(angleCentreObstacle) < 0)
                        {
                            //On est dans le demi-plan inférieur, et on n'a que des périmètre supérieur
                            //Angle Max est forcément supérieur à -Pi/2
                            for (int x = (int)ptAngleMax.X; x < nbCellInBaseHeatMapWidth; x++)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMax.Y + (x - ptAngleMax.X) * Math.Tan(maxAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionSuperieure[x] = (int)perimetrePt.Y;
                            }
                            //Angle Min est forcément inférieur à -Pi/2
                            for (int x = (int)ptAngleMin.X; x >= 0; x--)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMin.Y + (x - ptAngleMin.X) * Math.Tan(minAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionSuperieure[x] = (int)perimetrePt.Y;
                            }

                            ///// On gère la partie circulaire
                            for (int x = (int)ptAngleMin.X; x <= ptAngleMax.X; x++)
                            {
                                int y = (int)(centerObstacleRefHeatMap.Y + Math.Sqrt(Math.Max(0, exclusionRadiusRefHeatMap * exclusionRadiusRefHeatMap - (x - centerObstacleRefHeatMap.X) * (x - centerObstacleRefHeatMap.X))));

                                listPtsPerimetreExclusion.Add(new PointD(x, y));
                                listAbscissesZoneExclusionSuperieure[x] = y;
                            }
                        }

                        else
                        {
                            //On est dans le demi-plan supérieur, et on n'a que des périmètre supérieur
                            //Angle Max est forcément inférieur à -Pi/2
                            /// On commence par la portion linéaire du cone
                            for (int x = (int)ptAngleMin.X; x < nbCellInBaseHeatMapWidth; x++)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMin.Y + (x - ptAngleMin.X) * Math.Tan(minAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionInferieure[x] = (int)perimetrePt.Y;
                            }
                            for (int x = (int)ptAngleMax.X; x >= 0; x--)
                            {
                                PointD perimetrePt = new PointD(x, (int)ptAngleMax.Y + (x - ptAngleMax.X) * Math.Tan(maxAngle));
                                if (perimetrePt.Y > nbCellInBaseHeatMapHeight || perimetrePt.Y < 0)
                                    break;
                                listPtsPerimetreExclusion.Add(perimetrePt);
                                listAbscissesZoneExclusionInferieure[x] = (int)perimetrePt.Y;
                            }
                            /// On gère la partie circulaire
                            for (int x = (int)ptAngleMax.X; x <= (int)ptAngleMin.X; x++)
                            {
                                int y = (int)(centerObstacleRefHeatMap.Y - Math.Sqrt(Math.Max(0, exclusionRadiusRefHeatMap * exclusionRadiusRefHeatMap - (x - centerObstacleRefHeatMap.X) * (x - centerObstacleRefHeatMap.X))));

                                listPtsPerimetreExclusion.Add(new PointD(x, y));
                                listAbscissesZoneExclusionInferieure[x] = y;
                            }
                        }
                    }
                    
                    //On affiche les points périmètre exclus
                    foreach (var pt in listPtsPerimetreExclusion)
                    {
                        BaseHeatMapData[(int)pt.Y, (int)pt.X] = -1;
                    }
                                        
                    //var centerRefHeatMap = GetBaseHeatMapPosFromFieldCoordinates(avoidanceZone.center);
                    //var radiusRefHeatMap = GetBaseHeatMapDistanceFromFieldDistance(avoidanceZone.radius);

                    //double angleObstacle = Math.Atan2(obstacle.Y - robotLocation.Y, obstacle.X - robotLocation.X);
                    //double distanceObstacle = Toolbox.Distance(obstacle.X, obstacle.Y, robotLocation.X, robotLocation.Y);

                    //double distancePt = Toolbox.Distance(ptCourant.X, ptCourant.Y, robotLocation.X, robotLocation.Y);
                    //double anglePtCourant = Math.Atan2(ptCourant.Y - robotLocation.Y, ptCourant.X - robotLocation.X);

                    ////double distancePtObstacle = Toolbox.Distance(ptCourant.X, ptCourant.Y, obstacle.X, obstacle.Y);
                    ////if (distanceObstacle> 0.3 && distancePtObstacle < 0.2)

                    //anglePtCourant = Toolbox.ModuloByAngle(angleObstacle, anglePtCourant);

                    //double seuilDistance = 0;

                    //switch (obstacle.Type)
                    //{
                    //    case ObjectType.Obstacle:
                    //        seuilDistance = 0.18;
                    //        break;
                    //    case ObjectType.Robot:
                    //        seuilDistance = 0.4;
                    //        break;
                    //    default:
                    //        seuilDistance = 0.2;
                    //        break;
                    //}

                    //if (//distanceObstacle > 0.28 && //distance mùinimum pour considérer un objet, en dessous, on a probablment un morceau de notre robot
                    //    Math.Abs(distanceObstacle * (anglePtCourant - angleObstacle)) < seuilDistance && //Si on est dans le cone de masquage d'un objet
                    //     distancePt > distanceObstacle - seuilDistance) //Si on est dans le cone de masquage - condition 2
                    //    penalisation += 1;
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
