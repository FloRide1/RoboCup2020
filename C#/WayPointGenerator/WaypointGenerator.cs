using Constants;
using EventArgsLibrary;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utilities;
using WorldMap;

namespace WayPointGenerator
{
    public class WaypointGenerator
    {
        int robotId;

        //Timer timerWayPointGeneration;

        Location destinationLocation;
        GlobalWorldMap globalWorldMap;
        //double[,] strategyManagerHeatMap = new double[0, 0];

        //double heatMapCellsize = 2; //doit être la même que celle du strategy manager
        //double fieldLength = 22;
        //double fieldHeight = 14;

        Heatmap StrategyHeatmap; 

        public WaypointGenerator(int id, GameMode competition)
        {
            robotId = id;
            switch(competition)
            {
                case GameMode.RoboCup:
                    waypointHeatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 8));
                    break;
                case GameMode.Eurobot:
                    waypointHeatMap = new Heatmap(3.0, 2.0, (int)Math.Pow(2, 5));
                    break;
                case GameMode.Demo:
                    waypointHeatMap = new Heatmap(3.0, 2.0, (int)Math.Pow(2, 5));
                    break;
                default:
                    waypointHeatMap = new Heatmap(22.0, 14.0, (int)Math.Pow(2, 8));
                    break;
            }
        }

        public void SetNextWayPoint(Location waypointLocation)
        {
            OnWaypoint(robotId, waypointLocation);
        }

        public void OnDestinationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (e.RobotId == robotId)
            {
                destinationLocation = e.Location;
            }
        }

        public void OnStrategyHeatMapReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {            
            if (robotId == e.RobotId)
            {
                StrategyHeatmap = e.HeatMap;
                CalculateOptimalWayPoint();
            }
        }
        
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
        }

        Stopwatch sw = new Stopwatch();
        Heatmap waypointHeatMap;
        private void CalculateOptimalWayPoint()
        {
            //Heatmap StrategyHeatmap = StrategyHeatmap.Copy();

            //Génération de la HeatMap
            //Heatmap heatMap = new Heatmap(22, 14, 0.01);    
            sw.Reset();
            sw.Start(); // début de la mesure

            ////Génération de la HeatMap
            //waypointHeatMap.InitHeatMapData();
            //int[] nbComputationsList = new int[waypointHeatMap.nbIterations];

            ////On construit le heatMap en mode multi-résolution :
            ////On commence par une heatmap très peu précise, puis on construit une heat map de taille réduite plus précise autour du point chaud,
            ////Puis on construit une heatmap très précise au cm autour du point chaud.
            //double optimizedAreaSize;

            //PointD OptimalPosition = new PointD(0, 0);
            //PointD OptimalPosInBaseHeatMapCoordinates = waypointHeatMap.GetBaseHeatMapPosFromFieldCoordinates(0, 0);

            
            //double subSamplingRate = waypointHeatMap.SubSamplingRateList[n];
            //    if (n >= 1)
            //        optimizedAreaSize = waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n] / waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n - 1];
            //    else
            //        optimizedAreaSize = waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n];

            //    optimizedAreaSize /= 2;

            //    double minY = Math.Max(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate - optimizedAreaSize, 0);
            //    double maxY = Math.Min(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate + optimizedAreaSize, Math.Min(waypointHeatMap.nbCellInSubSampledHeatMapHeightList[n], waypointHeatMap.nbCellInBaseHeatMapHeight));
            //    double minX = Math.Max(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate - optimizedAreaSize, 0);
            //    double maxX = Math.Min(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate + optimizedAreaSize, Math.Min(waypointHeatMap.nbCellInSubSampledHeatMapWidthList[n], waypointHeatMap.nbCellInBaseHeatMapWidth));

            //    double max = double.NegativeInfinity;
            //    int maxXpos = 0;
            //    int maxYpos = 0;

            //    //Parallel.For((int)minY, (int)maxY + 1, (y) =>
            //    for (double y = (int)minY; y < (int)maxY + 1; y += 1)
            //    {
            //        //Parallel.For((int)minX, (int)maxX+1, (x) =>
            //        for (double x = (int)minX; x < (int)maxX + 1; x += 1)
            //        {
            //            //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            //            //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
            //            var heatMapPos = waypointHeatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y, n);
            //            double pen = CalculPenalisation(heatMapPos);
            //            //double value = EvaluateStrategyCostFunction(robotRole, heatMapPos);
            //            //heatMap.SubSampledHeatMapData1[y, x] = value;
            //            int yBase = (int)(y * subSamplingRate);
            //            int xBase = (int)(x * subSamplingRate);
            //            double value = StrategyHeatmap.BaseHeatMapData[yBase, xBase] - pen;
            //            waypointHeatMap.BaseHeatMapData[yBase, xBase] = value;
            //            nbComputationsList[n]++;

            //            if (value > max)
            //            {
            //                max = value;
            //                maxXpos = xBase;
            //                maxYpos = yBase;
            //            }

            //            ////Code ci-dessous utile si on veut afficher la heatmap complete(video), mais consommateur en temps
            //            //for (int i = 0; i < waypointHeatMap.SubSamplingRateList[n]; i += 1)
            //            //{
            //            //    for (int j = 0; j < waypointHeatMap.SubSamplingRateList[n]; j += 1)
            //            //    {
            //            //        if ((xBase + j < waypointHeatMap.nbCellInBaseHeatMapWidth) && (yBase + i < waypointHeatMap.nbCellInBaseHeatMapHeight))
            //            //            waypointHeatMap.BaseHeatMapData[yBase + i, xBase + j] = value;
            //            //    }
            //            //}
            //        }
            //    }
            //        //});
            //    //});
            //    //OptimalPosInBaseHeatMapCoordinates = heatMap.GetMaxPositionInBaseHeatMapCoordinates();
            //    OptimalPosInBaseHeatMapCoordinates = new PointD(maxXpos, maxYpos);
            //}

            ////var OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();
            //OptimalPosition = waypointHeatMap.GetFieldPosFromBaseHeatMapCoordinates(OptimalPosInBaseHeatMapCoordinates.X, OptimalPosInBaseHeatMapCoordinates.Y);

            ////Si la position optimale est très de la cible théorique, on prend la cible théorique
            //double seuilPositionnementFinal = 0.1;
            //if (destinationLocation != null && OptimalPosition != null)
            //{
            //    if (Toolbox.Distance(new PointD(destinationLocation.X, destinationLocation.Y), new PointD(OptimalPosition.X, OptimalPosition.Y)) < seuilPositionnementFinal)
            //    {
            //        OptimalPosition = new PointD(destinationLocation.X, destinationLocation.Y);
            //    }
            //}

            //OnHeatMap(robotId, waypointHeatMap);
            //if (OptimalPosition != null && destinationLocation != null)
            //    SetNextWayPoint(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, (float)destinationLocation.Theta, 0, 0, 0));

            sw.Stop(); // Fin de la mesure
            //for (int n = 0; n < nbComputationsList.Length; n++)
            //{
            //    Console.WriteLine("Calcul WayPoint - Nb Calculs Etape " + n + " : " + nbComputationsList[n]);
            //}
            //Console.WriteLine("Temps de calcul de la heatMap WayPoint : " + sw.Elapsed.TotalMilliseconds.ToString("N4")+" ms"); // Affichage de la mesure
        }


        public delegate void NewWayPointEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnWaypointEvent;
        public virtual void OnWaypoint(int id, Location wayPointlocation)
        {
            var handler = OnWaypointEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = wayPointlocation});
            }
        }
    }
}
