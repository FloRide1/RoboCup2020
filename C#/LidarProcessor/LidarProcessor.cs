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
        public LidarProcessor(int id)
        {
            robotId = id;
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
            double zoomCoeff = 1.0;

            //List<PolarPoint> PtListProcessed = new List<PolarPoint>();

            List<LidarDetectedObject> ObjetsSaillantsList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsFondList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsPoteauPossible = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject();

            ////A enlever une fois le debug terminé
            //for (int i = 1; i < ptList.Count; i++)
            //{
            //    ptList[i].Distance *= zoomCoeff;
            //}

            //Opérations de traitement du signal LIDAR
            //ptList = PrefiltragePointsIsoles(ptList, 0.04, zoomCoeff);
            ObjetsSaillantsList = DetectionBalisesCatadioptriques(ptList, 3, zoomCoeff);
            //ObjetsSaillantsList = DetectionObjetsSaillants(ptList, zoomCoeff);
            //ObjetsFondList = DetectionObjetsFond(ptList, zoomCoeff);

            //Filtrage des points pouvant être un poteau
            foreach (var obj in ObjetsSaillantsList)
            {
                //if (Math.Abs(obj.Largeur - 0.125 * zoomCoeff) < 0.1 * zoomCoeff)
                {
                    ObjetsPoteauPossible.Add(obj);
                }
            }



            //Affichage des résultats
            List<PolarPointListExtended> objectList = new List<PolarPointListExtended>();
            foreach (var obj in ObjetsSaillantsList)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPointRssi>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    currentPolarPointListExtended.polarPointList = obj.PtList;
                    int variation = rand.Next(0, 255);
                    currentPolarPointListExtended.displayColor = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0,0);
                    currentPolarPointListExtended.displayWidth = 2;
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            foreach (var obj in ObjetsPoteauPossible)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPointRssi>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    currentPolarPointListExtended.polarPointList = obj.PtList;
                    currentPolarPointListExtended.displayColor = System.Drawing.Color.Yellow;
                    currentPolarPointListExtended.displayWidth = 1;
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            foreach (var obj in ObjetsFondList)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPointRssi>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    currentPolarPointListExtended.polarPointList = obj.PtList;
                    currentPolarPointListExtended.displayColor = System.Drawing.Color.Blue;
                    currentPolarPointListExtended.displayWidth = 1;
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            //OnLidarProcessed(robotId, AngleListProcessed, DistanceListProcessed);

            OnLidarProcessed(robotId, ptList);
            OnLidarObjectProcessed(robotId, objectList);
        }

        private List<PolarPointRssi> PrefiltragePointsIsoles(List<PolarPointRssi> ptList, double seuilPtIsole, double zoomCoeff)
        {
            //Préfiltrage des points isolés : un pt dont la distance aux voisin est supérieur à un seuil des deux coté est considere comme isolé.
            List<PolarPointRssi> ptListFiltered = new List<PolarPointRssi>();
            seuilPtIsole *= zoomCoeff;   //0.03 car le Lidar a une précision intrinsèque de +/- 1 cm.
            for (int i = 1; i < ptList.Count - 1; i++)
            {
                if ((Math.Abs(ptList[i - 1].Distance - ptList[i].Distance) < seuilPtIsole) || (Math.Abs(ptList[i + 1].Distance - ptList[i].Distance) < seuilPtIsole))
                {
                    ptListFiltered.Add(ptList[i]);
                }
            }
            return ptListFiltered;
        }

        double seuilResiduLine = 0.03;

        private List<LidarDetectedObject> DetectionObjetsFond(List<PolarPointRssi> ptList, double zoomCoeff)
        {
            //Détection des objets de fond
            List<LidarDetectedObject> ObjetsFondList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject();
            bool objetFondEnCours = false;

            for (int i = 1; i < ptList.Count; i++)
            {
                //On commence un objet de fond sur un front montant de distance
                if (ptList[i].Distance - ptList[i - 1].Distance > 0.06 * zoomCoeff)
                {
                    currentObject = new LidarDetectedObject();
                    currentObject.PtList.Add(ptList[i]);
                    objetFondEnCours = true;
                }
                //On termine un objet de fond sur un front descendant de distance
                else if (ptList[i].Distance - ptList[i - 1].Distance < -0.12 * zoomCoeff && objetFondEnCours)
                {
                    objetFondEnCours = false;
                    if (currentObject.PtList.Count > 20)
                    {
                        currentObject.ExtractObjectAttributes();
                        //Console.WriteLine("Résidu fond : " + currentObject.ResiduLineModel);
                        //if (currentObject.ResiduLineModel < seuilResiduLine*2)
                        {

                        }
                            ObjetsFondList.Add(currentObject);
                    }
                }
                //Sinon on reste sur le même objet
                else
                {
                    if (objetFondEnCours)
                    {
                        currentObject.PtList.Add(ptList[i]);
                    }
                }
            }

            return ObjetsFondList;
        }

        private List<LidarDetectedObject> DetectionObjetsSaillants(List<PolarPointRssi> ptList, double zoomCoeff)
        {
            List<LidarDetectedObject> ObjetsSaillantsList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject(); ;

            //Détection des objets saillants
            bool objetSaillantEnCours = false;
            for (int i = 1; i < ptList.Count; i++)
            {
                //On commence un objet saillant sur un front descendant de distance
                if (ptList[i].Distance - ptList[i - 1].Distance < -0.1 * zoomCoeff)
                {
                    currentObject = new LidarDetectedObject();
                    currentObject.PtList.Add(ptList[i]);
                    objetSaillantEnCours = true;
                }
                //On termine un objet saillant sur un front montant de distance
                else if ((ptList[i].Distance - ptList[i - 1].Distance > 0.15 * zoomCoeff) && objetSaillantEnCours)
                {
                    objetSaillantEnCours = false;
                    if (currentObject.PtList.Count > 20)
                    {
                        currentObject.ExtractObjectAttributes();
                        if(currentObject.ResiduLineModel< seuilResiduLine)
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

        private List<LidarDetectedObject> DetectionBalisesCatadioptriques(List<PolarPointRssi> ptList, double distanceMax, double zoomCoeff)
        {
            List<LidarDetectedObject> BalisesCatadioptriquesList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject(); ;

            //Détection des objets ayant un très fort RSSI
            //On récupère dans un prermier temps le max de RSSI
            double maxRssi = ptList.Max(p => p.Rssi);
            var selectedPoints = ptList.Where(p => (p.Rssi >= maxRssi * 0.7) && (p.Distance< distanceMax));
            List<PolarPointRssi> balisesPointsList = (List<PolarPointRssi>)selectedPoints.ToList();

            //On segmente la liste de points sélectionnée en objets par la distance
            currentObject = new LidarDetectedObject();
            currentObject.PtList.Add(balisesPointsList[0]);
            for (int i = 1; i < balisesPointsList.Count; i++)
            {
                if (Math.Abs(balisesPointsList[i].Angle - balisesPointsList[i - 1].Angle) < Toolbox.DegToRad(1)) //Si les pts successifs sont distants de moins de 1 degré
                {
                    //Le point est cohérent avec l'objet en cours, on ajoute le point à l'objet courant
                    currentObject.PtList.Add(balisesPointsList[i]);
                }
                else
                {
                    currentObject.ExtractObjectAttributes();
                    BalisesCatadioptriquesList.Add(currentObject);
                    currentObject = new LidarDetectedObject();
                }
            }
            currentObject.ExtractObjectAttributes();
            BalisesCatadioptriquesList.Add(currentObject);
            return BalisesCatadioptriquesList;
        }


        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarProcessedEvent;
        public virtual void OnLidarProcessed(int id, List<PolarPointRssi> ptList)
        {
            var handler = OnLidarProcessedEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, PtList = ptList});
            }
        }

        public delegate void LidarObjectProcessedEventHandler(object sender, PolarPointListExtendedListArgs e);
        public event EventHandler<PolarPointListExtendedListArgs> OnLidarObjectProcessedEvent;
        public virtual void OnLidarObjectProcessed(int id, List<PolarPointListExtended> objectList)
        {
            var handler = OnLidarObjectProcessedEvent;
            if (handler != null)
            {
                handler(this, new PolarPointListExtendedListArgs { RobotId = id, ObjectList = objectList});
            }
        }
    }

    public class LidarDetectedObject
    {
        public List<PolarPointRssi> PtList;
        public List<double> XList;
        public List<double> YList;
        public double Largeur;
        public double DistanceMoyenne;
        public double ResiduLineModel;




        public LidarDetectedObject()
        {
            PtList = new List<PolarPointRssi>();
        }
        public void ExtractObjectAttributes()
        {
            if (PtList.Count > 1)
            {
                DistanceMoyenne = PtList.Average(r => r.Distance);
                Largeur = (PtList.Max(r => r.Angle) - PtList.Min(r => r.Angle)) * DistanceMoyenne;
                XList = PtList.Select(r => r.Distance * Math.Cos(r.Angle)).ToList();
                YList = PtList.Select(r => r.Distance * Math.Sin(r.Angle)).ToList();
                var coeff = Fit.Line(XList.ToArray(), YList.ToArray());
                double a = coeff.Item1;
                double b = coeff.Item2;
                var YListFitted = XList.Select(r => a + r * b);
                ResiduLineModel = GoodnessOfFit.PopulationStandardError(YListFitted, YList);
            }
        }
    }
}

