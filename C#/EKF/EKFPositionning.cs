using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventArgsLibrary;
using Utilities;
using MathNet.Numerics.LinearAlgebra.Complex;
using Constants;

namespace EKF
{
    public class EKFPositionning
    {
        //Paramètres à regler
        bool wantToClean = false;

        int init_de_P = 1000000000;
        private double tEch = 0.02;
        private double fEch = 50;
        int nb_total_landmarks = 4;
        double anglePerceptionRobot = Math.PI;


        #region variables 
        List<PointD> liste_landmarks = new List<PointD> { }; //pour odo
        int robotId = (int)TeamId.Team1 + (int)RobotId.Robot1;
        int nb_ld_deja_vus = 0;

        List<int> list_indice_landmarks;

        private double[] MatrixX;
        private double[,] XPredUpdate;
        private double[,] MatrixP;
        private double[,] MatrixDelta;
        private double[,] MatrixR;
        private double[,] MatrixQ;
        private double[,] MatrixFx;
        private double[,] MatrixZ;
        private double[,] MatrixHi;
        private double[,] MatrixKi;
        private double[,] MatrixParentheses;
        private double[,] MatrixIdentity;
        private double[,] PPredUpdate;
        double xld;
        double yld;
        double currentGpsXRefTerrain = 0;
        double currentGpsYRefTerrain = 0;
        double currentGpsTheta = 0;
        double currentOdoVxRefRobot = 0;
        double currentOdoVyRefRobot = 0;
        double currentOdoVxRefTerrain = 0;
        double currentOdoVyRefTerrain = 0;
        double currentOdoVtheta = 0;

        Location PosInitRobot;

        Location EKFLocationRefTerrain = new Location(0, 0, 0, 0, 0, 0);

        #endregion variables

        #region Init

        public EKFPositionning(LocationArgs Init)
        {
            PosInitRobot = Init.Location;

            robotId = Init.RobotId;
            MatrixX = new double[3+ 2* nb_total_landmarks];
            XPredUpdate = new double[3 + 2 * nb_total_landmarks,1];
            MatrixP = new double[3+ 2*nb_total_landmarks, 3+ 2*nb_total_landmarks];
            for (int i = 3; i< 3 + 2 * nb_total_landmarks; i++) { MatrixP[i, i] = init_de_P; }

            currentGpsXRefTerrain = MatrixX[0] = Init.Location.X;
            currentGpsYRefTerrain = MatrixX[1] = Init.Location.Y;
            currentGpsTheta = MatrixX[2] = Init.Location.Theta;

            currentOdoVxRefRobot = Init.Location.Vx;
            currentOdoVyRefRobot = Init.Location.Vy;
            currentOdoVtheta = Init.Location.Vtheta;

            InitEKF(robotId, fEch);

        }
        //initialisation de l'ekf quand ce programme est appelé pour la première fois 
        public void InitEKF(int id, double freqEchOdometry)
        {                                                                                                       // Ici on doit initialiser MatrixDelta, R et Q et les trucs qui ne changeront pas 
            robotId = id;
            fEch = freqEchOdometry;
            tEch = 1 / freqEchOdometry;

            MatrixZ = new double[2, 1];
            MatrixParentheses = new double[2, 2];

            MatrixQ = new double[2, 2];
            MatrixQ[0, 0] = 0.012;                                                               //incertitude du lidar
            MatrixQ[1, 1] = 0.014 * 2 * Math.PI / 360;

            MatrixR = new double[3, 3];
            MatrixR[0, 0] = MatrixR[1, 1] = 0.01;                                               //incertitudes odo 
            MatrixR[2, 2] = 0.2 * Math.PI / 360;

            MatrixDelta = new double[2, 1];

            MatrixHi = new double[2, 5];

            MatrixIdentity = new double[3 + 2 * nb_total_landmarks, 3 + 2 * nb_total_landmarks];
            for (int i = 0; i < 3 + 2 * nb_total_landmarks; i++)
                MatrixIdentity[i, i] = 1;
        }

