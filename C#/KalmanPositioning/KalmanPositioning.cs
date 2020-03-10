using EventArgsLibrary;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace KalmanPositioning
{
    public class KalmanPositioning
    {
        int robotId;

        private double tEch;
        private double fEch;

        private readonly Matrix<double> MatrixA;
        private readonly Matrix<double> MatrixH;
        private readonly Matrix<double> MatrixQ;
        private readonly Matrix<double> MatrixR;

        double currentGpsX = 0;
        double currentGpsY = 0;
        double currentGpsTheta = 0; 
        double currentOdoVx = 0;
        double currentOdoVy = 0;
        double currentOdoVtheta = 0;
        double currentGyroVtheta = 0;

        Location kalmanLocationRefTerrain = new Location(0, 0, 0, 0, 0, 0);
               
        public KalmanPositioning(int id,
            double freqEchOdometry, 
            double stdDev_X_GPS, double stdDev_Y_GPS, double stdDev_Th_GPS,
            double stdDev_VX_Odo, double stdDev_VY_Odo, double stdDev_VTh_Odo,
            double stdDev_VTh_Gyro)
        {
            robotId = id;
            fEch = freqEchOdometry;
            tEch = 1 / freqEchOdometry;

            double[,] A =
            {
                {1, tEch, tEch * tEch / 2, 0, 0, 0, 0, 0, 0},
                {0, 1, tEch, 0, 0, 0, 0, 0, 0},
                {0, 0, 1, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 1, tEch, tEch * tEch / 2, 0, 0, 0},
                {0, 0, 0, 0, 1, tEch, 0, 0, 0},
                {0, 0, 0, 0, 0, 1, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 1, tEch, tEch * tEch / 2},
                {0, 0, 0, 0, 0, 0, 0, 1, tEch},
                {0, 0, 0, 0, 0, 0, 0, 0, 1},
            };
            MatrixA = Matrix<double>.Build.DenseOfArray(A);

            double[,] H =
            {
                {1, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 1, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 1, 0, 0},
                {0, 1, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 1, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 1, 0},
                {0, 0, 0, 0, 0, 0, 0, 1, 0}
            };
            MatrixH = Matrix<double>.Build.DenseOfArray(H);

            double[] qx = { Math.Pow(tEch, 3) / 6, Math.Pow(tEch, 2) / 2, tEch, 0, 0, 0, 0, 0, 0 };
            double[] qy = { 0, 0, 0, Math.Pow(tEch, 3) / 6, Math.Pow(tEch, 2) / 2, tEch, 0, 0, 0 };
            double[] qtheta = { 0, 0, 0, 0, 0, 0, Math.Pow(tEch, 3) / 6, Math.Pow(tEch, 2) / 2, tEch };

            Vector<double> Qx = Vector<double>.Build.DenseOfArray(qx);
            Vector<double> Qy = Vector<double>.Build.DenseOfArray(qy);
            Vector<double> Qtheta = Vector<double>.Build.DenseOfArray(qtheta);

            Matrix<double> Mqx = Qx.ToRowMatrix().Transpose().Multiply(Qx.ToRowMatrix());
            Matrix<double> Mqy = Qy.ToRowMatrix().Transpose().Multiply(Qy.ToRowMatrix());
            Matrix<double> Mqtheta = Qtheta.ToRowMatrix().Transpose().Multiply(Qtheta.ToRowMatrix());

            MatrixQ = 10000 * stdDev_VX_Odo * (Mqx + Mqy + Mqtheta); //default 10000

            MatrixR = Matrix<double>.Build.DiagonalOfDiagonalArray(new double[]
            {
                stdDev_X_GPS,
                stdDev_Y_GPS,
                stdDev_Th_GPS,
                stdDev_VX_Odo,
                stdDev_VY_Odo,
                stdDev_VTh_Odo,
                stdDev_VTh_Gyro
            });

            InitFilter(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        private Vector<double> xPred;
        private Matrix<double> pPred;
        private Vector<double> xEst;
        private Matrix<double> pEst;
        private Matrix<double> K;
        private Matrix<double> Inx;

        public void InitFilter(double x, double vx, double ax, double y, double vy, double ay, double theta, double vtheta, double atheta)
        {
            Vector<double> xInit = Vector<double>.Build.DenseOfArray(new double[] { x, vx, ax, y, vy, ay, theta, vtheta, atheta });

            Matrix<double> pInit = Matrix<double>.Build.DenseDiagonal(MatrixQ.RowCount, MatrixQ.RowCount, 0.1);

            xPred = Vector<double>.Build.Dense(MatrixQ.RowCount, 0);
            pPred = Matrix<double>.Build.Dense(MatrixQ.RowCount, MatrixQ.RowCount, 0);
            xEst = Vector<double>.Build.Dense(MatrixQ.RowCount, 0);
            pEst = Matrix<double>.Build.Dense(MatrixQ.RowCount, MatrixQ.RowCount, 0);
            K = Matrix<double>.Build.Dense(MatrixQ.RowCount, MatrixR.RowCount, 0);
            Inx = Matrix<double>.Build.DenseDiagonal(MatrixQ.RowCount, MatrixQ.RowCount, 1);

            xPred = xInit;
            xEst = xInit;
            pPred = pInit;
            pEst = pInit;
        }

        public void IterateFilter(double GPS_X, double GPS_Y, double GPS_Theta, double Odo_VX, double Odo_VY, double Odo_Theta, double Gyro_Theta)
        {
            // Prédiction
            xPred = MatrixA.Multiply(xEst);
            pPred = MatrixA.Multiply(pEst.Multiply(MatrixA.Transpose())) + MatrixQ;

            // Estimation
            Vector<double> observation = Vector<double>.Build.DenseOfArray(new double[]
            {
                GPS_X,
                GPS_Y,
                GPS_Theta,
                Odo_VX,
                Odo_VY,
                Odo_Theta,
                Gyro_Theta
            });

            //Formule magique !
            var tt = MatrixH.Multiply(pPred).Multiply(MatrixH.Transpose()) + MatrixR;
            K = pPred.Multiply(MatrixH.Transpose()).Multiply(tt.Inverse());
            xEst = xPred + K.Multiply(observation - MatrixH.Multiply(xPred));

            if (double.IsNaN(xEst[0]))
                xEst[0] = 0;

            pEst = (Inx - K.Multiply(MatrixH)).Multiply(pPred);
        }

        public Vector<double> GetEstimation() => xEst;


        //Input events

        public void OnCollisionReceived(object sender, EventArgsLibrary.CollisionEventArgs e)
        {
            InitFilter(e.RobotRealPosition.X, e.RobotRealPosition.Vx, 0,
                e.RobotRealPosition.Y, e.RobotRealPosition.Vy, 0,
                e.RobotRealPosition.Theta, e.RobotRealPosition.Vtheta, 0);
        }

        public void OnOdometrySimulatedRobotSpeedReceived(object sender, SpeedArgs e)
        {                        
            currentOdoVx = e.Vx;
            currentOdoVy = e.Vy;
            currentOdoVtheta = e.Vtheta;
            
            //double VxRefTerrain = currentOdoVx * Math.Cos(-kalmanLocationRefTerrain.Theta) - currentOdoVy * Math.Sin(-kalmanLocationRefTerrain.Theta);
            //double VyRefRobot = currentOdoVx * Math.Sin(-kalmanLocationRefTerrain.Theta) + currentOdoVy * Math.Cos(-kalmanLocationRefTerrain.Theta);
            
            //On extrapole les valeurs de position Gps en utilisant les vitesses mesurées
            currentGpsX += currentOdoVx / fEch;
            currentGpsY += currentOdoVy / fEch;
            currentGpsTheta += currentOdoVtheta / fEch;

            IterateFilter(currentGpsX, currentGpsY, currentGpsTheta, currentOdoVx, currentOdoVy, currentOdoVtheta, currentGyroVtheta);

            //IterateFilter(1, 0, 0, 0, 0, 0, 0);

            var output = GetEstimation();

            kalmanLocationRefTerrain.X = output[0];
            kalmanLocationRefTerrain.Vx = output[1];
            double AxKalman = output[2];

            kalmanLocationRefTerrain.Y = output[3];
            kalmanLocationRefTerrain.Vy = output[4];
            double AyKalman = output[5];

            kalmanLocationRefTerrain.Theta = output[6];
            kalmanLocationRefTerrain.Vtheta = output[7];
            double AthetaKalman = output[8];

            OnKalmanLocation(robotId, kalmanLocationRefTerrain);
        }

        public void OnGyroSimulatedRobotSpeedReceived(object sender, GyroArgs e)
        {
            currentGyroVtheta = e.Vtheta;
        }

        public void OnCamLidarSimulatedRobotPositionReceived(object sender, PositionArgs e)
        {
            currentGpsX = e.X;
            currentGpsY = e.Y;
            currentGpsTheta = e.Theta;
        }


        //Output events
        public event EventHandler<LocationArgs> OnKalmanLocationEvent;
        public virtual void OnKalmanLocation(int id, Location location)
        {
            var handler = OnKalmanLocationEvent;
            if (handler != null)
            {
                handler(this, new LocationArgs { RobotId = id, Location = location });
            }
        }
    }
}

