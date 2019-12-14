using AdvancedTimers;
using EventArgsLibrary;
using HeatMap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Utilities;
using WorldMap;

namespace WayPointGenerator
{
    public class WaypointGenerator
    {
        string robotName;

        //Timer timerWayPointGeneration;

        Location destinationLocation;
        GlobalWorldMap globalWorldMap;
        //double[,] strategyManagerHeatMap = new double[0, 0];

        //double heatMapCellsize = 2; //doit être la même que celle du strategy manager
        //double fieldLength = 22;
        //double fieldHeight = 14;

        Heatmap StrategyHeatmap; 

        public WaypointGenerator(string name)
        {
            robotName = name;
            //timerWayPointGeneration = new Timer(100);
            //timerWayPointGeneration.Elapsed += TimerWayPointGeneration_Elapsed;
            //timerWayPointGeneration.Start();
        }

        public void SetNextWayPoint(Location waypointLocation)
        {
            OnWaypoint(robotName, waypointLocation);
        }

        public void OnDestinationReceived(object sender, EventArgsLibrary.LocationArgs e)
        {
            if (e.RobotName == robotName)
            {
                destinationLocation = e.Location;
            }
        }

        public void OnStrategyHeatMapReceived(object sender, EventArgsLibrary.HeatMapArgs e)
        {            
            if (robotName == e.RobotName)
            {
                StrategyHeatmap = e.HeatMap;
                CalculateOptimalWayPoint();
            }
        }
        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
        }

