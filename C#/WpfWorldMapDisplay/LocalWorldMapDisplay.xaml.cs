
using Constants;
using PerceptionManagement;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Drawing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Utilities;
using WorldMap;

namespace WpfWorldMapDisplay
{

    public enum LocalWorldMapDisplayType
    {
        StrategyMap,
        WayPointMap,
    }

    /// <summary>
    /// Logique d'interaction pour ExtendedHeatMap.xaml
    /// </summary>    /// 
    public partial class LocalWorldMapDisplay : UserControl
    {
        LocalWorldMapDisplayType lwmdType = LocalWorldMapDisplayType.StrategyMap; //Par défaut

        Random random = new Random();

        public bool IsExtended = false;

        double LengthGameArea = 0;
        double WidthGameArea = 0;
        double LengthDisplayArea = 0;
        double WidthDisplayArea = 0;
        
        //Liste des robots à afficher
        Dictionary<int, RobotDisplay> TeamMatesDisplayDictionary = new Dictionary<int, RobotDisplay>();
        Dictionary<int, RobotDisplay> OpponentDisplayDictionary = new Dictionary<int, RobotDisplay>();
        List<PolygonExtended> ObjectDisplayList = new List<PolygonExtended>();

        //Liste des balles vues par le robot à afficher
        List<BallDisplay> BallDisplayList = new List<BallDisplay>();

        string typeTerrain = "RoboCup";
        

        public LocalWorldMapDisplay()
        {
            InitializeComponent();
        }

        public void Init(string competition, LocalWorldMapDisplayType type)
        {
            lwmdType = type;
            if (lwmdType == LocalWorldMapDisplayType.StrategyMap)
                LocalWorldMapTitle.Content = "Strategy Local World Map";
            if (lwmdType == LocalWorldMapDisplayType.WayPointMap)
                LocalWorldMapTitle.Content = "Waypoint Local World Map";

            switch (competition)
            {
                case "Cachan":
                    LengthDisplayArea = 10;
                    WidthDisplayArea = 6;
                    LengthGameArea = 8;
                    WidthGameArea = 4;
                    InitCachanField();
                    break;
                case "Eurobot":
                    LengthDisplayArea = 3.4;
                    WidthDisplayArea = 2.4;
                    LengthGameArea = 3.0;
                    WidthGameArea = 2.0;
                    InitEurobotField();
                    break;
                case "RoboCup":
                    LengthDisplayArea = 30;
                    WidthDisplayArea = 18;
                    LengthGameArea = 22;
                    WidthGameArea = 14;
                    InitSoccerField();
                    break;
                default:
                    LengthDisplayArea = 30;
                    WidthDisplayArea = 18;
                    LengthGameArea = 22;
                    WidthGameArea = 14;
                    InitSoccerField();
                    break;
            }

            this.sciChart.XAxis.VisibleRange.SetMinMax(-LengthDisplayArea / 2, LengthDisplayArea / 2);
            this.sciChart.YAxis.VisibleRange.SetMinMax(-WidthDisplayArea / 2, WidthDisplayArea / 2);
        }