        #endregion

        #region Cleaning Weard Landmarks

        //public List<int> CleanXestFromWeardLandmarks(double[,] X)
        //{
        //    List<double> liste_matrice = new List<double> { };   //on connait pas encore la dim de sortie donc on fait une liste 

        //    List<int> List_indices = new List<int> { }; // ici une liste des indices à enlever de P ensuite 

        //    for (int i = 0; i < 3; i++)
        //    {
        //        liste_matrice.Add(X[i, 0]);                           //pas de critère sur la position
        //    }

        //    for (int i = 3; i < X.Length; i++)
        //    {
        //        if ((X[i, 0] < -(LongueurTerrain + 0.5)) | (X[i, 0] > LongueurTerrain + 0.5))
        //        {
        //            if (i % 2 == 1) //impair => x
        //            {
        //                List_indices.Add(i);
        //                i++; // en faisant ca on skip le y si le x est >2 ou <-2
        //            }
        //            else
        //            {
        //                liste_matrice.RemoveAt(liste_matrice.Count() - 1); // si c'est le y qui est >2 ou <-2 on supprime le x de la liste
        //                List_indices.Add(i - 1);
        //            }
        //        }
        //        else
        //            liste_matrice.Add(X[i, 0]);
        //    }

        //    double[,] matrice_sortie = new double[liste_matrice.Count, 1];

        //    for (int valeur = 0; valeur < liste_matrice.Count; valeur++)
        //        matrice_sortie[valeur, 0] = liste_matrice[valeur];

        //    Matrixxest = matrice_sortie;

        //    return List_indices;
        //}
        //public void CleanPestFromWeardLandmarks(List<int> AEnleverDePest)
        //{
        //    if (AEnleverDePest.Count != 0)
        //    {
        //        matrice_apres = new double[(int)Math.Sqrt(Matrixpest.Length) - 2 * AEnleverDePest.Count, (int)Math.Sqrt(Matrixpest.Length) - 2 * AEnleverDePest.Count];

        //        for (int row = 0; row < 3; row++)
        //        {
        //            for (int column = 0; column < 3; column++) { matrice_apres[row, column] = Matrixpest[row, column]; }
        //        }

        //        for (int indice = 3; indice < (int)Math.Sqrt(matrice_apres.Length); indice += 2)
        //        {
        //            bool AEnlever = false;

        //            for (int i = 0; i < AEnleverDePest.Count; i++)
        //            {
        //                if (indice == AEnleverDePest[i]) { AEnlever = true; }
        //                else { AEnlever = false; }
        //            }  //savoir si on doit enlever cet indice ou pas 

        //            if (!AEnlever)
        //            {
        //                matrice_apres[0, indice] = Matrixpest[0, indice]; //k
        //                matrice_apres[0, indice + 1] = Matrixpest[0, indice + 1]; //l
        //                matrice_apres[1, indice] = Matrixpest[1, indice]; //m
        //                matrice_apres[1, indice + 1] = Matrixpest[1, indice + 1]; //n
        //                matrice_apres[2, indice] = Matrixpest[2, indice]; //o
        //                matrice_apres[2, indice + 1] = Matrixpest[2, indice + 1]; //p

        //                matrice_apres[indice, 0] = Matrixpest[indice, 0]; //q
        //                matrice_apres[indice, 1] = Matrixpest[indice, 1]; //r
        //                matrice_apres[indice, 2] = Matrixpest[indice, 2]; //s
        //                matrice_apres[indice + 1, 0] = Matrixpest[indice + 1, 0]; //v
        //                matrice_apres[indice + 1, 1] = Matrixpest[indice + 1, 1]; //w
        //                matrice_apres[indice + 1, 2] = Matrixpest[indice + 1, 2]; //x

