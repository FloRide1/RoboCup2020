using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Utilities;
using WorldMap;

namespace StrategyManager
{
    public class StrategyManager
    {
        string robotName = "";

        double xDestination = 0;
        double yDestination = 0;
        double thetaDestination = 0;
        double vxDestination = 0;
        double vyDestination = 0;
        double vthetaDestination = 0;

        bool AttackOnRight = true;

        double heatMapCellsize = 0.1;
        double fieldLength = 22;
        double fieldHeight = 14;

        Point pt;

        PlayerRole robotRole = PlayerRole.Stop;

        public StrategyManager(string name)
        {
            robotName = name;
        }

        public void ProcessStrategy()
        {
            switch(robotRole)
            {
                case PlayerRole.Stop:
                    break;
                case PlayerRole.Gardien:
                    ProcessStrategieGardien();
                    break;
                case PlayerRole.DefenseurPlace:
                    ProcessStrategieDefenseurPlace();
                    break;
                case PlayerRole.DefenseurActif:
                    ProcessStrategieDefenseurActif();
                    break;
                case PlayerRole.AttaquantPlace:
                    ProcessStrategieAttaquantPlace();
                    break;
                case PlayerRole.AttaquantAvecBalle:
                    ProcessStrategieAttaquantAvecBalle();
                    break;
            }
        }

        public void SetRole(PlayerRole role)
        {
            robotRole = role;
        }

