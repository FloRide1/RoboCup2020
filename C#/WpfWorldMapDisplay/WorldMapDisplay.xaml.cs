
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

    /// <summary>
    /// Logique d'interaction pour ExtendedHeatMap.xaml
    /// </summary>
    public partial class WorldMapDisplay : UserControl
    {

        Random random = new Random();
        DispatcherTimer timerAffichage;

        public bool IsExtended = false;

        double TerrainLowerX = -11;
        double TerrainUpperX = 11;
        double TerrainLowerY = -7;
        double TerrainUpperY = 7;

        //Liste des robots à afficher
        Dictionary<int, RobotDisplay> TeamMatesDisplayDictionary = new Dictionary<int, RobotDisplay>();
        Dictionary<int, RobotDisplay> OpponentDisplayDictionary = new Dictionary<int, RobotDisplay>();

        List<PolygonExtended> ObjectDisplayList = new List<PolygonExtended>();

        //Liste des balles à afficher
        BallDisplay Balle = new BallDisplay();
        //List<BallDisplay> ListBalles = new List<BallDisplay>();

        public WorldMapDisplay()
        {
            InitializeComponent();

            //Timer de simulation
            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();
            InitSoccerField();
        }

        public void InitTeamMate(int robotId)
        {
            PolygonExtended robotShape = new PolygonExtended();
            robotShape.polygon.Points.Add(new Point(-0.25, -0.25));
            robotShape.polygon.Points.Add(new Point(0.25, -0.25));
            robotShape.polygon.Points.Add(new Point(0.2, 0));
            robotShape.polygon.Points.Add(new Point(0.25, 0.25));
            robotShape.polygon.Points.Add(new Point(-0.25, 0.25));
            robotShape.polygon.Points.Add(new Point(-0.25, -0.25));
            RobotDisplay rd = new RobotDisplay(robotShape, System.Drawing.Color.Red, 1);
            rd.SetPosition(0, 0, 0);
            TeamMatesDisplayDictionary.Add(robotId, rd);
        }

        public void InitOpponent(int robotId)
        {
            PolygonExtended robotShape = new PolygonExtended();
            robotShape.polygon.Points.Add(new Point(-0.25, -0.25));
            robotShape.polygon.Points.Add(new Point(0.25, -0.25));
            robotShape.polygon.Points.Add(new Point(0.2, 0));
            robotShape.polygon.Points.Add(new Point(0.25, 0.25));
            robotShape.polygon.Points.Add(new Point(-0.25, 0.25));
            robotShape.polygon.Points.Add(new Point(-0.25, -0.25));
            RobotDisplay rd = new RobotDisplay(robotShape, System.Drawing.Color.Blue, 1);
            rd.SetPosition(0, 0, 0);
            OpponentDisplayDictionary.Add(robotId, rd);
        }

        private void TimerAffichage_Tick(object sender, EventArgs e)
        {
            DrawBall();
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
            UpdateRobotDestination(robotId, localWorldMap.destinationLocation);
            UpdateRobotWaypoint(robotId, localWorldMap.waypointLocation);
            if (localWorldMap.heatMap != null)
                UpdateHeatMap(robotId, localWorldMap.heatMap.BaseHeatMapData);
            UpdateLidarMap(robotId, localWorldMap.lidarMap);
            UpdateLidarObjects(robotId, localWorldMap.lidarObjectList);
            UpdateBallLocation(localWorldMap.ballLocation);
        }

        public void UpdateGlobalWorldMap(GlobalWorldMap globalWorldMap)
        {
            lock (globalWorldMap.teammateLocationList)
            {
                foreach (var robotLoc in globalWorldMap.teammateLocationList)
                {
                    UpdateRobotLocation(robotLoc.Key, robotLoc.Value);
                }
            }
            lock (globalWorldMap.opponentLocationList)
            {
                int i = 0;
                foreach (var opponentLocation in globalWorldMap.opponentLocationList)
                {
                    if (globalWorldMap.TeamId == (int)TeamId.Team1)
                        UpdateOpponentsLocation((int)TeamId.Team2 + i, opponentLocation);
                    else if (globalWorldMap.TeamId == (int)TeamId.Team2)
                        UpdateOpponentsLocation((int)TeamId.Team1 + i, opponentLocation);
                    i++;
                }
            }
            UpdateBallLocation(globalWorldMap.ballLocation);
        }

        private void DrawHeatMap(int robotId)
        {
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                if (TeamMatesDisplayDictionary[robotId].heatMap == null)
                    return;
                //heatmapSeries.DataSeries = new UniformHeatmapDataSeries<double, double, double>(data, startX, stepX, startY, stepY);
                double xStep = (TerrainUpperX - TerrainLowerX) / (TeamMatesDisplayDictionary[robotId].heatMap.GetUpperBound(1) + 1);
                double yStep = (TerrainUpperY - TerrainLowerY) / (TeamMatesDisplayDictionary[robotId].heatMap.GetUpperBound(0) + 1);
                var heatmapDataSeries = new UniformHeatmapDataSeries<double, double, double>(TeamMatesDisplayDictionary[robotId].heatMap, TerrainLowerX, yStep, TerrainLowerY, xStep);

                // Apply the dataseries to the heatmap
                heatmapSeries.DataSeries = heatmapDataSeries;
                heatmapDataSeries.InvalidateParentSurface(RangeMode.None);
            }
        }

        public void DrawBall()
        {
            //Affichage de la balle
            BallPolygon.AddOrUpdatePolygonExtended((int)BallId.Ball, Balle.GetBallPolygon());
            BallPolygon.AddOrUpdatePolygonExtended((int)BallId.Ball + (int)Caracteristique.Speed, Balle.GetBallSpeedArrow());
        }

        public void DrawTeam()
        {
            XyDataSeries<double, double> lidarPts = new XyDataSeries<double, double>();
            ObjectsPolygonSeries.Clear();

            foreach (var r in TeamMatesDisplayDictionary)
            {               
                //Affichage des robots
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key, TeamMatesDisplayDictionary[r.Key].GetRobotPolygon());
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Speed, TeamMatesDisplayDictionary[r.Key].GetRobotSpeedArrow());
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.Destination, TeamMatesDisplayDictionary[r.Key].GetRobotDestinationArrow());
                PolygonSeries.AddOrUpdatePolygonExtended(r.Key + (int)Caracteristique.WayPoint, TeamMatesDisplayDictionary[r.Key].GetRobotWaypointArrow());

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
                TeamMatesDisplayDictionary[robotId].SetPosition(location.X, location.Y, location.Theta);
                TeamMatesDisplayDictionary[robotId].SetSpeed(location.Vx, location.Vy, location.Vtheta);
            }
            else
            {
                Console.WriteLine("UpdateRobotLocation : Robot non trouvé");
            }
        }

        private void UpdateHeatMap(int robotId, double[,] data)
        {
            if (data == null)
                return;
            if (TeamMatesDisplayDictionary.ContainsKey(robotId))
            {
                TeamMatesDisplayDictionary[robotId].SetHeatMap(data);
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

        public void UpdateBallLocation(Location ballLocation)
        {
            Balle.SetLocation(ballLocation);
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
                OpponentDisplayDictionary[robotId].SetPosition(location.X, location.Y, location.Theta);
                OpponentDisplayDictionary[robotId].SetSpeed(location.Vx, location.Vy, location.Vtheta);
            }
            else
            {
                Console.WriteLine("UpdateOpponentsLocation : Robot non trouvé");
            }
        }
        
        //void InitTeam()
        //{
        //    //Team1
        //    for (int i = 0; i < 1; i++)
        //    {
        //        PolygonExtended robotShape = new PolygonExtended();
        //        robotShape.polygon.Points.Add(new Point(-0.25, -0.25));
        //        robotShape.polygon.Points.Add(new Point(0.25, -0.25));
        //        robotShape.polygon.Points.Add(new Point(0.2, 0));
        //        robotShape.polygon.Points.Add(new Point(0.25, 0.25));
        //        robotShape.polygon.Points.Add(new Point(-0.25, 0.25));
        //        robotShape.polygon.Points.Add(new Point(-0.25, -0.25));
        //        RobotDisplay rd = new RobotDisplay(robotShape);
        //        rd.SetPosition((float)(i * 0.50), (float)(Math.Pow(i, 1.3) * 0.50), (float)Math.PI / 4 * i);
        //        robotDictionary.Add((int)TeamId.Team1+i, rd);
        //    }
        //}

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

        }
    }
    
    public class PolygonRenderableSeries : CustomRenderableSeries
    {
        Dictionary<int, PolygonExtended> polygonList = new Dictionary<int, PolygonExtended>();
        XyDataSeries<double, double> lineData = new XyDataSeries<double, double> { }; //Nécessaire pour l'update d'affichage

        public PolygonRenderableSeries()
        {
        }

        public void AddOrUpdatePolygonExtended(int id, PolygonExtended p)
        {
            if (polygonList.ContainsKey(id))
                polygonList[id] = p;
            else
                polygonList.Add(id, p);
        }

        public void Clear()
        {
            polygonList.Clear();
        }

        public int Count()
        {
            return polygonList.Count();
        }

        public void RedrawAll()
        {
            //Attention : Permet de déclencher l'update : workaround pas classe du tout
            lineData.Clear();
            lineData.Append(1, 1);
            DataSeries = lineData;
        }

        protected override void Draw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            base.Draw(renderContext, renderPassData);

            // Create a line drawing context. Make sure you dispose it!
            // NOTE: You can create mutliple line drawing contexts to draw segments if you want
            //       You can also call renderContext.DrawLine() and renderContext.DrawLines(), but the lineDrawingContext is higher performance
            foreach (var p in polygonList)
            {
                Polygon polygon = p.Value.polygon;

                if (polygon.Points.Count > 0)
                {
                    Point initialPoint = GetRenderingPoint(polygon.Points[0]);

                    System.Windows.Media.Color backgroundColor = System.Windows.Media.Color.FromArgb(p.Value.backgroundColor.A, p.Value.backgroundColor.R, p.Value.backgroundColor.G, p.Value.backgroundColor.B);
                    
                    using (var brush = renderContext.CreateBrush(backgroundColor))
                    {
                        //IEnumerable<Point> points; // define your points
                        renderContext.FillPolygon(brush, GetRenderingPoints(polygon.Points));
                    }

                    //// Create a pen to draw. Make sure you dispose it! 
                    System.Windows.Media.Color borderColor = System.Windows.Media.Color.FromArgb(p.Value.borderColor.A, p.Value.borderColor.R, p.Value.borderColor.G, p.Value.borderColor.B);

                    using (var linePen = renderContext.CreatePen(borderColor, this.AntiAliasing, p.Value.borderWidth, p.Value.borderOpacity, p.Value.borderDashPattern))
                    {
                        using (var lineDrawingContext = renderContext.BeginLine(linePen, initialPoint.X, initialPoint.Y))
                        {
                            for (int i = 1; i < polygon.Points.Count; i++)
                            {
                                lineDrawingContext.MoveTo(GetRenderingPoint(polygon.Points[i]).X, GetRenderingPoint(polygon.Points[i]).Y);
                            }
                            lineDrawingContext.End();
                        }
                    }
                }
            }
        }
        private Point GetRenderingPoint(Point pt)
        {
            // Get the coordinateCalculators. See 'Converting Pixel Coordinates to Data Coordinates' documentation for coordinate transforms
            var xCoord = CurrentRenderPassData.XCoordinateCalculator.GetCoordinate(pt.X);
            var yCoord = CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(pt.Y);

            //if (CurrentRenderPassData.IsVerticalChart)
            //{
            //    Swap(ref xCoord, ref yCoord);
            //}

            return new Point(xCoord, yCoord);
        }
        private PointCollection GetRenderingPoints(PointCollection ptColl)
        {
            PointCollection ptCollRender = new PointCollection();
            foreach (var pt in ptColl)
            {
                // Get the coordinateCalculators. See 'Converting Pixel Coordinates to Data Coordinates' documentation for coordinate transforms
                var xCoord = CurrentRenderPassData.XCoordinateCalculator.GetCoordinate(pt.X);
                var yCoord = CurrentRenderPassData.YCoordinateCalculator.GetCoordinate(pt.Y);
                ptCollRender.Add(new Point(xCoord, yCoord));
            }

            return ptCollRender;
        }
    }



    public class RobotDisplay
    {
        private PolygonExtended shape;
        private Random rand = new Random();

        private Location location;
        private Location destinationLocation;
        private Location waypointLocation;
        public double[,] heatMap;
        List<PointD> lidarMap;
        List<Location> opponentLocationList;
        List<Location> teamLocationList;
        List<PolarPointListExtended> lidarObjectList;

        System.Drawing.Color displayColor;
        int displayTransparency = 0xFF;

        public RobotDisplay(PolygonExtended pe, System.Drawing.Color color, double transparency)
        {
            location = new Location(0, 0, 0, 0, 0, 0);
            destinationLocation = new Location(0, 0, 0, 0, 0, 0);
            waypointLocation = new Location(0, 0, 0, 0, 0, 0);
            shape = pe;
            lidarMap = new List<PointD>();
            displayTransparency = (int)(transparency*255);
            displayColor = System.Drawing.Color.FromArgb((byte)displayTransparency, color.R, color.G, color.B);
        }

        public void SetPosition(double x, double y, double theta)
        {
            location.X = x;
            location.Y = y;
            location.Theta = theta;
        }
        public void SetSpeed(double vx, double vy, double vTheta)
        {
            location.Vx = vx;
            location.Vy = vy;
            location.Vtheta = vTheta;
        }
        public void SetDestination(double x, double y, double theta)
        {
            destinationLocation.X = x;
            destinationLocation.Y = y;
            destinationLocation.Theta = theta;
        }
        public void SetWayPoint(double x, double y, double theta)
        {
            waypointLocation.X = x;
            waypointLocation.Y = y;
            waypointLocation.Theta = theta;
        }

        public void SetHeatMap(double[,] heatMap)
        {
            this.heatMap = heatMap;
        }

        public void SetLidarMap(List<PointD> lidarMap)
        {
            this.lidarMap = lidarMap;
        }
        public void SetLidarObjectList(List<PolarPointListExtended> lidarObjectList)
        {
            this.lidarObjectList = lidarObjectList;
        }

        public void SetOpponentLocationList(List<Location> list)
        {
            this.opponentLocationList = list;
        }

        public void SetTeamLocationList(List<Location> list)
        {
            this.teamLocationList = list;
        }
        public void SetPositionAndSpeed(double x, double y, double theta, double vx, double vy, double vTheta)
        {
            location.X = x;
            location.Y = y;
            location.Theta = theta;
            location.Vx = vx;
            location.Vy = vy;
            location.Vtheta = vTheta;
        }

        public PolygonExtended GetRobotPolygon()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            foreach (var pt in shape.polygon.Points)
            {
                Point polyPt = new Point(pt.X * Math.Cos(location.Theta) - pt.Y * Math.Sin(location.Theta), pt.X * Math.Sin(location.Theta) + pt.Y * Math.Cos(location.Theta));
                polyPt.X += location.X;
                polyPt.Y += location.Y;
                polygonToDisplay.polygon.Points.Add(polyPt);
                polygonToDisplay.backgroundColor = displayColor;// shape.backgroundColor;
                polygonToDisplay.borderColor = shape.borderColor;
                polygonToDisplay.borderWidth = shape.borderWidth;
            }
            return polygonToDisplay;
        }
        public PolygonExtended GetRobotSpeedArrow()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            double angleTeteFleche = Math.PI / 6;
            double longueurTeteFleche = 0.30;
            double LongueurFleche = Math.Sqrt(location.Vx * location.Vx + location.Vy * location.Vy);
            double headingAngle = Math.Atan2(location.Vy, location.Vx) + location.Theta;
            double xTete = LongueurFleche * Math.Cos(headingAngle);
            double yTete = LongueurFleche * Math.Sin(headingAngle);

            polygonToDisplay.polygon.Points.Add(new Point(location.X, location.Y));
            polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete, location.Y + yTete));
            double angleTeteFleche1 = headingAngle + angleTeteFleche;
            double angleTeteFleche2 = headingAngle - angleTeteFleche;
            polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete - longueurTeteFleche * Math.Cos(angleTeteFleche1), location.Y + yTete - longueurTeteFleche * Math.Sin(angleTeteFleche1)));
            polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete, location.Y + yTete));
            polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete - longueurTeteFleche * Math.Cos(angleTeteFleche2), location.Y + yTete - longueurTeteFleche * Math.Sin(angleTeteFleche2)));
            polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete, location.Y + yTete));
            polygonToDisplay.borderWidth = 2;
            polygonToDisplay.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0x00, 0x00);
            polygonToDisplay.borderDashPattern = new double[] { 3, 3 };
            polygonToDisplay.borderOpacity = 1;
            polygonToDisplay.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            return polygonToDisplay;
        }
        public PolygonExtended GetRobotDestinationArrow()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            double angleTeteFleche = Math.PI / 6;
            double longueurTeteFleche = 0.30;
            double headingAngle = Math.Atan2(destinationLocation.Y - location.Y, destinationLocation.X - location.X);

            polygonToDisplay.polygon.Points.Add(new Point(location.X, location.Y));
            polygonToDisplay.polygon.Points.Add(new Point(destinationLocation.X, destinationLocation.Y));
            double angleTeteFleche1 = headingAngle + angleTeteFleche;
            double angleTeteFleche2 = headingAngle - angleTeteFleche;
            polygonToDisplay.polygon.Points.Add(new Point(destinationLocation.X - longueurTeteFleche * Math.Cos(angleTeteFleche1), destinationLocation.Y - longueurTeteFleche * Math.Sin(angleTeteFleche1)));
            polygonToDisplay.polygon.Points.Add(new Point(destinationLocation.X, destinationLocation.Y));
            polygonToDisplay.polygon.Points.Add(new Point(destinationLocation.X - longueurTeteFleche * Math.Cos(angleTeteFleche2), destinationLocation.Y - longueurTeteFleche * Math.Sin(angleTeteFleche2)));
            polygonToDisplay.polygon.Points.Add(new Point(destinationLocation.X, destinationLocation.Y));
            polygonToDisplay.borderWidth = 5;
            polygonToDisplay.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            polygonToDisplay.borderDashPattern = new double[] { 5, 5 };
            polygonToDisplay.borderOpacity = 0.4;
            polygonToDisplay.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            return polygonToDisplay;
        }
        public PolygonExtended GetRobotWaypointArrow()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            double angleTeteFleche = Math.PI / 6;
            double longueurTeteFleche = 0.30;
            double headingAngle = Math.Atan2(waypointLocation.Y - location.Y, waypointLocation.X - location.X);

            polygonToDisplay.polygon.Points.Add(new Point(location.X, location.Y));
            polygonToDisplay.polygon.Points.Add(new Point(waypointLocation.X, waypointLocation.Y));
            double angleTeteFleche1 = headingAngle + angleTeteFleche;
            double angleTeteFleche2 = headingAngle - angleTeteFleche;
            polygonToDisplay.polygon.Points.Add(new Point(waypointLocation.X - longueurTeteFleche * Math.Cos(angleTeteFleche1), waypointLocation.Y - longueurTeteFleche * Math.Sin(angleTeteFleche1)));
            polygonToDisplay.polygon.Points.Add(new Point(waypointLocation.X, waypointLocation.Y));
            polygonToDisplay.polygon.Points.Add(new Point(waypointLocation.X - longueurTeteFleche * Math.Cos(angleTeteFleche2), waypointLocation.Y - longueurTeteFleche * Math.Sin(angleTeteFleche2)));
            polygonToDisplay.polygon.Points.Add(new Point(waypointLocation.X, waypointLocation.Y));

            return polygonToDisplay;
        }

        public XyDataSeries<double, double> GetRobotLidarPoints()
        {
            var dataSeries = new XyDataSeries<double, double>();
            if (lidarMap == null)
                return dataSeries;


            //lock (lidarMap)
            {
                var listX = lidarMap.Select(e => e.X);
                var listY = lidarMap.Select(e => e.Y);

                if (listX.Count() == listY.Count())
                {
                    dataSeries.AcceptsUnsortedData = true;
                    dataSeries.Append(listX, listY);
                }
            }
            return dataSeries;
        }

        public List<PolygonExtended> GetRobotLidarObjects()
        {
            var polygonExtendedList = new List<PolygonExtended>();
            if (this.lidarObjectList == null)
                return polygonExtendedList;

            foreach (var obj in this.lidarObjectList)
            {
                PolygonExtended polygonToDisplay = new PolygonExtended();
                foreach (var pt in obj.polarPointList)
                {
                    polygonToDisplay.polygon.Points.Add(new Point(location.X + pt.Distance * Math.Cos(pt.Angle), location.Y + pt.Distance * Math.Sin(pt.Angle)));
                }
                polygonToDisplay.borderColor = obj.displayColor;
                polygonToDisplay.borderWidth = (float)obj.displayWidth;
                polygonToDisplay.backgroundColor = obj.displayColor;
                polygonExtendedList.Add(polygonToDisplay);
            }
            return polygonExtendedList;
        }
    }

    public class BallDisplay
    {
        private Random rand = new Random();
        private Location location = new Location(0,0,0,0,0,0);
        private System.Drawing.Color backgroundColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xF2, 0x00);
        private System.Drawing.Color borderColor = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
        private int borderWidth = 2;

        public BallDisplay()
        {
            location = new Location(0, 0, 0, 0, 0, 0);
        }

        public void SetPosition(double x, double y, double theta)
        {
            location.X = x;
            location.Y = y;
            location.Theta = theta;
        }
        public void SetSpeed(double vx, double vy, double vTheta)
        {
            location.Vx = vx;
            location.Vy = vy;
            location.Vtheta = vTheta;
        }

        public void SetLocation(double x, double y, double theta, double vx, double vy, double vTheta)
        {
            location.X = x;
            location.Y = y;
            location.Theta = theta;
            location.Vx = vx;
            location.Vy = vy;
            location.Vtheta = vTheta;
        }
        public void SetLocation(Location l)
        {
            location = l;
        }

        public PolygonExtended GetBallPolygon()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            if (location != null)
            {
                int nbSegments = 10;
                double radius = 0.4;
                for (double theta = 0; theta <= Math.PI * 2; theta += Math.PI * 2 / nbSegments)
                {
                    Point pt = new Point(radius * Math.Cos(theta), radius * Math.Sin(theta));
                    pt.X += location.X;
                    pt.Y += location.Y;
                    polygonToDisplay.polygon.Points.Add(pt);
                    polygonToDisplay.backgroundColor = backgroundColor;
                    polygonToDisplay.borderColor = borderColor;
                    polygonToDisplay.borderWidth = borderWidth;
                }
            }
            return polygonToDisplay;
        }
        public PolygonExtended GetBallSpeedArrow()
        {
            PolygonExtended polygonToDisplay = new PolygonExtended();
            if (location != null)
            {
                double angleTeteFleche = Math.PI / 6;
                double longueurTeteFleche = 0.30;
                double LongueurFleche = Math.Sqrt(location.Vx * location.Vx + location.Vy * location.Vy);
                double headingAngle = Math.Atan2(location.Vy, location.Vx) + location.Theta;
                double xTete = LongueurFleche * Math.Cos(headingAngle);
                double yTete = LongueurFleche * Math.Sin(headingAngle);

                polygonToDisplay.polygon.Points.Add(new Point(location.X, location.Y));
                polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete, location.Y + yTete));
                double angleTeteFleche1 = headingAngle + angleTeteFleche;
                double angleTeteFleche2 = headingAngle - angleTeteFleche;
                polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete - longueurTeteFleche * Math.Cos(angleTeteFleche1), location.Y + yTete - longueurTeteFleche * Math.Sin(angleTeteFleche1)));
                polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete, location.Y + yTete));
                polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete - longueurTeteFleche * Math.Cos(angleTeteFleche2), location.Y + yTete - longueurTeteFleche * Math.Sin(angleTeteFleche2)));
                polygonToDisplay.polygon.Points.Add(new Point(location.X + xTete, location.Y + yTete));
                polygonToDisplay.borderWidth = 2;
                polygonToDisplay.borderColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0x00, 0x00);
                polygonToDisplay.borderDashPattern = new double[] { 3, 3 };
                polygonToDisplay.borderOpacity = 1;
                polygonToDisplay.backgroundColor = System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            }
            return polygonToDisplay;
        }
    }
}