        //                matrice_apres[indice, indice] = Matrixpest[indice, indice]; //t
        //                matrice_apres[indice, indice + 1] = Matrixpest[indice, indice + 1]; //u
        //                matrice_apres[indice + 1, indice] = Matrixpest[indice + 1, indice]; //y
        //                matrice_apres[indice + 1, indice + 1] = Matrixpest[indice + 1, indice + 1]; //z
        //            }
        //        }
        //        Matrixpest = matrice_apres;
        //    } // pas la peine de reflechir, si la liste est vide on a rien a faire 
        //}
        //public List<int> CleanListIndices(List<int> AEnlever, List<int> ListIndices)
        //{
        //    foreach (int indice_a_enlever in AEnlever)
        //    {
        //        for (int indice_en_cours = 0; indice_en_cours < ListIndices.Count; indice_en_cours++)
        //        {
        //            int indice_dans_list_indices = ListIndices[indice_en_cours];
        //            if (indice_dans_list_indices == indice_a_enlever)
        //            {
        //                for (int i = 0; i < ListIndices.Count; i++)
        //                    if (ListIndices[i] > ListIndices[indice_en_cours]) { ListIndices[i] -= 2; }
        //                ListIndices.RemoveAt(indice_en_cours);
        //                indice_en_cours -= 1;
        //            }


        //        }
        //    }

        //    return ListIndices;
        //}

        #endregion  //A FAIRE VOIR SI ON VEUT TJR DE CA 

        public List<int> acceuil_landmarks(List<List<double>> list_ld_recus) //ref robot
        {
            List<int> list_index = new List<int> { };
            bool ld_identifié = false;
            for (int landmark = 0; landmark < list_ld_recus.Count; landmark++)
            {
                xld = ConversionLdRefTerrain(list_ld_recus[landmark])[0]; // ref terrain pour stocker dans x
                yld = ConversionLdRefTerrain(list_ld_recus[landmark])[1];
                int indice_en_cours = 0;
                while ((!ld_identifié) & (3 + 2 * indice_en_cours < MatrixX.Length))
                {
                    double x = MatrixX[3 + 2 * indice_en_cours];
                    double y = MatrixX[4 + 2 * indice_en_cours];
                    if (Math.Sqrt((x - xld) * (x - xld) + (y - yld) * (y - yld)) < 0.4) // distance entre deux landmarks distinct : 40 cm 
                    {
                        list_index.Add(2 * indice_en_cours + 3);
                        ld_identifié = true;
                    }
                    else
                    {
                        indice_en_cours += 1;
                    }
                } // on parcourt la liste des ld qu'on connait et s'il y a un ld à moins de 40 cm de ce qu'on a reçu on dit que c'est le même 
                if (!ld_identifié)
                {
                    MatrixX[3 + 2 * nb_ld_deja_vus] = xld;
                    MatrixX[4 + 2 * nb_ld_deja_vus] = yld;

                    XPredUpdate[3 + 2 * nb_ld_deja_vus, 0] = xld;
                    XPredUpdate[4 + 2 * nb_ld_deja_vus, 0] = yld;
                    PPredUpdate[3 + 2 * nb_ld_deja_vus, 3 + 2 * nb_ld_deja_vus] = init_de_P;
                    PPredUpdate[4 + 2 * nb_ld_deja_vus, 4 + 2 * nb_ld_deja_vus] = init_de_P;

                    list_index.Add(3+2*(nb_ld_deja_vus));
                    nb_ld_deja_vus += 1;
                } //c'est la 1ere fois qu'on le voit 
                ld_identifié = false;
            }
            return list_index; // x ref terrain
        }

        #region Conversion Reférentiels 

        public List<double> ConversionLdRefTerrain(List<double> LdEntrée)
        {
            double xldRefRobot = LdEntrée[0];
            double thetaldRefRobot = LdEntrée[1];

            double xldRefTerrain = xldRefRobot * Math.Cos(thetaldRefRobot + MatrixX[2]) + MatrixX[0];
            double yldRefTerrain = xldRefRobot * Math.Sin(thetaldRefRobot + MatrixX[2]) + MatrixX[1];


            return new List<double> { xldRefTerrain, yldRefTerrain };
        }
        
