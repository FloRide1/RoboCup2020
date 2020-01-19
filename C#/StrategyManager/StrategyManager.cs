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
        int robotId = 0;
        
        GlobalWorldMap globalWorldMap = new GlobalWorldMap();

        bool AttackOnRight = true;
        
        PlayerRole robotRole = PlayerRole.Stop;
        double heatMapBaseCellSize = 0.01;

        public StrategyManager(int id)
        {
            robotId = id;
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
            sw.Reset();
            sw.Start(); // début de la mesure
                        
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
                double maxY = Math.Min(OptimalPosInBaseHeatMapCoordinates.Y / subSamplingRate + optimizedAreaSize, (int)heatMap.nbCellInSubSampledHeatMapHeightList[n]);
                double minX = Math.Max(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate - optimizedAreaSize, 0);
                double maxX = Math.Min(OptimalPosInBaseHeatMapCoordinates.X / subSamplingRate + optimizedAreaSize, (int)heatMap.nbCellInSubSampledHeatMapWidthList[n]);

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


            OnHeatMap(robotId, heatMap);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));

            //heatMap.Dispose();
            sw.Stop(); // Fin de la mesure
            //for (int n = 0; n < nbComputationsList.Length; n++)
            //{
            //    Console.WriteLine("Calcul Strategy - Nb Calculs Etape " + n + " : " + nbComputationsList[n]);
            //}
            //Console.WriteLine("Temps de calcul de la heatMap de stratégie : " + sw.Elapsed.TotalMilliseconds.ToString("N4")+" ms"); // Affichage de la mesure
        }


        PointD theoreticalOptimalPosGardien = new PointD(-10.5, 0);
        PointD theoreticalOptimalPosDefenseurPlace = new PointD(-8, 3);
        PointD theoreticalOptimalPosDefenseurActif = new PointD(-8, -3);
        PointD theoreticalOptimalPosAttaquantPlace = new PointD(6, 3);
        PointD theoreticalOptimalPosAttaquantAvecBalle = new PointD(6, -3);
        PointD theoreticalOptimalPosCentre = new PointD(0, 0);

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
                        if (globalWorldMap.ballLocation != null)
                        {
                            var ptInterception = GetInterceptionLocation(new Location(globalWorldMap.ballLocation.X, globalWorldMap.ballLocation.Y, 0, globalWorldMap.ballLocation.Vx, globalWorldMap.ballLocation.Vy, 0), new Location(fieldPos.X, fieldPos.Y, 0, 0, 0, 0), 3);
                            //return Math.Max(0, 1 - Toolbox.Distance(new PointD(globalWorldMap.ballLocation.X, globalWorldMap.ballLocation.Y), fieldPos) / 20.0);
                            
                            if(ptInterception  != null) 
                                return Math.Max(0, 1 - Toolbox.Distance(ptInterception, fieldPos) / 20.0);
                            else
                                return Math.Max(0, 1 - Toolbox.Distance(new PointD(globalWorldMap.ballLocation.X, globalWorldMap.ballLocation.Y), fieldPos) / 20.0);
                        }
                        else
                            return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPosAttaquantAvecBalle, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.Centre:
                    {
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPosCentre, fieldPos) / 20.0);
                    }
                    break;
                default:
                    return 0;
            }
        }

        public PointD GetInterceptionLocation(Location target, Location hunter, double huntingSpeed)
        {
            //D'après Al-Kashi, si d est la distance entre le pt target et le pt chasseur, que les vitesses sont constantes 
            //et égales à Vtarget et Vhunter
            //Rappel Al Kashi : A² = B²+C²-2BCcos(alpha) , alpha angle opposé au segment A
            //On a au moment de l'interception à l'instant Tinter: 
            //A = Vh * Tinter
            //B = VT * Tinter
            //C = initialDistance;
            //alpha = Pi - capCible - angleCible

            double targetSpeed = Math.Sqrt(Math.Pow(target.Vx, 2) + Math.Pow(target.Vy, 2));
            double initialDistance = Toolbox.Distance(new PointD(hunter.X, hunter.Y), new PointD(target.X, target.Y));
            double capCible = Math.Atan2(target.Vy, target.Vx);
            double angleCible = Math.Atan2(target.Y- hunter.Y, target.X- hunter.X);
            double angleCapCibleDirectionCibleChasseur = Math.PI - capCible + angleCible;

            //Résolution de ax²+bx+c=0 pour trouver Tinter
            double a = Math.Pow(huntingSpeed, 2) - Math.Pow(targetSpeed, 2);
            double b = 2 * initialDistance * targetSpeed * Math.Cos(angleCapCibleDirectionCibleChasseur);
            double c = -Math.Pow(initialDistance, 2);

            double delta = b * b - 4 * a * c;
            double t1 = (-b - Math.Sqrt(delta)) / (2 * a);
            double t2 = (-b + Math.Sqrt(delta)) / (2 * a);

            if (delta > 0 && t2<10)
            {
                double xInterception = target.X + targetSpeed * Math.Cos(capCible) * t2;
                double yInterception = target.Y + targetSpeed * Math.Sin(capCible) * t2;
                return new PointD(xInterception, yInterception);
            }
            else
                return null;
        }
        public void SetRole(PlayerRole role)
        {
            robotRole = role;
        }
                     
        public void SetDestination(Location location)
        {
            OnDestination(robotId, location);
        }

        public delegate void DestinationEventHandler(object sender, LocationArgs e);
        public event EventHandler<LocationArgs> OnDestinationEvent;
        public virtual void OnDestination(int id, Location location)
        {
            var handler = OnDestinationEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = location });
            }
        }

        public delegate void HeatMapEventHandler(object sender, HeatMapArgs e);
        public event EventHandler<HeatMapArgs> OnHeatMapEvent;
        public virtual void OnHeatMap(int id, Heatmap heatMap)
        {
            var handler = OnHeatMapEvent;
            if (handler != null)
            {
                handler(this, new HeatMapArgs { RobotId = id, HeatMap = heatMap });
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
        AttaquantPlace,
        Centre,
    }

    
}

