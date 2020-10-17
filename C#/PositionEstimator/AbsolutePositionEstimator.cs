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
            PointD ptBalise1Theorique = new PointD(-1.55, -0.95); //Pt balise droite coté départ
            PointD ptBalise2Theorique = new PointD(1.55, 0); //Pt balise centre opposé départ
            PointD ptBalise3Theorique = new PointD(-1.55, 0.95); //Pt balise gauche coté départ
            double normVector12Theorique = Toolbox.Distance(ptBalise1Theorique, ptBalise2Theorique);
            double normVector13Theorique = Toolbox.Distance(ptBalise1Theorique, ptBalise3Theorique);
            double angleVector12Vector13Theorique = Math.Atan2(ptBalise3Theorique.Y - ptBalise1Theorique.Y, ptBalise3Theorique.X - ptBalise1Theorique.X) 
                - Math.Atan2(ptBalise2Theorique.Y - ptBalise1Theorique.Y, ptBalise2Theorique.X - ptBalise1Theorique.X);
            angleVector12Vector13Theorique = Toolbox.Modulo2PiAngleRad(angleVector12Vector13Theorique);

            double minScore = double.PositiveInfinity;
            int iSelected = 0;
            int jSelected = 0;
            int kSelected  = 0;
            PointD ptBalise1 = new PointD(0, 0);
            PointD ptBalise3 = new PointD(0, 0);

            double tolerancePositionnement = 0.2;

            if (listeBalisesPotentielle.Count() >= 3)
            {
                //On calcule toutes les combinaisons de deux vecteurs possibles 
                //à partir des points candidats à être des balises, et on regarde qu'elle est leur 
                //distance par rapport aux vecteurs balises théoriques
                for (int i = 0; i < listeBalisesPotentielle.Count(); i++) //Identifiant 1
                {
                    PointD pt1 = new PointD(listeBalisesPotentielle[i].XMoyen, listeBalisesPotentielle[i].YMoyen);
                    for (int j = 0; j < listeBalisesPotentielle.Count(); j++) //Identifiant 2
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
                                                        
                            if (ScoreCandidatureBalises < minScore)
                            {
                                minScore = ScoreCandidatureBalises;
                                iSelected = i;
                                jSelected = j;
                                kSelected = k;
                            }
                        }
                    }
                }

                //Si l'indicateur de fiabilité de mesure est suffisant, on évalue la position du robot
                if (minScore <= tolerancePositionnement)
                {
                    //On a identifé le trio de balises correspondant au terrain réel, 
                    //on calcule à présent les coordonnées du robot dans le repère des balises théoriques.
                    ptBalise1 = new PointD(listeBalisesPotentielle[iSelected].XMoyen, listeBalisesPotentielle[iSelected].YMoyen);
                    ptBalise3 = new PointD(listeBalisesPotentielle[kSelected].XMoyen, listeBalisesPotentielle[kSelected].YMoyen);                    
                }
            }

            //Dans le cas où le score optimal est mauvais, on se positionne uniquement avec deux balises.
            if (minScore > tolerancePositionnement && listeBalisesPotentielle.Count >= 2)
            {
                minScore = double.PositiveInfinity;
                for (int i = 0; i < listeBalisesPotentielle.Count(); i++) //Identifiant 1
                {
                    PointD pt1 = new PointD(listeBalisesPotentielle[i].XMoyen, listeBalisesPotentielle[i].YMoyen);
                    for (int k = 0; k < listeBalisesPotentielle.Count(); k++) //Identifiant 2
                    {
                        PointD pt3 = new PointD(listeBalisesPotentielle[k].XMoyen, listeBalisesPotentielle[k].YMoyen);
                        double normVector13 = Toolbox.Distance(pt1, pt3);
                        double ScoreCandidatureBalises = Math.Abs(normVector13 - normVector13Theorique) / normVector13Theorique;

                        //On ajoute le fait que le robot doive se trouver dans le bon demi plan, ou l'angle 13-1R est entre -Pi/2 et 0
                        double angleVector13Vector1Robot = Math.Atan2(0 - pt1.Y, 0 - pt1.X) - Math.Atan2(pt3.Y - pt1.Y, pt3.X - pt1.X);
                        if ((angleVector13Vector1Robot < 0) && (angleVector13Vector1Robot > -Math.PI / 2))
                            ;//On est ok, on ne pénalise pas
                        else
                            ScoreCandidatureBalises += 1;


                        if (ScoreCandidatureBalises < minScore)
                        {
                            minScore = ScoreCandidatureBalises;
                            iSelected = i;
                            kSelected = k;
                        }
                    }
                }

                //Si l'indicateur de fiabilité de mesure est suffisant, on évalue la position du robot
                if (minScore <= tolerancePositionnement) //0.2 indicativement... tolérance de 20% d'erreur sur le critère choisi
                {
                    //On a identifé le trio de balises correspondant au terrain réel, 
                    ptBalise1 = new PointD(listeBalisesPotentielle[iSelected].XMoyen, listeBalisesPotentielle[iSelected].YMoyen);
                    ptBalise3 = new PointD(listeBalisesPotentielle[kSelected].XMoyen, listeBalisesPotentielle[kSelected].YMoyen);
                }
            }

            //Si le score de matching du positionnement optimal est ok, 
            //on calcule à présent les coordonnées du robot dans le repère des balises théoriques.
            if (minScore <= tolerancePositionnement)
            {
                double angleVector13Vector1Robot = Math.Atan2(0 - ptBalise1.Y, 0 - ptBalise1.X) - Math.Atan2(ptBalise3.Y - ptBalise1.Y, ptBalise3.X - ptBalise1.X);
                double normVector1Robot = Toolbox.Distance(ptBalise1, new PointD(0, 0));
                double xRobot = ptBalise1Theorique.X + normVector1Robot * Math.Cos(Math.PI / 2 + angleVector13Vector1Robot);
                double yRobot = ptBalise1Theorique.Y + normVector1Robot * Math.Sin(Math.PI / 2 + angleVector13Vector1Robot);
                double angleRobot1ThVectorRobot1 = Math.Atan2(ptBalise1Theorique.Y - yRobot, ptBalise1Theorique.X - xRobot)- Math.Atan2(ptBalise1.Y, ptBalise1.X);

                Console.WriteLine("Position estimée - X : " + xRobot.ToString("N2") + " - Y : " + yRobot.ToString("N2") + " - Theta : " + angleRobot1ThVectorRobot1.ToString("N2"));
                OnPositionCalculatedEvent((float)xRobot, (float)yRobot, (float)angleRobot1ThVectorRobot1, (float)Math.Max(0, 1 - minScore));
            }
        }


        // Event position évaluée
        public event EventHandler<PositionArgs> OnAbsolutePositionCalculatedEvent;
        public virtual void OnPositionCalculatedEvent(float x, float y, float angle, float reliability)
        {
            var handler = OnAbsolutePositionCalculatedEvent;
            if (handler != null)
            {
                handler(this, new PositionArgs { X = x, Y = y, Theta = angle, Reliability = reliability });
            }
        }
        
    }
}
