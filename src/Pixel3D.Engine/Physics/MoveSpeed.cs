using System;
using System.Diagnostics;
using Pixel3D.Engine.Maths;

namespace Pixel3D.Engine.Physics
{
    // There is probably a clever formula we can use to generate this on-the-fly... or we can just make a LUT by hand...

    public static class MoveSpeed
    {
#if DEBUG
        // Validate table:
        static MoveSpeed()
        {
            for(int d = 0; d < table.Length; d++)
            {
                Debug.Assert(table[d].Length == d);

                for(int n = 0; n < table[d].Length; n++)
                {
                    Debug.Assert(table[d][n].Length == d);

                    int counter = 0;
                    for(int f = 0; f < table[d][n].Length; f++)
                    {
                        Debug.Assert(table[d][n][f] == 0 || table[d][n][f] == 1);
                        counter += table[d][n][f];
                    }

                    Debug.Assert(counter == n);
                }
            }
        }
#endif


        // Lookup table: [denominator][numerator % denominator][frame % denominator]
        private static readonly int[][][] table = 
        {
            // x/0
            new int[][] { },

            // x/1
            new int[][] { new int[] { 0 } },

            // x/2
            new int[][]
            {
                new int[] { 0, 0 }, // 0/2
                new int[] { 1, 0 }, // 1/2
            },
            
            // x/3
            new int[][]
            {
                new int[] { 0, 0, 0 }, // 0/3
                new int[] { 1, 0, 0 }, // 1/3
                new int[] { 1, 1, 0 }, // 2/3
            },

            // x/4
            new int[][]
            {
                new int[] { 0, 0, 0, 0 }, // 0/4
                new int[] { 1, 0, 0, 0 }, // 1/4
                new int[] { 1, 0, 1, 0 }, // 2/4
                new int[] { 1, 1, 1, 0 }, // 3/4
            },

            // x/5
            new int[][]
            {
                new int[] { 0, 0, 0, 0, 0 }, // 0/5
                new int[] { 1, 0, 0, 0, 0 }, // 1/5
                new int[] { 1, 0, 1, 0, 0 }, // 2/5
                new int[] { 1, 0, 1, 0, 1 }, // 3/5
                new int[] { 1, 1, 1, 0, 1 }, // 4/5
            },

            // x/6
            new int[][]
            {
                new int[] { 0, 0, 0, 0, 0, 0 }, // 0/6
                new int[] { 1, 0, 0, 0, 0, 0 }, // 1/6
                new int[] { 1, 0, 0, 1, 0, 0 }, // 2/6
                new int[] { 1, 0, 1, 0, 1, 0 }, // 3/6
                new int[] { 1, 1, 0, 1, 1, 0 }, // 4/6
                new int[] { 1, 1, 1, 1, 1, 0 }, // 5/6
            },
            
            // x/7
            new int[][]
            {
                new int[] { 0, 0, 0, 0, 0, 0, 0 }, // 0/7
                new int[] { 1, 0, 0, 0, 0, 0, 0 }, // 1/7
                new int[] { 1, 0, 0, 1, 0, 0, 0 }, // 2/7
                new int[] { 1, 0, 1, 0, 1, 0, 0 }, // 3/7
                new int[] { 1, 1, 0, 1, 0, 1, 0 }, // 4/7
                new int[] { 1, 1, 0, 1, 1, 0, 1 }, // 5/7
                new int[] { 1, 1, 1, 1, 1, 1, 0 }, // 6/7
            },

            // x/8
            new int[][]
            {
                new int[] { 0, 0, 0, 0, 0, 0, 0, 0 }, // 0/8
                new int[] { 1, 0, 0, 0, 0, 0, 0, 0 }, // 1/8
                new int[] { 1, 0, 0, 0, 1, 0, 0, 0 }, // 2/8
                new int[] { 1, 0, 0, 1, 0, 0, 1, 0 }, // 3/8
                new int[] { 1, 0, 1, 0, 1, 0, 1, 0 }, // 4/8
                new int[] { 1, 1, 1, 0, 1, 0, 1, 0 }, // 5/8
                new int[] { 1, 1, 1, 0, 1, 1, 1, 0 }, // 6/8
                new int[] { 1, 1, 1, 1, 1, 1, 1, 0 }, // 7/8
            },
        };

        public static int MaxDenominator { get { return table.Length-1; } }


        public static int GetMoveSpeedForFrame(int frame, int numerator, int denominator)
        {
            Debug.Assert(denominator < table.Length);

            frame = Math.Abs(frame);
            int whole = numerator / denominator;
            int remainder = numerator % denominator;

            return whole + table[denominator][remainder][frame % denominator];
        }


        public static int GetMoveSpeedForFrame(int frame, Fraction rate)
        {
            return GetMoveSpeedForFrame(frame, rate.numerator, rate.denominator);
        }
    }
}
