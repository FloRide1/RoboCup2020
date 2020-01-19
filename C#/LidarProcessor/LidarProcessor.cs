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
                ProcessLidarData(e.AngleList, e.DistanceList);
            }
        }

        void ProcessLidarData(List<double> angleList, List<double> distanceList)
        {
            double zoomCoeff = 1.8;
            List<double> AngleListProcessed = new List<double>();
            List<double> DistanceListProcessed = new List<double>();

            List<LidarDetectedObject> ObjetsSaillantsList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsFondList = new List<LidarDetectedObject>();
            List<LidarDetectedObject> ObjetsPoteauPossible = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject();

            //A enlever une fois le debug terminé
            for (int i = 1; i < angleList.Count; i++)
            {
                distanceList[i] *= zoomCoeff;
            }


            //Préfiltrage des points isolés : un pt dont la distance aux voisin est supérieur à un seuil des deux coté est considere comme isolé.
            double seuilPtIsole = 0.03*zoomCoeff;
            for (int i = 1; i < angleList.Count-1; i++)
            {
                if((Math.Abs(distanceList[i-1]-distanceList[i]) < seuilPtIsole) || (Math.Abs(distanceList[i + 1] - distanceList[i]) < seuilPtIsole))
                {
                    AngleListProcessed.Add(angleList[i]);
                    DistanceListProcessed.Add(distanceList[i]);
                }
            }

            angleList = AngleListProcessed;
            distanceList = DistanceListProcessed;

            //Détection des objets saillants
            bool objetSaillantEnCours = false;
            for (int i = 1; i < angleList.Count; i++)
            {
                //On commence un objet saillant sur un front descendant de distance
                if (distanceList[i] - distanceList[i - 1] < -0.1 * zoomCoeff)
                {
                    currentObject = new LidarDetectedObject();
                    currentObject.AngleList.Add(angleList[i]);
                    currentObject.DistanceList.Add(distanceList[i]);
                    objetSaillantEnCours = true;
                }
                //On termine un objet saillant sur un front montant de distance
                if (distanceList[i] - distanceList[i - 1] > 0.15 * zoomCoeff)
                {
                    ExtractObjectAttributes(currentObject);
                    objetSaillantEnCours = false;
                    if (currentObject.AngleList.Count > 20)
                    {
                        ObjetsSaillantsList.Add(currentObject);
                    }
                }
                //Sinon on reste sur le même objet
                else
                {
                    if (objetSaillantEnCours)
                    {
                        currentObject.AngleList.Add(angleList[i]);
                        currentObject.DistanceList.Add(distanceList[i]);
                    }
                }
            }

            //Détection des objets saillants
            bool objetFondEnCours = false;
            for (int i = 1; i < angleList.Count; i++)
            {
                //On commence un objet de fond sur un front montant de distance
                if (distanceList[i] - distanceList[i - 1] > 0.1 * zoomCoeff)
                {
                    currentObject = new LidarDetectedObject();
                    currentObject.AngleList.Add(angleList[i]);
                    currentObject.DistanceList.Add(distanceList[i]);
                    objetFondEnCours = true;
                }
                //On termine un objet de fond sur un front descendant de distance
                if (distanceList[i] - distanceList[i - 1] < -0.15 * zoomCoeff)
                {
                    ExtractObjectAttributes(currentObject);
                    objetFondEnCours = false;
                    if (currentObject.AngleList.Count > 20)
                    {
                        ObjetsFondList.Add(currentObject);
                    }
                }
                //Sinon on reste sur le même objet
                else
                {
                    if (objetFondEnCours)
                    {
                        currentObject.AngleList.Add(angleList[i]);
                        currentObject.DistanceList.Add(distanceList[i]);
                    }
                }
            }

            foreach(var obj in ObjetsSaillantsList)
            {
                if(Math.Abs(obj.Largeur-0.125*zoomCoeff)<0.1*zoomCoeff)
                {
                    ObjetsPoteauPossible.Add(obj);
                }
            }

            List<PolarPointListExtended> objectList = new List<PolarPointListExtended>();
            foreach (var obj in ObjetsSaillantsList)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPoint>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {                    
                    for (int i = 0; i < obj.AngleList.Count; i++)
                    {
                        currentPolarPointListExtended.polarPointList.Add(new PolarPoint(obj.DistanceList[i], obj.AngleList[i]));
                        currentPolarPointListExtended.displayColor = System.Drawing.Color.Red;
                        currentPolarPointListExtended.displayWidth = 8;
                    }
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            foreach (var obj in ObjetsPoteauPossible)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPoint>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    for (int i = 0; i < obj.AngleList.Count; i++)
                    {
                        currentPolarPointListExtended.polarPointList.Add(new PolarPoint(obj.DistanceList[i], obj.AngleList[i]));
                        currentPolarPointListExtended.displayColor = System.Drawing.Color.Yellow;
                        currentPolarPointListExtended.displayWidth = 3;
                    }
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            foreach (var obj in ObjetsFondList)
            {
                PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPoint>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {
                    for (int i = 0; i < obj.AngleList.Count; i++)
                    {
                        currentPolarPointListExtended.polarPointList.Add(new PolarPoint(obj.DistanceList[i], obj.AngleList[i]));
                        currentPolarPointListExtended.displayColor = System.Drawing.Color.Blue;
                        currentPolarPointListExtended.displayWidth = 6;
                    }
                    objectList.Add(currentPolarPointListExtended);
                }
            }
            //OnLidarProcessed(robotId, AngleListProcessed, DistanceListProcessed);

            OnLidarProcessed(robotId, angleList, distanceList);
            OnLidarObjectProcessed(robotId, objectList);

        }

        public void ExtractObjectAttributes(LidarDetectedObject obj)
        {
            if (obj.AngleList.Count > 0)
            {
                obj.DistanceMoyenne = obj.DistanceList.Average();
                obj.Largeur = (obj.AngleList.Max() - obj.AngleList.Min()) * obj.DistanceMoyenne;
            }
        }

        public delegate void SimulatedLidarEventHandler(object sender, RawLidarArgs e);
        public event EventHandler<RawLidarArgs> OnLidarProcessedEvent;
        public virtual void OnLidarProcessed(int id, List<double> angleList, List<double> distanceList)
        {
            var handler = OnLidarProcessedEvent;
            if (handler != null)
            {
                handler(this, new RawLidarArgs { RobotId = id, AngleList = angleList, DistanceList = distanceList });
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
        public List<double> AngleList;
        public List<double> DistanceList;
        public double Largeur;
        public double DistanceMoyenne;

        public LidarDetectedObject()
        {
            AngleList = new List<double>();
            DistanceList = new List<double>();
        }
    }
}

