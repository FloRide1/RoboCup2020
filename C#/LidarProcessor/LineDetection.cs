using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using System.Drawing;

namespace LidarProcessor
{
    public static class LineDetection
    {
        public static List<PolarPointRssiExtended> ExtractLinesFromCurvature(List<PolarPointRssiExtended> ptList, List<PolarCourbure> curvatureList, double seuilCourbure = 1.01)
        {
            List<PolarPointRssiExtended> linePoints = new List<PolarPointRssiExtended>();

            for (int i = 0; i < curvatureList.Count; i++)
            {

                if (curvatureList[i].Courbure < seuilCourbure)
                {
                    linePoints.Add(ptList[i]);
                }
            }
            return linePoints;
        }

        public static List<SegmentExtended> ExtractSegmentsFromCurvature(List<PolarPointRssiExtended> ptList, List<PolarCourbure> curvatureList, double seuilCourbure = 1.01, double minimum_distance = 0.3)
        {
            List<SegmentExtended> segmentList = new List<SegmentExtended>();


            bool segmentEnCours = false;
            PolarPointRssiExtended ptDebutSegmentCourant = new PolarPointRssiExtended(new PolarPointRssi(), 1, Color.White);
            PolarPointRssiExtended ptFinSegmentCourant = new PolarPointRssiExtended(new PolarPointRssi(), 1, Color.White);

            for (int i = 0; i < curvatureList.Count; i++)
            {
                if (curvatureList[i].Courbure < seuilCourbure && Toolbox.Distance(ptList[i].Pt, ptList[Math.Max(0, i - 1)].Pt) < 3)
                {
                    if (segmentEnCours == false)
                    {
                        //On a un nouveau segment
                        ptDebutSegmentCourant = ptList[i];
                    }
                    //linePoints.Add(ptList[i]);
                    segmentEnCours = true;
                }
                else
                {
                    if (segmentEnCours == true)
                    {
                        //On a une fin de segment
                        ptFinSegmentCourant = ptList[i - 1];
                        PointD segment_start_point = new PointD(ptDebutSegmentCourant.Pt.Distance * Math.Cos(ptDebutSegmentCourant.Pt.Angle), ptDebutSegmentCourant.Pt.Distance * Math.Sin(ptDebutSegmentCourant.Pt.Angle));
                        PointD segment_end_point = new PointD(ptFinSegmentCourant.Pt.Distance * Math.Cos(ptFinSegmentCourant.Pt.Angle), ptFinSegmentCourant.Pt.Distance * Math.Sin(ptFinSegmentCourant.Pt.Angle));
                        if (Toolbox.Distance(segment_start_point, segment_end_point) >= minimum_distance)
                        {
                            segmentList.Add(new SegmentExtended(segment_start_point, segment_end_point, Color.Orange, 5));
                        }

                    }
                    segmentEnCours = false;
                }
            }
            return segmentList;
        }

        public static List<SegmentExtended> SetColorsOfSegments(List<SegmentExtended> segments_objets)
        {
            int i;
            for (i = 0; i < segments_objets.Count; i++)
            {
                Color color = Toolbox.HLSToColor((35 * i) % 240, 120, 240);
                segments_objets[i].Color = color;

            }

            return segments_objets;
        }

        public static List<SegmentExtended> SetColorOfFamily(List<List<SegmentExtended>> list_of_family)
        {
            List<SegmentExtended> list_of_segments = new List<SegmentExtended>();
            int i = 0;
            foreach (List<SegmentExtended> family in list_of_family)
            {
                List<SegmentExtended> family_colorised = family;
                Color color = Toolbox.HLSToColor((35 * i++) % 240, 120, 240);

                family_colorised.ForEach(x => x.Color = color);
                list_of_segments.AddRange(family_colorised);
            }

            return list_of_segments;

        }

