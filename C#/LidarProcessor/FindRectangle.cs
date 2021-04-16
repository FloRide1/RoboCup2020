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

			List<List<PolarPointRssiExtended>> list_of_combinations_of_corner = list_of_combinations_of_corner_index.Select(x => new List<PolarPointRssiExtended>() { corner_list[x[0]], corner_list[x[1]] }).ToList();

			List<Tuple<double, List<PolarPointRssiExtended>>> list_of_combinations_distance = list_of_combinations_of_corner.Select(
				x => new Tuple<double, List<PolarPointRssiExtended>> (
					Toolbox.Distance(x[0].Pt, x[1].Pt), 
					new List<PolarPointRssiExtended>() { x[0], x[1] }
				)
			).ToList();



			double distance = 0;
			Tuple<PointD, PointD> old_point = new Tuple<PointD, PointD>(null, null);

			List<List<PointD>> list_of_vectors = new List<List<PointD>>();

			foreach (var combi_dist in list_of_combinations_distance.OrderByDescending(x => x.Item1))
            {
				if (distance + thresold >= combi_dist.Item1 && distance - thresold <= combi_dist.Item1)
                {
					PointD actual_point_a = Toolbox.ConvertPolarToPointD(combi_dist.Item2[0].Pt);
					PointD actual_point_b = Toolbox.ConvertPolarToPointD(combi_dist.Item2[1].Pt);

					PointD vector_point_1 = new PointD((old_point.Item1.X + actual_point_a.X) / 2, (old_point.Item1.Y + actual_point_a.Y) / 2);
					PointD vector_point_2 = new PointD((old_point.Item2.X + actual_point_b.X) / 2, (old_point.Item2.Y + actual_point_b.Y) / 2);

					if (Toolbox.Distance(vector_point_1, vector_point_2) < thresold)
                    {
						PointD mean_center_point = new PointD((vector_point_1.X + vector_point_2.X) / 2, (vector_point_1.Y + vector_point_2.Y) / 2);

						double lenght = Toolbox.Distance(actual_point_a, old_point.Item1);
						double width = Toolbox.Distance(actual_point_b, old_point.Item2);
						double angle = Math.Atan2(actual_point_a.Y - mean_center_point.Y, actual_point_a.X - mean_center_point.X);
						RectangleOriented rectangle = new RectangleOriented(mean_center_point, lenght, width, angle);
						Console.WriteLine(Toolbox.Distance(vector_point_1, vector_point_2));
					}
					
                } else
                {
					distance = combi_dist.Item1;
					old_point = new Tuple<PointD, PointD> (Toolbox.ConvertPolarToPointD(combi_dist.Item2[0].Pt), Toolbox.ConvertPolarToPointD(combi_dist.Item2[1].Pt));
                }
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
