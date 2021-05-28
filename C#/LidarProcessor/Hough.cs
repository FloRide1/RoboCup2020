using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Constants;
using Utilities;
using EventArgsLibrary;
using MathNet.Numerics.LinearAlgebra;

namespace LidarProcessor
{
    class Hough
    {


        public void Algorithm_1(double delta_theta)
        {
            PointD[] array_of_points = new PointD[0];

            /// Maybe optimise with probabalistic selection
            foreach (PointD point in array_of_points)
            {
                double x = point.X;
                double y = point.Y;

                for (double theta = 0; theta < Math.PI; theta += delta_theta)
                {
                    double rho = x * Math.Cos(theta) + y * Math.Sin(theta);
                    /// Votes
                }
            }
        }

        public void Algorithm_2()
        {
            Vector<double>[][] array_of_vectorize_cluster = new Vector<double>[0][];


            foreach (Vector<double>[] cluster in array_of_vectorize_cluster)
            {
                int numbers_of_pixels_in_cluster = cluster.Length;

                Vector<double> mean_point = Vector<double>.Build.DenseOfArray(new double[2] { cluster.Sum(x => x[0]) / numbers_of_pixels_in_cluster, cluster.Sum(x => x[1]) / numbers_of_pixels_in_cluster });
                //(cluster - mean_point)


            }

        }

        public void Algorithm_3()
        {

        }

        public void Algorithm_4()
        {

        }
    }
}
