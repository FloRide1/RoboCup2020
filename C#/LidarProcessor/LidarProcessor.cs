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
            List<double> AngleListProcessed = new List<double>();
            List<double> DistanceListProcessed = new List<double>();

            List<LidarDetectedObject> LidarObjectList = new List<LidarDetectedObject>();
            LidarDetectedObject currentObject = new LidarDetectedObject();

            //A enlever une fois le debug terminé
            for (int i = 1; i < angleList.Count; i++)
            {
                distanceList[i] *= 2;
            }
            
            //Segmentation en objets cohérents
            for (int i = 1; i < angleList.Count; i++)
            {
                if (Math.Abs(distanceList[i] - distanceList[i - 1]) < 0.02)
                {
                    //On reste sur le même objet
                    currentObject.AngleList.Add(angleList[i]);
                    currentObject.DistanceList.Add(distanceList[i]);
                }
                else
                {
                    if ((distanceList[i] - distanceList[i - 1]) > 0.02)
                    {
                        //On a front montant de distance, un objet saillant se termine
                        ExtractObjectAttributes(currentObject);
                        if (currentObject.AngleList.Count > 5)
                            LidarObjectList.Add(currentObject);
                    }
                    currentObject = new LidarDetectedObject();
                    currentObject.AngleList.Add(angleList[i]);
                    currentObject.DistanceList.Add(distanceList[i]);
                }
            }

            List<PolarPointListExtended> objectList = new List<PolarPointListExtended>();
            PolarPointListExtended currentPolarPointListExtended = new PolarPointListExtended();

            foreach (var obj in LidarObjectList)
            {
                currentPolarPointListExtended = new PolarPointListExtended();
                currentPolarPointListExtended.polarPointList = new List<PolarPoint>();
                //if (obj.Largeur > 0.05 && obj.Largeur < 0.5)
                {                    
                    for (int i = 0; i < obj.AngleList.Count; i++)
                    {
                        currentPolarPointListExtended.polarPointList.Add(new PolarPoint(obj.DistanceList[i], obj.AngleList[i]));
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