        /// <summary>
        /// Return a list of Segment where all parallel and continuous segment are Merge
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static List<SegmentExtended> MergeSegment(List<SegmentExtended> segments, double threshold)
        {
            List<SegmentExtended> merged_segment;

            merged_segment = new List<SegmentExtended>();

            for (int i=0; i < segments.Count; i++)
            {
                /// On ajoute le segment courant à la liste des segments fusionnés
                /// il faudra donc éliminer les segments mergeable au fur et à mesure de l'algo
                merged_segment.Add(segments[i]);
                for (int j = i+1; j < segments.Count; j++)
                {
                    if (testIfSegmentAreParrallel(merged_segment[i], segments[j]))
                    {
                        /// Les segments sont bien parallèles
                        if (Toolbox.DistancePointToLine(new PointD(segments[j].Segment.X1, segments[j].Segment.Y1),
                                                        new PointD(merged_segment[i].Segment.X1, merged_segment[i].Segment.Y1),
                                                        new PointD(merged_segment[i].Segment.X2, merged_segment[i].Segment.Y2)) < threshold)
                        {
                            /// Le pt 1 appartient au merged_segment[i]
                            if (Toolbox.DistancePointToLine(new PointD(segments[j].Segment.X2, segments[j].Segment.Y2),
                                                        new PointD(merged_segment[i].Segment.X1, merged_segment[i].Segment.Y1),
                                                        new PointD(merged_segment[i].Segment.X2, merged_segment[i].Segment.Y2)) < threshold)
                            {
                                /// Le pt 2 appartient au merged_segment[i]
                                /// On fusionne les segments !!!
                                double xMin = Math.Min(Math.Min(merged_segment[i].Segment.X1, merged_segment[i].Segment.X2), Math.Min(segments[j].Segment.X1, segments[j].Segment.X2));
                                double xMax = Math.Max(Math.Max(merged_segment[i].Segment.X1, merged_segment[i].Segment.X2), Math.Max(segments[j].Segment.X1, segments[j].Segment.X2));
                                double yMin = Math.Min(Math.Min(merged_segment[i].Segment.Y1, merged_segment[i].Segment.Y2), Math.Min(segments[j].Segment.Y1, segments[j].Segment.Y2));
                                double yMax = Math.Max(Math.Max(merged_segment[i].Segment.Y1, merged_segment[i].Segment.Y2), Math.Max(segments[j].Segment.Y1, segments[j].Segment.Y2));

                                if (Toolbox.ModuloPiAngleRadian(Math.Atan2(merged_segment[i].Segment.Y2 - merged_segment[i].Segment.Y1, merged_segment[i].Segment.X2 - merged_segment[i].Segment.X1)) > 0)
                                    merged_segment[i] = new SegmentExtended(new PointD(xMin, yMin), new PointD(xMax, yMax), merged_segment[i].Color, merged_segment[i].Width);
                                else
                                    merged_segment[i] = new SegmentExtended(new PointD(xMin, yMax), new PointD(xMax, yMin), merged_segment[i].Color, merged_segment[i].Width);

                                /// On supprime le segment fusionné
                                segments.RemoveAt(j);
                            }
                        }
                    }
                }
            }

            return merged_segment;
        }

        public static List<List<SegmentExtended>> FindFamilyOfSegment(List<SegmentExtended> list_of_segments, double minimum_size = 0.0)
        {
            List<SegmentExtended> copy_of_list_of_segment = list_of_segments;
            List<List<SegmentExtended>> list_of_family = new List<List<SegmentExtended>>();

            while (copy_of_list_of_segment.Count > 0)
            {
                SegmentExtended selected_segment = copy_of_list_of_segment[0];
                if (Toolbox.Distance(selected_segment.Segment.X1, selected_segment.Segment.Y1, selected_segment.Segment.X2, selected_segment.Segment.Y2) > minimum_size)
                {
                    int index_of_this_family = list_of_family.Count;
                    list_of_family.Add(new List<SegmentExtended>());

                    list_of_family[index_of_this_family].Add(selected_segment);

                    for (int j = 0; j < copy_of_list_of_segment.Count; j++)
                    {
                        SegmentExtended tested_segment = copy_of_list_of_segment[j];
                        if (testIfSegmentAreParrallel(selected_segment, tested_segment) || testIfSegmentArePerpendicular(selected_segment, tested_segment))
                        {
                            list_of_family[index_of_this_family].Add(tested_segment);
                        }
                    }

                    foreach (SegmentExtended element in list_of_family[index_of_this_family])
                    {
                        copy_of_list_of_segment.Remove(element);
                    }
                }
                else
                {
                    copy_of_list_of_segment.RemoveAt(0);
                }
            }
            return list_of_family;
        }


