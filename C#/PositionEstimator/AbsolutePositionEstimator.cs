using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EventArgsLibrary;
using LidarProcessor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace PositionEstimator
{
    public class AbsolutePositionEstimator
    {
        int robotId = 0;
        List<PolarPointRssi> LidarPtList;

        public AbsolutePositionEstimator(int id)
        {
            robotId = id;
        }

        public void OnRawLidarDataReceived(object sender, RawLidarArgs e)
        {
            LidarPtList = e.PtList;
        }

        public void AbsolutePositionEvaluation(object sender, BitmapImageArgs e)
        {

        }
        
        public void OnLidarBalisesListExtractedEvent(object sender, LidarDetectedObjectListArgs e)
        {
            //Liste de balises potentielles
            var listeBalisesPotentielle = e.LidarObjectList;

            //Normes et angles théoriques
            PointD pt1Theorique = new PointD(-1.55, -0.95); //Pt balise droite coté départ
            PointD pt2Theorique = new PointD(1.55, 0); //Pt balise centre opposé départ
            PointD pt3Theorique = new PointD(-1.55, 0.95); //Pt balise gauche coté départ
            double normVector12Theorique = Toolbox.Distance(pt1Theorique, pt2Theorique);
            double normVector13Theorique = Toolbox.Distance(pt1Theorique, pt3Theorique);
            double angleVector12Vector13Theorique = Math.Atan2(pt3Theorique.Y - pt1Theorique.Y, pt3Theorique.X - pt1Theorique.X) 
                - Math.Atan2(pt2Theorique.Y - pt1Theorique.Y, pt2Theorique.X - pt1Theorique.X);
            angleVector12Vector13Theorique = Toolbox.Modulo2PiAngleRad(angleVector12Vector13Theorique);

            double minScore = double.PositiveInfinity;
            int iSelected = 0;
            int jSelected = 0;
            int kSelected  = 0;

            //On calcule toutes les combinaisons de deux vecteurs possibles 
            //à partir des points candidats à être des balises, et on regarde qu'elle est leur 
            //distance par rapport aux vecteurs balises théoriques
            for (int i = 0; i< listeBalisesPotentielle.Count(); i++) //Identifiant 1
            {
                PointD pt1 = new PointD(listeBalisesPotentielle[i].XMoyen, listeBalisesPotentielle[i].YMoyen);
                for (int j=0; j< listeBalisesPotentielle.Count(); j++) //Identifiant 2
                {
                    PointD pt2 = new PointD(listeBalisesPotentielle[j].XMoyen, listeBalisesPotentielle[j].YMoyen);
                    for (int k = 0; k < listeBalisesPotentielle.Count(); k++) //Identifiant 3
                    {
                        PointD pt3 = new PointD(listeBalisesPotentielle[k].XMoyen, listeBalisesPotentielle[k].YMoyen);
                        double normVector12 = Toolbox.Distance(pt1, pt2);
                        double normVector13 = Toolbox.Distance(pt1, pt3);
                        double angleVector12Vector13 = Math.Atan2(pt3.Y - pt1.Y, pt3.X - pt1.X) - Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X);

                        double ScoreCandidatureBalises = Math.Abs(normVector12 - normVector12Theorique) / normVector12Theorique
                            + Math.Abs(normVector13 - normVector13Theorique) / normVector13Theorique
                            + Math.Abs(Toolbox.ModuloByAngle(angleVector12Vector13Theorique, angleVector12Vector13) - angleVector12Vector13Theorique) / Math.PI;
                        if(ScoreCandidatureBalises<minScore)
                        {
                            minScore = ScoreCandidatureBalises;
                            iSelected = i;
                            jSelected = j;
                            kSelected = k;
                        }
                    }
                }
            }

            //On a identifé le trio de balises correspondant au terrain réel, 
            //on calcule à présent les coordonnées du robot dans le repère des balises théoriques.
        }


        // Event position dans l'image calculée
        public event EventHandler<PositionArgs> PositionEvent;
        public virtual void OnPositionCalculatedEvent(float x, float y, float angle, float reliability)
        {
            var handler = PositionEvent;
            if (handler != null)
            {
                handler(this, new PositionArgs { X = x, Y = y, Theta = angle, Reliability = reliability });
            }
        }

        // Event image postprocessée
        public event EventHandler<OpenCvMatImageArgs> OnOpenCvMatImageProcessedEvent;

        public virtual void OnOpenCvMatImageProcessedReady(Mat mat, string descriptor)
        {
            var handler = OnOpenCvMatImageProcessedEvent;
            if (handler != null)
            {
                handler(this, new OpenCvMatImageArgs { Mat = mat, Descriptor = descriptor });
            }
        }

        public event EventHandler<BitmapImageArgs> OnBitmapImageProcessedEvent;
        public virtual void OnBitmapImageProcessedReady(Bitmap image, string descriptor)
        {

            var handler = OnBitmapImageProcessedEvent;
            if (handler != null)
            {
                handler(this, new BitmapImageArgs { Bitmap = image, Descriptor = descriptor });
            }
        }
    }
}