        #endregion

        #region events
        //Inputs events
        public void OnOdoReceived(object sender, LocationArgs e)
        {
            currentOdoVxRefRobot = e.Location.Vx;
            currentOdoVyRefRobot = e.Location.Vy;
            currentOdoVtheta = e.Location.Vtheta;
            currentOdoVxRefTerrain = currentOdoVxRefRobot * Math.Cos(MatrixX[2]) - currentOdoVyRefRobot * Math.Sin(MatrixX[2]);
            currentOdoVyRefTerrain = currentOdoVxRefRobot * Math.Sin(MatrixX[2]) + currentOdoVyRefRobot * Math.Cos(MatrixX[2]);
            
        }
        public void OnLandmarksReceived(object sender, PointDExtendedListArgs e)
        {
            if (robotId == e.RobotId)
            {
                
                if (MatrixX[2] < -Math.PI) { MatrixX[2] += 2 * Math.PI; }
                else if (MatrixX[2]>= Math.PI) { MatrixX[2] -= 2 * Math.PI; } 
                
                #region reception et stockage des ld 

                liste_landmarks.Clear();

                foreach (PointDExtended Point in e.LandmarkList)
                {
                    liste_landmarks.Add(Point.Pt);
                }

                List<List<double>> landmarks = liste_landmarks.Select(l => new List<double>(2) { l.X, l.Y }).ToList(); //on commence par mettre les ld en liste de liste
                #endregion

                //prediction step  

                #region state prediction

                XPredUpdate[0,0] = MatrixX[0] + currentOdoVxRefRobot * tEch * Math.Cos(MatrixX[2]) - currentOdoVyRefRobot * tEch * Math.Sin(MatrixX[2]);
                XPredUpdate[1,0] = MatrixX[1] + currentOdoVxRefRobot * tEch * Math.Sin(MatrixX[2]) + currentOdoVyRefRobot * tEch * Math.Cos(MatrixX[2]);
                XPredUpdate[2,0] = MatrixX[2] + currentOdoVtheta / fEch;

                //if (XPredUpdate[2, 0] > Math.PI) { XPredUpdate[2, 0] -= 2 * Math.PI; }

                #endregion

                # region covariance prediction

                PPredUpdate = new double[3 + 2 * nb_total_landmarks, 3 + 2 * nb_total_landmarks]; 

                double[,] Gx = new double[3, 3]; //ca c'est la jacobienne dont on a besoin pour update la position
                Gx[0, 0] = Gx[1, 1] = Gx[2, 2] = 1;
                Gx[0,2]= -(currentOdoVxRefRobot * Math.Sin(XPredUpdate[2,0]) / fEch) - currentOdoVyRefRobot * Math.Cos(XPredUpdate[2,0]) / fEch;
                Gx[1, 2] = (currentOdoVxRefRobot * Math.Cos(XPredUpdate[2,0]) / fEch) - currentOdoVyRefRobot * Math.Sin(XPredUpdate[2,0]) / fEch;

                double[,] Pup = new double[3, 2 * nb_ld_deja_vus]; 

                for (int ligne =0; ligne <3; ligne++)
                {
                    for (int colonne = 3; colonne <3+ 2 * nb_ld_deja_vus ; colonne++)
                    {
                        Pup[ligne, colonne-3] = MatrixP[ligne, colonne];
                    }
                }

                Pup = Toolbox.Multiply(Gx,Pup);

                double[,] Pupleft = new double[3, 3];

                for (int i = 0; i < 3; i++) { for (int j = 0; j < 3; j++) { Pupleft[i, j] = MatrixP[i, j]; } }

                Pupleft = Toolbox.Addition_Matrices(Toolbox.Multiply(Gx,Toolbox.Multiply(Pupleft,Toolbox.Transpose(Gx))),MatrixR);

                for (int i = 0; i < 3; i++) { for (int j = 0; j < 3; j++) { PPredUpdate[i, j] = Pupleft[i, j]; } } //actualise Pupleft

                for (int ligne = 0; ligne < 3; ligne++)
                {
                    for (int colonne = 3; colonne < 3+2 * nb_ld_deja_vus; colonne++)
                    {
                        PPredUpdate[ligne, colonne] = PPredUpdate[colonne, ligne] = Pup[ligne, (colonne-3)];
                    }
                } // actualise Pup et Pleft


                #endregion

                list_indice_landmarks = acceuil_landmarks(landmarks);
                // Correction step 

                #region Init de Fx
                MatrixFx = new double[5, MatrixX.Length];
                MatrixFx[0, 0] = MatrixFx[1, 1] = MatrixFx[2, 2] = 1;
                #endregion

                //MEGA BOUCLE
                for (int j = 0; j<list_indice_landmarks.Count; j++)
                {

                    #region Calcul de Z

                    double deltax = MatrixX[list_indice_landmarks[j]] - MatrixX[0];
                    double deltay = MatrixX[list_indice_landmarks[j]+1] - MatrixX[1];

                    MatrixDelta[0, 0] = deltax;
                    MatrixDelta[1, 0] = deltay;
                    double q = Toolbox.Multiply(Toolbox.Transpose(MatrixDelta), MatrixDelta)[0, 0];         // c'est un réel car dimension 2*1 fois sa transposée (voir brouillon) 
                    MatrixZ[0, 0] = Math.Sqrt(q);                                                           // Là on à une observation attendue par rapport a la dernière fois ou on a vu le ld 
                    MatrixZ[1, 0] = Math.Atan2(deltay, deltax) - MatrixX[2];

                    if (MatrixZ[1,0] < -anglePerceptionRobot / 2)
                    {
                        MatrixZ[1, 0] += 2 * Math.PI;
                    }
                    else if (MatrixZ[1, 0] > anglePerceptionRobot / 2)
                    {
                        MatrixZ[1, 0] -= 2 * Math.PI;
                    }

                    #endregion

                    MatrixFx[3, list_indice_landmarks[j]] = MatrixFx[4, list_indice_landmarks[j]+1] = 1; //calcul du Fx pour ce ld

                    #region Calcul de H
                    MatrixHi = new double[2, 5];

                    MatrixHi[0, 0] = -deltax / Math.Sqrt(q);
                    MatrixHi[0, 1] = -deltay / Math.Sqrt(q);
                    MatrixHi[0, 2] = 0;
                    MatrixHi[0, 3] = deltax / Math.Sqrt(q);
                    MatrixHi[0, 4] = deltay / Math.Sqrt(q);
                    MatrixHi[1, 0] = deltay / q;
                    MatrixHi[1, 1] = -deltax / q;
                    MatrixHi[1, 2] = -1;
                    MatrixHi[1, 3] = -deltay / q;
                    MatrixHi[1, 4] = deltax / q;

                    MatrixHi =Toolbox.Multiply(MatrixHi, MatrixFx); 
                    #endregion

                    #region Calcul de K

                    MatrixParentheses = Toolbox.Multiply(MatrixHi, Toolbox.Multiply(PPredUpdate, Toolbox.Transpose(MatrixHi)));

                    MatrixParentheses = Toolbox.Addition_Matrices(MatrixParentheses, MatrixQ);  //On ajoute Q à la parenthèse   

                    MatrixParentheses = Toolbox.Inverse(MatrixParentheses);                     //on fais l'inverse de la parenthèses

                    MatrixKi = Toolbox.Multiply(PPredUpdate, Toolbox.Multiply(Toolbox.Transpose(MatrixHi), MatrixParentheses)); //On trouve enfin Ki

                    #endregion

                    MatrixZ[0, 0] = landmarks[j][0]-MatrixZ[0, 0];
                    MatrixZ[1, 0] = landmarks[j][1]-MatrixZ[1, 0];

                    if (MatrixZ[1,0] > 2 * Math.PI)
                    {
                        MatrixX[2] -= 2 * Math.PI;
                        MatrixZ[1, 0] -= 2 * Math.PI;
                    }

                    XPredUpdate = Toolbox.Addition_Matrices(XPredUpdate,Toolbox.Multiply(MatrixKi,MatrixZ));

                    #region Calculs pour Ppred
                    double[,] MatrixTempo = Toolbox.Multiply(MatrixKi,MatrixHi);
                    
                    for (int ligne = 0; ligne < MatrixTempo.GetLength(0); ligne++)
                    {
                        for(int colonne = 0; colonne <MatrixTempo.GetLength(1); colonne++)
                        {
                            MatrixTempo[ligne, colonne] = -MatrixTempo[ligne, colonne]; 
                        }
                    } //A FAIRE voir si tu peux pas simplifier ca 

                    #endregion

                    PPredUpdate = Toolbox.Multiply(Toolbox.Addition_Matrices(MatrixIdentity,MatrixTempo),PPredUpdate);

                    MatrixFx[3, list_indice_landmarks[j]] = MatrixFx[4, list_indice_landmarks[j]+1] = 0; //Remise du Fx a 0
                }//MEGA BOUCLE

                //X = xpred
                for (int i = 0; i < XPredUpdate.Length; i++)
                {
                    MatrixX[i] = XPredUpdate[i, 0];
                }

                // P = Ppred
                for (int li = 0; li < PPredUpdate.GetLength(0); li++) 
                {
                    for (int col = 0; col< PPredUpdate.GetLength(1); col++)
                    {
                        MatrixP[li, col] = PPredUpdate[li, col];
                    }
                }

                #region Ca part à l'affichage 
                EKFLocationRefTerrain.X = MatrixX[0];

                EKFLocationRefTerrain.Y = MatrixX[1];

                EKFLocationRefTerrain.Theta = MatrixX[2];

                //Attention la location a renvoyer est dans le ref terrain pour les positions et dans le ref robot pour les vitesses
                double EKFLocationRefRobotVx = currentOdoVxRefRobot; // du coup on retourner direct l'odo pour les vitesses
                double EKFLocationRefRobotVy = currentOdoVyRefRobot;

                Location EKFOutputLocation = new Location(EKFLocationRefTerrain.X, EKFLocationRefTerrain.Y, EKFLocationRefTerrain.Theta,
                                                            EKFLocationRefRobotVx, EKFLocationRefRobotVy, EKFLocationRefTerrain.Vtheta);

                OnEKFLocation(robotId, EKFOutputLocation, MatrixX); //On balance à l'event les landmarks vus à cet instant et leurs covariances 
                                                                    // NOTE : possible d'afficher tout les ld connus si on veut (MatrixX)
                #endregion
            }
        }                       //Fin de l'algo ! 


        //Output events
        public event EventHandler<PosRobotAndLandmarksArgs> OnEKFLocationEvent;
        public virtual void OnEKFLocation(int id, Location locationRefTerrain, double[] X)
        {
            var handler = OnEKFLocationEvent;

            List<PointDExtended> Liste_Sortie = new List<PointDExtended>();

            if (handler != null)
            {
                foreach(int i in list_indice_landmarks)
                {
                    PointDExtended Ptde = new PointDExtended(new PointD(X[i], X[i + 1]), System.Drawing.Color.Aqua, 5);
                    Liste_Sortie.Add(Ptde);
                }

                handler(this, new PosRobotAndLandmarksArgs { RobotId = id, PosLandmarkList = Liste_Sortie, PosRobot = locationRefTerrain });
            }
        }
        #endregion


    }
}

