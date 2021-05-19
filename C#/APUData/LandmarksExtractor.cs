﻿using System;
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

        //const double CONVERT_DEG_TO_RAD = Math.PI / 180.0; // Convert to radians
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

        public class Landmark

        {

            public PointD Position; //landmarks (x,y) position relative to map
            public int id; //the landmarks unique ID
            public int life; //a life counter used to determine whether to discard a landmark
            public int totalTimesObserved; //the number of times we have seen landmark
            public double range; //last observed range to landmark
            public double bearing; //last observed bearing to landmark
                                   //RANSAC: Now store equation of a line
            public double a;
            public double b;


            public double rangeError; //distance from robot position to the wall we are using as a landmark (to calculate error)

            public double bearingError; //bearing from robot position to the wall we are using as a landmark (to calculate error)

            public Landmark()

            {
                totalTimesObserved = 0;
                id = -1;
                life = LIFE;
                Position = new PointD(0, 0);
                a = -1;
                b = -1;
            }
        }
        Landmark[] landmarkDB = new Landmark[MAXLANDMARKS];

        int DBSize = 0;
        int[,] IDtoID = new int[MAXLANDMARKS, 2];
        int EKFLandmarks = 0;

        public LandmarksExtractor(double degreesPerScan)
        {

            this.LIDAR_ANGLE_RESOLUTION = degreesPerScan;


            for (int i = 0; i < landmarkDB.Length; i++)

            {
                landmarkDB[i] = new Landmark();
            }
        }

        public void OnRobotPositionReceived(object sender, Location location)
        {
            RobotLocation = location;
        }

        public void OnRobotLidarReceived(object sender, RawLidarArgs rawLidar)
        {
            LIDAR_ANGLE_RESOLUTION = Math.Abs(rawLidar.PtList[0].Angle - rawLidar.PtList[1].Angle);
            
            double[] laserdata = rawLidar.PtList.Select(x => x.Distance).ToArray(); /// EDIT THIS TOO

            ExtractLineLandmarks(laserdata, RobotLocation);
        }

        public Landmark[] ExtractLineLandmarks(double[] laserdata, Location robotPosition)
        {

            //two arrays corresponding to found lines
            double[] la = new double[100];
            double[] lb = new double[100];

            int totalLines = 0;
            //array of laser data points corresponding to the seen lines
            int[] linepoints = new int[laserdata.Length];
            int totalLinepoints = 0;

            //have a large array to keep track of found landmarks
            Landmark[] tempLandmarks = new Landmark[400];
            for (int i = 0; i < tempLandmarks.Length; i++)
                tempLandmarks[i] = new Landmark();

            int totalFound = 0;
            double val = laserdata[0];

            double lastreading = laserdata[2];
            double lastlastreading = laserdata[2];
            
            //FIXME - OR RATHER REMOVE ME SOMEHOW...
            for (int i = 0; i < laserdata.Length - 1; i++) /// ... DOUBT ABOUT THE MINUS 1 ...
            {
                linepoints[totalLinepoints] = i;
                totalLinepoints++;
            }

            #region RANSAC

            //RANSAC ALGORITHM
            int noTrials = 0;

            Random rnd = new Random();

            while (noTrials < MAXTRIALS && totalLinepoints > MINLINEPOINTS)
            {

                int[] rndSelectedPoints = new int[MAXSAMPLE];
                int temp = 0;
                bool newpoint;

                //– Randomly select a subset S1 of n data points and compute the model M1
                //Initial version chooses entirely randomly. Now choose one point randomly and then sample from neighbours within some defined radius

                int centerPoint = rnd.Next(MAXSAMPLE, totalLinepoints - 1);
                rndSelectedPoints[0] = centerPoint;
                for (int i = 1; i < MAXSAMPLE; i++)

                {
                    newpoint = false;
                    while (!newpoint)

                    {
                        temp = centerPoint + (rnd.Next(2) - 1) * rnd.Next(0, MAXSAMPLE);

                        for (int j = 0; j < i; j++)

                        {

                            if (rndSelectedPoints[j] == temp)
                                break; //point has already been selected
                            if (j >= i - 1)

                                newpoint = true; //point has not already been selected
                        }
                    }
                    rndSelectedPoints[i] = temp;
                }

                //compute model M1
                double slope = 0;
                double y_intercept = 0;
                //y = a+ bx

                LeastSquaresLineEstimate(laserdata, robotPosition, rndSelectedPoints, MAXSAMPLE, ref slope, ref y_intercept);

                //– Determine the consensus set S1* of points is P
                //compatible with M1 (within some error tolerance)
                int[] consensusPoints = new int[laserdata.Length];
                int totalConsensusPoints = 0;
                int[] newLinePoints = new int[laserdata.Length];
                int totalNewLinePoints = 0;
                double x = 0, y = 0;
                double d = 0;
                for (int i = 0; i < totalLinepoints; i++)
                {

                    //convert ranges and bearing to coordinates

                    x = (Math.Cos((linepoints[i] * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * laserdata[linepoints[i]]) + robotPosition.X;
                    y = (Math.Sin((linepoints[i] * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * laserdata[linepoints[i]]) + robotPosition.Y;

                    //x =(Math.Cos((linepoints[i] * degreesPerScan * conv)) * laserdata[linepoints[i]]);//+robotPosition[0];
                    //y =(Math.Sin((linepoints[i] * degreesPerScan * conv)) * laserdata[linepoints[i]]);//+robotPosition[1];

                    d = DistanceToLine(x, y, slope, y_intercept);
                    if (d < RANSAC_TOLERANCE)
                    {

                        //add points which are close to line

                        consensusPoints[totalConsensusPoints] = linepoints[i];
                        totalConsensusPoints++;
                    }
                    else
                    {

                        //add points which are not close to line

                        newLinePoints[totalNewLinePoints] = linepoints[i];
                        totalNewLinePoints++;
                    }
                }

                //– If #(S1*) > t, use S1* to compute (maybe using least
                //squares) a new model M1*


                if (totalConsensusPoints > RANSAC_CONSENSUS)
                {

                    //Calculate updated line equation based on consensus points
                    LeastSquaresLineEstimate(laserdata, robotPosition, consensusPoints, totalConsensusPoints, ref slope, ref y_intercept);

                    //for now add points associated to line as landmarks to see results
                    for (int i = 0; i < totalConsensusPoints; i++)

                    {

                        //tempLandmarks[consensusPoints[i]] = GetLandmark(laserdata[consensusPoints[i]],consensusPoints[i], robotPosition);
                        //Remove points that have now been associated to this line
                        newLinePoints.CopyTo(linepoints, 0);
                        totalLinepoints = totalNewLinePoints;
                    }

                    //add line to found lines
                    la[totalLines] = slope;
                    lb[totalLines] = y_intercept;
                    totalLines++;

                    //restart search since we found a line
                    //noTrials = MAXTRIALS; //when maxtrials = debugging

                    noTrials = 0;
                }
                else


                    //DEBUG add point that we chose as middle value
                    //tempLandmarks[centerPoint] = GetLandmark(laserdata[centerPoint], centerPoint,robotPosition);

                    //– If #(S1*) < t, randomly select another subset S2 and
                    //repeat
                    //– If, after some predetermined number of trials there is
                    //no consensus set with t points, return with failure

                    noTrials++;
            }
            #endregion

            //for each line we found:
            //calculate the point on line closest to origin (0,0)
            //add this point as a landmark
            for (int i = 0; i < totalLines; i++)

            {
                tempLandmarks[i] = GetLineLandmark(la[i], lb[i], robotPosition);

                //tempLandmarks[i+1] = GetLine(la[i], lb[i]);
            }

            //now return found landmarks in an array of correct dimensions

            Landmark[] foundLandmarks = new Landmark[totalLines];
            //copy landmarks into array of correct dimensions
            for (int i = 0; i < foundLandmarks.Length; i++)
            {
                foundLandmarks[i] = (Landmark)tempLandmarks[i];
            }

            return foundLandmarks;

        }

        private void LeastSquaresLineEstimate(double[] laserdata, Location robotPosition, int[] SelectedPoints, int arraySize, ref double slope, ref double y_intercept)
        {

            double y; //y coordinate
            double x; //x coordinate
            double sumY = 0; //sum of y coordinates
            double sumYY = 0; //sum of y^2 for each coordinat

            double sumX = 0; //sum of x coordinates
            double sumXX = 0; //sum of x^2 for each coordinate
            double sumYX = 0; //sum of y*x for each point


            for (int i = 0; i < arraySize; i++)
            {

                //convert ranges and bearing to coordinates

                x = (Math.Cos((SelectedPoints[i] * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * laserdata[SelectedPoints[i]]) + robotPosition.X;
                y = (Math.Sin((SelectedPoints[i] * LIDAR_ANGLE_RESOLUTION) + robotPosition.Theta) * laserdata[SelectedPoints[i]]) + robotPosition.Y;

                //x =(Math.Cos((rndSelectedPoints[i] * degreesPerScan * conv)) * laserdata[rndSelectedPoints[i]]);//+robotPosition[0];

                //y =(Math.Sin((rndSelectedPoints[i] * degreesPerScan * conv)) * laserdata[rndSelectedPoints[i]]);//+robotPosition[1];
                sumY += y;
                sumYY += Math.Pow(y, 2);
                sumX += x;
                sumXX += Math.Pow(x, 2);
                sumYX += y * x;
            }

            y_intercept = (sumY * sumXX - sumX * sumYX) / (arraySize * sumXX - Math.Pow(sumX, 2));
            slope = (arraySize * sumYX - sumX * sumY) / (arraySize * sumXX - Math.Pow(sumX, 2));
        }

        private double DistanceToLine(double x, double y, double a, double b)
        {

            //our goal is to calculate point on line closest to x,y
            //then use this to calculate distance between them.
            //calculate line perpendicular to input line. a*ao = -1
            double ao = -1.0 / a;
            //y = aox + bo => bo = y - aox
            double bo = y - ao * x;
            //get intersection between y = ax + b and y = aox + bo
            //so aox + bo = ax + b => aox - ax = b - bo => x = (b - bo)/(ao - a), y = ao*(b - bo)/(ao - a) + bo

            double px = (b - bo) / (ao - a);
            double py = ((ao * (b - bo)) / (ao - a)) + bo;

            return Toolbox.Distance(x, y, px, py);
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
            Landmark lm = new Landmark();
            //convert landmark to map coordinate

            lm.Position = new PointD(x, y);
            lm.range = range;
            lm.bearing = bearing;
            lm.a = a;
            lm.b = b;
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
            double leastDistance = 99999; //99999m is least initial distance, its big
            for (int i = 0; i < DBSize; i++)

            {

                //only associate to landmarks we have seen more than MINOBSERVATIONS times
                if (landmarkDB[i].totalTimesObserved > MINOBSERVATIONS)

                {
                    temp = Distance(lm, landmarkDB[i]);

                    if (temp < leastDistance)

                    {
                        leastDistance = temp;
                        closestLandmark = landmarkDB[i].id;
                    }
                }
            }

            if (leastDistance == 99999)

                id = -1;

            else
            {
                id = landmarkDB[closestLandmark].id;
                totalTimesObserved = landmarkDB[closestLandmark].totalTimesObserved;
            }

        }

        private double Distance(Landmark lm1, Landmark lm2)

        {
            return Math.Sqrt(Math.Pow(lm1.Position.X - lm2.Position.X, 2) + Math.Pow(lm1.Position.Y - lm2.Position.Y, 2));
        }
    }
}