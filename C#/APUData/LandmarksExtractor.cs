using System;
using System.Collections.Generic;
using System.Linq;
using Constants;
using EventArgsLibrary;
using Utilities;

namespace LandmarkExtractorNS
{
    /// <summary>
    /// Summary description for Landmarks.
    /// </summary>
    public class LandmarksExtractor
    {
        const int MAXLANDMARKS = 3000;
        const double MAXERROR = 0.5; // if a landmark is within 20 cm of another landmark its the same landmark
        public int MINOBSERVATIONS = 15; // Number of times a landmark must be observed to be recognized as a landmark

        const int LIFE = 40;
        const double MAX_RANGE = 1; /// I BET: THIS IS IN MILES
        const int MAXTRIALS = 1000; //RANSAC: max times to run algorithm
        const int MAXSAMPLE = 10; //RANSAC: randomly select X points
        const int MINLINEPOINTS = 30; //RANSAC: if less than 40 points left don't bother trying to find consensus(stop algorithm)


        const double RANSAC_TOLERANCE = 0.05; //RANSAC: if point is within x distance of line its part of line
        const int RANSAC_CONSENSUS = 30; //RANSAC: at least 30 votes required to determine if a line
        double LIDAR_ANGLE_RESOLUTION = 0.5;

        Location RobotLocation = new Location();
        List<Landmark> list_of_landmarks = new List<Landmark>(MAXLANDMARKS);

        public event EventHandler<Landmark> OnLinesLandmarksExtractedEvent;

        public LandmarksExtractor(double degreesPerScan)
        {
            this.LIDAR_ANGLE_RESOLUTION = degreesPerScan;
        }

        public void OnRobotPositionReceived(object sender, PositionArgs location)
        {
            RobotLocation = new Location(location.X, location.Y, location.Theta, 0, 0, 0);
        }

        public void OnRobotLidarReceived(object sender, RawLidarArgs rawLidar)
        {
            LIDAR_ANGLE_RESOLUTION = Math.Abs(rawLidar.PtList[0].Angle - rawLidar.PtList[1].Angle);
           
            ExtractLineLandmarks(rawLidar.PtList, RobotLocation);
        }

