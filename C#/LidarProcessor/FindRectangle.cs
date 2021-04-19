using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace LidarProcessor
{
	// C# program to find largest rectangle
	// with all 1s in a binary matrix
	using System;
	using System.Collections.Generic;

	public static class FindRectangle
    {
		public static List<RectangleOriented> FindAllPossibleRectangle(List<PolarPointRssiExtended> corner_list, double thresold)
        {
			List<RectangleOriented> list_of_rectangles = new List<RectangleOriented>();

			List<int> list_of_case = Enumerable.Range(0, corner_list.Count).ToList(); /// [0,1,2,3,...,n]
			List<List<int>> list_of_combinations_of_corner_index = Toolbox.GetKCombs(list_of_case, 2).ToList().Select(x => x.ToList()).ToList(); /// [[0,1],[0,2],[0,3],[1,2],[1,3],[2,3],...]

			/// [[Dist_0-1, Corner0, Corner1],[Dist_0-2, Corner0,Corner2],[Dist_0-3, Corner0,Corner3],[Dist_1-2, Corner1,Corner2],etc...]
			List<Tuple<double, PolarPointRssiExtended, PolarPointRssiExtended>> list_of_combinations_corners_and_distance = list_of_combinations_of_corner_index.Select(
				x => new Tuple<double, PolarPointRssiExtended, PolarPointRssiExtended> (
					Toolbox.Distance(corner_list[x[0]].Pt, corner_list[x[1]].Pt), 
					corner_list[x[0]], 
					corner_list[x[1]] 
				)
			).ToList();



			double previous_distance = 0;
			PointD previous_vector_point_a = new PointD(0, 0);
			PointD previous_vector_point_b = new PointD(0, 0);
			PointD previous_center_vector_point = new PointD(0,0);

			foreach (var vector_distance in list_of_combinations_corners_and_distance.OrderByDescending(x => x.Item1))
            {
				PointD actual_vector_point_a = Toolbox.ConvertPolarToPointD(vector_distance.Item2.Pt);
				PointD actual_vector_point_b = Toolbox.ConvertPolarToPointD(vector_distance.Item3.Pt);
				PointD actual_center_vector_point = new PointD((actual_vector_point_b.X + actual_vector_point_a.X) / 2, (actual_vector_point_b.Y + actual_vector_point_a.Y) / 2);
				
				if (Toolbox.Distance(actual_vector_point_a, previous_vector_point_a) != 0 && Toolbox.Distance(actual_vector_point_a, previous_vector_point_b) != 0 && Toolbox.Distance(actual_vector_point_b, previous_vector_point_a) != 0 && Toolbox.Distance(actual_vector_point_b, previous_vector_point_b) != 0)
				{
					if (previous_distance + thresold >= vector_distance.Item1 && previous_distance - thresold <= vector_distance.Item1)
					{
						if (Toolbox.Distance(actual_center_vector_point, previous_center_vector_point) < thresold)
						{
							PointD mean_center_point = new PointD((actual_center_vector_point.X + previous_center_vector_point.X) / 2, (actual_center_vector_point.Y + previous_center_vector_point.Y) / 2);

							double lenght = Toolbox.Distance(actual_vector_point_a, previous_vector_point_a);
							double width = Toolbox.Distance(actual_vector_point_b, previous_vector_point_a);
							double angle = Math.Atan2(actual_vector_point_a.Y - mean_center_point.Y, actual_vector_point_a.X - mean_center_point.X);

							RectangleOriented rectangle = new RectangleOriented(mean_center_point, lenght, width, angle);
							list_of_rectangles.Add(rectangle);
							Console.WriteLine("L: " + lenght + " W: " + width + " A: " + Toolbox.RadToDeg(angle));
						}

					}
				}
				previous_distance = vector_distance.Item1;
				previous_vector_point_a = actual_vector_point_a;
				previous_vector_point_b = actual_vector_point_b;
				previous_center_vector_point = actual_center_vector_point;
				

            }



            return list_of_rectangles;
        } 
    }


	class GFG
	{
		// Finds the maximum area under the
		// histogram represented by histogram.
		// See below article for details.
		// https://
		// www.geeksforgeeks.org/largest-rectangle-under-histogram/
		public static int maxHist(int R, int C, int[] row)
		{
			// Create an empty stack. The stack
			// holds indexes of hist[] array.
			// The bars stored in stack are always
			// in increasing order of their heights.
			Stack<int> result = new Stack<int>();

			int top_val; // Top of stack

			int max_area = 0; // Initialize max area in
							  // current row (or histogram)

			int area = 0; // Initialize area with
						  // current top

			// Run through all bars of
			// given histogram (or row)
			int i = 0;
			while (i < C)
			{
				// If this bar is higher than the
				// bar on top stack, push it to stack
				if (result.Count == 0
					|| row[result.Peek()] <= row[i])
				{
					result.Push(i++);
				}

				else
				{
					// If this bar is lower than top
					// of stack, then calculate area of
					// rectangle with stack top as
					// the smallest (or minimum height)
					// bar. 'i' is 'right index' for
					// the top and element before
					// top in stack is 'left index'
					top_val = row[result.Peek()];
					result.Pop();
					area = top_val * i;

					if (result.Count > 0)
					{
						area
							= top_val * (i - result.Peek() - 1);
					}
					max_area = Math.Max(area, max_area);
				}
			}

			// Now pop the remaining bars from
			// stack and calculate area with
			// every popped bar as the smallest bar
			while (result.Count > 0)
			{
				top_val = row[result.Peek()];
				result.Pop();
				area = top_val * i;
				if (result.Count > 0)
				{
					area = top_val * (i - result.Peek() - 1);
				}

				max_area = Math.Max(area, max_area);
			}
			return max_area;
		}

		// Returns area of the largest
		// rectangle with all 1s in A[][]
		public static int maxRectangle(int R, int C, int[][] A)
		{
			// Calculate area for first row
			// and initialize it as result
			int result = maxHist(R, C, A[0]);

			// iterate over row to find
			// maximum rectangular area
			// considering each row as histogram
			for (int i = 1; i < R; i++)
			{
				for (int j = 0; j < C; j++)
				{

					// if A[i][j] is 1 then
					// add A[i -1][j]
					if (A[i][j] == 1)
					{
						A[i][j] += A[i - 1][j];
					}
				}

				// Update result if area with current
				// row (as last row of rectangle) is more
				result = Math.Max(result, maxHist(R, C, A[i]));
			}

			return result;
		}

		// Driver code
		public static void Main(string[] args)
		{
			int R = 4;
			int C = 4;

			int[][] A
				= new int[][] { new int[] { 0, 1, 1, 0 },
							new int[] { 1, 1, 1, 1 },
							new int[] { 1, 1, 1, 1 },
							new int[] { 1, 1, 0, 0 } };
			Console.Write("Area of maximum rectangle is "
						+ maxRectangle(R, C, A));
		}
	}

	// This code is contributed by Shrikant13

}
