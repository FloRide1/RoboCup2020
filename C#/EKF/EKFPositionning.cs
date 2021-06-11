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
        //Paramètres
        bool wantToClean = false;
        bool newPMethode = false; 

        #region variables 
        List<PointD> liste_landmarks = new List<PointD> { }; //pour odo
        int robotId = (int)TeamId.Team1 + (int)RobotId.Robot1;
        double freqEchOdometry = 50;
        int init_de_P = 1000000000;

        private int Appel_pour_la_première_fois = 0;

        int LongueurTerrain = 3; // on met la grande longueur

        private double tEch = 0.02;
        private double fEch = 50;

        private double[] MatrixX;
        private double[,] Matrixxest;
        private double[,] MatrixxPred;
        private double[,] MatrixXiPred;
        private double[,] MatrixP;
        private double[,] MatrixpPred;
        private double[,] MatrixDelta;
        private double[,] Matrixpest;
        private double[,] MatrixPi;
        private double[,] MatrixR;
        private double[,] MatrixQ;
        private double[,] MatrixFx;
        private double[,] MatrixZPred;
        private double[,] MatrixZ;
        private double[,] MatrixG;
        private double[,] MatrixHi;
        private double[,] MatrixKi;
        private double[,] MatrixParentheses;
        private double[,] MatrixKdeltaz;
        private double[] vecteur_apres;
        private double[,] matrice_apres;
        private double[,] MatrixXi;

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

        Location EKFLocationRefTerrain = new Location(0, 0, 0, 0, 0, 0);
        public EKFPositionning(LocationArgs Init)
        {
            robotId = Init.RobotId;
            MatrixX = new double[3];
            MatrixP = new double[3, 3];
            Matrixxest = new double[3, 0];
            Matrixpest = new double[3, 3];

            currentGpsXRefTerrain = MatrixX[0] = Init.Location.X;
            currentGpsYRefTerrain = MatrixX[1] = Init.Location.Y;
            currentGpsTheta = MatrixX[2] = Init.Location.Theta;

            currentOdoVxRefRobot = Init.Location.Vx;
            currentOdoVyRefRobot = Init.Location.Vy;
            currentOdoVtheta = Init.Location.Vtheta;

            MatrixP[0, 0] = MatrixP[1, 1] = MatrixP[1, 1] = 0;

            InitEKF(robotId, 50); //ALEX  je fais des petits tests 

        }
        #endregion variables

        #region Cleaning Weard Landmarks
        public List<int> CleanXestFromWeardLandmarks(double[,] X)
        {
            List<double> liste_matrice = new List<double> { };   //on connait pas encore la dim de sortie donc on fait une liste 

            List<int> List_indices = new List<int> { }; // ici une liste des indices à enlever de P ensuite 

            for (int i = 0; i < 3; i++)
            {
                liste_matrice.Add(X[i, 0]);                           //pas de critère sur la position
            }

            for (int i = 3; i < X.Length; i++)
            {
                if ((X[i, 0] < -(LongueurTerrain + 0.5)) | (X[i, 0] > LongueurTerrain + 0.5))
                {
                    if (i % 2 == 1) //impair => x
                    {
                        List_indices.Add(i);
                        i++; // en faisant ca on skip le y si le x est >2 ou <-2
                    }
                    else
                    {
                        liste_matrice.RemoveAt(liste_matrice.Count() - 1); // si c'est le y qui est >2 ou <-2 on supprime le x de la liste
                        List_indices.Add(i - 1);
                    }
                }
                else
                    liste_matrice.Add(X[i, 0]);
            }

            double[,] matrice_sortie = new double[liste_matrice.Count, 1];

            for (int valeur = 0; valeur < liste_matrice.Count; valeur++)
                matrice_sortie[valeur, 0] = liste_matrice[valeur];

            Matrixxest = matrice_sortie;

            return List_indices;
        }
        public void CleanPestFromWeardLandmarks(List<int> AEnleverDePest)
        {
            if (AEnleverDePest.Count != 0)
            {
                matrice_apres = new double[(int)Math.Sqrt(Matrixpest.Length) - 2 * AEnleverDePest.Count, (int)Math.Sqrt(Matrixpest.Length) - 2 * AEnleverDePest.Count];

                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 3; column++) { matrice_apres[row, column] = Matrixpest[row, column]; }
                }

                for (int indice = 3; indice < (int)Math.Sqrt(matrice_apres.Length); indice += 2)
                {
                    bool AEnlever = false;

                    for (int i = 0; i < AEnleverDePest.Count; i++)
                    {
                        if (indice == AEnleverDePest[i]) { AEnlever = true; }
                        else { AEnlever = false; }
                    }  //savoir si on doit enlever cet indice ou pas 

                    if (!AEnlever)
                    {
                        matrice_apres[0, indice] = Matrixpest[0, indice]; //k
                        matrice_apres[0, indice + 1] = Matrixpest[0, indice + 1]; //l
                        matrice_apres[1, indice] = Matrixpest[1, indice]; //m
                        matrice_apres[1, indice + 1] = Matrixpest[1, indice + 1]; //n
                        matrice_apres[2, indice] = Matrixpest[2, indice]; //o
                        matrice_apres[2, indice + 1] = Matrixpest[2, indice + 1]; //p

                        matrice_apres[indice, 0] = Matrixpest[indice, 0]; //q
                        matrice_apres[indice, 1] = Matrixpest[indice, 1]; //r
                        matrice_apres[indice, 2] = Matrixpest[indice, 2]; //s
                        matrice_apres[indice + 1, 0] = Matrixpest[indice + 1, 0]; //v
                        matrice_apres[indice + 1, 1] = Matrixpest[indice + 1, 1]; //w
                        matrice_apres[indice + 1, 2] = Matrixpest[indice + 1, 2]; //x

                        matrice_apres[indice, indice] = Matrixpest[indice, indice]; //t
                        matrice_apres[indice, indice + 1] = Matrixpest[indice, indice + 1]; //u
                        matrice_apres[indice + 1, indice] = Matrixpest[indice + 1, indice]; //y
                        matrice_apres[indice + 1, indice + 1] = Matrixpest[indice + 1, indice + 1]; //z
                    }
                }
                Matrixpest = matrice_apres;
            } // pas la peine de reflechir, si la liste est vide on a rien a faire 
        }
        public List<int> CleanListIndices(List<int> AEnlever, List<int> ListIndices)
        {
            foreach (int indice_a_enlever in AEnlever)
            {
                for (int indice_en_cours = 0; indice_en_cours < ListIndices.Count; indice_en_cours++)
                {
                    int indice_dans_list_indices = ListIndices[indice_en_cours];
                    if (indice_dans_list_indices == indice_a_enlever)
                    {
                        for (int i = 0; i < ListIndices.Count; i++)
                            if (ListIndices[i] > ListIndices[indice_en_cours]) { ListIndices[i] -= 2; }
                        ListIndices.RemoveAt(indice_en_cours);
                        indice_en_cours -= 1;
                    }


                }
            }

            return ListIndices;
        }

        #endregion  // A FAIRE, PRENDRE LES DIM TERRAIN 

        #region Fonctions pour trouver ou remettre des matrices dans d'autres 

        public void Remettre_Pest_Dans_P(List<int> list_indices_dans_P)
        {
            for (int ligne = 0; ligne < 3; ligne++)
            {
                for (int colonne = 0; colonne < 3; colonne++)
                {
                    MatrixP[ligne, colonne] = Matrixpest[ligne, colonne];                                  //ici on rempli de a à i
                }
            }

            for (int indice_pest = 3; indice_pest < 2 * (list_indices_dans_P.Count) + 3; indice_pest += 2)
            {

                MatrixP[0, list_indices_dans_P[(int)((indice_pest - 3) / 2)]] = Matrixpest[0, indice_pest]; //k
                MatrixP[0, list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1] = Matrixpest[0, indice_pest + 1]; //l
                MatrixP[1, list_indices_dans_P[(int)((indice_pest - 3) / 2)]] = Matrixpest[1, indice_pest]; //m
                MatrixP[1, list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1] = Matrixpest[1, indice_pest + 1]; //n
                MatrixP[2, list_indices_dans_P[(int)((indice_pest - 3) / 2)]] = Matrixpest[2, indice_pest]; //o
                MatrixP[2, list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1] = Matrixpest[2, indice_pest + 1]; //p

                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)], 0] = Matrixpest[indice_pest, 0]; //q
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)], 1] = Matrixpest[indice_pest, 1]; //r
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)], 2] = Matrixpest[indice_pest, 2]; //s
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1, 0] = Matrixpest[indice_pest + 1, 0]; //v
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1, 1] = Matrixpest[indice_pest + 1, 1]; //w
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1, 2] = Matrixpest[indice_pest + 1, 2]; //x

                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)], list_indices_dans_P[(int)((indice_pest - 3) / 2)]] = Matrixpest[indice_pest, indice_pest]; //t
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)], list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1] = Matrixpest[indice_pest, indice_pest + 1]; //u
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1, list_indices_dans_P[(int)((indice_pest - 3) / 2)]] = Matrixpest[indice_pest + 1, indice_pest]; //y
                MatrixP[list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1, list_indices_dans_P[(int)((indice_pest - 3) / 2)] + 1] = Matrixpest[indice_pest + 1, indice_pest + 1]; //z
            }

        }
        public void Remettre_Pi_dans_Pest(int num_ld)
        {
            for (int ligne = 0; ligne < 3; ligne++)
            {
                for (int colonne = 0; colonne < 3; colonne++)
                {
                    Matrixpest[ligne, colonne] = MatrixPi[ligne, colonne];                                  //ici on rempli de a à i
                }
            }

            Matrixpest[0, 2 * num_ld + 3] = MatrixPi[0, 3]; //k
            Matrixpest[0, 2 * num_ld + 4] = MatrixPi[0, 4]; //l
            Matrixpest[1, 2 * num_ld + 3] = MatrixPi[1, 3]; //m
            Matrixpest[1, 2 * num_ld + 4] = MatrixPi[1, 4]; //n
            Matrixpest[2, 2 * num_ld + 3] = MatrixPi[2, 3]; //o
            Matrixpest[2, 2 * num_ld + 4] = MatrixPi[2, 4]; //p

            Matrixpest[2 * num_ld + 3, 0] = MatrixPi[3, 0]; //q
            Matrixpest[2 * num_ld + 3, 1] = MatrixPi[3, 1]; //r
            Matrixpest[2 * num_ld + 3, 2] = MatrixPi[3, 2]; //s
            Matrixpest[2 * num_ld + 4, 0] = MatrixPi[4, 0]; //v
            Matrixpest[2 * num_ld + 4, 1] = MatrixPi[4, 1]; //w
            Matrixpest[2 * num_ld + 4, 2] = MatrixPi[4, 2]; //x

            Matrixpest[2 * num_ld + 3, 2 * num_ld + 3] = MatrixPi[3, 3]; //t
            Matrixpest[2 * num_ld + 3, 2 * num_ld + 4] = MatrixPi[3, 4]; //u
            Matrixpest[2 * num_ld + 4, 2 * num_ld + 3] = MatrixPi[4, 3]; //y
            Matrixpest[2 * num_ld + 4, 2 * num_ld + 4] = MatrixPi[4, 4]; //z
        }
        public void Remettre_Xest_Dans_X(List<int> list_indices_dans_X)
        {
            MatrixX[0] = Matrixxest[0, 0];
            MatrixX[1] = Matrixxest[1, 0];
            MatrixX[2] = Matrixxest[2, 0];

            int indiceDansXest = 3;

            foreach (int indiceDansX in list_indices_dans_X)
            {
                MatrixX[indiceDansX] = Matrixxest[indiceDansXest, 0];
                MatrixX[indiceDansX + 1] = Matrixxest[indiceDansXest + 1, 0];
                indiceDansXest += 2;
            }

        }

        public double[,] Trouver_Xi_Dans_Xest(int num_ld)
        {
            double[,] MatrixSortie = new double[5, 1];
            MatrixSortie[0, 0] = Matrixxest[0, 0];
            MatrixSortie[1, 0] = Matrixxest[1, 0];
            MatrixSortie[2, 0] = Matrixxest[2, 0];

            MatrixSortie[3, 0] = Matrixxest[2 * num_ld + 3, 0];
            MatrixSortie[4, 0] = Matrixxest[2 * num_ld + 4, 0];

            return MatrixSortie;
        }
        public double[,] Trouver_Pi_dans_Pest(int num_ld)
        {
            double[,] MatrixSortie = new double[5, 5];

            for (int ligne = 0; ligne < 3; ligne++)
            {
                for (int colonne = 0; colonne < 3; colonne++)
                {
                    MatrixSortie[ligne, colonne] = Matrixpest[ligne, colonne];                                  //ici on rempli de a à i
                }
            }

            MatrixSortie[0, 3] = Matrixpest[0, 2 * num_ld + 3]; //k
            MatrixSortie[0, 4] = Matrixpest[0, 2 * num_ld + 4]; //l
            MatrixSortie[1, 3] = Matrixpest[1, 2 * num_ld + 3]; //m
            MatrixSortie[1, 4] = Matrixpest[1, 2 * num_ld + 4]; //n
            MatrixSortie[2, 3] = Matrixpest[2, 2 * num_ld + 3]; //o
            MatrixSortie[2, 4] = Matrixpest[2, 2 * num_ld + 4]; //p

            MatrixSortie[3, 0] = Matrixpest[2 * num_ld + 3, 0]; //q
            MatrixSortie[3, 1] = Matrixpest[2 * num_ld + 3, 1]; //r
            MatrixSortie[3, 2] = Matrixpest[2 * num_ld + 3, 2]; //s
            MatrixSortie[4, 0] = Matrixpest[2 * num_ld + 4, 0]; //v
            MatrixSortie[4, 1] = Matrixpest[2 * num_ld + 4, 1]; //w
            MatrixSortie[4, 2] = Matrixpest[2 * num_ld + 4, 2]; //x

            MatrixSortie[3, 3] = Matrixpest[2 * num_ld + 3, 2 * num_ld + 3]; //t
            MatrixSortie[3, 4] = Matrixpest[2 * num_ld + 3, 2 * num_ld + 4]; //u
            MatrixSortie[4, 3] = Matrixpest[2 * num_ld + 4, 2 * num_ld + 3]; //y
            MatrixSortie[4, 4] = Matrixpest[2 * num_ld + 4, 2 * num_ld + 4]; //z

            return MatrixSortie;

        }
        public void TrouverXestEtXpredDansX(List<int> Indices, double[] X, List<List<double>> landmarks)
        {
            #region Xest
            double[,] MatrixXestSortie = new double[2 * Indices.Count + 3, 1];
            MatrixXestSortie[0, 0] = X[0];
            MatrixXestSortie[1, 0] = X[1];
            MatrixXestSortie[2, 0] = X[2];
            int indice = 3;

            foreach (int item in Indices)
            {
                MatrixXestSortie[indice, 0] = X[item];
                MatrixXestSortie[indice + 1, 0] = X[item + 1];
                indice += 2;
            }
            Matrixxest = MatrixXestSortie;   // Xest se fait a partir de ce qu'on avait avant 
            #endregion

            #region Xpred
            double[,] MatrixXpredSortie = new double[2 * Indices.Count + 3, 1];
            indice = 3;                                        // la pred se fait avec ce qu'on vient de recevoir 

            foreach (List<double> ld in landmarks)
            {
                MatrixXpredSortie[indice, 0] = ld[0];
                MatrixXpredSortie[indice + 1, 0] = ld[1];
                indice += 2;
            }

            MatrixxPred = MatrixXpredSortie;
            #endregion 
        }

        public void TrouverPestEtPpredDansP(List<int> Indices, double[,] P)
        {
            #region Pest
            double[,] MatrixPestSortie = new double[2 * Indices.Count + 3, 2 * Indices.Count + 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    MatrixPestSortie[i, j] = P[i, j];
                }
            }
            int indice = 3;
            foreach (int item in Indices)
            {

                MatrixPestSortie[0, indice] = P[0, item]; //k
                MatrixPestSortie[0, indice + 1] = P[0, item + 1]; //l
                MatrixPestSortie[1, indice] = P[1, item]; //m
                MatrixPestSortie[1, indice + 1] = P[1, item + 1]; //n
                MatrixPestSortie[2, indice] = P[2, item]; //o
                MatrixPestSortie[2, indice + 1] = P[2, item + 1]; //p
                MatrixPestSortie[indice, 0] = P[item, 0]; //q
                MatrixPestSortie[indice, 1] = P[item, 1]; //r
                MatrixPestSortie[indice, 2] = P[item, 2]; //s
                MatrixPestSortie[indice + 1, 0] = P[item + 1, 0]; //v
                MatrixPestSortie[indice + 1, 1] = P[item + 1, 1]; //w
                MatrixPestSortie[indice + 1, 2] = P[item + 1, 2]; //x
                MatrixPestSortie[indice, indice] = P[item, item]; //t
                MatrixPestSortie[indice, indice + 1] = P[item, item + 1]; //u
                MatrixPestSortie[indice + 1, indice] = P[item + 1, item]; //y
                MatrixPestSortie[indice + 1, indice + 1] = P[item + 1, item + 1]; //z

                indice += 2;
            }

            Matrixpest = MatrixPestSortie;
            #endregion

            #region Ppred 
            double[,] MatrixPpredSortie = new double[2 * Indices.Count + 3, 2 * Indices.Count + 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    MatrixPestSortie[i, j] = P[i, j];
                }
            }

            int indice2 = 3;

            foreach (int item in Indices)
            {
                MatrixPpredSortie[indice2, indice2] = init_de_P; // t
                MatrixPpredSortie[indice2 + 1, indice2 + 1] = init_de_P; //z

                indice += 2;
            }

            MatrixpPred = MatrixPpredSortie; 
            #endregion 
        }
        public double[,] TrouverPestDansP(List<int> List_indices, double[,] P)
        {
            double[,] MatrixSortie = new double[2 * List_indices.Count + 3, 2 * List_indices.Count + 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Matrixpest[i, j] = P[i, j];
                }
            }
            int indice = 3;
            foreach (int item in List_indices)
            {

                MatrixSortie[0, indice] = P[0, item]; //k
                MatrixSortie[0, indice + 1] = P[0, item + 1]; //l
                MatrixSortie[1, indice] = P[1, item]; //m
                MatrixSortie[1, indice + 1] = P[1, item + 1]; //n
                MatrixSortie[2, indice] = P[2, item]; //o
                MatrixSortie[2, indice + 1] = P[2, item + 1]; //p
                MatrixSortie[indice, 0] = P[item, 0];     //q
                MatrixSortie[indice, 1] = P[item, 1];     //r
                MatrixSortie[indice, 2] = P[item, 2];     //s
                MatrixSortie[indice + 1, 0] = P[item + 1, 0]; //v
                MatrixSortie[indice + 1, 1] = P[item + 1, 1]; //w
                MatrixSortie[indice + 1, 2] = P[item + 1, 2]; //x

                MatrixSortie[indice, indice] = P[item, item]; //t
                MatrixSortie[indice, indice + 1] = P[item, item + 1]; //u
                MatrixSortie[indice + 1, indice] = P[item + 1, item]; //y
                MatrixSortie[indice + 1, indice + 1] = P[item + 1, item + 1]; //z

                indice += 2;
            }

            MatrixpPred = MatrixSortie;
            return MatrixSortie;
        }
        public double[,] TrouverXiPredDansXpred(int num_ld)
        {
            double[,] MatrixSortie = new double[5, 1];
            MatrixSortie[0, 0] = MatrixxPred[0, 0];
            MatrixSortie[1, 0] = MatrixxPred[1, 0];
            MatrixSortie[2, 0] = MatrixxPred[2, 0];

            MatrixSortie[3, 0] = MatrixxPred[2 * num_ld + 3, 0];
            MatrixSortie[4, 0] = MatrixxPred[2 * num_ld + 4, 0];

            return MatrixSortie;
        }

        #endregion 

        #region Fonctions pour acceuillir les ld 

        public double[] Ajout_ld_X(double[] vecteur_avant, double valeur, double valeur2)
        {
            vecteur_apres = new double[vecteur_avant.Length + 2];
            for (int i = 0; i < vecteur_avant.Length; i++)
            {
                vecteur_apres[i] = vecteur_avant[i];
            }
            vecteur_apres[vecteur_avant.Length] = valeur;
            vecteur_apres[vecteur_avant.Length + 1] = valeur2;

            return vecteur_apres;
        }
        public double[,] Ajout_ld_P(double[,] P)
        {
            matrice_apres = new double[(int)Math.Sqrt(P.Length) + 2, (int)Math.Sqrt(P.Length) + 2];
            for (int row = 0; row < (int)Math.Sqrt(P.Length); row++)
            {
                for (int column = 0; column < (int)Math.Sqrt(P.Length); column++)
                {
                    matrice_apres[row, column] = P[row, column];
                }
            }
            matrice_apres[(int)Math.Sqrt(P.Length), (int)Math.Sqrt(P.Length)] = init_de_P;                                                                     //Initialisation de P à "l'infini"
            matrice_apres[(int)Math.Sqrt(P.Length) + 1, (int)Math.Sqrt(P.Length) + 1] = init_de_P;

            return matrice_apres;
        }
        public List<int> acceuil_landmarks(List<List<double>> list_ld_recus)
        {
            List<int> list_index = new List<int> { };
            bool ld_identifié = false;
            for (int landmark = 0; landmark < list_ld_recus.Count; landmark++)
            {
                xld = list_ld_recus[landmark][0];
                yld = list_ld_recus[landmark][1];
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
                }
                if (!ld_identifié)
                {
                    MatrixX = Ajout_ld_X(MatrixX, xld, yld);
                    MatrixP = Ajout_ld_P(MatrixP);
                    list_index.Add(MatrixX.Length - 2);
                }
                ld_identifié = false;
            }
            return list_index;
        }

        #endregion Fonctions pour acceuillir les ld 

        //initialisation de l'ekf quand ce programme est appelé pour la première fois 
        public void InitEKF(int id, double freqEchOdometry)
        {                                                                                                       // Ici on doit initialiser MatrixDelta, R et Q et les trucs qui ne changeront pas 
            robotId = id;
            fEch = freqEchOdometry;
            tEch = 1 / freqEchOdometry;

            MatrixR = new double[5, 5];
            MatrixZ = new double[2, 1];
            MatrixZPred = new double[2, 1];
            MatrixParentheses = new double[2, 2];

            MatrixXiPred = new double[5, 1];

            MatrixQ = new double[2, 2];
            MatrixQ[0, 0] = 0.01;                                                               //incertitude odo en cylindriques  0.1 deg et 1 cm
            MatrixQ[1, 1] = 0.2 * Math.PI / 360;

            MatrixR[0, 0] = MatrixR[1, 1] = 0.01;                                               //incertitudes odo puis lidar 
            MatrixR[2, 2] = 0.2 * Math.PI / 360;
            MatrixR[3, 3] = 0.012;
            MatrixR[4, 4] = 0.014 * 2 * Math.PI / 360;

            MatrixDelta = new double[2, 1];

            MatrixHi = new double[2, 5];
            MatrixFx = new double[5, 5];
            MatrixXi = new double[5, 5];

            for (int i = 0; i < 5; i++)
            {
                MatrixFx[i, i] = 1;                                                                             // vu comment j'ai paramétré le truc on a fx identité 
            }

            MatrixG = new double[5, 5];                                                                         //on fait ici l'identité de G zt il ne restera plus que les données odo à prendre 
            for (int i = 0; i < 5; i++)
            {
                MatrixG[i, i] = 1;                                                                              //on fait déja l'identité de G et on fera après l'odo
            }

            MatrixKdeltaz = new double[5, 1];
        }
        // itération de l'ekf
        public void SLAMCorrection(double GPS_Theta, double Odo_VX, double Odo_VY, double Odo_VTheta, int nbre_landmarks, List<List<double>> landmarks_observés)
        {
            MatrixG[0, 2] = -(Odo_VX * Math.Sin(GPS_Theta) / fEch) - Odo_VY * Math.Cos(GPS_Theta) / fEch;         // ici cest la dérivée du modele (xpred)
            MatrixG[1, 2] = (Odo_VX * Math.Cos(GPS_Theta) / fEch) - Odo_VY * Math.Sin(GPS_Theta) / fEch;

            // Prédiction
            //ici j'ai simplifié, plutot que de faire *F je met direct dans la bonne case donc pas besoin de MatrixOdo

            MatrixxPred[0, 0] = currentGpsXRefTerrain;
            MatrixxPred[1, 0] = currentGpsYRefTerrain;
            MatrixxPred[2, 0] = currentGpsTheta;


            //MEGA BOUCLE
            for (int j = 0; j < nbre_landmarks; j++)                                                        //on parcours 1 par 1 les landmarks
            {

                MatrixXi = Trouver_Xi_Dans_Xest(j);                                                         //on initialise Xi
                MatrixPi = Trouver_Pi_dans_Pest(j);                                                         //on initialise Pi

                MatrixXiPred = TrouverXiPredDansXpred(j);

                MatrixpPred = Toolbox.Addition_Matrices(
                Toolbox.Multiply(MatrixG, Toolbox.Multiply(MatrixPi, Toolbox.Transpose(MatrixG))),
                Toolbox.Multiply(Toolbox.Transpose(MatrixFx), Toolbox.Multiply(MatrixR, MatrixFx)));

                #region Calcul de Z

                double deltax = MatrixXi[3, 0] - MatrixXi[0, 0];                                            //construction du vecteur delta du dernier ld 
                double deltay = MatrixXi[4, 0] - MatrixXi[1, 0];
                MatrixDelta[0, 0] = deltax;
                MatrixDelta[1, 0] = deltay;
                double q = Toolbox.Multiply(Toolbox.Transpose(MatrixDelta), MatrixDelta)[0, 0];             // c'est un réel car dimension 2*1 fois sa transposée (voir brouillon) 
                MatrixZ[0, 0] = Math.Sqrt(q);                                                           // Là on à une observation attendue par rapport a la dernière fois ou on a vu le ld 
                MatrixZ[1, 0] = Math.Atan2(deltay, deltax) - GPS_Theta;

                #endregion
                // A FAIRE UTILISER XPRED AU LIEU DES LD ET ENLEVER landmarks_observés 
                #region Calcul de ZPred          
                double deltax2 = landmarks_observés[j][0] - Matrixxest[0, 0];                               //on refait les calculs avec le landmark observé maintenant 
                double deltay2 = landmarks_observés[j][1] - Matrixxest[1, 0];

                MatrixDelta[0, 0] = deltax2;
                MatrixDelta[1, 0] = deltay2;
                q = Toolbox.Multiply(Toolbox.Transpose(MatrixDelta), MatrixDelta)[0, 0];
                MatrixZPred[0, 0] = Math.Sqrt(q);                                                               // Là on à une observation attendue par rapport a la dernière fois ou on a vu le ld 
                MatrixZPred[1, 0] = Math.Atan2(deltay2, deltax2) - GPS_Theta;

                #endregion

                #region Calcul de H //normalement ca c ok 
                MatrixHi[0, 0] = -(1 / Math.Sqrt(q)) * deltax;                                              //ici on prépare Hi = lowH
                MatrixHi[0, 1] = -(1 / Math.Sqrt(q)) * deltay;
                MatrixHi[0, 2] = 0;
                MatrixHi[0, 3] = (1 / Math.Sqrt(q)) * deltax;
                MatrixHi[0, 4] = (1 / Math.Sqrt(q)) * deltay;
                MatrixHi[1, 0] = (1 / q) * deltay;
                MatrixHi[1, 1] = (-1 / q) * deltax;
                MatrixHi[1, 2] = -1;
                MatrixHi[1, 3] = (-1 / q) * deltay;
                MatrixHi[1, 4] = (1 / q) * deltax;
                #endregion      

                MatrixParentheses = Toolbox.Multiply(MatrixHi, Toolbox.Multiply(MatrixPi, Toolbox.Transpose(MatrixHi)));

                MatrixParentheses = Toolbox.Addition_Matrices(MatrixParentheses, MatrixQ);                                  //On ajoute Q à la parenthèse

                MatrixParentheses = Toolbox.Inverse(MatrixParentheses);                                                     //on fais l'inverse de la parenthèses

                MatrixKi = Toolbox.Multiply(MatrixpPred, Toolbox.Multiply(Toolbox.Transpose(MatrixHi), MatrixParentheses)); //On trouve enfin Ki

                for (int indices = 0; indices < MatrixZ.Length; indices++)
                {
                    MatrixZPred[indices, 0] -= MatrixZ[indices, 0];                         // A partir de là MatrixZ contient la différence entre prédiction et observation 
                }

                MatrixKdeltaz = Toolbox.Multiply(MatrixKi, MatrixZPred);

                MatrixKdeltaz = Toolbox.Addition_Matrices(MatrixXiPred, MatrixKdeltaz);

                Matrixxest[0, 0] = MatrixKdeltaz[0, 0];                                                     //sert a remettre xi dans xest 
                Matrixxest[1, 0] = MatrixKdeltaz[1, 0];
                Matrixxest[2, 0] = MatrixKdeltaz[2, 0];
                Matrixxest[2 * j + 3, 0] = MatrixKdeltaz[3, 0];
                Matrixxest[2 * j + 4, 0] = MatrixKdeltaz[4, 0];

                MatrixKi = Toolbox.Multiply(MatrixKi, MatrixHi);                                            //maintenant ki continient K*H
                for (int ligne = 0; ligne < 5; ligne++)
                {
                    for (int colonne = 0; colonne < 5; colonne++)
                    {
                        MatrixKi[ligne, colonne] = -MatrixKi[ligne, colonne];
                    }
                }

                MatrixPi = Toolbox.Multiply(Toolbox.Addition_Matrices(MatrixFx, MatrixKi), MatrixpPred);

                Remettre_Pi_dans_Pest(j);

            }   // FIN DE MEGA BOUCLE

        }
        


        #region events
        //Inputs events
        public void OnOdoReceived(object sender, LocationArgs e)
        {
            currentOdoVxRefRobot = e.Location.Vx;
            currentOdoVyRefRobot = e.Location.Vy;


            currentOdoVxRefTerrain = currentOdoVxRefRobot * Math.Cos(currentGpsTheta) - currentOdoVyRefRobot * Math.Sin(currentGpsTheta);
            currentOdoVyRefTerrain = currentOdoVxRefRobot * Math.Sin(currentGpsTheta) + currentOdoVyRefRobot * Math.Cos(currentGpsTheta);
            currentOdoVtheta = e.Location.Vtheta;

            currentGpsXRefTerrain += currentOdoVxRefRobot * tEch * Math.Cos(currentGpsTheta) - currentOdoVyRefRobot * tEch * Math.Sin(currentGpsTheta);
            currentGpsYRefTerrain += currentOdoVxRefRobot * tEch * Math.Sin(currentGpsTheta) + currentOdoVyRefRobot * tEch * Math.Cos(currentGpsTheta);
            currentGpsTheta += currentOdoVtheta / fEch;

        }
        public void OnLandmarksReceived(object sender, PointDExtendedListArgs e)
        {
            if (robotId == e.RobotId)
            {
                liste_landmarks.Clear();

                foreach (PointDExtended Point in e.LandmarkList)
                {
                    liste_landmarks.Add(Point.Pt);
                }

                List<List<double>> landmarks = liste_landmarks.Select(l => new List<double>(2) { l.X, l.Y }).ToList(); //on commence par mettre les ld en liste de liste

                if (Appel_pour_la_première_fois == 0)
                {
                    InitEKF(robotId, freqEchOdometry);
                    Appel_pour_la_première_fois = 1;
                }               //Initialisation de l'ekf si c'est la premiere fois qu'on l'appelle 

                int nbre_landmarks = landmarks.Count;

                //on crée X ET P en regardant s'il y a des new ld ou pas et on trouve en même temps la liste d'indice des landmarks 

                List<int> list_indice_landmarks = acceuil_landmarks(landmarks);

                TrouverXestEtXpredDansX(list_indice_landmarks, MatrixX, landmarks); //Ici on remplie Xest avec ce qu'on connaissait et Xpred avec ce qu'on vient de recevoir


                if (newPMethode)
                    TrouverPestEtPpredDansP(list_indice_landmarks, MatrixP);
                else 
                    Matrixpest = TrouverPestDansP(list_indice_landmarks, MatrixP);

                SLAMCorrection(currentGpsTheta, currentOdoVxRefTerrain, currentOdoVyRefTerrain, currentOdoVtheta, nbre_landmarks, landmarks);

                #region Clean weard landmraks

                if (wantToClean)
                {
                    List<int> AEnlever = CleanXestFromWeardLandmarks(Matrixxest);
                    CleanPestFromWeardLandmarks(AEnlever);
                    list_indice_landmarks = CleanListIndices(AEnlever, list_indice_landmarks);
                }


                #endregion

                Remettre_Pest_Dans_P(list_indice_landmarks);

                Remettre_Xest_Dans_X(list_indice_landmarks);

                EKFLocationRefTerrain.X = Matrixxest[0, 0];

                EKFLocationRefTerrain.Y = Matrixxest[1, 0];

                EKFLocationRefTerrain.Theta = Matrixxest[2, 0];

                //Attention la location a renvoyer est dans le ref terrain pour les positions et dans le ref robot pour les vitesses
                double EKFLocationRefRobotVx = currentOdoVxRefTerrain * Math.Cos(-EKFLocationRefTerrain.Theta) - currentOdoVyRefTerrain * Math.Sin(-EKFLocationRefTerrain.Theta);
                double EKFLocationRefRobotVy = currentOdoVxRefTerrain * Math.Sin(-EKFLocationRefTerrain.Theta) + currentOdoVyRefTerrain * Math.Cos(-EKFLocationRefTerrain.Theta);

                Location EKFOutputLocation = new Location(EKFLocationRefTerrain.X, EKFLocationRefTerrain.Y, EKFLocationRefTerrain.Theta,
                                                            EKFLocationRefRobotVx, EKFLocationRefRobotVy, EKFLocationRefTerrain.Vtheta);



                OnEKFLocation(robotId, EKFOutputLocation, Matrixxest, Matrixpest); //On balance à l'event les landmarks vus à cet instant et leurs covariances 
                                                                                   // NOTE : possible d'afficher tout les ld connus si on veut (MatrixX)

            }

        }                       //Fin de l'algo ! 
        //Output events
        public event EventHandler<PosRobotAndLandmarksArgs> OnEKFLocationEvent;
        public virtual void OnEKFLocation(int id, Location locationRefTerrain, double[,] X, double[,] Covariances)
        {
            var handler = OnEKFLocationEvent;

            List<PointDExtended> Liste_Sortie = new List<PointDExtended>();

            if (handler != null)
            {
                for (int i = 3; i < X.Length; i = i + 2)
                {
                    PointD ptd = new PointD(X[i, 0], X[i + 1, 0]);
                    PointDExtended Ptde = new PointDExtended(ptd, System.Drawing.Color.Aqua, 5);
                    Liste_Sortie.Add(Ptde);
                }

                handler(this, new PosRobotAndLandmarksArgs { RobotId = id, PosLandmarkList = Liste_Sortie, PosRobot = locationRefTerrain });
            }
        }
        #endregion
    }
}