        public List<Landmark> ExtractLineLandmarks(List<PolarPointRssi> list_of_lidar_points, Location robotPosition)
        {
            //two arrays corresponding to found lines
            List<Tuple<double, double>> list_of_lines = new List<Tuple<double, double>>();
            List<int> list_of_points = Enumerable.Range(0, list_of_lidar_points.Count - 1).ToList();

            List<Landmark> list_of_landmarks = new List<Landmark>();

            #region RANSAC

            //RANSAC ALGORITHM
            int noTrials = 0;

            Random rnd = new Random();

            while (noTrials < MAXTRIALS && list_of_points.Count > MINLINEPOINTS)
            {
                List<int> list_of_random_selected_points = new List<int>();
                int temp = 0;
                bool newpoint;

                //– Randomly select a subset S1 of n data points and compute the model M1
                // Initial version chooses entirely randomly. Now choose one point randomly and then sample from neighbours within some defined radius

                int centerPoint = rnd.Next(MAXSAMPLE, list_of_points.Count - 1);
                list_of_random_selected_points.Add(centerPoint);
                for (int i = 1; i < MAXSAMPLE; i++)
                {
                    newpoint = false;
                    while (!newpoint)
                    {
                        temp = centerPoint + (rnd.Next(2) - 1) * rnd.Next(0, MAXSAMPLE);

                        if (list_of_random_selected_points.IndexOf(temp) == -1)
                            newpoint = true;
                    }
                    list_of_random_selected_points.Add(centerPoint);
                }

                //compute model M1
                double slope = 0;
                double y_intercept = 0;

                LeastSquaresLineEstimate(list_of_lidar_points, robotPosition, list_of_random_selected_points, ref slope, ref y_intercept);

                //– Determine the consensus set S1* of points is P compatible with M1 (within some error tolerance)
                List<int> list_of_consensus_points = new List<int>();
                List<int> list_of_new_lines_points = new List<int>();

                double x = 0, y = 0;
                double d = 0;

                for (int i = 0; i < list_of_points.Count; i++)
                {
                    //convert ranges and bearing to coordinates

                    x = (Math.Cos((i * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * list_of_lidar_points[i].Distance) + robotPosition.X;
                    y = (Math.Sin((i * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * list_of_lidar_points[i].Distance) + robotPosition.Y;


                    d = Toolbox.DistancePointToLine(new PointD(x, y), slope, y_intercept);
                    if (d < RANSAC_TOLERANCE)
                    {
                        //add points which are close to line
                        list_of_consensus_points.Add(i);
                    }
                    else
                    {
                        //add points which are not close to line
                        list_of_new_lines_points.Add(i);
                    }
                }

                //– If #(S1*) > t, use S1* to compute (maybe using least squares) a new model M1
                if (list_of_consensus_points.Count > RANSAC_CONSENSUS)
                {
                    //Calculate updated line equation based on consensus points
                    LeastSquaresLineEstimate(list_of_lidar_points, robotPosition, list_of_consensus_points, ref slope, ref y_intercept);

                    //for now add points associated to line as landmarks to see results
                    for (int i = 0; i < list_of_consensus_points.Count; i++)
                    {
                        //Remove points that have now been associated to this line
                        list_of_points = list_of_new_lines_points.ToList();
                    }

                    list_of_lines.Add(new Tuple<double, double> (slope, y_intercept));
                    noTrials = 0;
                }
                else
                    noTrials++;
            }
            #endregion

            //for each line we found: calculate the point on line closest to origin (0,0) add this point as a landmark
            for (int i = 0; i < list_of_lines.Count; i++)
            {
                list_of_landmarks.Add(GetLineLandmark(list_of_lines[i].Item1, list_of_lines[i].Item2, robotPosition));
            }

            return list_of_landmarks;
        }

        private void LeastSquaresLineEstimate(List<PolarPointRssi> list_of_lidar_points, Location robotPosition, List<int> list_of_selected_points, ref double slope, ref double y_intercept)
        {

            double y; //y coordinate
            double x; //x coordinate
            double sumY = 0; //sum of y coordinates
            double sumYY = 0; //sum of y^2 for each coordinat

            double sumX = 0; //sum of x coordinates
            double sumXX = 0; //sum of x^2 for each coordinate
            double sumYX = 0; //sum of y*x for each point

            foreach(int selected_point in list_of_selected_points)
            {
                //convert ranges and bearing to coordinates

                x = (Math.Cos((selected_point * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * list_of_lidar_points[selected_point].Distance) + robotPosition.X;
                y = (Math.Sin((selected_point * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * list_of_lidar_points[selected_point].Distance) + robotPosition.Y;

                sumY += y;
                sumYY += Math.Pow(y, 2);
                sumX += x;
                sumXX += Math.Pow(x, 2);
                sumYX += y * x;
            }

            y_intercept = (sumY * sumXX - sumX * sumYX) / (list_of_selected_points.Count * sumXX - Math.Pow(sumX, 2));
            slope = (list_of_selected_points.Count * sumYX - sumX * sumY) / (list_of_selected_points.Count * sumXX - Math.Pow(sumX, 2));
        }


        private Landmark GetLineLandmark(double a, double b, Location robotPosition)
        {
            //our goal is to calculate point on line closest to origin (0,0)

            //calculate line perpendicular to input line. a*ao = -1
            double ao = -1.0 / a;
            //landmark position
            double x = b / (ao - a);
            double y = (ao * b) / (ao - a);
            double range = Math.Sqrt(Math.Pow(x - robotPosition.X, 2) + Math.Pow(y - robotPosition.Y, 2));
            double bearing = Math.Atan2((y - robotPosition.Y), (x - robotPosition.X)) - robotPosition.Theta; /// SERIOUSLY THEY DIDN'T EVEN KNOW ATAN2
            //now do same calculation but get point on wall closest to robot instead
            //y = aox + bo => bo = y - aox
            double bo = robotPosition.Y - ao * robotPosition.X;
            //get intersection between y = ax + b and y = aox + bo
            //so aox + bo = ax + b => aox - ax = b - bo => x = (b - bo)/(ao - a), y = ao*(b - bo)/(ao - a) + bo

            double px = (b - bo) / (ao - a);


            double py = ((ao * (b - bo)) / (ao - a)) + bo;
            double rangeError = Toolbox.Distance(robotPosition.X, robotPosition.Y, px, py);
            double bearingError = Math.Atan((py - robotPosition.Y) / (px - robotPosition.X)) - robotPosition.Theta; //do you subtract or add robot bearing? I am not sure! --- SERIOUSLY WHO ARE THESES GUYS !?!?!?! 
            Landmark lm = new Landmark(LIFE);
            //convert landmark to map coordinate

            lm.Position = new PointD(x, y);
            lm.range = range;
            lm.bearing = bearing;
            lm.slope = a;
            lm.y_intercept = b;
            lm.rangeError = rangeError;
            lm.bearingError = bearingError;

            //associate landmark to closest landmark.
            int id = 0;
            int totalTimesObserved = 0;

            GetClosestAssociation(lm, ref id, ref totalTimesObserved);
            lm.id = id;
            lm.totalTimesObserved = totalTimesObserved;

            //return landmarks
            return lm;

        }

        private void GetClosestAssociation(Landmark lm, ref int id, ref int totalTimesObserved)
        { //given a landmark we find the closest landmark in DB

            int closestLandmark = 0;
            double temp;
            double? leastDistance = null;
            for (int i = 0; i < list_of_landmarks.Count; i++)
            {

                //only associate to landmarks we have seen more than MINOBSERVATIONS times
                if (list_of_landmarks[i].totalTimesObserved > MINOBSERVATIONS)
                {
                    temp = Toolbox.Distance(lm.Position.X, lm.Position.Y, list_of_landmarks[i].Position.X, list_of_landmarks[i].Position.Y);

                    if (temp < leastDistance)

                    {
                        leastDistance = temp;
                        closestLandmark = list_of_landmarks[i].id;
                    }
                }
            }

            if (leastDistance == null)
                id = -1;
            else
            {
                id = list_of_landmarks[closestLandmark].id;
                totalTimesObserved = list_of_landmarks[closestLandmark].totalTimesObserved;
            }

        }
    }
}