        public static bool testIfSegmentAreParrallel(SegmentExtended segment_1, SegmentExtended segment_2, double thresold = 3 * (Math.PI / 180))
        {
            double segment_1_angle = Toolbox.ModuloPiAngleRadian(Math.Atan2(segment_1.Segment.Y2 - segment_1.Segment.Y1, segment_1.Segment.X2 - segment_1.Segment.X1));
            double segment_2_angle = Toolbox.ModuloPiAngleRadian(Math.Atan2(segment_2.Segment.Y2 - segment_2.Segment.Y1, segment_2.Segment.X2 - segment_2.Segment.X1));

            if (segment_1_angle - thresold <= segment_2_angle && segment_1_angle + thresold >= segment_2_angle)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public static bool testIfSegmentArePerpendicular(SegmentExtended segment_1, SegmentExtended segment_2, double thresold = 3 * (Math.PI / 180))
        {
            double segment_1_angle = Toolbox.ModuloPiAngleRadian(Math.Atan2(segment_1.Segment.Y2 - segment_1.Segment.Y1, segment_1.Segment.X2 - segment_1.Segment.X1));
            double segment_2_angle = Toolbox.ModuloPiAngleRadian(Toolbox.ModuloPiAngleRadian(Math.Atan2(segment_2.Segment.Y2 - segment_2.Segment.Y1, segment_2.Segment.X2 - segment_2.Segment.X1)) + Math.PI / 2);

            if (segment_1_angle - thresold <= segment_2_angle && segment_1_angle + thresold >= segment_2_angle)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Implementation of the Ramer–Douglas–Peucker algorithm, which consist for reducing the nomber of usefull point for creating a line;
        /// en.wikipedia.org/wiki/Ramer-Douglas-Peucker_algorithm
        /// </summary>
        /// <param name="list_of_points"></param>
        /// <param name="epsilon"></param>
        public static List<PolarPointRssiExtended> IEPF_Algorithm(List<PolarPointRssiExtended> list_of_points, double epsilon)
        {
            double dmax = 0;
            int index = 0;
            int end = list_of_points.Count();

            PointDExtended first_point = Toolbox.ConvertPolarToPointD(list_of_points[0]);
            PointDExtended end_point = Toolbox.ConvertPolarToPointD(list_of_points[end - 1]);
            double angle = Math.Atan2(first_point.Pt.Y - end_point.Pt.Y, first_point.Pt.X - end_point.Pt.X);

            for (int i = 1; i < end - 1; i++)
            {
                double distance = Toolbox.DistancePointToLine(Toolbox.ConvertPolarToPointD(list_of_points[i]).Pt ,first_point.Pt , angle);
                if (distance > dmax)
                {
                    index = i;
                    dmax = distance;
                }

            }

            List<PolarPointRssiExtended> ResultList = new List<PolarPointRssiExtended>();

            /// If max dist is greater than epsilon -> Recursively Simplify
            if (dmax > epsilon)
            {
                List<PolarPointRssiExtended> recursiveResult1 = IEPF_Algorithm(list_of_points.GetRange(0, index), epsilon);
                List<PolarPointRssiExtended> recursiveResult2 = IEPF_Algorithm(list_of_points.GetRange(index+1, end - 1 - index), epsilon);

                ResultList.AddRange(recursiveResult1);
                ResultList.AddRange(recursiveResult2);

            }
            else
            {
                ResultList.Add(list_of_points[0]);
                ResultList.Add(list_of_points[end - 1]);
            }
            return ResultList;
        }


        public static List<List<PointDExtended>> FindAllValidCrossingPoints(List<List<SegmentExtended>> list_of_family)
        {
            List<List<PointDExtended>> list_of_crossing_points = new List<List<PointDExtended>>();
            foreach (List<SegmentExtended> family in list_of_family)
            {
                List<int> list_of_case = Enumerable.Range(0, family.Count).ToList(); /// [0,1,2,3,...,n]
                List<List<int>> list_of_combinations_of_the_family = Toolbox.GetKCombs(list_of_case, 2).ToList().Select(x => x.ToList()).ToList(); /// [[0,1],[0,2],[0,3],[1,2],[1,3],[2,3],...]

                List<List<SegmentExtended>> list_of_parallel_combination = list_of_combinations_of_the_family.Select(
                    x => testIfSegmentArePerpendicular(family[x[0]], family[x[1]]) ? new List<SegmentExtended>() { family[x[0]], family[x[1]] } : null
                ).ToList();

                list_of_parallel_combination.RemoveAll(item => item == null);

                list_of_crossing_points.Add(list_of_parallel_combination.Select(x => Toolbox.GetCrossingPointBetweenSegment(x[0], x[1])).ToList());

            }

            list_of_crossing_points = list_of_crossing_points.Distinct().ToList();

            return list_of_crossing_points;
        }
    }
}
