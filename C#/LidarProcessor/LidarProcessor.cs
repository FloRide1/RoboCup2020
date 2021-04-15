using Constants;
using EventArgsLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace LidarProcessor
{
    public class LidarProcessor
    {   
        int robotId;
        GameMode competition;  // Permet de customisezr les traitemetns en fonction de la compétition

        public LidarProcessor(int id, GameMode compet)
        {
            robotId = id;
            competition = compet;
        }
        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            //Segmentation en objets
            if (robotId == e.RobotId)
            {
                ProcessLidarData(e.PtList);
                EvaluateSpeed(e.PtList);
            }
        }

        Random rand = new Random();

        RollingList<List<PolarPointRssi>> LidarPtsListList = new RollingList<List<PolarPointRssi>>(2);
        //List<PolarPoint> ptListT_1 = new List<PolarPoint>();
        void EvaluateSpeed(List<PolarPointRssi> ptList)
        {
            List<PolarPointRssi> ptListProcessed = new List<PolarPointRssi>();
            LidarPtsListList.Add(ptList);

            if (LidarPtsListList.Count >= 2)
            {
                var LidarListT_1 = LidarPtsListList[0];

                if (LidarListT_1.Count == ptList.Count)
                {
                    for (int i = 0; i < ptList.Count; i++)
                    {
                        double diff = Math.Abs(ptList[i].Distance - LidarListT_1[i].Distance);
                        if (diff < 0.2)
                        {
                            ptListProcessed.Add(new PolarPointRssi(diff, ptList[i].Angle, ptList[i].Rssi));
                        }
                    }
                }
                else
                {
                    ;
                }
            }
            //OnLidarProcessed(robotId, ptList);
        }

        void ProcessLidarData(List<PolarPointRssi> ptList)
        {
            List<LidarDetectedObject> ObjetsSaillantsList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> BalisesCatadioptriqueList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> BalisesCatadioptriqueList2 = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsProchesList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsFondList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsPoteauPossible = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject();

            List<PolarPointListExtended> objectList = new List<PolarPointListExtended>();
            List<PolarPointRssi> ptObstacleList = new List<PolarPointRssi>();

            List<PolarPointRssiExtended> ptListSampled = new List<PolarPointRssiExtended>();
            List<PolarPointRssiExtended> ptListLines = new List<PolarPointRssiExtended>();

            List<SegmentExtended> segmentList = new List<SegmentExtended>();
            List<SegmentExtended> bestSegmentList = new List<SegmentExtended>();

            List<PolarPointRssiExtended> list_of_point_clusters = new List<PolarPointRssiExtended>();
            List<PolarPointRssiExtended> list_of_corner_points = new List<PolarPointRssiExtended>();
            List<PolarPointRssiExtended> ptCornerList = new List<PolarPointRssiExtended>();

            switch (competition)
            {
                case GameMode.Eurobot:
                    BalisesCatadioptriqueList2 = DetectionBalisesCatadioptriquesParRssiEtTaille(ptList, 3.6);
                    ObjetsProchesList = DetectionObjetsProches(ptList, 0.17, 2.0, 0.2);

                    foreach (var obj in BalisesCatadioptriqueList)
                    {
                        PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                        currentPolarPointListExtended.polarPointList = new List<PolarPointRssi>();
                        {
                            currentPolarPointListExtended.polarPointList = obj.PtList;
                            currentPolarPointListExtended.type = ObjectType.Balise;
                            objectList.Add(currentPolarPointListExtended);
                        }
                    }
                    OnLidarBalisesListExtracted(robotId, BalisesCatadioptriqueList2);
                    break;
                case GameMode.RoboCup:
                    double tailleNoyau = 0.2;
                    ptListSampled = FixedStepLidarMap(ptList, 0.25);


                    #region Clusters
                    List<ClusterObjects> list_of_clusters = ClustersDetection.DetectClusterOfPoint(ptListSampled, 1);
                    foreach (ClusterObjects cluster in list_of_clusters)
                    {
                        cluster.points = LineDetection.IEPF_Algorithm(cluster.points, 0.0001);
                    }
                    list_of_point_clusters = ClustersDetection.SetColorsOfClustersObjects(list_of_clusters);
                    #endregion

                    #region Lines
                    var curvatureList = ExtractCurvature(list_of_point_clusters, 10);
                    //var curvatureSampledList = ExtractCurvature(ptListSampled, 5);

                    segmentList = LineDetection.ExtractSegmentsFromCurvature(list_of_point_clusters, curvatureList, 1.05);
                    segmentList = LineDetection.MergeSegment(segmentList, 0.1);

                    List<List<SegmentExtended>> list_family_of_segments = LineDetection.FindFamilyOfSegment(segmentList);

                    segmentList = LineDetection.SetColorOfFamily(list_family_of_segments.OrderByDescending(i => i.Count).ToList());
                    bestSegmentList = list_family_of_segments.OrderByDescending(i => i.Count).FirstOrDefault();

                    Console.WriteLine("Numbers of Filter Segment: " + segmentList.Count + " : " + list_family_of_segments.Count);
                    #endregion

                    list_of_corner_points = LineDetection.FindAllValidCrossingPoints(list_family_of_segments).Select(x => Toolbox.ConvertPointDToPolar(x)).ToList();

                    #region Deleted
                    //ptListLines = LineDetection.ExtractLinesFromCurvature(ptListSampled, curvatureList, 1.01);
                    #endregion
                    ptCornerList = ExtractCornerFromCurvature(ptListSampled, curvatureList);
                    
                    break;
            }
            
            //Affichage des résultats
            foreach(var pt in ptObstacleList)
            {
                var pple = new PolarPointListExtended();
                pple.polarPointList = new List<PolarPointRssi>();
                pple.polarPointList.Add(pt);
                pple.type = ObjectType.Obstacle;
                objectList.Add(pple);
            }
            foreach (var obj in ObjetsProchesList)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPointRssi>();
                {
                    currentPolarPointListExtended.polarPointList.Add(new PolarPointRssi(obj.AngleMoyen, obj.DistanceMoyenne, 0));
                    currentPolarPointListExtended.type = ObjectType.Obstacle;
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            //OnLidarObjectProcessed(robotId, objectList);

            OnLidarProcessed(robotId, list_of_corner_points);
            //OnLidarProcessedSegments(robotId, segmentList);
        }

        #region Useless Methods
        private List<PolarPointRssi> MedianFilter(List<PolarPointRssi> ptList, int size)
        {
            List<PolarPointRssi> ptListFiltered = new List<PolarPointRssi>();

            FixedSizedQueue<PolarPointRssi> medianQueue = new FixedSizedQueue<PolarPointRssi>(2 * size + 1);

            //Init
            for (int i = ptList.Count - 1 - size; i < ptList.Count; i++)
                medianQueue.Enqueue(ptList[i]);
            for (int i = 0; i <= size; i++)
                medianQueue.Enqueue(ptList[i]);

            //Itération
            for (int i = 0; i < ptList.Count - size - 1; i++)
            {
                var medList = medianQueue.OrderBy(x => x.Distance).ToList();
                var ptToAdd = ptList[i];
                ptToAdd.Distance = medList[size].Distance;
                ptListFiltered.Add(ptToAdd);
                medianQueue.Enqueue(ptList[i + size + 1]);
            }

            //Fin
            for (int i = ptList.Count - size - 1; i < ptList.Count; i++)
            {
                var medList = medianQueue.OrderBy(x => x.Distance).ToList();
                var ptToAdd = ptList[i];
                ptToAdd.Distance = medList[size].Distance;
                ptListFiltered.Add(ptToAdd);
                medianQueue.Enqueue(ptList[i + size + 1 - ptList.Count]);
            }
            return ptListFiltered;
        }

        
        private List<PolarPointRssi> SubSampleLidar(List<PolarPointRssi> ptList, int subsamplingFactor)
        {
            List<PolarPointRssi> ptListSubSampled = new List<PolarPointRssi>();
            for(int i=0; i< ptList.Count; i+= subsamplingFactor)
            {
                ptListSubSampled.Add(ptList[i]);
            }
            return ptListSubSampled;
        }
        private List<PolarPointRssi> PrefiltragePointsIsoles(List<PolarPointRssi> ptList, double seuilPtIsole)
        {
            //Préfiltrage des points isolés : un pt dont la distance aux voisin est supérieur à un seuil des deux coté est considere comme isolé.
            List<PolarPointRssi> ptListFiltered = new List<PolarPointRssi>();
            for (int i = 1; i < ptList.Count - 1; i++)
            {
                if ((Math.Abs(ptList[i - 1].Distance - ptList[i].Distance) < seuilPtIsole) || (Math.Abs(ptList[i + 1].Distance - ptList[i].Distance) < seuilPtIsole))
                {
                    ptListFiltered.Add(ptList[i]);
                }
                else
                {
                    ptList[i].Distance = 0;
                    ptListFiltered.Add(ptList[i]);
                }
            }
            return ptListFiltered;
        }
        private List<PolarPointRssi> Dilatation(List<PolarPointRssi> ptList, double rayon)
        {
            /// On déclare une liste pour les points de sortie. 
            /// L'initialisation manuelle est obligatoire, la copie ne marche pas car elle se fait par référence,
            /// donc tout modification au tableau dilaté se reporterait dans le tableau initial
            List<PolarPointRssi> ptListDilated = new List<PolarPointRssi>();
            for (int i = 0; i < ptList.Count; i++)
            {
                ptListDilated.Add(new PolarPointRssi(ptList[i].Angle, ptList[i].Distance, ptList[i].Rssi));
            }
            double resolutionAngulaire = 2 * Math.PI / ptList.Count;

            for (int i = 0; i < ptList.Count; i++)
            {
                if (i == 100)
                    ;
                var pt = ptList[i];
                double dilatationAngulaire = Math.Atan2(rayon, pt.Distance);
                int nbPasAngulaire = (int)(dilatationAngulaire / resolutionAngulaire);
                int borneInf = Math.Max(0, i - nbPasAngulaire);
                int borneSup = Math.Min(i + nbPasAngulaire, ptList.Count - 1);
                for (int j = borneInf; j <= borneSup; j++)
                {
                    /// Pour avoir une formule qui fonctionne aux petites distances avec un grand rayon d'érosion, 
                    /// il faut utiliser les lois des cosinus
                    double a = 1;
                    double b = -2 * ptList[i].Distance * Math.Cos((j - i) * resolutionAngulaire);
                    double c = ptList[i].Distance * ptList[i].Distance - rayon * rayon;

                    double discrimant = b * b - 4 * a * c;
                    double distanceErodee = (-b + Math.Sqrt(discrimant)) / (2 * a);
                    double distanceDilatee = (-b - Math.Sqrt(discrimant)) / (2 * a);

                    /// Version simple
                    /// ptListEroded[j].Distance = Math.Max(ptListEroded[j].Distance, distanceErodee);
                    
                    //double distancePtAxeDilatation = Math.Sin((j - i) * resolutionAngulaire) * ptList[i].Distance;
                    //double ajout = Math.Sqrt(rayon * rayon - distancePtAxeDilatation * distancePtAxeDilatation);
                    
                    ptListDilated[j].Distance = Math.Max(0, Math.Min(ptListDilated[j].Distance, distanceDilatee));
                }
                //if (i == 100)
                //    Console.WriteLine("/n");
            }
            return ptListDilated.ToList();
        }
        
        private List<PolarPointRssi> Erosion(List<PolarPointRssi> ptList, double rayon)
        {
            int originalSize = ptList.Count;
            /// On construit une liste de points ayant le double du nombre de points de la liste d'origine, de manière 
            /// à avoir un recoupement d'un tour complet 
            List<PolarPointRssi> ptListExtended = new List<PolarPointRssi>();

            for (int i = (int)(originalSize / 2); i < originalSize; i++)
            {
                ptListExtended.Add(new PolarPointRssi(ptList[i].Angle - 2 * Math.PI, ptList[i].Distance, ptList[i].Rssi));
            }
            for (int i = 0; i < originalSize; i++)
            {
                ptListExtended.Add(new PolarPointRssi(ptList[i].Angle, ptList[i].Distance, ptList[i].Rssi));
            }
            for (int i = 0; i < (int)(originalSize / 2); i++)
            {
                ptListExtended.Add(new PolarPointRssi(ptList[i].Angle + 2 * Math.PI, ptList[i].Distance, ptList[i].Rssi));
            }

            ptList = ptListExtended;

            /// On déclare une liste pour les points de sortie. 
            /// L'initialisation manuelle est obligatoire, la copie ne marche pas car elle se fait par référence,
            /// donc tout modification au tableau dilaté se reporterait dans le tableau initial
            List<PolarPointRssi> ptListEroded = new List<PolarPointRssi>();
            List<int> ptListErodedbyObjectId = new List<int>();
            for (int i = 0; i < ptList.Count; i++)
            {
                ptListEroded.Add(new PolarPointRssi(ptList[i].Angle, ptList[i].Distance, ptList[i].Rssi));
                ptListErodedbyObjectId.Add(0);
            }
            double resolutionAngulaire = 2 * Math.PI / ptList.Count;

            /// On effectue une segmentation en objet connexes, de manière à éviter les effets de bords 
            /// d'érosion au voisinnage des objets en saillie.
            /// On effetue donc l'érosion en notant pour chaque angle quel est l'objet ayant contribué à l'érosion
            /// Une fois l'érosion terminée, on retire, en passant la distance pt à l'infini,
            /// les points érodé par un objet différent de celui dans lequel ils sont dans l'ombre.
            /// 

            ///On commence par la segmentation en objets
            List<LidarDetectedObject> lidarSceneSegmentation = new List<LidarDetectedObject>();
            LidarDetectedObject objet = new LidarDetectedObject();
            double seuilDetectionObjet = 0.5;
            objet.PtList.Add(ptList[0]);
            for (int i = 1; i < ptList.Count; i++)
            {
                if (Math.Abs(ptList[i].Distance - ptList[i - 1].Distance) < seuilDetectionObjet)
                {
                    ///Si la distance entre deux points successifs n'est pas trop grande, ils appartiennent au même objet
                    objet.PtList.Add(ptList[i]);
                }
                else
                {
                    //Sinon, on crée un nouvel objet
                    lidarSceneSegmentation.Add(objet);
                    objet = new LidarDetectedObject();
                    objet.PtList.Add(ptList[i]);
                }
            }

            int numPtCourant = 0;
            for (int n = 0; n < lidarSceneSegmentation.Count; n++)
            {
                /// On itère sur tous les objets dans l'ordre
                /// 
                var obj = lidarSceneSegmentation[n];
                var objPtList = obj.PtList;
                for (int k = 0; k < objPtList.Count; k++)
                {
                    var pt = ptList[numPtCourant];
                    double erosionAngulaire = Math.Atan2(rayon, pt.Distance);
                    int nbPasAngulaire = (int)(erosionAngulaire / resolutionAngulaire);
                    int borneInf = Math.Max(0, numPtCourant - nbPasAngulaire);
                    int borneSup = Math.Min(numPtCourant + nbPasAngulaire, ptList.Count - 1);

                    for (int j = borneInf; j <= borneSup; j++)
                    {
                        /// Pour avoir une formule qui fonctionne aux petites distances avec un grand rayon d'érosion, 
                        /// il faut utiliser les lois des cosinus
                        double a = 1;
                        double b = -2 * ptList[numPtCourant].Distance * Math.Cos((j - numPtCourant) * resolutionAngulaire);
                        double c = ptList[numPtCourant].Distance * ptList[numPtCourant].Distance - rayon * rayon;

                        double discrimant = b * b - 4 * a * c;
                        double distanceErodee = (-b + Math.Sqrt(discrimant)) / (2 * a);
                        double distanceSeuil = (-b - Math.Sqrt(discrimant)) / (2 * a);

                        /// Version simple
                        if (distanceErodee >= ptListEroded[j].Distance)
                        {
                            ptListEroded[j].Distance = distanceErodee;
                            ptListErodedbyObjectId[j] = n;
                        }
                    }
                    numPtCourant++;
                }
            }

            /// On fait une seconde passe pour retirer tous les points érodés par un objet autre que celui 
            /// qui les masque.
            numPtCourant = 0;
            for (int n = 0; n < lidarSceneSegmentation.Count; n++)
            {
                /// On itère sur tous les objets dans l'ordre
                var obj = lidarSceneSegmentation[n];
                var objPtList = obj.PtList;
                for (int k = 0; k < objPtList.Count; k++)
                {
                    if (ptListErodedbyObjectId[numPtCourant] != n)
                        ptListEroded[numPtCourant].Distance = double.PositiveInfinity;
                    numPtCourant++;
                }
            }

            return ptListEroded.ToList().GetRange((int)(originalSize / 2), originalSize);
        }

        //List<PolarCourbure> ExtractCurvature2(List<PolarPointRssi> ptList)
        //{

        //}
        private Random rnd = new Random();
        private List<PolarPointRssiExtended> FixedStepLidarMap(List<PolarPointRssi> ptList, double step)
        {
            /// On construit une liste de points ayant le double du nombre de points de la liste d'origine, de manière 
            /// à avoir un recoupement d'un tour complet 
            List<PolarPointRssiExtended> ptListFixedStep = new List<PolarPointRssiExtended>();
            double minAngle = 0;
            double maxAngle = 2*Math.PI;
            double currentAngle = minAngle;
            double constante =  (ptList.Count) / (maxAngle - minAngle);
            while (currentAngle < maxAngle && currentAngle >= minAngle)
            {
                //On détermine l'indice du point d'angle courant dans la liste d'origine
                int ptIndex = (int)((currentAngle - minAngle) * constante);
                var ptCourant = ptList[ptIndex];
                //On ajoute ce point à la liste des points de sortie
                //System.Drawing.Color randomColor = System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));

                ptListFixedStep.Add(new PolarPointRssiExtended(ptCourant, 3, Color.Magenta));
                //On calcule l'incrément d'angle de manière à avoir une résolution constante si la paroi est orthogonale au rayon issu du robot
                double incrementAngle = step / Math.Max(ptCourant.Distance, 0.1);
                //On regarde le ration entre la distance entre les pts à dxroite
                int n = 5;
                var ptDroite = ptList[Math.Min(ptIndex + n, ptList.Count - 1)];
                var ptGauche = ptList[Math.Max(ptIndex - n, 0)];
                var distancePtGauchePtDroit = Toolbox.Distance(ptDroite, ptGauche);
                var distancePtGauchePtDroitCasOrthogonal = (ptDroite.Angle - ptGauche.Angle) * ptCourant.Distance;
                var ratioAngle = distancePtGauchePtDroit / distancePtGauchePtDroitCasOrthogonal;
                ratioAngle = Toolbox.LimitToInterval(ratioAngle, 1, 5);
                
                currentAngle += Math.Min(0.3, incrementAngle * step / ratioAngle);
            }

            return ptListFixedStep;
        }
        #endregion

        Object lockExtractCurvature = new object();
        List<PolarCourbure> ExtractCurvature(List<PolarPointRssiExtended> ptList, int tailleNoyau = 5)
        {
            List<PolarPointRssi> curvatureListDebug = new List<PolarPointRssi>();

            lock (lockExtractCurvature)
            {
                /// Implanation basée sur 
                /// "Natural landmark extraction for mobile robot navigation based on an adaptive curvature estimation"
                /// P. Nunez, R. Vazquez-Martın, J.C. del Toro, A. Bandera, F. Sandoval
                /// Robotics and Autonomous Systems 56 (2008) 247–264
                /// 

                List<PolarCourbure> curvatureList = new List<PolarCourbure>();

                for(int i=0; i< ptList.Count; i++)
                {
                    double normeContour = 0;
                    for (int j= -tailleNoyau/2; j<tailleNoyau/2+1; j++)
                    {
                        if (i + j >= 0 && i + j + 1 < ptList.Count)
                        {
                            normeContour += Toolbox.Distance(ptList[i + j].Pt, ptList[i + j + 1].Pt);
                        }
                    }
                    double normeDirecte = Toolbox.Distance(ptList[Math.Max(0, i - tailleNoyau / 2)].Pt, ptList[Math.Min(i + tailleNoyau / 2+1, ptList.Count - 1)].Pt);
                    curvatureList.Add(new PolarCourbure(ptList[i].Pt.Angle, normeContour / normeDirecte, false));

                    curvatureListDebug.Add(new PolarPointRssi(ptList[i].Pt.Angle, normeContour / normeDirecte, 0));
                }

                OnLidarBalisePointListForDebug(robotId, curvatureListDebug);

                return curvatureList;
            }
        }

        List<PolarPointRssiExtended> ExtractCornerFromCurvature(List<PolarPointRssiExtended> ptList, List<PolarCourbure> curvatureList, double seuilCourbure = 2.01)
        {
            List<PolarPointRssiExtended> linePoints = new List<PolarPointRssiExtended>();

            for (int i = 0; i < curvatureList.Count; i++)
            {

                if (curvatureList[i].Courbure > seuilCourbure)
                {
                    linePoints.Add(ptList[i]);
                    linePoints[linePoints.Count - 1].Color = Color.Red;
                }
            }
            return linePoints;
        }

        List<PolarPointRssiExtended> ExtractCornersFromCurvature(List<PolarPointRssiExtended> ptList, List<PolarCourbure> curvatureList)
        {
            List<PolarPointRssiExtended> cornerPoints = new List<PolarPointRssiExtended>();
            for (int i = 0; i < curvatureList.Count; i++)
            {
                int i_Moins1 = i - 1;
                if (i_Moins1 < 0)
                    i_Moins1 += ptList.Count;
                int i_Plus1 = i + 1;
                if (i_Plus1 >= ptList.Count)
                    i_Plus1 -= ptList.Count;
                if (curvatureList[i].Courbure > curvatureList[i_Moins1].Courbure && curvatureList[i].Courbure > curvatureList[i_Plus1].Courbure && curvatureList[i].Courbure>1) //On a maximum local de courbure
                {
                    cornerPoints.Add(ptList[i]);
                    
                }
            }
            return cornerPoints;
        }

        #region Futur Methods
        List<PolarPointRssi> FindEnclosingRectangle(List<PolarPointRssi> ptList, double rectangleLength, double rectangleHeight, ShiftParameters shiftConfig)
        {

            double seuilProximiteLigne = 2 * shiftConfig.xShiftSpan / shiftConfig.nbStep / 3;
            int n = 5;

            PointD RectSegment1Pt1, RectSegment1Pt2;
            PointD RectSegment2Pt1, RectSegment2Pt2;
            PointD RectSegment3Pt1, RectSegment3Pt2;
            PointD RectSegment4Pt1, RectSegment4Pt2;

            PointD corner1 = new PointD(rectangleLength / 2, rectangleHeight / 2);
            PointD corner2 = new PointD(-rectangleLength / 2, rectangleHeight / 2);
            PointD corner3 = new PointD(-rectangleLength / 2, -rectangleHeight / 2);
            PointD corner4 = new PointD(rectangleLength / 2, -rectangleHeight / 2);

            double maxScore = 0;
            double optimalAngle = 0;
            double optimalX = 0;
            double optimalY = 0;

            for (double angle = -shiftConfig.thetaShiftSpan + shiftConfig.centerAround.shiftAngle; angle < shiftConfig.thetaShiftSpan + shiftConfig.centerAround.shiftAngle; angle += 2*shiftConfig.thetaShiftSpan/shiftConfig.nbStep)
            {
                double cosAngle = Math.Cos(angle);
                double sinAngle = Math.Sin(angle);

                for (double X = -shiftConfig.xShiftSpan+shiftConfig.centerAround.shiftX ; X < shiftConfig.xShiftSpan + shiftConfig.centerAround.shiftX; X += 2*shiftConfig.xShiftSpan/shiftConfig.nbStep)
                {
                    for (double Y = -shiftConfig.yShiftSpan + shiftConfig.centerAround.shiftY; Y < shiftConfig.yShiftSpan + shiftConfig.centerAround.shiftY; Y += 2* shiftConfig.yShiftSpan/shiftConfig.nbStep)
                    {

                        /// On commencce par déterminer les coordonnées des 4 segments du rectangle tourné et décalé
                        /// On tourne en premier et on décale après, les calculs sont plus simples
                        /// 
                        PointD corner1RotatedShifted = new PointD(corner1.X * cosAngle - corner1.Y * sinAngle + X, corner1.X * sinAngle + corner1.Y * cosAngle + Y);
                        PointD corner2RotatedShifted = new PointD(corner2.X * cosAngle - corner2.Y * sinAngle + X, corner2.X * sinAngle + corner2.Y * cosAngle + Y);
                        PointD corner3RotatedShifted = new PointD(corner3.X * cosAngle - corner3.Y * sinAngle + X, corner3.X * sinAngle + corner3.Y * cosAngle + Y);
                        PointD corner4RotatedShifted = new PointD(corner4.X * cosAngle - corner4.Y * sinAngle + X, corner4.X * sinAngle + corner4.Y * cosAngle + Y);

                        double score = 0;
                        for (int i = 0; i < ptList.Count; i++)
                        {
                            var currentPt = new PointD(ptList[i].Distance * Math.Cos(ptList[i].Angle), ptList[i].Distance * Math.Sin(ptList[i].Angle));
                            double distSegment1 = Toolbox.DistancePointToSegment(currentPt, corner1RotatedShifted, corner2RotatedShifted);
                            double distSegment2 = Toolbox.DistancePointToSegment(currentPt, corner2RotatedShifted, corner3RotatedShifted);
                            double distSegment3 = Toolbox.DistancePointToSegment(currentPt, corner3RotatedShifted, corner4RotatedShifted);
                            double distSegment4 = Toolbox.DistancePointToSegment(currentPt, corner4RotatedShifted, corner1RotatedShifted);

                            var distMin = Math.Min(Math.Min(distSegment1, distSegment2), Math.Min(distSegment3, distSegment4));
                            if (distMin < seuilProximiteLigne)
                                score += 1;
                            else if (distMin < n * seuilProximiteLigne)
                                score += (n * seuilProximiteLigne - distMin) / ((n - 1) * seuilProximiteLigne);
                        }

                        if (score > maxScore)
                        {
                            maxScore = score;
                            optimalAngle = angle;
                            optimalX = X;
                            optimalY = Y;
                        }
                    }
                }
            }
            Console.WriteLine("Optimum - X : " + optimalX.ToString("N2") + " - Y : " + optimalY.ToString("N2") + " - Angle : " + Toolbox.RadToDeg(optimalAngle).ToString("N0") + "° - score :" + maxScore);

            PointD corner1RotatedShiftedOptimal = new PointD(corner1.X * Math.Cos(optimalAngle) - corner1.Y * Math.Sin(optimalAngle) + optimalX, corner1.X * Math.Sin(optimalAngle) + corner1.Y * Math.Cos(optimalAngle) + optimalY);
            PointD corner2RotatedShiftedOptimal = new PointD(corner2.X * Math.Cos(optimalAngle) - corner2.Y * Math.Sin(optimalAngle) + optimalX, corner2.X * Math.Sin(optimalAngle) + corner2.Y * Math.Cos(optimalAngle) + optimalY);
            PointD corner3RotatedShiftedOptimal = new PointD(corner3.X * Math.Cos(optimalAngle) - corner3.Y * Math.Sin(optimalAngle) + optimalX, corner3.X * Math.Sin(optimalAngle) + corner3.Y * Math.Cos(optimalAngle) + optimalY);
            PointD corner4RotatedShiftedOptimal = new PointD(corner4.X * Math.Cos(optimalAngle) - corner4.Y * Math.Sin(optimalAngle) + optimalX, corner4.X * Math.Sin(optimalAngle) + corner4.Y * Math.Cos(optimalAngle) + optimalY);


            List<PolarPointRssi> rectanglePointList = new List<PolarPointRssi>();
            int nbPtsBySegment = 20;
            for (int i = 0; i < nbPtsBySegment; i++)
            {
                PointD linePt12 = new PointD(corner1RotatedShiftedOptimal.X + (corner2RotatedShiftedOptimal.X - corner1RotatedShiftedOptimal.X) * i / nbPtsBySegment,
                                           corner1RotatedShiftedOptimal.Y + (corner2RotatedShiftedOptimal.Y - corner1RotatedShiftedOptimal.Y) * i / nbPtsBySegment);
                PointD linePt23 = new PointD(corner2RotatedShiftedOptimal.X + (corner3RotatedShiftedOptimal.X - corner2RotatedShiftedOptimal.X) * i / nbPtsBySegment,
                                           corner2RotatedShiftedOptimal.Y + (corner3RotatedShiftedOptimal.Y - corner2RotatedShiftedOptimal.Y) * i / nbPtsBySegment);
                PointD linePt34 = new PointD(corner3RotatedShiftedOptimal.X + (corner4RotatedShiftedOptimal.X - corner3RotatedShiftedOptimal.X) * i / nbPtsBySegment,
                                           corner3RotatedShiftedOptimal.Y + (corner4RotatedShiftedOptimal.Y - corner3RotatedShiftedOptimal.Y) * i / nbPtsBySegment);
                PointD linePt41 = new PointD(corner4RotatedShiftedOptimal.X + (corner1RotatedShiftedOptimal.X - corner4RotatedShiftedOptimal.X) * i / nbPtsBySegment,
                                           corner4RotatedShiftedOptimal.Y + (corner1RotatedShiftedOptimal.Y - corner4RotatedShiftedOptimal.Y) * i / nbPtsBySegment);
                double distance12 = Toolbox.Distance(new PointD(0, 0), linePt12);
                double angle12 = Math.Atan2(linePt12.Y, linePt12.X); 
                double distance23 = Toolbox.Distance(new PointD(0, 0), linePt23);
                double angle23 = Math.Atan2(linePt23.Y, linePt23.X); 
                double distance34 = Toolbox.Distance(new PointD(0, 0), linePt34);
                double angle34 = Math.Atan2(linePt34.Y, linePt34.X); 
                double distance41 = Toolbox.Distance(new PointD(0, 0), linePt41);
                double angle41 = Math.Atan2(linePt41.Y, linePt41.X);
                rectanglePointList.Add(new PolarPointRssi(angle12, distance12, 0));
                rectanglePointList.Add(new PolarPointRssi(angle23, distance23, 0)); 
                rectanglePointList.Add(new PolarPointRssi(angle34, distance34, 0));
                rectanglePointList.Add(new PolarPointRssi(angle41, distance41, 0));
            }

            return rectanglePointList;
        }

        List<PolarPointRssi> FindEnclosingLines(List<PolarPointRssi> ptList, int maxLength, int maxHeight, double resolution)
        {
            /// On teste toute les lignes possibles pour différents X>0 avec Theta entre PI/4 et 3*PI/4
            /// On teste toute les lignes possibles pour différents X<0 avec Theta entre PI/4 et 3*PI/4
            /// On teste toute les lignes possibles pour différents Y>0 avec Theta entre -PI/4 et PI/4
            /// On teste toute les lignes possibles pour différents Y<0 avec Theta entre -PI/4 et PI/4
            /// 
            /// Si le pt est distant de la droite de moins de xxx m, on le considère comma appartenant à la droite, et on incrémente le score de la droite
            /// 
            /// On garde les meillurs scores dans chaque sous ensemble
            /// Le cout algo est donc nbX * nbAngles * nbPoints * 4.
            /// 
            ///Attention, cet algo est adapté aux cas de bordure de type quadrilatère, mais peut donner des résultats étranges sinon.
            ///

            double maxScore=0;
            double optimalAngle = 0;
            double optimalX = 0;
            double optimalY = 0;

            List<PolarPointRssi> linePointList = new List<PolarPointRssi>();

            double seuilProximiteLigne = resolution/3;
            int n = 5;
            

            for (double X = 0; X< maxLength; X+=resolution)
            {
                double Y = 0;
                var LinePt = new PointD(X, 0);
                for (double theta = Math.PI / 4; theta <= 3*Math.PI / 4; theta += 0.1)
                {
                    EvaluateProximity(ptList, ref maxScore, ref optimalAngle, ref optimalX, ref optimalY, seuilProximiteLigne, n, X, Y, LinePt, theta);
                }
            }

            Console.WriteLine("Optimum X>0 - X : " + optimalX.ToString("N2") + " - Angle : " + Toolbox.RadToDeg(optimalAngle).ToString("N0") + "° - score :" + maxScore);
            for (double inc = -maxLength; inc < maxLength; inc += 0.1)
            {
                AddLineExtractedPoint(optimalAngle, optimalX, optimalY, linePointList, inc);
            }

            maxScore = 0;
            optimalAngle = 0;
            optimalX = 0;
            optimalY = 0;

            for (double X = 0; X > -maxLength; X -= resolution)
            {
                double Y = 0;
                var LinePt = new PointD(X, 0);
                for (double theta = Math.PI / 4; theta <= 3 * Math.PI / 4; theta += 0.1)
                {
                    EvaluateProximity(ptList, ref maxScore, ref optimalAngle, ref optimalX, ref optimalY, seuilProximiteLigne, n, X, Y, LinePt, theta);
                }
            }

            Console.WriteLine("Optimum X<0 - X : " + optimalX.ToString("N2") + " - Angle : " + Toolbox.RadToDeg(optimalAngle).ToString("N0") + "° - score :" + maxScore);
            for (double inc = -maxLength; inc < maxLength; inc += 0.1)
            {
                AddLineExtractedPoint(optimalAngle, optimalX, optimalY, linePointList, inc);
            }

            maxScore = 0;
            optimalAngle = 0;
            optimalX = 0;
            optimalY = 0;

            //On s'occupe des Y > 0
            for (double Y = 0; Y < maxHeight; Y += resolution)
            {
                double X = 0;
                var LinePt = new PointD(0, Y);
                for (double theta = -Math.PI / 4; theta <= Math.PI / 4; theta += 0.1)
                {
                    EvaluateProximity(ptList, ref maxScore, ref optimalAngle, ref optimalX, ref optimalY, seuilProximiteLigne, n, X, Y, LinePt, theta);
                }
            }

            Console.WriteLine("Optimum Y>0 - Y : " + optimalY.ToString("N2") + " - Angle : " + Toolbox.RadToDeg(optimalAngle).ToString("N0") + "° - score :" + maxScore);
            for (double inc = -maxLength; inc < maxLength; inc += 0.1)
            {
                AddLineExtractedPoint(optimalAngle, optimalX, optimalY, linePointList, inc);
            }

            maxScore = 0;
            optimalAngle = 0;
            optimalX = 0;
            optimalY = 0;

            //On s'occupe des Y < 0
            for (double Y = 0; Y > -maxHeight; Y -= resolution)
            {
                double X = 0;
                var LinePt = new PointD(0, Y);
                for (double theta = -Math.PI / 4; theta <= Math.PI / 4; theta += 0.1)
                {
                    EvaluateProximity(ptList, ref maxScore, ref optimalAngle, ref optimalX, ref optimalY, seuilProximiteLigne, n, X, Y, LinePt, theta);
                }
            }

            Console.WriteLine("Optimum Y<0 - Y : " + optimalY.ToString("N2") + " - Angle : " + Toolbox.RadToDeg(optimalAngle).ToString("N0") + "° - score :" + maxScore);
            for (double inc = -maxLength; inc < maxLength; inc += 0.1)
            {
                AddLineExtractedPoint(optimalAngle, optimalX, optimalY, linePointList, inc);
            }

            return linePointList;
        }

        private static void AddLineExtractedPoint(double optimalAngle, double optimalX, double optimalY, List<PolarPointRssi> linePointList, double inc)
        {
            PointD linePt = new PointD(optimalX + inc * Math.Cos(optimalAngle), optimalY + inc * Math.Sin(optimalAngle));
            double distance = Toolbox.Distance(new PointD(0, 0), linePt);
            double angle = Math.Atan2(linePt.Y, linePt.X);
            linePointList.Add(new PolarPointRssi(angle, distance, 0));
        }

        private static void EvaluateProximity(List<PolarPointRssi> ptList, ref double maxScore, ref double optimalAngle, ref double optimalX, ref double optimalY, double seuilProximiteLigne, int n, double X, double Y, PointD LinePt, double theta)
        {
            double score = 0;
            for (int i = 0; i < ptList.Count; i++)
            {
                //On teste si la distance du pt Lidar à la droite est inférieure à un certain seuil
                PointD ptCourant = new PointD(ptList[i].Distance * Math.Cos(ptList[i].Angle), ptList[i].Distance * Math.Sin(ptList[i].Angle));
                var distance = Toolbox.DistancePointToLine(ptCourant, LinePt, theta);
                if (distance < seuilProximiteLigne)
                    score += 1;
                else if (distance < n * seuilProximiteLigne)
                    score += (n * seuilProximiteLigne - distance) / ((n - 1) * seuilProximiteLigne);
            }
            if (score > maxScore)
            {
                maxScore = score;
                optimalAngle = theta;
                optimalX = X;
                optimalY = Y;
            }
        }

        void FindLargestRectangle(List<PolarPointRssi> ptList, int maxLength, int maxHeight, double angleshift, double resolution = 1)
        {
            ///L'algorithme repose sur la construction d'une matrice dans laquelle on place les points par blocs de 1m²
            ///La taille maximum de recherche est spécifiée dans les arguments
            ///
            maxLength = (int)(maxLength / resolution);
            maxHeight = (int)(maxHeight / resolution);

            /// On commence par remplir la matrice d'occupation
            double[,] fieldOccupancy = new double[2 * maxLength + 1, 2 * maxHeight + 1];
            for (int i = 1; i < ptList.Count; i++)
            {
                double xpos = ptList[i].Distance / resolution * Math.Cos(ptList[i].Angle - angleshift);
                double ypos = ptList[i].Distance / resolution * Math.Sin(ptList[i].Angle - angleshift);
                if (xpos >= 0 && xpos < maxLength && ypos >= 0 && ypos < maxHeight)
                    fieldOccupancy[(int)xpos + maxLength, (int)ypos + maxHeight] += 1;
            }

            ///On referme la matrice d'occupation de manière à ce que le contour soit continu tout autour du robot


            /// On généère ensuite une matrice avec des 1 dans toutes les cases en relation directe avec le pt central
            int[][] fieldBool = new int[2 * maxLength + 1][];
            for(int i=0; i< 2*maxLength+1; i++)
            {
                fieldBool[i] = new int[2 * maxHeight + 1];
            }


            fieldBool[maxLength][maxHeight] = 1;
            /// On commence par la croix centrée sur le robot
            /// vers le bas
            for (int j = maxHeight - 1; j >= 0; j--)
            {
                if (fieldBool[maxLength][j +1] == 1 && fieldOccupancy[maxLength, j] == 0)
                    fieldBool[maxLength][j] = 1;
            }
            /// vers le haut
            for (int j = maxHeight + 1; j < 2*maxHeight+1; j++)
            {
                if (fieldBool[maxLength][j -1] == 1 && fieldOccupancy[maxLength, j] == 0)
                    fieldBool[maxLength][j] = 1;
            }
            /// vers la gauche
            for (int i = maxLength - 1; i >= 0; i--)
            {
                if (fieldBool[i + 1][maxHeight] == 1 && fieldOccupancy[i, maxHeight] == 0)
                    fieldBool[i][maxHeight] = 1;
            }
            /// vers la droite
            for (int i = maxLength + 1; i < 2*maxLength+1; i++)
            {
                if (fieldBool[i - 1][maxHeight] == 1 && fieldOccupancy[i, maxHeight] == 0)
                    fieldBool[i][maxHeight] = 1;
            }

            ///Remplissage du coin bas à gauche -> i=0 et j=0;
            for (int j = maxHeight - 1; j >= 0; j--)
            {
                for (int i = maxLength - 1; i >= 0; i--)
                {
                    if (fieldBool[i + 1][j] == 1 && fieldBool[i][j + 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i][j] = 1;
                }
            }

            ///Remplissage du coin bas à droit -> i= 2 * maxLength + 1 et j=0;
            for (int j = maxHeight - 1; j >= 0; j--)
            {
                for (int i = maxLength + 1; i < 2 * maxLength + 1; i++)
                {
                    if (fieldBool[i - 1][j] == 1 && fieldBool[i][j + 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i][j] = 1;
                }
            }

            ///Remplissage du coin haut à gauche : i->0 et j->2*maxHeight+1;
            for (int j = maxHeight + 1; j < 2 * maxHeight + 1; j++)
            {
                for (int i = maxLength - 1; i >= 0; i--)
                {
                    if (fieldBool[i + 1][j] == 1 && fieldBool[i][j - 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i][j] = 1;
                }
            }

            ///Remplissage du coin haut à droite : i->2*maxLength+1 et j->2*maxHeight+1;
            for (int j = maxHeight + 1; j < 2 * maxHeight + 1; j++)
            {
                for (int i = maxLength + 1; i < 2 * maxLength + 1; i++)
                {
                    if (fieldBool[i - 1][j] == 1 && fieldBool[i][j - 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i][j] = 1;
                }
            }

            /// On cherche ensuite le plus grand rectangle inscrit dans fieldBool
            /// Un bon exemple d'algo peut être trouvé ici :
            /// https://www.geeksforgeeks.org/maximum-size-rectangle-binary-sub-matrix-1s/
            /// 

            Console.Write("Area of maximum rectangle is "
                      + GFG.maxRectangle(2 * maxLength + 1, 2 * maxHeight + 1, fieldBool) + ", angleShift = " + angleshift.ToString("N2") + "\n");
        }



        //double seuilResiduLine = 0.03;

        private List<PolarPointRssi> DetectionBackgroundPoints(List<PolarPointRssi> ptList)
        {
            //Détection des objets de fond
            List<PolarPointRssi> BackgroundPointList = new List<PolarPointRssi>();
            bool objetFondEnCours = false;

            for (int i = 1; i < ptList.Count; i++)
            {
                //On commence un objet de fond sur un front montant de distance
                if (ptList[i].Distance - ptList[i - 1].Distance > 0.4 )
                {
                    BackgroundPointList.Add(ptList[i]);
                    objetFondEnCours = true;
                }
                //On termine un objet de fond sur un front descendant de distance
                else if (ptList[i].Distance - ptList[i - 1].Distance < -0.1 && objetFondEnCours)
                {
                    objetFondEnCours = false;                    
                }
                //Sinon on reste sur le même objet
                else
                {
                    if (objetFondEnCours)
                    {
                        BackgroundPointList.Add(ptList[i]);
                    }
                }
            }

            return BackgroundPointList;
        }
       

        private List<LidarDetectedObject> DetectionObjetsSaillants(List<PolarPointRssi> ptList, double seuilSaillance)
        {
            List<LidarDetectedObject> ObjetsSaillantsList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject(); ;

            //Détection des objets saillants
            bool objetSaillantEnCours = false;
            for (int i = 1; i < ptList.Count; i++)
            {
                //On commence un objet saillant sur un front descendant de distance
                if (ptList[i].Distance - ptList[i - 1].Distance < -seuilSaillance)
                {
                    currentObject = new LidarDetectedObject();
                    currentObject.PtList.Add(ptList[i]);
                    objetSaillantEnCours = true;
                }
                //On termine un objet saillant sur un front montant de distance
                else if ((ptList[i].Distance - ptList[i - 1].Distance > seuilSaillance*1.2) && objetSaillantEnCours)
                {
                    objetSaillantEnCours = false;
                    if (currentObject.PtList.Count > 20)
                    {
                        currentObject.ExtractObjectAttributes();
                        ObjetsSaillantsList.Add(currentObject);
                    }
                }
                //Sinon on reste sur le même objet
                else
                {
                    if (objetSaillantEnCours)
                    {
                        currentObject.PtList.Add(ptList[i]);
                    }
                }
            }

            return ObjetsSaillantsList;
        }

        

        private List<LidarDetectedObject> DetectionBalisesCatadioptriques(List<PolarPointRssi> ptList, double distanceMax)
        {
            List<LidarDetectedObject> BalisesCatadioptriquesList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject(); ;

            //Détection des objets ayant un RSSI dans un intervalle correspondant aux catadioptres utilisés et proches
            //double maxRssiCatadioptre = 90;
            double minRssiCatadioptre = 50;
            var selectedPoints = ptList.Where(p => (p.Rssi >= minRssiCatadioptre) && (p.Distance < distanceMax));
            List<PolarPointRssi> balisesPointsList = (List<PolarPointRssi>)selectedPoints.ToList();

            //On segmente la liste de points sélectionnée en objets par la distance
            if (balisesPointsList.Count() > 0)
            {
                currentObject = new LidarDetectedObject();
                currentObject.PtList.Add(balisesPointsList[0]);
                for (int i = 1; i < balisesPointsList.Count; i++)
                {
                    if (Math.Abs(balisesPointsList[i].Angle - balisesPointsList[i - 1].Angle) < Toolbox.DegToRad(2)) //Si les pts successifs sont distants de moins de 1 degré
                    {
                        //Le point est cohérent avec l'objet en cours, on ajoute le point à l'objet courant
                        currentObject.PtList.Add(balisesPointsList[i]);
                    }
                    else
                    {
                        currentObject.ExtractObjectAttributes();
                        BalisesCatadioptriquesList.Add(currentObject);
                        currentObject = new LidarDetectedObject();
                        currentObject.PtList.Add(balisesPointsList[i]);
                    }
                }
                currentObject.ExtractObjectAttributes();
                BalisesCatadioptriquesList.Add(currentObject);
            }
            return BalisesCatadioptriquesList;
        }
        #endregion

        private List<LidarDetectedObject> DetectionBalisesCatadioptriquesParRssiEtTaille(List<PolarPointRssi> ptList, double distanceMax)
        {
            double minRssiBalise = 60;
            List<LidarDetectedObject> BalisesCatadioptriquesList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject(); ;
            List<PolarPointRssi> BalisePointList = new List<PolarPointRssi>();

            ////Détection des objets ayant un RSSI dans un intervalle correspondant aux catadioptres utilisés et proches
            ////double maxRssiCatadioptre = 90;
            //double minRssiCatadioptre = 60;
            //var selectedPoints = ptList.Where(p => (p.Rssi >= minRssiCatadioptre) && (p.Distance < distanceMax));
            //List<PolarPointRssi> balisesPointsList = (List<PolarPointRssi>)selectedPoints.ToList();

            //On détecte les max de RSSI supérieurs à un minRssiBalise
            List<int> maxRssiIndexList = new List<int>();
            if (ptList.Count() > 0)
            {
                for (int i = 1; i < ptList.Count - 1; i++)
                {
                    if (ptList[i].Rssi >= ptList[i - 1].Rssi && ptList[i].Rssi > ptList[i + 1].Rssi && ptList[i].Rssi > minRssiBalise)
                    {
                        maxRssiIndexList.Add(i);
                    }
                }
                //Gestion des cas de bord de tableau pour ne pas avoir de zone morte
                if (ptList[0].Rssi >= ptList[ptList.Count-1].Rssi && ptList[0].Rssi > ptList[1].Rssi && ptList[0].Rssi > minRssiBalise)
                {
                    maxRssiIndexList.Add(0);
                }
                if (ptList[ptList.Count - 1].Rssi >= ptList[ptList.Count - 2].Rssi && ptList[ptList.Count - 1].Rssi > ptList[0].Rssi && ptList[ptList.Count - 1].Rssi > minRssiBalise)
                {
                    maxRssiIndexList.Add(ptList.Count - 1);
                }
            }


            //ON génère la liste des points des balises pour affichage de debug uniquement
            BalisePointList = new List<PolarPointRssi>();
            for (int i = 0; i < ptList.Count ; i++)
            {
                PolarPointRssi pt = new PolarPointRssi(ptList[i].Angle, 0, ptList[i].Rssi);
                BalisePointList.Add(pt);
            }

            //On regarde la taille des objets autour des max de Rssi
            double seuilSaillance = 0.2;
            foreach (int indexPicRssi in maxRssiIndexList)
            {
                //On cherche à détecter les fronts montants de distance (distance qui augmente brutalement) autour des pics pour déterminer la taille des objets
                //On définit une taille max de recherche correspondant à 5 fois la taille d'une balise
                double tailleAngulaireBalisePotentielle = 0.15 / ptList[indexPicRssi].Distance;
                double incrementAngleLidar = ptList[1].Angle - ptList[0].Angle;
                //Défini la fenetre de recherche
                int indexSearchWindow = (int)(tailleAngulaireBalisePotentielle / incrementAngleLidar);

                int indexFrontMontantAngleSup = -1;
                int indexFrontMontantAngleInf = -1;
                //Détection des fronts montants coté des angles inférieurs à l'angle central
                int index = indexPicRssi;
                int indexShift = 0; //Pour gérer les balises situées à l'angle 0
                while (index > indexPicRssi-indexSearchWindow)
                {
                    if (index - 1 < 0)
                        indexShift = ptList.Count(); //Gestion des bords de tableau

                    BalisePointList[index+ indexShift - 1].Distance = 5; //For debug display

                    if (Math.Abs(ptList[index + indexShift - 1].Distance - ptList[indexPicRssi].Distance) > seuilSaillance)
                    {
                        //On a un front montant coté des angles inférieurs à l'angle central
                        indexFrontMontantAngleInf = index + indexShift - 1;
                        break;
                    }
                    index--;                    
                }

                //Détection des fronts montants  coté des angles supérieurs à l'angle central
                index = indexPicRssi;
                indexShift = 0; //Pour gérer les balises situées à l'angle 0
                while (index < indexPicRssi + indexSearchWindow)
                {
                    if (index + 1 >= ptList.Count)
                        indexShift = -ptList.Count(); //Gestion des bords de tableau

                    BalisePointList[index + indexShift + 1].Distance = 6; //For debug display
                    
                    if (Math.Abs(ptList[index + indexShift + 1].Distance - ptList[indexPicRssi].Distance) > seuilSaillance)
                    {
                        //On a un front montant coté des angles supérieurs à l'angle central
                        indexFrontMontantAngleSup = index + indexShift + 1;
                        break;
                    }
                    index++;
                }
                if(indexFrontMontantAngleInf>=0 && indexFrontMontantAngleSup >= 0)
                {
                    //On a deux fronts montants de part et d'autre du pic, on regarde la taille de l'objet
                    double tailleObjet = (ptList[indexFrontMontantAngleSup].Angle - ptList[indexFrontMontantAngleInf].Angle)
                        * (ptList[indexFrontMontantAngleSup].Distance + ptList[indexFrontMontantAngleInf].Distance + ptList[indexPicRssi].Distance) / 3;
                    //if(tailleObjet>0.05&& tailleObjet<0.25)
                    {
                        //On a probablement un catadioptre de type balise Eurobot !
                        currentObject = new LidarDetectedObject();

                        if (indexFrontMontantAngleInf < indexFrontMontantAngleSup)
                        {
                            for (int i = indexFrontMontantAngleInf+1; i < indexFrontMontantAngleSup-1; i++) //Décalages pour éviter de mettre une distance erronnée dans les amas de points
                            {
                                currentObject.PtList.Add(ptList[i]);
                                BalisePointList[i].Distance = 10; //For debug display
                            }
                            currentObject.ExtractObjectAttributes();
                            BalisesCatadioptriquesList.Add(currentObject);
                        }
                        else  //Gestion des objets coupés en deux par l'angle 0
                        {
                            for (int i = indexFrontMontantAngleInf+1; i < ptList.Count; i++) //Décalages pour éviter de mettre une distance erronnée dans les amas de points
                            {
                                currentObject.PtList.Add(ptList[i]);
                                BalisePointList[i].Distance = 10; //For debug display
                            }

                            for (int i = 0; i < indexFrontMontantAngleSup-1; i++) //Décalages pour éviter de mettre une distance erronnée dans les amas de points
                            {
                                currentObject.PtList.Add(new PolarPointRssi(ptList[i].Angle + 2 * Math.PI, ptList[i].Distance, ptList[i].Rssi));
                                BalisePointList[i].Distance = 10; //For debug display
                            }
                            currentObject.ExtractObjectAttributes();
                            BalisesCatadioptriquesList.Add(currentObject);
                        }
                    }
                }
            }

            OnLidarBalisePointListForDebug(robotId, BalisePointList);

            return BalisesCatadioptriquesList;
        }


        private List<LidarDetectedObject> DetectionObjetsProches(List<PolarPointRssi> ptList, double distanceMin, double distanceMax, double tailleSegmentationObjet, double tolerance=0.1)
        {
            List<LidarDetectedObject> ObjetsProchesList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject(); ;

            //Détection des objets proches
            var selectedPoints = ptList.Where(p => (p.Distance < distanceMax) && (p.Distance > distanceMin));
            List<PolarPointRssi> objetsProchesPointsList = (List<PolarPointRssi>)selectedPoints.ToList();

            //On segmente la liste de points sélectionnée en objets par la distance
            if (objetsProchesPointsList.Count() > 0)
            {
                currentObject = new LidarDetectedObject();
                currentObject.PtList.Add(objetsProchesPointsList[0]);
                double angleInitialObjet = objetsProchesPointsList[0].Angle;
                for (int i = 1; i < objetsProchesPointsList.Count; i++)
                {
                    if ((Math.Abs(objetsProchesPointsList[i].Angle - objetsProchesPointsList[i - 1].Angle) < Toolbox.DegToRad(2)) &&
                        (Math.Abs(objetsProchesPointsList[i].Distance - objetsProchesPointsList[i - 1].Distance) < tolerance) &&
                        (Math.Abs(objetsProchesPointsList[i].Angle - angleInitialObjet) *objetsProchesPointsList[i].Distance< tailleSegmentationObjet))
                        //Si les pts successifs sont distants de moins de x degrés et de moins de y mètres
                        //et que l'objet fait moins de tailleMax en largeur
                    {
                        //Le point est cohérent avec l'objet en cours, on ajoute le point à l'objet courant
                        currentObject.PtList.Add(objetsProchesPointsList[i]);
                    }
                    else
                    {
                        currentObject.ExtractObjectAttributes();
                        ObjetsProchesList.Add(currentObject);
                        currentObject = new LidarDetectedObject();
                        currentObject.PtList.Add(objetsProchesPointsList[i]);
                        angleInitialObjet = objetsProchesPointsList[i].Angle;
                    }
                }
                currentObject.ExtractObjectAttributes();
                ObjetsProchesList.Add(currentObject);
            }
            return ObjetsProchesList;
        }


        public event EventHandler<LidarPolarPtListExtendedArgs> OnLidarProcessedEvent;
        public virtual void OnLidarProcessed(int id, List<PolarPointRssiExtended> ptList)
        {
            var handler = OnLidarProcessedEvent;
            if (handler != null)
            {
                handler(this, new LidarPolarPtListExtendedArgs { RobotId = id, PtList = ptList, Type = LidarDataType.ProcessedData1 });
            }
        }

        public event EventHandler<SegmentExtendedListArgs> OnLidarProcessedSegmentsEvent;
        public virtual void OnLidarProcessedSegments(int id, List<SegmentExtended> sList)
        {
            var handler = OnLidarProcessedSegmentsEvent;
            if (handler != null)
            {
                handler(this, new SegmentExtendedListArgs {RobotId=id, SegmentList = sList});
            }
        }


        public event EventHandler<RawLidarArgs> OnLidarBalisePointListForDebugEvent;
        public virtual void OnLidarBalisePointListForDebug(int id, List<PolarPointRssi> ptList)
        {
            var handler = OnLidarBalisePointListForDebugEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, PtList = ptList });
            }
        }

        public event EventHandler<PolarPointListExtendedListArgs> OnLidarObjectProcessedEvent;
        public virtual void OnLidarObjectProcessed(int id, List<PolarPointListExtended> objectList)
        {
            var handler = OnLidarObjectProcessedEvent;
            if (handler != null)
            {
                handler(this, new PolarPointListExtendedListArgs { RobotId = id, ObjectList = objectList});
            }
        }

        public event EventHandler<LidarDetectedObjectListArgs> OnLidarBalisesListExtractedEvent;
        public virtual void OnLidarBalisesListExtracted(int id, List<LidarDetectedObject> objectList)
        {
            var handler = OnLidarBalisesListExtractedEvent;
            if (handler != null)
            {
                handler(this, new LidarDetectedObjectListArgs { RobotId = id, LidarObjectList = objectList });
            }
        }
    }
    
    public class LidarDetectedObjectListArgs : EventArgs
    {
        public int RobotId { get; set; }
        public List<LidarDetectedObject> LidarObjectList { get; set; }
    }

    public class LidarDetectedObject
    {
        public List<PolarPointRssi> PtList;
        public List<double> XList;
        public List<double> YList;
        public double Largeur;
        public double DistanceMoyenne;
        public double AngleMoyen;
        public double XMoyen;
        public double YMoyen;
        public double ResiduLineModel;

        public bool IsEdgeObject = false; //Pour les objets coupés en deux par le tableau d'angle

        public LidarDetectedObject()
        {
            PtList = new List<PolarPointRssi>();
        }
        public void ExtractObjectAttributes()
        {
            if (PtList.Count > 1)
            {
                DistanceMoyenne = PtList.Average(r => r.Distance);
                AngleMoyen = PtList.Average(r => r.Angle);
                Largeur = (PtList.Max(r => r.Angle) - PtList.Min(r => r.Angle)) * DistanceMoyenne;
                XMoyen = DistanceMoyenne * Math.Cos(AngleMoyen);
                YMoyen = DistanceMoyenne * Math.Sin(AngleMoyen);
                XList = PtList.Select(r => r.Distance * Math.Cos(r.Angle)).ToList();
                YList = PtList.Select(r => r.Distance * Math.Sin(r.Angle)).ToList();
                //var coeff = Fit.Line(XList.ToArray(), YList.ToArray());
                //double a = coeff.Item1;
                //double b = coeff.Item2;
                //var YListFitted = XList.Select(r => a + r * b);
                //ResiduLineModel = GoodnessOfFit.PopulationStandardError(YListFitted, YList);
            }
        }
    }


    class RotationTranslation
    {
        public double shiftX;
        public double shiftY;
        public double shiftAngle;
    }

    class ShiftParameters
    {
        public RotationTranslation centerAround;
        public double xShiftSpan;
        public double yShiftSpan;
        public double thetaShiftSpan;
        public double nbStep;
    }
}

