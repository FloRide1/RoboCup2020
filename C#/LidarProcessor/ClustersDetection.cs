using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using System.Drawing;

namespace LidarProcessor
{
    public static class ClustersDetection
    {

        /// <summary>
        /// Detect and return a list of Clusters point in pointList
        /// </summary>
        /// <param name="pointsList"></param>
        /// <param name="thresold"></param>
        /// <param name="mininum_amount_of_points"></param>
        /// <returns></returns>
        public static List<ClusterObjects> DetectClusterOfPoint(List<PolarPointRssiExtended> pointsList, double thresold, int mininum_amount_of_points = 5)
        {
            /// ABD Stand for Adaptative breakpoint Detection
            List<ClusterObjects> listOfClusters = new List<ClusterObjects>();
            ClusterObjects cluster = new ClusterObjects();
            int i;
            for (i = 1; i < pointsList.Count - 1; i++)
            {
                PolarPointRssiExtended point_n_minus_1 = pointsList[i - 1];
                PolarPointRssiExtended point_n_plus_1 = pointsList[i + 1];
                PolarPointRssiExtended point_n = pointsList[i];

                

                //double dist_n_minus_1 = point_n_minus_1.Distance;
                //double delta_theta = Math.Abs(point_n_minus_1.Angle - point_n.Angle);
                //double lambda = point_n_plus_1.Angle - point_n_minus_1.Angle;

                double ABD_Thresold = thresold; // dist_n_minus_1 * (Math.Sin(delta_theta) / Math.Sin(lambda - delta_theta));
                double distance_between_point = Toolbox.Distance(point_n, point_n_minus_1);
                if (distance_between_point < ABD_Thresold)
                {

                    cluster.points.Add(point_n);
                }
                else
                {

                    if (Toolbox.Distance(point_n_plus_1, point_n_minus_1) <= 2 * thresold)
                    {
                        //cluster.points.Add(point_n);
                    }
                    else
                    {
                        if (cluster.points.Count() > mininum_amount_of_points)
                        {
                            listOfClusters.Add(cluster);
                        }
                        cluster = new ClusterObjects();
                    }
                }
            }

            if (cluster.points.Count() > mininum_amount_of_points)
            {
                listOfClusters.Add(cluster);
            }

            return listOfClusters;
        }

        public static List<ClusterObjects> ExtractClusterFromIEPF(List<PolarPointRssiExtended> ptList, List<PolarPointRssiExtended> ptIEPF)
        {
            List<ClusterObjects> list_of_clusters = new List<ClusterObjects>(); 


            /// Maybe sort
            for (int i = 0; i < ptIEPF.Count - 2; i += 2)
            {
                ClusterObjects cluster = new ClusterObjects(ptList.GetRange(ptList.IndexOf(ptIEPF[i]), ptList.IndexOf(ptIEPF[i + 1]) - ptList.IndexOf(ptIEPF[i])));
                list_of_clusters.Add(cluster);
            }
            return list_of_clusters;
        }

        public static List<PolarPointRssiExtended> SetColorsOfClustersObjects(List<ClusterObjects> clusterObjects)
        {
            int i;
            List<PolarPointRssiExtended> array_of_points = new List<PolarPointRssiExtended>();

            for (i = 0; i < clusterObjects.Count; i++)
            {
                Color color = Toolbox.HLSToColor((35 * i) % 240 , 120, 240);

                foreach (PolarPointRssiExtended points in clusterObjects[i].points)
                {
                    points.Color = color;
                    points.Width = 3.5;

                    array_of_points.Add(points);
                }
                
            }

            return array_of_points;
        }
    }
}
