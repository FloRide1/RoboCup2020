using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Utilities;
using WorldMap;
using HeatMap;
using System.Diagnostics;
using PerceptionManagement;

namespace StrategyManager
{
    public class StrategyManager
    {
        string robotName = "";
        
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();

        bool AttackOnRight = true;
        
        PlayerRole robotRole = PlayerRole.Stop;
        double heatMapBaseCellSize = 0.01;

        public StrategyManager(string name)
        {
            robotName = name;
            heatMap = new Heatmap(22.0, 14.0, 22.0/Math.Pow(2,8), 2);

        }

        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
            ProcessStrategy();
        }

        Heatmap heatMap;
        Stopwatch sw = new Stopwatch();
        public void ProcessStrategy()
        {
            //sw.Start(); // début de la mesure
                        
            //Génération de la HeatMap
            heatMap.ReInitHeatMapData();
            int[] nbComputationsList = new int[heatMap.nbIterations];

            //On construit le heatMap en mode multi-résolution :
            //On commence par une heatmap très peu précise, puis on construit une heat map de taille réduite plus précise autour du point chaud,
            //Puis on construit une heatmap très précise au cm autour du point chaud.
            double optimizedAreaSize;

            PointD OptimalPosition = new PointD(0, 0);
            PointD OptimalPosInBaseHeatMapCoordinates = heatMap.GetBaseHeatMapPosFromFieldCoordinates(0, 0); 

            for (int n=0; n<heatMap.nbIterations; n++)
            {
                double subSamplingRate = heatMap.SubSamplingRateList[n];
                if(n>=1)
                    optimizedAreaSize = heatMap.nbCellInSubSampledHeatMapWidthList[n] / heatMap.nbCellInSubSampledHeatMapWidthList[n-1];
                else
                    optimizedAreaSize = heatMap.nbCellInSubSampledHeatMapWidthList[n];

                optimizedAreaSize /= 2;

                double minY = Math.Max(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate - optimizedAreaSize, 0);
                double maxY = Math.Min(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate + optimizedAreaSize, heatMap.nbCellInSubSampledHeatMapHeightList[n]);
                double minX = Math.Max(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate - optimizedAreaSize, 0);
                double maxX = Math.Min(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate + optimizedAreaSize, heatMap.nbCellInSubSampledHeatMapWidthList[n]);

                double max = double.NegativeInfinity;
                int maxXpos = 0;
                int maxYpos = 0;
                
                for (double y = minY; y < maxY; y += 1)
                {
                    for (double x = minX; x < maxX; x += 1)
                    {
                        //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
                        //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
                        var heatMapPos = heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y, n);
                        double value = EvaluateStrategyCostFunction(robotRole, heatMapPos);
                        //heatMap.SubSampledHeatMapData1[y, x] = value;
                        int yBase = (int)(y * subSamplingRate);
                        int xBase = (int)(x * subSamplingRate);
                        heatMap.BaseHeatMapData[yBase, xBase] = value;
                        nbComputationsList[n]++;

                        if(value> max)
                        {
                            max = value;
                            maxXpos = xBase;
                            maxYpos = yBase;
                        }

                        ////Code ci-dessous utile si on veut afficher la heatmap complete(video), mais consommateur en temps
                        //for (int i = 0; i < heatMap.SubSamplingRateList[n]; i += 1)
                        //{
                        //    for (int j = 0; j < heatMap.SubSamplingRateList[n]; j += 1)
                        //    {
                        //        if ((xBase + j < heatMap.nbCellInBaseHeatMapWidth) && (yBase + i < heatMap.nbCellInBaseHeatMapHeight))
                        //            heatMap.BaseHeatMapData[yBase + i, xBase + j] = value;
                        //    }
                        //}
                    }
                }
                //OptimalPosInBaseHeatMapCoordinates = heatMap.GetMaxPositionInBaseHeatMapCoordinates();
                OptimalPosInBaseHeatMapCoordinates = new PointD(maxXpos, maxYpos);
            }
            //OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();
            OptimalPosition = heatMap.GetFieldPosFromBaseHeatMapCoordinates(OptimalPosInBaseHeatMapCoordinates.X, OptimalPosInBaseHeatMapCoordinates.Y);


            OnHeatMap(robotName, heatMap);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));

            //heatMap.Dispose();
            //sw.Stop(); // Fin de la mesure
            //for (int n = 0; n < nbComputationsList.Length; n++)
            //{
            //    Console.WriteLine("Calcul Strategy - Nb Calculs Etape " + n + " : " + nbComputationsList[n]);
            //}
            //Console.WriteLine("Temps de calcul de la heatMap de stratégie : " + (sw.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond).ToString("N4")); // Affichage de la mesure
        }


        PointD theoreticalOptimalPosGardien = new PointD(-10.5, 0);
        PointD theoreticalOptimalPosDefenseurPlace = new PointD(-8, 3);
        PointD theoreticalOptimalPosDefenseurActif = new PointD(-8, -3);
        PointD theoreticalOptimalPosAttaquantPlace = new PointD(6, 3);
        PointD theoreticalOptimalPosAttaquantAvecBalle = new PointD(6, -3);

        double EvaluateStrategyCostFunction(PlayerRole role, PointD fieldPos)
        {
            //C'est ici qu'il faut calculer les fonctions de cout pour chacun des roles.
            switch (role)
            {
                case PlayerRole.Stop:
                    {
                        PointD theoreticalOptimalPos = new PointD(-8, 3);
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.Gardien:
                    {
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPosGardien, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.DefenseurPlace:
                    {
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPosDefenseurPlace, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.DefenseurActif:
                    {
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPosDefenseurActif, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.AttaquantPlace:
                    {
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPosAttaquantPlace, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.AttaquantAvecBalle:
                    {
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPosAttaquantAvecBalle, fieldPos) / 20.0);
                    }
                    break;
                default:
                    return 0;
            }
        }

        public void SetRole(PlayerRole role)
        {
            robotRole = role;
        }
                     
        public void SetDestination(Location location)
        {
            OnDestination(robotName, location);
        }

        public delegate void DestinationEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnDestinationEvent;
        public virtual void OnDestination(string name, Location location)
        {
            var handler = OnDestinationEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotName = name, Location = location });
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

    public enum PlayerRole
    {
        Stop,
        Gardien,
        DefenseurPlace,
        DefenseurActif,
        AttaquantAvecBalle,
        AttaquantPlace
    }

    
}