        private void CalculateOptimalWayPoint()
        {
            //Heatmap StrategyHeatmap = StrategyHeatmap.Copy();

            //Génération de la HeatMap
            //Heatmap heatMap = new Heatmap(22, 14, 0.01);
            Heatmap heatMap = StrategyHeatmap;// new Heatmap(22, 14, 0.01);
            PointD theoreticalOptimalPos = new PointD(-8, 3);

            //On construit le heatMap en mode multi-résolution :
            //On commence par une heatmap très peu précise, puis on construit une heat map de taille réduite plus précise autour du point chaud,
            //Puis on construit une heatmap très précise au cm autour du point chaud.
            //int nbComputations = 0;
            //for (int y = 0; y < heatMap.nbCellInSubSampledHeatMapHeight2; y += 1)
            //{
            //    for (int x = 0; x < heatMap.nbCellInSubSampledHeatMapWidth2; x += 1)
            //    {
            //        //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            //        //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
            //        double value = CalculPenalisation(heatMap.GetFieldPosFromSubSampledHeatMapCoordinates2(x, y));
            //        heatMap.SubSampledHeatMapData2[y, x] = value;
            //        int yBase = (int)(y * heatMap.SubSamplingRate2);
            //        int xBase = (int)(x * heatMap.SubSamplingRate2);
            //        heatMap.BaseHeatMapData[yBase, xBase] = value;
            //        nbComputations++;
            //        for (int i = 0; i < heatMap.SubSamplingRate2; i += 1)
            //        {
            //            for (int j = 0; j < heatMap.SubSamplingRate2; j += 1)
            //            {
            //                heatMap.BaseHeatMapData[yBase + i, xBase + j] -= value;
            //            }
            //        }
            //    }
            //}

            ////Console.WriteLine("Nombre d'opérations pour le calcul de la HeatMap sous échantillonnée de niveau 2 : " + nbComputations);

            //PointD OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();
            //PointD OptimalPosInBaseHeatMapCoordinates = heatMap.GetMaxPositionInBaseHeatMapCoordinates();

            //int optimizedAreaSize = (int)(heatMap.SubSamplingRate2 / heatMap.SubSamplingRate1);
            //nbComputations = 0;
            //for (int y = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y / heatMap.SubSamplingRate1 - optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapHeight1);
            //    y < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y / heatMap.SubSamplingRate1 + optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapHeight1); y += 1)
            //{
            //    for (int x = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X / heatMap.SubSamplingRate1 - optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapWidth1);
            //        x < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X / heatMap.SubSamplingRate1 + optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapWidth1); x += 1)
            //    {
            //        //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            //        //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
            //        double value = CalculPenalisation(heatMap.GetFieldPosFromSubSampledHeatMapCoordinates1(x, y));
            //        heatMap.SubSampledHeatMapData1[y, x] = value;
            //        int yBase = (int)(y * heatMap.SubSamplingRate1);
            //        int xBase = (int)(x * heatMap.SubSamplingRate1);
            //        heatMap.BaseHeatMapData[yBase, xBase] = value;
            //        nbComputations++;
            //        for (int i = 0; i < heatMap.SubSamplingRate1; i += 1)
            //        {
            //            for (int j = 0; j < heatMap.SubSamplingRate1; j += 1)
            //            {
            //                heatMap.BaseHeatMapData[yBase + i, xBase + j] = StrategyHeatmap.BaseHeatMapData[yBase + i, xBase + j] - value;
            //            }
            //        }
            //    }
            //}
            ////Console.WriteLine("Nombre d'opérations pour le calcul du raffinement de la HeatMap intermédiaire : " + nbComputations);

            //optimizedAreaSize = (int)(heatMap.SubSamplingRate1);
            //nbComputations = 0;
            //for (int y = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y - optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapHeight);
            //    y < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y + optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapHeight); y += 1)
            //{
            //    for (int x = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X - optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapWidth);
            //        x < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X + optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapWidth); x += 1)
            //    {
            //        //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            //        //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromBaseHeatMapCoordinates(x, y)) / 20.0);
            //        double value = CalculPenalisation(heatMap.GetFieldPosFromBaseHeatMapCoordinates(x, y));
            //        heatMap.BaseHeatMapData[y, x] = StrategyHeatmap.BaseHeatMapData[y, x] - value;
            //        nbComputations++;
            //    }
            //}
            ////Console.WriteLine("Nombre d'opérations pour le calcul du raffinement de la HeatMap final : " + nbComputations);

            var OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();

            OnHeatMap(robotName, heatMap);            
            SetNextWayPoint(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));
        }

        double CalculPenalisation(PointD ptCourant)
        {
            double penalisation = 0;
            if (globalWorldMap != null)
            {
                if (globalWorldMap.robotLocationDictionary.ContainsKey(robotName))
                {
                    Location robotLocation = globalWorldMap.robotLocationDictionary[robotName];
                    double angleDestination = Math.Atan2(destinationLocation.Y - robotLocation.Y, destinationLocation.X - robotLocation.X);

                    //On veut éviter de taper les autres robots
                    for (int i = 0; i < globalWorldMap.robotLocationDictionary.Count; i++)
                    {
                        string competitorName = globalWorldMap.robotLocationDictionary.Keys.ElementAt(i);
                        Location competitorLocation = globalWorldMap.robotLocationDictionary.Values.ElementAt(i);
                        //On itère sur tous les robots sauf celui-ci
                        if (competitorName != robotName)
                        {
                            double angleRobotAdverse = Math.Atan2(competitorLocation.Y - robotLocation.Y, competitorLocation.X - robotLocation.X);
                            double distanceRobotAdverse = Toolbox.Distance(competitorLocation.X, competitorLocation.Y, robotLocation.X, robotLocation.Y);


                            //PointD ptCourant = GetFieldPosFromHeatMapCoordinates(x, y);
                            double distancePt = Toolbox.Distance(ptCourant.X, ptCourant.Y, robotLocation.X, robotLocation.Y);
                            double anglePtCourant = Math.Atan2(ptCourant.Y - robotLocation.Y, ptCourant.X - robotLocation.X);

                            if (Math.Abs(distanceRobotAdverse * (anglePtCourant - angleRobotAdverse)) < 2.0 && distancePt > distanceRobotAdverse - 3)
                                penalisation += 1;// Math.Max(0, 1 - Math.Abs(anglePtCourant - angleRobotAdverse) *10.0);

                        }
                    }
                }
            }
            return penalisation;
        }


        //private PointD GetFieldPosFromHeatMapCoordinates(int x, int y)
        //{
        //    return new PointD(-fieldLength / 2 + x * heatMapCellsize, -fieldHeight / 2 + y * heatMapCellsize);
        //}

        public delegate void NewWayPointEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnWaypointEvent;
        public virtual void OnWaypoint(string name, Location wayPointlocation)
        {
            var handler = OnWaypointEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotName = name, Location = wayPointlocation});
            }
        }


        public delegate void HeatMapEventHandler(object sender, HeatMapArgs e);
        public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        public virtual void OnHeatMap(string name, Heatmap heatMap)
        {
            var handler = OnHeatMapEvent;
            if (handler != null)
            {
                handler(this, new HeatMapArgs { RobotName = name, HeatMap = heatMap });
            }
        }
    }
}