        private void ProcessStrategieGardien()
        {
            //Génération de la HeatMap
            int nbCellInHeatMapHeight = (int)(fieldHeight / heatMapCellsize);
            int nbCellInHeatMapWidth = (int)(fieldLength / heatMapCellsize);
            var data = new double[nbCellInHeatMapHeight, nbCellInHeatMapWidth];

            //On calcule les valeurs de la HeatMap en chacun des points
            double max = 0;
            int maxPosX = 0;
            int maxPosY = 0;

            //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            for (int y = 0; y < nbCellInHeatMapHeight; y++)
                for (int x = 0; x < nbCellInHeatMapWidth; x++)
                {
                    if (AttackOnRight)
                    {
                        //Prise en compte de la position théorique du gardien au centre des buts
                        data[y, x] = 1 / (1 + Toolbox.Distance(new PointD(-10.5, 0), GetFieldPosFromHeatMapCoordinates(x, y)));
                        if (data[y, x] > max)
                        {
                            max = data[y, x];
                            maxPosX = x;
                            maxPosY = y;
                        }
                    }
                }
            PointD OptimalPosition = GetFieldPosFromHeatMapCoordinates(maxPosX, maxPosY);
            OnHeatMap(robotName, data);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));
        }

        Random rand = new Random();

        private void ProcessStrategieDefenseurPlace()
        {
            //Génération de la HeatMap
            int nbCellInHeatMapHeight = (int)(fieldHeight / heatMapCellsize);
            int nbCellInHeatMapWidth = (int)(fieldLength / heatMapCellsize);
            var data = new double[nbCellInHeatMapHeight, nbCellInHeatMapWidth];

            //On calcule les valeurs de la HeatMap en chacun des points
            double max = 0;
            int maxPosX = 0;
            int maxPosY = 0;


            double xBestLocation = -8 + (rand.NextDouble()-0.5) * 2;
            double yBestLocation = 3 + (rand.NextDouble() - 0.5) * 2;

            //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            for (int y = 0; y < nbCellInHeatMapHeight; y++)
                for (int x = 0; x < nbCellInHeatMapWidth; x++)
                {
                    if (AttackOnRight)
                    {
                        //Prise en compte de la position théorique du gardien au centre des buts
                        data[y, x] = 1 / (1 + Toolbox.Distance(new PointD(xBestLocation, yBestLocation), GetFieldPosFromHeatMapCoordinates(x, y)));
                        if (data[y, x] > max)
                        {
                            max = data[y, x];
                            maxPosX = x;
                            maxPosY = y;
                        }
                    }
                }
            PointD OptimalPosition = GetFieldPosFromHeatMapCoordinates(maxPosX, maxPosY);
            OnHeatMap(robotName, data);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));
        }

        private void ProcessStrategieDefenseurActif()
        {
            //Génération de la HeatMap
            int nbCellInHeatMapHeight = (int)(fieldHeight / heatMapCellsize);
            int nbCellInHeatMapWidth = (int)(fieldLength / heatMapCellsize);
            var data = new double[nbCellInHeatMapHeight, nbCellInHeatMapWidth];

            //On calcule les valeurs de la HeatMap en chacun des points
            double max = 0;
            int maxPosX = 0;
            int maxPosY = 0;


            double xBestLocation = -8 + (rand.NextDouble() - 0.5) * 2;
            double yBestLocation = -3 + (rand.NextDouble() - 0.5) * 2;

            //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            for (int y = 0; y < nbCellInHeatMapHeight; y++)
                for (int x = 0; x < nbCellInHeatMapWidth; x++)
                {
                    if (AttackOnRight)
                    {
                        //Prise en compte de la position théorique du gardien au centre des buts
                        data[y, x] = 1 / (1 + Toolbox.Distance(new PointD(xBestLocation, yBestLocation), GetFieldPosFromHeatMapCoordinates(x, y)));
                        if (data[y, x] > max)
                        {
                            max = data[y, x];
                            maxPosX = x;
                            maxPosY = y;
                        }
                    }
                }
            PointD OptimalPosition = GetFieldPosFromHeatMapCoordinates(maxPosX, maxPosY);
            OnHeatMap(robotName, data);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));
        }

        private void ProcessStrategieAttaquantAvecBalle()
        {
            //Génération de la HeatMap
            int nbCellInHeatMapHeight = (int)(fieldHeight / heatMapCellsize);
            int nbCellInHeatMapWidth = (int)(fieldLength / heatMapCellsize);
            var data = new double[nbCellInHeatMapHeight, nbCellInHeatMapWidth];

            //On calcule les valeurs de la HeatMap en chacun des points
            double max = 0;
            int maxPosX = 0;
            int maxPosY = 0;


            double xBestLocation = 3 + (rand.NextDouble() - 0.5) * 1;
            double yBestLocation = 0 + (rand.NextDouble() - 0.5) * 5;

            //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            for (int y = 0; y < nbCellInHeatMapHeight; y++)
                for (int x = 0; x < nbCellInHeatMapWidth; x++)
                {
                    if (AttackOnRight)
                    {
                        //Prise en compte de la position théorique du gardien au centre des buts
                        data[y, x] = 1 / (1 + Toolbox.Distance(new PointD(xBestLocation, yBestLocation), GetFieldPosFromHeatMapCoordinates(x, y)));
                        if (data[y, x] > max)
                        {
                            max = data[y, x];
                            maxPosX = x;
                            maxPosY = y;
                        }
                    }
                }
            PointD OptimalPosition = GetFieldPosFromHeatMapCoordinates(maxPosX, maxPosY);
            OnHeatMap(robotName, data);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));
        }
        private void ProcessStrategieAttaquantPlace()
        {
            //Génération de la HeatMap
            int nbCellInHeatMapHeight = (int)(fieldHeight / heatMapCellsize);
            int nbCellInHeatMapWidth = (int)(fieldLength / heatMapCellsize);
            var data = new double[nbCellInHeatMapHeight, nbCellInHeatMapWidth];

            //On calcule les valeurs de la HeatMap en chacun des points
            double max = 0;
            int maxPosX = 0;
            int maxPosY = 0;
            
            double xBestLocation = 6 + (rand.NextDouble() - 0.5) * 1;
            double yBestLocation = 0 + (rand.NextDouble() - 0.5) * 5;

            //Attention, le remplissage de la HeatMap se fait avec une inversion des coordonnées
            for (int y = 0; y < nbCellInHeatMapHeight; y++)
                for (int x = 0; x < nbCellInHeatMapWidth; x++)
                {
                    if (AttackOnRight)
                    {
                        //Prise en compte de la position théorique du gardien au centre des buts
                        data[y, x] = 1 / (1 + Toolbox.Distance(new PointD(xBestLocation, yBestLocation), GetFieldPosFromHeatMapCoordinates(x, y)));
                        if (data[y, x] > max)
                        {
                            max = data[y, x];
                            maxPosX = x;
                            maxPosY = y;
                        }
                    }
                }
            PointD OptimalPosition = GetFieldPosFromHeatMapCoordinates(maxPosX, maxPosY);
            OnHeatMap(robotName, data);
            SetDestination(new Location((float)OptimalPosition.X, (float)OptimalPosition.Y, 0, 0, 0, 0));
        }



        private PointD GetFieldPosFromHeatMapCoordinates(int x, int y)
        {
            return new PointD(-fieldLength / 2 + x * heatMapCellsize, -fieldHeight / 2 + y * heatMapCellsize);
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
        public virtual void OnHeatMap(string name, double[,] heatMap)
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
