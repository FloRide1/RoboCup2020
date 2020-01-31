using EventArgsLibrary;
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
            }
        }

        void ProcessLidarData(List<PolarPoint> ptList)
        {
            double zoomCoeff = 1.0;

            //List<PolarPoint> PtListProcessed = new List<PolarPoint>();

            List<LidarDetectedObject> ObjetsSaillantsList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsFondList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsPoteauPossible = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject();

            //A enlever une fois le debug terminé
            for (int i = 1; i < ptList.Count; i++)
            {
                ptList[i].Distance *= zoomCoeff;
            }

            //Opérations de traitement du signal LIDAR
            ptList = PrefiltragePointsIsoles(ptList, zoomCoeff);   
            ObjetsSaillantsList = DetectionObjetsSaillants(ptList, zoomCoeff);
            ObjetsFondList = DetectionObjetsFond(ptList, zoomCoeff);

            //Filtrage des points pouvant être un poteau
            foreach (var obj in ObjetsSaillantsList)
            {
                if (Math.Abs(obj.Largeur - 0.125 * zoomCoeff) < 0.1 * zoomCoeff)
                {
                    ObjetsPoteauPossible.Add(obj);
                }
            }



            //Affichage des résultats
            List<PolarPointListExtended> objectList = new List<PolarPointListExtended>();
            foreach (var obj in ObjetsSaillantsList)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPoint>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    currentPolarPointListExtended.polarPointList = obj.PtList;
                    currentPolarPointListExtended.displayColor = System.Drawing.Color.Red;
                    currentPolarPointListExtended.displayWidth = 8;
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            foreach (var obj in ObjetsPoteauPossible)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPoint>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    currentPolarPointListExtended.polarPointList = obj.PtList;
                    currentPolarPointListExtended.displayColor = System.Drawing.Color.Yellow;
                    currentPolarPointListExtended.displayWidth = 3;
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            foreach (var obj in ObjetsFondList)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPoint>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    currentPolarPointListExtended.polarPointList = obj.PtList;
                    currentPolarPointListExtended.displayColor = System.Drawing.Color.Blue;
                    currentPolarPointListExtended.displayWidth = 6;
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            //OnLidarProcessed(robotId, AngleListProcessed, DistanceListProcessed);

            OnLidarProcessed(robotId, ptList);
            OnLidarObjectProcessed(robotId, objectList);
        }

        private List<PolarPoint> PrefiltragePointsIsoles(List<PolarPoint> ptList, double zoomCoeff)
        {
            //Préfiltrage des points isolés : un pt dont la distance aux voisin est supérieur à un seuil des deux coté est considere comme isolé.
            List<PolarPoint> ptListFiltered = new List<PolarPoint>();
            double seuilPtIsole = 0.04 * zoomCoeff;   //0.03 car le Lidar a une précision intrinsèque de +/- 1 cm.
            for (int i = 1; i < ptList.Count - 1; i++)
            {
                if ((Math.Abs(ptList[i - 1].Distance - ptList[i].Distance) < seuilPtIsole) || (Math.Abs(ptList[i + 1].Distance - ptList[i].Distance) < seuilPtIsole))
                {
                    ptListFiltered.Add(ptList[i]);
                }
            }
            return ptListFiltered;
        }

        private List<LidarDetectedObject> DetectionObjetsFond(List<PolarPoint> ptList, double zoomCoeff)
        {
            //Détection des objets de fond
            List<LidarDetectedObject> ObjetsFondList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject();
            bool objetFondEnCours = false;

            for (int i = 1; i < ptList.Count; i++)
            {
                //On commence un objet de fond sur un front montant de distance
                if (ptList[i].Distance - ptList[i - 1].Distance > 0.1 * zoomCoeff)
                {
                    currentObject = new LidarDetectedObject();
                    currentObject.PtList.Add(ptList[i]);
                    objetFondEnCours = true;
                }
                //On termine un objet de fond sur un front descendant de distance
                if (ptList[i].Distance - ptList[i - 1].Distance < -0.15 * zoomCoeff && objetFondEnCours)
                {
                    objetFondEnCours = false;
                    if (currentObject.PtList.Count > 20)
                    {
                        currentObject.ExtractObjectAttributes();
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

        private List<LidarDetectedObject> DetectionObjetsSaillants(List<PolarPoint> ptList, double zoomCoeff)
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
                if ((ptList[i].Distance - ptList[i - 1].Distance > 0.15 * zoomCoeff) && objetSaillantEnCours)
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

        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarProcessedEvent;
        public virtual void OnLidarProcessed(int id, List<PolarPoint> ptList)
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
        public List<PolarPoint> PtList;
        public List<double> XList;
        public List<double> YList;
        public double Largeur;
        public double DistanceMoyenne;


        public LidarDetectedObject()
        {
            PtList = new List<PolarPoint>();
        }
        public void ExtractObjectAttributes()
        {
            if (PtList.Count > 0)
            {
                //DistanceMoyenne = DistanceList.Average();
                //Largeur = (AngleList.Max() - AngleList.Min()) * DistanceMoyenne;
            }


        }
    }
}