        public void InitTeamMate(int robotId, string competition)
        {
            switch (competition)
            {
                case "Cachan":
                    PolygonExtended robotShape = new PolygonExtended();
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.25, -0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.25, -0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.2, 0));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.25, 0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.25, 0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.25, -0.25));
                    robotShape.borderColor = System.Drawing.Color.Blue;
                    robotShape.backgroundColor = System.Drawing.Color.Red;
                    RobotDisplay rd = new RobotDisplay(robotShape);
                    rd.SetLocation(new Location(0, 0, 0, 0, 0, 0));
                    TeamMatesDisplayDictionary.Add(robotId, rd);
                    break;
                case "Eurobot":
                    robotShape = new PolygonExtended();
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.12, -0.12));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.12, -0.12));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.02, 0));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.12, 0.12));
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.12, 0.12));
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.12, -0.12));
                    robotShape.borderColor = System.Drawing.Color.Blue;
                    robotShape.backgroundColor = System.Drawing.Color.DarkRed;
                    rd = new RobotDisplay(robotShape);
                    rd.SetLocation(new Location(0, 0, 0, 0, 0, 0));
                    TeamMatesDisplayDictionary.Add(robotId, rd);
                    break;
                default:
                    robotShape = new PolygonExtended();
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.25, -0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.25, -0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.2, 0));
                    robotShape.polygon.Points.Add(new System.Windows.Point(0.25, 0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.25, 0.25));
                    robotShape.polygon.Points.Add(new System.Windows.Point(-0.25, -0.25));
                    robotShape.borderColor = System.Drawing.Color.Blue;
                    robotShape.backgroundColor = System.Drawing.Color.Red;
                    rd = new RobotDisplay(robotShape);
                    rd.SetLocation(new Location(0, 0, 0, 0, 0, 0));
                    TeamMatesDisplayDictionary.Add(robotId, rd);
                    break;
                    }
        }

        public void UpdateWorldMapDisplay()
        {
            DrawBalls();
            DrawTeam();
            //DrawLidar();
            if (TeamMatesDisplayDictionary.Count == 1) //Cas d'un affichage de robot unique (localWorldMap)
                DrawHeatMap(TeamMatesDisplayDictionary.First().Key);
            PolygonSeries.RedrawAll();
            ObjectsPolygonSeries.RedrawAll();
            BallPolygon.RedrawAll();
        }

        public void UpdateLocalWorldMap(LocalWorldMap localWorldMap)
        {
            int robotId = localWorldMap.RobotId;
            UpdateRobotLocation(robotId, localWorldMap.robotLocation);
            UpdateRobotGhostLocation(robotId, localWorldMap.robotGhostLocation);
            UpdateRobotDestination(robotId, localWorldMap.destinationLocation);
            UpdateRobotWaypoint(robotId, localWorldMap.waypointLocation);
            if (lwmdType == LocalWorldMapDisplayType.StrategyMap)
            {
                if (localWorldMap.heatMapStrategy != null)
                    UpdateHeatMap(robotId, localWorldMap.heatMapStrategy.BaseHeatMapData);
            }
            else if (lwmdType == LocalWorldMapDisplayType.WayPointMap)
            {
                if (localWorldMap.heatMapWaypoint != null)
                    UpdateHeatMap(robotId, localWorldMap.heatMapWaypoint.BaseHeatMapData);
            }
            UpdateLidarMap(robotId, localWorldMap.lidarMap);
            UpdateLidarObjects(robotId, localWorldMap.lidarObjectList);
            UpdateBallLocationList(localWorldMap.ballLocationList);
        }
        
        private void DrawHeatMap(int robotId)
        {
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                UniformHeatmapDataSeries<double, double, double> heatmapDataSeries = null;
                if (lwmdType == LocalWorldMapDisplayType.StrategyMap)
                {
                    if (TeamMatesDisplayDictionary[robotId].heatMapStrategy == null)
                        return;
                    //heatmapSeries.DataSeries = new UniformHeatmapDataSeries<double, double, double>(data, startX, stepX, startY, stepY);
                    double xStep = (LengthGameArea) / (TeamMatesDisplayDictionary[robotId].heatMapStrategy.GetUpperBound(1));
                    double yStep = (WidthGameArea) / (TeamMatesDisplayDictionary[robotId].heatMapStrategy.GetUpperBound(0));
                    heatmapDataSeries = new UniformHeatmapDataSeries<double, double, double>(TeamMatesDisplayDictionary[robotId].heatMapStrategy, -LengthGameArea / 2 - xStep / 2, xStep, -WidthGameArea / 2 - yStep / 2, yStep);
                }
                else
                {
                    if (TeamMatesDisplayDictionary[robotId].heatMapWaypoint == null)
                        return;
                    //heatmapSeries.DataSeries = new UniformHeatmapDataSeries<double, double, double>(data, startX, stepX, startY, stepY);
                    double xStep = (LengthGameArea) / (TeamMatesDisplayDictionary[robotId].heatMapWaypoint.GetUpperBound(1));
                    double yStep = (WidthGameArea) / (TeamMatesDisplayDictionary[robotId].heatMapWaypoint.GetUpperBound(0));
                    heatmapDataSeries = new UniformHeatmapDataSeries<double, double, double>(TeamMatesDisplayDictionary[robotId].heatMapWaypoint, -LengthGameArea / 2 - xStep / 2, xStep, -WidthGameArea / 2 - yStep / 2, yStep);
                }

                // Apply the dataseries to the heatmap
                if (heatmapDataSeries != null)
                {
                    heatmapSeries.DataSeries = heatmapDataSeries;
                    heatmapDataSeries.InvalidateParentSurface(RangeMode.None);
                }
            }
        }

        public void DrawBalls()
        {
            lock (BallDisplayList)
            {
                int indexBall = 0;
                foreach (var ball in BallDisplayList)
                {
                    //Affichage de la balle
                    BallPolygon.AddOrUpdatePolygonExtended((int)BallId.Ball + indexBall, ball.GetBallPolygon());
                    BallPolygon.AddOrUpdatePolygonExtended((int)BallId.Ball + indexBall + (int)Caracteristique.Speed, ball.GetBallSpeedArrow());
                    indexBall++;
                }
            }
        }

        public void DrawTeam()
        {
            XyDataSeries<double, double> lidarPts = new XyDataSeries<double, double>();
            ObjectsPolygonSeries.Clear();

            foreach (var r in TeamMatesDisplayDictionary)
            {
                //Affichage des robots
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Ghost, TeamMatesDisplayDictionary[r.Key].GetRobotGhostPolygon());
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Speed, TeamMatesDisplayDictionary[r.Key].GetRobotSpeedArrow());
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Destination, TeamMatesDisplayDictionary[r.Key].GetRobotDestinationArrow());
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.WayPoint, TeamMatesDisplayDictionary[r.Key].GetRobotWaypointArrow());
                //On trace le robot en dernier pour l'avoir en couche de dessus
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key, TeamMatesDisplayDictionary[r.Key].GetRobotPolygon());

                //Rendering des points Lidar
                lidarPts.AcceptsUnsortedData = true;
                var lidarData = TeamMatesDisplayDictionary[r.Key].GetRobotLidarPoints();
                lidarPts.Append(lidarData.XValues, lidarData.YValues);

                //Rendering des objets Lidar
                foreach (var polygonObject in TeamMatesDisplayDictionary[r.Key].GetRobotLidarObjects())
                    ObjectsPolygonSeries.AddOrUpdatePolygonExtended(ObjectsPolygonSeries.Count(), polygonObject);
            }
            
            foreach (var r in OpponentDisplayDictionary)
            {
                //Affichage des robots
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key, OpponentDisplayDictionary[r.Key].GetRobotPolygon());
                //PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Speed, OpponentDisplayDictionary[r.Key].GetRobotSpeedArrow());
                //PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Destination, TeamMatesDictionary[r.Key].GetRobotDestinationArrow());
                //PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.WayPoint, TeamMatesDictionary[r.Key].GetRobotWaypointArrow());
            }
            //Affichage des points lidar
            LidarPoints.DataSeries = lidarPts;
        }

        public void DrawLidar()
        {
            XyDataSeries<double, double> lidarPts = new XyDataSeries<double, double>();
            foreach (var r in TeamMatesDisplayDictionary)
            {
                ////Affichage des robots
                //PolygonSeries.AddOrUpdatePolygonExtended(r.Key, TeamMatesDisplayDictionary[r.Key].GetRobotPolygon());
                //PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Speed, TeamMatesDisplayDictionary[r.Key].GetRobotSpeedArrow());
                //PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Destination, TeamMatesDisplayDictionary[r.Key].GetRobotDestinationArrow());
                //PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.WayPoint, TeamMatesDisplayDictionary[r.Key].GetRobotWaypointArrow());

                //Rendering des points Lidar
                lidarPts.AcceptsUnsortedData = true;
                var lidarData = TeamMatesDisplayDictionary[r.Key].GetRobotLidarPoints();
                lidarPts.Append(lidarData.XValues, lidarData.YValues);
                LidarPoints.DataSeries = lidarPts;
            }
        }

        private void UpdateRobotLocation(int robotId, Location location)
        {
            if (location == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                TeamMatesDisplayDictionary[robotId].SetLocation(location);
                //TeamMatesDisplayDictionary[robotId].SetPosition(location.X, location.Y, location.Theta);
                //TeamMatesDisplayDictionary[robotId].SetSpeed(location.Vx, location.Vy, location.Vtheta);
            }
            else
            {
                Console.WriteLine("UpdateRobotLocation : Robot non trouvé");
            }
        }

        private void UpdateRobotGhostLocation(int robotId, Location location)
        {
            if (location == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                TeamMatesDisplayDictionary[robotId].SetGhostLocation(location);
            }
            else
            {
                Console.WriteLine("UpdateRobotGhostLocation : Robot non trouvé");
            }
        }

        private void UpdateHeatMap(int robotId, double[,] data)
        {
            if (data == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                if (lwmdType == LocalWorldMapDisplayType.StrategyMap)
                    TeamMatesDisplayDictionary[robotId].SetHeatMapStrategy(data);
                if (lwmdType == LocalWorldMapDisplayType.WayPointMap)
                    TeamMatesDisplayDictionary[robotId].SetHeatMapWaypoint(data);
            }
        }

        private void UpdateLidarMap(int robotId, List<PointD> lidarMap)
        {
            if (lidarMap == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                TeamMatesDisplayDictionary[robotId].SetLidarMap(lidarMap);
            }
            //Dispatcher.Invoke(new Action(delegate ()
            //{
            //    DrawLidar();
            //}));
        }
        
        private void UpdateLidarObjects(int robotId, List<PolarPointListExtended> lidarObjectList)
        {
            if (lidarObjectList == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                TeamMatesDisplayDictionary[robotId].SetLidarObjectList(lidarObjectList);
            }
        }

        public void UpdateBallLocationList(List<Location> ballLocationList)
        {
            if (ballLocationList != null)
            {
                lock (BallDisplayList)
                {
                    BallDisplayList.Clear();
                    foreach (var ballLocation in ballLocationList)
                    {
                        BallDisplayList.Add(new BallDisplay(ballLocation));
                    }
                }
            }
        }

        public void UpdateRobotWaypoint(int robotId, Location waypointLocation)
        {
            if (waypointLocation == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                TeamMatesDisplayDictionary[robotId].SetWayPoint(waypointLocation.X, waypointLocation.Y, waypointLocation.Theta);
            }
        }

        public void UpdateRobotDestination(int robotId, Location destinationLocation)
        {
            if (destinationLocation == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                TeamMatesDisplayDictionary[robotId].SetDestination(destinationLocation.X, destinationLocation.Y, destinationLocation.Theta);
            }
        }

        public void UpdateOpponentsLocation(int robotId, Location location)
        {
            if (location == null)
                return;
            if (OpponentDisplayDictionary.ContainsKey(robotId))
            {
                OpponentDisplayDictionary[robotId].SetLocation(location);
            }
            else
            {
                Console.WriteLine("UpdateOpponentsLocation : Robot non trouvé");
            }
        }
        

        void InitSoccerField()
        {
            int fieldLineWidth = 2;
            PolygonExtended p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-12, -8));
            p.polygon.Points.Add(new Point(12, -8));
            p.polygon.Points.Add(new Point(12, 8));
            p.polygon.Points.Add(new Point(-12, 8));
            p.polygon.Points.Add(new Point(-12, -8));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0x22, 0x22, 0x22);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.ZoneProtegee, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(11, -7));
            p.polygon.Points.Add(new Point(0, -7));
            p.polygon.Points.Add(new Point(0, 7));
            p.polygon.Points.Add(new Point(11, 7));
            p.polygon.Points.Add(new Point(11, -7));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x66, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.DemiTerrainDroit, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-11, -7));
            p.polygon.Points.Add(new Point(0, -7));
            p.polygon.Points.Add(new Point(0, 7));
            p.polygon.Points.Add(new Point(-11, 7));
            p.polygon.Points.Add(new Point(-11, -7));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x66, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.DemiTerrainGauche, p);


            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-11, -1.95));
            p.polygon.Points.Add(new Point(-10.25, -1.95));
            p.polygon.Points.Add(new Point(-10.25, 1.95));
            p.polygon.Points.Add(new Point(-11.00, 1.95));
            p.polygon.Points.Add(new Point(-11.00, -1.95));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.SurfaceButGauche, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(11.00, -1.95));
            p.polygon.Points.Add(new Point(10.25, -1.95));
            p.polygon.Points.Add(new Point(10.25, 1.95));
            p.polygon.Points.Add(new Point(11.00, 1.95));
            p.polygon.Points.Add(new Point(11.00, -1.95));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.SurfaceButDroit, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(11.00, -3.45));
            p.polygon.Points.Add(new Point(8.75, -3.45));
            p.polygon.Points.Add(new Point(8.75, 3.45));
            p.polygon.Points.Add(new Point(11.00, 3.45));
            p.polygon.Points.Add(new Point(11.00, -3.45));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.SurfaceReparationDroit, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-11.00, -3.45));
            p.polygon.Points.Add(new Point(-8.75, -3.45));
            p.polygon.Points.Add(new Point(-8.75, 3.45));
            p.polygon.Points.Add(new Point(-11.00, 3.45));
            p.polygon.Points.Add(new Point(-11.00, -3.45));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.SurfaceReparationGauche, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-11.00, -1.20));
            p.polygon.Points.Add(new Point(-11.00, 1.20));
            p.polygon.Points.Add(new Point(-11.50, 1.20));
            p.polygon.Points.Add(new Point(-11.50, -1.20));
            p.polygon.Points.Add(new Point(-11.00, -1.20));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.ButGauche, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(11.00, -1.20));
            p.polygon.Points.Add(new Point(11.00, 1.20));
            p.polygon.Points.Add(new Point(11.50, 1.20));
            p.polygon.Points.Add(new Point(11.50, -1.20));
            p.polygon.Points.Add(new Point(11.00, -1.20));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.ButDroit, p);


            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-12.00, -8.00));
            p.polygon.Points.Add(new Point(-12.00, -9.00));
            p.polygon.Points.Add(new Point(-4.00, -9.00));
            p.polygon.Points.Add(new Point(-4.00, -8.00));
            p.polygon.Points.Add(new Point(-12.00, -8.00));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00, 0xFF);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.ZoneTechniqueGauche, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(+12.00, -8.00));
            p.polygon.Points.Add(new Point(+12.00, -9.00));
            p.polygon.Points.Add(new Point(+4.00, -9.00));
            p.polygon.Points.Add(new Point(+4.00, -8.00));
            p.polygon.Points.Add(new Point(+12.00, -8.00));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00, 0xFF);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.ZoneTechniqueDroite, p);

            p = new PolygonExtended();
            int nbSteps = 30;
            for (int i = 0; i < nbSteps + 1; i++)
                p.polygon.Points.Add(new Point(1.0f * Math.Cos((double)i * (2 * Math.PI / nbSteps)), 1.0f * Math.Sin((double)i * (2 * Math.PI / nbSteps))));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.RondCentral, p);

            p = new PolygonExtended();
            for (int i = 0; i < (int)(nbSteps / 4) + 1; i++)
                p.polygon.Points.Add(new Point(-11.00 + 0.75 * Math.Cos((double)i * (2 * Math.PI / nbSteps)), -7.0 + 0.75 * Math.Sin((double)i * (2 * Math.PI / nbSteps))));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.CornerBasGauche, p);

            p = new PolygonExtended();
            for (int i = (int)(nbSteps / 4) + 1; i < (int)(2 * nbSteps / 4) + 1; i++)
                p.polygon.Points.Add(new Point(11 + 0.75 * Math.Cos((double)i * (2 * Math.PI / nbSteps)), -7 + 0.75 * Math.Sin((double)i * (2 * Math.PI / nbSteps))));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.CornerBasDroite, p);

            p = new PolygonExtended();
            for (int i = (int)(2 * nbSteps / 4); i < (int)(3 * nbSteps / 4) + 1; i++)
                p.polygon.Points.Add(new Point(11 + 0.75 * Math.Cos((double)i * (2 * Math.PI / nbSteps)), 7 + 0.75 * Math.Sin((double)i * (2 * Math.PI / nbSteps))));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.CornerHautDroite, p);

            p = new PolygonExtended();
            for (int i = (int)(3 * nbSteps / 4) + 1; i < (int)(nbSteps) + 1; i++)
                p.polygon.Points.Add(new Point(-11 + 0.75 * Math.Cos((double)i * (2 * Math.PI / nbSteps)), 7 + 0.75 * Math.Sin((double)i * (2 * Math.PI / nbSteps))));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.CornerHautGauche, p);

            p = new PolygonExtended();
            for (int i = 0; i < (int)(nbSteps) + 1; i++)
                p.polygon.Points.Add(new Point(-7.4 + 0.075 * Math.Cos((double)i * (2 * Math.PI / nbSteps)), 0.075 * Math.Sin((double)i * (2 * Math.PI / nbSteps))));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.PtAvantSurfaceGauche, p);

            p = new PolygonExtended();
            for (int i = 0; i < (int)(nbSteps) + 1; i++)
                p.polygon.Points.Add(new Point(7.4 + 0.075 * Math.Cos((double)i * (2 * Math.PI / nbSteps)), 0.075 * Math.Sin((double)i * (2 * Math.PI / nbSteps))));
            p.borderWidth = fieldLineWidth;
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0xFF, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.PtAvantSurfaceDroit, p);

        }void InitEurobotField()
        {
            double TerrainLowerX = -LengthGameArea/2-0.2;
            double TerrainUpperX = LengthGameArea/2+0.2;
            double TerrainLowerY = -WidthGameArea/2-0.2;
            double TerrainUpperY = WidthGameArea/2+0.2;

            int fieldLineWidth = 1;
            PolygonExtended p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-1.5, -1));
            p.polygon.Points.Add(new Point(1.5, -1));
            p.polygon.Points.Add(new Point(1.5, 1));
            p.polygon.Points.Add(new Point(-1.5, 1));
            p.polygon.Points.Add(new Point(-1.5, -1));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 46, 49, 146);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.TerrainComplet, p);
                                    
            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-1.5 - 0.1, 1));
            p.polygon.Points.Add(new Point(-1.5 , 1));
            p.polygon.Points.Add(new Point(-1.5 , 1-0.1));
            p.polygon.Points.Add(new Point(-1.5 - 0.1, 1 - 0.1));
            p.polygon.Points.Add(new Point(-1.5 - 0.1, 1));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseGaucheHaut, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(+1.5, -0.1));
            p.polygon.Points.Add(new Point(+1.5 + 0.1, -0.1));
            p.polygon.Points.Add(new Point(+1.5 + 0.1, 0.1));
            p.polygon.Points.Add(new Point(+1.5, 0.1));
            p.polygon.Points.Add(new Point(+1.5, -0.1));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseGaucheCentre, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-1.5 - 0.1, -1));
            p.polygon.Points.Add(new Point(-1.5, -1));
            p.polygon.Points.Add(new Point(-1.5, -1 + 0.1));
            p.polygon.Points.Add(new Point(-1.5 - 0.1, -1 + 0.1));
            p.polygon.Points.Add(new Point(-1.5 - 0.1, -1));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseGaucheBas, p);

        }

        void InitCachanField()
        {
            double TerrainLowerX = -LengthGameArea/2;
            double TerrainUpperX = LengthGameArea/2;
            double TerrainLowerY = -WidthGameArea/2;
            double TerrainUpperY = WidthGameArea/2;

            int fieldLineWidth = 1;
            PolygonExtended p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-4, -2));
            p.polygon.Points.Add(new Point(4, -2));
            p.polygon.Points.Add(new Point(4, 2));
            p.polygon.Points.Add(new Point(-4, 2));
            p.polygon.Points.Add(new Point(-4, -2));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 46, 49, 146);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.TerrainComplet, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-4, 1.5));
            p.polygon.Points.Add(new Point(-0.03, 1.5));
            p.polygon.Points.Add(new Point(-2.15, 1.5));
            p.polygon.Points.Add(new Point(-2.15, 0));
            p.polygon.Points.Add(new Point(-0.03, 0));
            p.polygon.Points.Add(new Point(-2.15, 0));
            p.polygon.Points.Add(new Point(-2.15, -1.5));
            p.polygon.Points.Add(new Point(-0.03, -1.5));
            p.polygon.Points.Add(new Point(-4, -1.5));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0, 0, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.LigneTerrainGauche, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(4, 1.5));
            p.polygon.Points.Add(new Point(0.03, 1.5));
            p.polygon.Points.Add(new Point(2.15, 1.5));
            p.polygon.Points.Add(new Point(2.15, 0));
            p.polygon.Points.Add(new Point(0.03, 0));
            p.polygon.Points.Add(new Point(2.15, 0));
            p.polygon.Points.Add(new Point(2.15, -1.5));
            p.polygon.Points.Add(new Point(0.03, -1.5));
            p.polygon.Points.Add(new Point(4, -1.5));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0, 0, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.LigneTerrainDroite, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-0.335, -2));
            p.polygon.Points.Add(new Point(0.335, -2));
            p.polygon.Points.Add(new Point(0.335, 2));
            p.polygon.Points.Add(new Point(-0.335, 2));
            p.polygon.Points.Add(new Point(-0.335, -2));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.LigneCentraleEpaisse, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-0.0095, -2));
            p.polygon.Points.Add(new Point(0.0095, -2));
            p.polygon.Points.Add(new Point(0.0095, 2));
            p.polygon.Points.Add(new Point(-0.0095, 2));
            p.polygon.Points.Add(new Point(-0.0095, -2));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.LigneCentraleFine, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-1 - 0.1, 2));
            p.polygon.Points.Add(new Point(-1 + 0.1, 2));
            p.polygon.Points.Add(new Point(-1 + 0.1, 2 + 0.2));
            p.polygon.Points.Add(new Point(-1 - 0.1, 2 + 0.2));
            p.polygon.Points.Add(new Point(-1 - 0.1, 2));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseGaucheHaut, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-4.2, -0.1));
            p.polygon.Points.Add(new Point(-4, -0.1));
            p.polygon.Points.Add(new Point(-4, 0.1));
            p.polygon.Points.Add(new Point(-4.2, 0.1));
            p.polygon.Points.Add(new Point(-4.2, -0.1));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseGaucheCentre, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(-1 - 0.1, -2));
            p.polygon.Points.Add(new Point(-1 + 0.1, -2));
            p.polygon.Points.Add(new Point(-1 + 0.1, -2 - 0.2));
            p.polygon.Points.Add(new Point(-1 - 0.1, -2 - 0.2));
            p.polygon.Points.Add(new Point(-1 - 0.1, -2));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseGaucheBas, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(1 - 0.1, 2));
            p.polygon.Points.Add(new Point(1 + 0.1, 2));
            p.polygon.Points.Add(new Point(1 + 0.1, 2 + 0.2));
            p.polygon.Points.Add(new Point(1 - 0.1, 2 + 0.2));
            p.polygon.Points.Add(new Point(1 - 0.1, 2));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseDroiteHaut, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(4.2, -0.1));
            p.polygon.Points.Add(new Point(4, -0.1));
            p.polygon.Points.Add(new Point(4, 0.1));
            p.polygon.Points.Add(new Point(4.2, 0.1));
            p.polygon.Points.Add(new Point(4.2, -0.1));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseDroiteCentre, p);

            p = new PolygonExtended();
            p.polygon.Points.Add(new Point(1 - 0.1, -2));
            p.polygon.Points.Add(new Point(1 + 0.1, -2));
            p.polygon.Points.Add(new Point(1 + 0.1, -2 - 0.2));
            p.polygon.Points.Add(new Point(1 - 0.1, -2 - 0.2));
            p.polygon.Points.Add(new Point(1 - 0.1, -2));
            p.borderWidth = fieldLineWidth;
            p.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            p.backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0);
            PolygonSeries.AddOrUpdatePolygonExtended((int)Terrain.BaliseDroiteBas, p);
        }
    }
}

