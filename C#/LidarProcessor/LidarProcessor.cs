using EventArgsLibrary;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
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
            
            //Opérations de traitement du signal LIDAR
            //ptList = PrefiltragePointsIsoles(ptList, 0.04);
            //ptList = MedianFilter(ptList, 5);

//            BalisesCatadioptriqueList = DetectionBalisesCatadioptriques(ptList, 3.6);
            BalisesCatadioptriqueList2 = DetectionBalisesCatadioptriquesParRssiEtTaille(ptList, 3.6);
            ObjetsProchesList = DetectionObjetsProches(ptList, 0.17, 2.0, 0.2);
            
            //Affichage des résultats
            List<PolarPointListExtended> objectList = new List<PolarPointListExtended>();
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

            if (competition == GameMode.RoboCup)
            {
                ///On fait la recherche du rectangle vide le plus grand 
                double tailleNoyau = 0.2;
                var ptListFiltered = Dilatation(Erosion(ptList, tailleNoyau), tailleNoyau);
                //var ptListFiltered = Erosion(Dilatation(ptList, tailleNoyau), tailleNoyau);
                //var ptListFiltered = Dilatation(ptList, tailleNoyau);
                //var ptListFiltered = Erosion(ptList, tailleNoyau);
                //var ptListFiltered = ptList;
                FindLargestRectangle(ptListFiltered, 15, 15);
                OnLidarProcessed(robotId, ptListFiltered);
            }
            OnLidarBalisesListExtracted(robotId, BalisesCatadioptriqueList2);
            OnLidarObjectProcessed(robotId, objectList);
        }

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

        void FindLargestRectangle(List<PolarPointRssi> ptList, int maxLength, int maxHeight)
        {
            ///L'algorithme repose sur la construction d'une matrice dans laquelle on place les points par blocs de 1m²
            ///La taille maximum de recherche est spécifiée dans les arguments
            ///

            /// On commence par remplir la matrice d'occupation
            double[,] fieldOccupancy = new double[2 * maxLength + 1, 2 * maxHeight + 1];
            for (int i = 1; i < ptList.Count; i++)
            {
                double xpos = ptList[i].Distance * Math.Cos(ptList[i].Angle);
                double ypos = ptList[i].Distance * Math.Sin(ptList[i].Angle);
                if (xpos < maxLength && ypos < maxHeight)
                    fieldOccupancy[(int)xpos + maxLength, (int)ypos + maxHeight] += 1;
            }


            /// On généère ensuite une matrice avec des 1 dans toutes les cases en relation directe avec le pt central
            double[,] fieldBool = new double[2 * maxLength + 1, 2 * maxHeight + 1];
            fieldBool[maxLength, maxHeight] = 1;
            /// On commence par la croix centrée sur le robot
            /// vers le bas
            for (int j = maxHeight - 1; j >= 0; j--)
            {
                if (fieldBool[maxLength, j+1] == 1 && fieldOccupancy[maxLength, j] == 0)
                    fieldBool[maxLength, j] = 1;
            }
            /// vers le haut
            for (int j = maxHeight + 1; j < 2*maxHeight+1; j++)
            {
                if (fieldBool[maxLength, j-1] == 1 && fieldOccupancy[maxLength, j] == 0)
                    fieldBool[maxLength, j] = 1;
            }
            /// vers la gauche
            for (int i = maxLength - 1; i >= 0; i--)
            {
                if (fieldBool[i + 1, maxHeight] == 1 && fieldOccupancy[i, maxHeight] == 0)
                    fieldBool[i, maxHeight] = 1;
            }
            /// vers la droite
            for (int i = maxLength + 1; i < 2*maxLength+1; i++)
            {
                if (fieldBool[i - 1, maxHeight] == 1 && fieldOccupancy[i, maxHeight] == 0)
                    fieldBool[i, maxHeight] = 1;
            }

            ///Remplissage du coin bas à gauche -> i=0 et j=0;
            for (int j = maxHeight - 1; j >= 0; j--)
            {
                for (int i = maxLength - 1; i >= 0; i--)
                {
                    if (fieldBool[i + 1, j] == 1 && fieldBool[i, j + 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i, j] = 1;
                }
            }

            ///Remplissage du coin bas à droit -> i= 2 * maxLength + 1 et j=0;
            for (int j = maxHeight - 1; j >= 0; j--)
            {
                for (int i = maxLength + 1; i < 2 * maxLength + 1; i++)
                {
                    if (fieldBool[i - 1, j] == 1 && fieldBool[i, j + 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i, j] = 1;
                }
            }

            ///Remplissage du coin haut à gauche : i->0 et j->2*maxHeight+1;
            for (int j = maxHeight + 1; j < 2 * maxHeight + 1; j++)
            {
                for (int i = maxLength - 1; i >= 0; i--)
                {
                    if (fieldBool[i + 1, j] == 1 && fieldBool[i, j - 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i, j] = 1;
                }
            }

            ///Remplissage du coin haut à droite : i->2*maxLength+1 et j->2*maxHeight+1;
            for (int j = maxHeight + 1; j < 2 * maxHeight + 1; j++)
            {
                for (int i = maxLength + 1; i < 2 * maxLength + 1; i++)
                {
                    if (fieldBool[i - 1, j] == 1 && fieldBool[i, j - 1] == 1 && fieldOccupancy[i, j] == 0)
                        fieldBool[i, j] = 1;
                }
            }

            /// On cherche ensuite le plus grand rectangle inscrit dans fieldBool
            /// Un bon exemple d'algo peut être trouvé ici :
            /// https://www.geeksforgeeks.org/maximum-size-rectangle-binary-sub-matrix-1s/
            /// 

        }



        //double seuilResiduLine = 0.03;

        //private List<LidarDetectedObject> DetectionObjetsFond(List<PolarPointRssi> ptList, double zoomCoeff)
        //{
        //    //Détection des objets de fond
        //    List<LidarDetectedObject> ObjetsFondList = new List<LidarDetectedObject>();
        //    LidarDetectedObject currentObject = new LidarDetectedObject();
        //    bool objetFondEnCours = false;

        //    for (int i = 1; i < ptList.Count; i++)
        //    {
        //        //On commence un objet de fond sur un front montant de distance
        //        if (ptList[i].Distance - ptList[i - 1].Distance > 0.06 * zoomCoeff)
        //        {
        //            currentObject = new LidarDetectedObject();
        //            currentObject.PtList.Add(ptList[i]);
        //            objetFondEnCours = true;
        //        }
        //        //On termine un objet de fond sur un front descendant de distance
        //        else if (ptList[i].Distance - ptList[i - 1].Distance < -0.12 * zoomCoeff && objetFondEnCours)
        //        {
        //            objetFondEnCours = false;
        //            if (currentObject.PtList.Count > 20)
        //            {
        //                currentObject.ExtractObjectAttributes();
        //                //Console.WriteLine("Résidu fond : " + currentObject.ResiduLineModel);
        //                //if (currentObject.ResiduLineModel < seuilResiduLine*2)
        //                {

        //                }
        //                    ObjetsFondList.Add(currentObject);
        //            }
        //        }
        //        //Sinon on reste sur le même objet
        //        else
        //        {
        //            if (objetFondEnCours)
        //            {
        //                currentObject.PtList.Add(ptList[i]);
        //            }
        //        }
        //    }

        //    return ObjetsFondList;
        //}

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


        private List<LidarDetectedObject> DetectionObjetsProches(List<PolarPointRssi> ptList, double distanceMin, double distanceMax, double tailleSegmentationObjet)
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
                        (Math.Abs(objetsProchesPointsList[i].Distance - objetsProchesPointsList[i - 1].Distance) < 0.1)&&
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


        public event EventHandler<RawLidarArgs> OnLidarProcessedEvent;
        public virtual void OnLidarProcessed(int id, List<PolarPointRssi> ptList)
        {
            var handler = OnLidarProcessedEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, PtList = ptList });
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
}

