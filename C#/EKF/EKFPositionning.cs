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
        int robotId;

        private int Appel_pour_la_première_fois = 0;

        private double tEch = 0.02;
        private double fEch = 50;

        private double deltax;
        private double deltay;

        private double[] MatrixX;
        private double[,] Matrixxest;
        private double[,] MatrixxPred;
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
        private double[,] MatrixA;
        private double[,] MatrixlowH;
        private double[,] MatrixHi;
        private double[,] MatrixH;
        private double[,] MatrixK;
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

        List<PointD> liste_landmarks = new List<PointD>{ }; //pour odo
        int id = (int)TeamId.Team1 + (int)RobotId.Robot1;
        double freqEchOdometry = 50;


        ////fonctions pour acquérir des positions

        //public void OnCamLidarSimulatedRobotPositionReceived(object sender, PositionArgs e)
        //{
        //    currentGpsXRefTerrain = e.X;
        //    currentGpsYRefTerrain = e.Y;
        //    currentGpsTheta = e.Theta;
        //}

        //cette fonction sert juste à recopier Xest et Pest quand on en a besoins 
        public double[,] GetXestEstimation() => Matrixxest;
        public double[,] GetPestEstimation() => Matrixpest; 
        public double[] GetXEstimation() => MatrixX;
        public double[,] GetPEstimation() => MatrixP;
        public double [,] GetG() => MatrixG;
        public double[,] GetR() => MatrixR;
        public double[,] GetPpred() => MatrixpPred;
        public double[,] GetXpred() => MatrixxPred;
        public double[,] GetZ() => MatrixZ;
        public double[,] GetZpred() => MatrixZPred;
        public double[,] GetParenthèses() => MatrixParentheses;
        public double[,] GetDelta() => MatrixDelta;


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
            matrice_apres[(int)Math.Sqrt(P.Length), (int)Math.Sqrt(P.Length)] = 30;                                                                     //Initialisation de P à "l'infini"
            matrice_apres[(int)Math.Sqrt(P.Length) + 1, (int)Math.Sqrt(P.Length) + 1] = 30;

            return matrice_apres;
        }
        public List<int> acceuil_landmarks(List<List<double>> list_ld_recus, int grande_taille)   
        {
            List<int> list_index = new List<int> { };
            bool ld_identifié = false;
            for (int landmark = 0; landmark < list_ld_recus.Count; landmark++)
            {
                xld = list_ld_recus[landmark][0];
                yld = list_ld_recus[landmark][1];
                int indice_en_cours = 0;
                while ((!ld_identifié) & (3 + 2 * indice_en_cours < MatrixX.Length)) // avant yavait 4 et j(ai mis 3, avant yavait grande taille et j'ai mis X.count 
                {
                    double x = MatrixX[3 + 2 * indice_en_cours];
                    double y = MatrixX[4 + 2 * indice_en_cours];
                    if (Math.Sqrt(Math.Pow((x - xld), 2) + Math.Pow((y - yld), 2)) < 0.1)
                    {
                        list_index.Add(indice_en_cours);
                        ld_identifié = true;
                    }
                    else
                    {
                        indice_en_cours += 1;
                    }
                }
                if (!ld_identifié)
                {
                    grande_taille += 2;
                    MatrixX = Ajout_ld_X(MatrixX, xld, yld);
                    MatrixP = Ajout_ld_P(MatrixP);
                    list_index.Add(MatrixX.Length-2);
                }
                ld_identifié = false;
            }
            return list_index;
        }

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

            MatrixQ = new double[2, 2];
            MatrixQ[0, 0] = 0.01;                                                                               //incertitude odo en cylindriques  0.1 deg et 1 cm
            MatrixQ[1, 1] = 0.2 * Math.PI / 360;

            MatrixR[0, 0] = MatrixR[1, 1] = 0.01;                                                               //incertitudes odo puis lidar 
            MatrixR[2, 2] = 0.2 * Math.PI / 360;
            MatrixR[3, 3] = 0.012;
            MatrixR[4, 4] = 0.014 * 2 * Math.PI / 360;

            MatrixDelta = new double[2, 1];

            MatrixH = new double[2, 5];
            MatrixK = new double[5, 2];
            MatrixlowH = new double[2, 5];
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

        public double[,] TrouverXestDansX(List<int> Indices, double[] X)
        {
            double[,] MatrixSortie = new double[2 * Indices.Count + 3, 1];
            MatrixSortie[0, 0] = X[0];
            MatrixSortie[1, 0] = X[1];
            MatrixSortie[2, 0] = X[2];
            int indice = 3;

            foreach (int item in Indices)
            {
                MatrixSortie[indice, 0] = X[item];
                MatrixSortie[indice + 1, 0] = X[item+1];
                indice += 2;
            }
            MatrixxPred = MatrixSortie;

            return MatrixSortie;
        }

        public double[,] TrouverPestDansP(List<int> List_indices, double[,] P)
        {
            double[,] MatrixSortie = new double[2 * List_indices.Count + 3, 2 * List_indices.Count + 3 ];
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

                MatrixSortie[0, indice] = P[0,item ]; //k
                MatrixSortie[0, indice + 1] = P[0,item+1]; //l
                MatrixSortie[1, indice] = P[1,item]; //m
                MatrixSortie[1, indice + 1] = P[1, item + 1]; //n
                MatrixSortie[2, indice] = P[2, item]; //o
                MatrixSortie[2, indice + 1] = P[2, item+1]; //p
                MatrixSortie[indice, 0] = P[item, 0];     //q
                MatrixSortie[indice, 1] = P[item, 1];     //r
                MatrixSortie[indice, 2] = P[item, 2];     //s
                MatrixSortie[indice + 1, 0] = P[item+1, 0]; //v
                MatrixSortie[indice + 1, 1] = P[item+1, 1]; //w
                MatrixSortie[indice + 1, 2] = P[item+1, 2]; //x

                MatrixSortie[indice, indice] = P[item,item]; //t
                MatrixSortie[indice, indice + 1] = P[item, item+1]; //u
                MatrixSortie[indice + 1, indice] = P[item+1, item]; //y
                MatrixSortie[indice + 1, indice + 1] = P[item + 1, item + 1]; //z

                indice += 2;
            }

            MatrixpPred = MatrixSortie;
            return MatrixSortie;
        }
        

        public void SLAMCorrection(double GPS_Theta, double Odo_VX, double Odo_VY, double Odo_VTheta, int nbre_landmarks, List<List<double>> landmarks_observés, List<int> list_indices)
        {
            
            MatrixG[0, 2] = -(Odo_VX * Math.Sin(GPS_Theta) / fEch) - Odo_VY * Math.Cos(GPS_Theta) / fEch;         // ici cest la dérivée du modele (xpred)
            MatrixG[1, 2] = (Odo_VX * Math.Cos(GPS_Theta) / fEch) - Odo_VY * Math.Sin(GPS_Theta) / fEch;

            // Prédiction
            //ici j'ai simplifié, plutot que de faire *F je met direct dans la bonne case donc pas besoin de MatrixOdo

            MatrixxPred[0, 0] = Matrixxest[0, 0] + Odo_VX * Math.Cos(GPS_Theta) / tEch - Odo_VY * Math.Sin(GPS_Theta) / tEch;
            MatrixxPred[1, 0] = Matrixxest[1, 0] - Odo_VX * Math.Sin(GPS_Theta) / tEch + Odo_VY * Math.Cos(GPS_Theta) / tEch;
            MatrixxPred[2, 0] = Matrixxest[2, 0] + Odo_VTheta / tEch;

            //MEGA BOUCLE//

            for (int j = 0; j < nbre_landmarks; j++)                                                                    //on parcours 1 par 1 les landmarks
            {

                MatrixXi = Trouver_Xi_Dans_Xest(j);                                                                     //on initialise Xi
                MatrixPi = Trouver_Pi_dans_Pest(j);                                                                     //on initialise Pi
                

                MatrixpPred = Toolbox.Addition_Matrices(
                Toolbox.Multiply(MatrixG, Toolbox.Multiply(MatrixPi, Toolbox.Transpose(MatrixG))),
                Toolbox.Multiply(Toolbox.Transpose(MatrixFx), Toolbox.Multiply(MatrixR, MatrixFx)));        

                deltax = MatrixX[2 * j + 3] - MatrixX[0];                                                               //construction du vecteur delta du dernier ld 
                deltay = MatrixX[2 * j + 4] - MatrixX[1];
                MatrixDelta[0, 0] = deltax;
                MatrixDelta[1, 0] = deltay;

                double q = Toolbox.Multiply(Toolbox.Transpose(MatrixDelta), MatrixDelta)[0, 0];                         // c'est un réel car dimension 2*1 fois sa transposée (voir brouillon) 

                MatrixZPred[0, 0] = Math.Sqrt(q);                                                                       // Là on à une observation attendue par rapport a la dernière fois ou on a vu le ld 
                MatrixZPred[1, 0] = Math.Atan2(deltay , deltax) - GPS_Theta;

                deltax = landmarks_observés[list_indices[j]][0] - Matrixxest[0,0];                                      //on refait les calculs avec le landmark observé maintenant 
                deltay = landmarks_observés[list_indices[j]][1] - Matrixxest[1,0];
                MatrixDelta[0, 0] = deltax;
                MatrixDelta[1, 0] = deltay;
                q= Toolbox.Multiply(Toolbox.Transpose(MatrixDelta), MatrixDelta)[0, 0];
                MatrixZ[0, 0] = Math.Sqrt(q);                                                                           // Là on à une observation attendue par rapport a la dernière fois ou on a vu le ld 
                MatrixZ[1, 0] = Math.Atan(deltay / deltax) - GPS_Theta;

                MatrixlowH[0, 0] = -(1 / Math.Sqrt(q)) * deltax;                                                        //ici on prépare lowH
                MatrixlowH[0, 1] = -(1 / Math.Sqrt(q)) * deltay;
                MatrixlowH[0, 2] = 0;
                MatrixlowH[0, 3] = (1 / Math.Sqrt(q)) * deltax;
                MatrixlowH[0, 4] = (1 / Math.Sqrt(q)) * deltay;                                                         //A FAIRE : vu que lowH=Hi supprimer lowH du programme
                MatrixlowH[1, 0] = (1 / q) * deltay;
                MatrixlowH[1, 1] = (-1 / q) * deltax;
                MatrixlowH[1, 2] = -1;
                MatrixlowH[1, 3] = (-1 / q) * deltay;
                MatrixlowH[1, 4] = (1 / q) * deltax;

                MatrixHi = MatrixlowH;                                                                                  //calcul de Hi=lowH dans notre cas car F=I

                MatrixParentheses = Toolbox.Multiply(MatrixHi, Toolbox.Multiply(MatrixPi, Toolbox.Transpose(MatrixHi)));

                MatrixParentheses = Toolbox.Addition_Matrices(MatrixParentheses, MatrixQ);                              //On ajoute Q à la parenthèse

                MatrixParentheses = Toolbox.Inverse(MatrixParentheses);                                                 //on fais l'inverse de la parenthèses

                MatrixKi = Toolbox.Multiply(MatrixpPred, Toolbox.Multiply(Toolbox.Transpose(MatrixHi), MatrixParentheses)); //On trouve enfin Ki

                for (int indices = 0; indices < MatrixZ.Length; indices++)
                {
                    MatrixZ[indices, 0] -= MatrixZPred[indices, 0];                                                       // A partir de là MatrixZ contient la différence entre prédiction et observation 
                }
                MatrixKdeltaz = Toolbox.Multiply(MatrixK, MatrixZ);

                MatrixKdeltaz = Toolbox.Addition_Matrices(MatrixXi, MatrixKdeltaz);

                Matrixxest[0, 0] = MatrixKdeltaz[0, 0];
                Matrixxest[1, 0] = MatrixKdeltaz[1, 0];
                Matrixxest[2, 0] = MatrixKdeltaz[2, 0];
                Matrixxest[2 * j + 3, 0] = MatrixKdeltaz[3, 0];
                Matrixxest[2 * j + 4, 0] = MatrixKdeltaz[4, 0];

                Remettre_Pi_dans_Pest(j);            //Alexandre Larribau, tu en es là c'est bien joué ! voir si tu arrives a sortir de la boucle et appeler l'event maintenant


            }   // FIN DE MEGA BOUCLE         
        }


        public void OnOdoReceived(object sender, LocationArgs e)
        {
            currentOdoVxRefRobot = e.Location.Vx;
            currentOdoVyRefRobot = e.Location.Vy;
            currentGpsTheta += currentOdoVtheta / fEch;

            currentOdoVxRefTerrain = currentOdoVxRefRobot * Math.Cos(currentGpsTheta) - currentOdoVyRefRobot * Math.Sin(currentGpsTheta);
            currentOdoVyRefTerrain = currentOdoVxRefRobot * Math.Sin(currentGpsTheta) + currentOdoVyRefRobot * Math.Cos(currentGpsTheta);
            currentOdoVtheta = e.Location.Vtheta;
            
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
                    InitEKF(id, freqEchOdometry);
                    Appel_pour_la_première_fois = 1;
                }               //Initialisation de l'ekf si c'est la premiere fois qu'on l'appelle 

                int nbre_landmarks = landmarks.Count;
                int taille = 3 + 2 * nbre_landmarks;
                int grande_taille = taille;        
                


                //on crée X ET P en regardant s'il y a des new ld ou pas et on trouve en même temps la liste d'indice des landmarks 

                List<int> list_indice_landmarks = acceuil_landmarks(landmarks,grande_taille);

                MatrixX = GetXEstimation();
                MatrixP = GetPEstimation();

                Matrixxest =  TrouverXestDansX(list_indice_landmarks, MatrixX);        //  A FAIRE ecrire deux fonction séparées
                Matrixpest = TrouverPestDansP(list_indice_landmarks, MatrixP);
                      
                SLAMCorrection(currentGpsTheta, currentOdoVxRefTerrain, currentOdoVyRefTerrain, currentOdoVtheta, nbre_landmarks, landmarks, list_indice_landmarks);

                Matrixxest = GetXestEstimation();
                Matrixpest = GetPestEstimation();

                EKFLocationRefTerrain.X = Matrixxest[0, 0];
                //EKFLocationRefTerrain.Vx = output[1,0];                                       

                EKFLocationRefTerrain.Y = Matrixxest[1, 0];
                //EKFLocationRefTerrain.Vy = output[3,0];

                EKFLocationRefTerrain.Theta = Matrixxest[2, 0];
                //EKFLocationRefTerrain.Vtheta = output[5,0];

                //Attention la location a renvoyer est dans le ref terrain pour les positions et dans le ref robot pour les vitesses
                double EKFLocationRefRobotVx = currentOdoVxRefTerrain * Math.Cos(-EKFLocationRefTerrain.Theta) - currentOdoVyRefTerrain * Math.Sin(-EKFLocationRefTerrain.Theta);
                double EKFLocationRefRobotVy = currentOdoVxRefTerrain * Math.Sin(-EKFLocationRefTerrain.Theta) + currentOdoVyRefTerrain * Math.Cos(-EKFLocationRefTerrain.Theta);

                Location EKFOutputLocation = new Location(EKFLocationRefTerrain.X, EKFLocationRefTerrain.Y, EKFLocationRefTerrain.Theta,
                                                            EKFLocationRefRobotVx, EKFLocationRefRobotVy, EKFLocationRefTerrain.Vtheta);

                OnEKFLocation(robotId, EKFOutputLocation, Matrixxest, Matrixpest); //On balance à l'event les landmarks vus à cet instant et leurs covariances 
                                                                                   // note : possible de retourner tout les landmarks connus mais chiant car il faut remettre xest dans X
            }
        }                                       //Fin de l'algo ! 





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
                    PointDExtended Ptde = new PointDExtended(ptd, System.Drawing.Color.Aqua, 5); //A FAIRE  voir si la taille et la couleur des ld sont bien 
                    Liste_Sortie.Add(Ptde);
                }

                handler(this, new PosRobotAndLandmarksArgs { RobotId = id, PosLandmarkList = Liste_Sortie, PosRobot = locationRefTerrain }); 
            }
        }

    }
}

