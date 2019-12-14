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

        }

        public void OnGlobalWorldMapReceived(object sender, GlobalWorldMapArgs e)
        {
            globalWorldMap = e.GlobalWorldMap;
            ProcessStrategy();
        }

        public void ProcessStrategy()
        {
            //Génération de la HeatMap
            Heatmap heatMap = new Heatmap(22, 14, 0.01);
            PointD theoreticalOptimalPos = new PointD(-8, 3);

            //On construit le heatMap en mode multi-résolution :
            //On commence par une heatmap très peu précise, puis on construit une heat map de taille réduite plus précise autour du point chaud,
            //Puis on construit une heatmap très précise au cm autour du point chaud.
            int nbComputations = 0;
            for (int y = 0; y < heatMap.nbCellInSubSampledHeatMapHeight2; y += 1)
            {
                for (int x = 0; x < heatMap.nbCellInSubSampledHeatMapWidth2; x += 1)
                {
                    //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
                    //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
                    double value = EvaluateStrategyCostFunction(robotRole, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates2(x, y));
                    heatMap.SubSampledHeatMapData2[y, x] = value;
                    int yBase = (int)(y * heatMap.SubSamplingRate2);
                    int xBase = (int)(x * heatMap.SubSamplingRate2);
                    heatMap.BaseHeatMapData[yBase, xBase] = value;
                    nbComputations++;
                    //for (int i = 0; i < heatMap.SubSamplingRate2; i += 1)
                    //{
                    //    for (int j = 0; j < heatMap.SubSamplingRate2; j += 1)
                    //    {
                    //        heatMap.BaseHeatMapData[yBase + i, xBase + j] = value;
                    //    }
                    //}
                }
            }

            Console.WriteLine("Nombre d'opérations pour le calcul de la HeatMap sous échantillonnée de niveau 2 : " + nbComputations);

            PointD OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();
            PointD OptimalPosInBaseHeatMapCoordinates = heatMap.GetMaxPositionInBaseHeatMapCoordinates();

            int optimizedAreaSize = (int)(heatMap.SubSamplingRate2/ heatMap.SubSamplingRate1);
            nbComputations = 0;
            for (int y = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y / heatMap.SubSamplingRate1 - optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapHeight1);
                y < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y / heatMap.SubSamplingRate1 + optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapHeight1); y += 1)
            {
                for (int x = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X / heatMap.SubSamplingRate1 - optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapWidth1);
                    x < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X / heatMap.SubSamplingRate1 + optimizedAreaSize, 0, heatMap.nbCellInSubSampledHeatMapWidth1); x += 1)
                {
                    //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
                    //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates(x, y)) / 20.0);
                    double value = EvaluateStrategyCostFunction(robotRole, heatMap.GetFieldPosFromSubSampledHeatMapCoordinates1(x, y));
                    heatMap.SubSampledHeatMapData1[y, x] = value;
                    int yBase = (int)(y * heatMap.SubSamplingRate1);
                    int xBase = (int)(x * heatMap.SubSamplingRate1);
                    heatMap.BaseHeatMapData[yBase, xBase] = value;
                    nbComputations++;
                    for (int i = 0; i < heatMap.SubSamplingRate1; i += 1)
                    {
                        for (int j = 0; j < heatMap.SubSamplingRate1; j += 1)
                        {
                            heatMap.BaseHeatMapData[yBase + i, xBase + j] = value;
                        }
                    }
                }
            }
            Console.WriteLine("Nombre d'opérations pour le calcul du raffinement de la HeatMap intermédiaire : " + nbComputations);

            optimizedAreaSize = (int)(heatMap.SubSamplingRate1);
            nbComputations = 0;
            for (int y = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y - optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapHeight); 
                y < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.Y + optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapHeight); y += 1)
            {
                for (int x = (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X - optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapWidth); 
                    x < (int)Toolbox.LimitToInterval(OptimalPosInBaseHeatMapCoordinates.X + optimizedAreaSize, 0, heatMap.nbCellInBaseHeatMapWidth) ; x += 1)
                {
                    //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
                    //double value = Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, heatMap.GetFieldPosFromBaseHeatMapCoordinates(x, y)) / 20.0);
                    double value = EvaluateStrategyCostFunction(robotRole, heatMap.GetFieldPosFromBaseHeatMapCoordinates(x, y));
                    heatMap.BaseHeatMapData[y, x] = value;
                    nbComputations++;
                }
            }
            Console.WriteLine("Nombre d'opérations pour le calcul du raffinement de la HeatMap final : " + nbComputations);

            OptimalPosition = heatMap.GetMaxPositionInBaseHeatMap();

            OnHeatMap(robotName, heatMap);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));
            
            
        }

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
                        PointD theoreticalOptimalPos = new PointD(-10.5, 0);
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.DefenseurPlace:
                    {
                        PointD theoreticalOptimalPos = new PointD(-8, 3);
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.DefenseurActif:
                    {
                        PointD theoreticalOptimalPos = new PointD(-8, -3);
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.AttaquantPlace:
                    {
                        PointD theoreticalOptimalPos = new PointD(6, 3);
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, fieldPos) / 20.0);
                    }
                    break;
                case PlayerRole.AttaquantAvecBalle:
                    {
                        PointD theoreticalOptimalPos = new PointD(6, -3);
                        return Math.Max(0, 1 - Toolbox.Distance(theoreticalOptimalPos, fieldPos) / 20.0);
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
