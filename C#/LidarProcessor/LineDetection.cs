﻿using System;
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
                Color color = Toolbox.HLSToColor((35 * i++) % 240, 120, 240);
                foreach(SegmentExtended segment in family)
                {
                    segment.Color = color;
                    list_of_segments.Add(segment);
                }
            }

            return list_of_segments;

        }


        public static List<SegmentExtended> MergeSegment(List<SegmentExtended> segments, double thresold)
        {
            List<SegmentExtended> merged_segment = segments;

            bool isMergingEnded = false;

            while (!isMergingEnded)
            {
                isMergingEnded = true;
                int i;
                for (i = 0; i < merged_segment.Count; i++)
                {
                    int j;
                    SegmentExtended selected_segment = merged_segment[i];

                    PointD point_segment_a = new PointD(selected_segment.Segment.X1, selected_segment.Segment.Y1);
                    PointD point_segment_b = new PointD(selected_segment.Segment.X2, selected_segment.Segment.Y2);
                    double lenght_of_selected_segment = Toolbox.Distance(point_segment_a, point_segment_b);

                    double selected_segment_angle = Math.Atan2(point_segment_b.Y - point_segment_a.Y, point_segment_b.X - point_segment_a.X);

                    for (j = 0; j < merged_segment.Count; j++)
                    {
                        if (i != j)
                        {
                            SegmentExtended tested_segment = merged_segment[j];
                            PointD point_tested_a = new PointD(tested_segment.Segment.X1, tested_segment.Segment.Y1);
                            PointD point_tested_b = new PointD(tested_segment.Segment.X2, tested_segment.Segment.Y2);

                            double lenght_of_tested_segment = Toolbox.Distance(point_tested_a, point_tested_b);

                            double distance_point_to_line_a = Toolbox.DistancePointToLine(point_tested_a, point_segment_a, selected_segment_angle);
                            double distance_point_to_line_b = Toolbox.DistancePointToLine(point_tested_b, point_segment_a, selected_segment_angle);

                            bool segment_are_parallel = testIfSegmentAreParrallel(selected_segment, tested_segment);

                            if (distance_point_to_line_a <= thresold && distance_point_to_line_b <= thresold && segment_are_parallel)
                            {
                                double distance_aa = Toolbox.Distance(point_segment_a, point_tested_a);
                                double distance_ab = Toolbox.Distance(point_segment_a, point_tested_b);
                                double distance_ba = Toolbox.Distance(point_segment_b, point_tested_a);
                                double distance_bb = Toolbox.Distance(point_segment_b, point_tested_b);

                                double max_distance = Math.Max(Math.Max(Math.Max(distance_aa, distance_ab), Math.Max(distance_ba, distance_bb)), Math.Max(lenght_of_selected_segment, lenght_of_tested_segment));

                                PointD begin_point = new PointD(0, 0);
                                PointD end_point = new PointD(0, 0);

                                if (max_distance == distance_aa)
                                {
                                    begin_point = point_segment_a;
                                    end_point = point_tested_a;
                                }
                                else if (max_distance == distance_ab)
                                {
                                    begin_point = point_segment_a;
                                    end_point = point_tested_b;
                                }
                                else if (max_distance == distance_ba)
                                {
                                    begin_point = point_segment_b;
                                    end_point = point_tested_a;
                                }
                                else if (max_distance == distance_bb)
                                {
                                    begin_point = point_segment_b;
                                    end_point = point_tested_b;
                                }
                                else if (max_distance == lenght_of_selected_segment)
                                {
                                    begin_point = point_segment_a;
                                    end_point = point_segment_b;
                                }
                                else if (max_distance == lenght_of_tested_segment)
                                {
                                    begin_point = point_tested_a;
                                    end_point = point_tested_b;
                                }

                                merged_segment[i] = new SegmentExtended(begin_point, end_point, selected_segment.Color, selected_segment.Width);
                                merged_segment.RemoveAt(j);
                                isMergingEnded = false;
                                break;
                            }
                            else if (segment_are_parallel)
                            {
                                merged_segment[j].Color = merged_segment[i].Color;
                            }
                            else if (testIfSegmentArePerpendicular(tested_segment, selected_segment))
                            {
                                merged_segment[j].Color = Color.FromArgb(merged_segment[i].Color.ToArgb() ^ 0xffffff);
                            }

                        }
                    }

                    if (!isMergingEnded)
                    {
                        break;
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
                List<PolarPointRssiExtended> recursiveResult2 = IEPF_Algorithm(list_of_points.GetRange(index, end - 1 - index), epsilon);

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
    }
}
