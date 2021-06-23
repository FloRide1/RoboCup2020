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
        const int MAXLANDMARKS = 4;
        const double MAXERROR = 0.5; // if a landmark is within 20 cm of another landmark its the same landmark
        public int MINOBSERVATIONS = 15; // Number of times a landmark must be observed to be recognized as a landmark

        const int LIFE = 40;
        const double MAX_RANGE = 1; /// I BET: THIS IS IN MILES
        //const int MAXTRIALS = 1000; //RANSAC: max times to run algorithm
        //const int MAXSAMPLE = 10; //RANSAC: randomly select X points
        //const int MINLINEPOINTS = 50; //RANSAC: if less than 40 points left don't bother trying to find consensus(stop algorithm)


        //const double RANSAC_TOLERANCE = 0.1; //RANSAC: if point is within x distance of line its part of line
        //const int RANSAC_CONSENSUS = 80; //RANSAC: at least 30 votes required to determine if a line
        double LIDAR_ANGLE_RESOLUTION = 0.5;

        Location RobotLocation = new Location();
        List<Landmark> list_of_landmarks = new List<Landmark>(MAXLANDMARKS);

        public event EventHandler<List<Landmark>> OnLinesLandmarksExtractedEvent;
        public LandmarksExtractor()
        {

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
            List<Tuple<double, double>> list_of_lines = new List<Tuple<double, double>>();// RansacAlgorithm(list_of_lidar_points);


            List<Landmark> list_of_landmarks = new List<Landmark>();


            //for each line we found: calculate the point on line closest to origin (0,0) add this point as a landmark
            for (int i = 0; i < list_of_lines.Count; i++)
            {
                list_of_landmarks.Add(GetLineLandmark(list_of_lines[i].Item1, list_of_lines[i].Item2, robotPosition));
            }
            OnLinesLandmarksExtractedEvent?.Invoke(this, list_of_landmarks);
            return list_of_landmarks;
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