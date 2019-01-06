// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Pixel3D
{
    /// <summary>2x2 integer transform. Row-major to match XNA.</summary>
    public struct IntMatrix2
    {
        public int xToX, yToX, xToY, yToY;

        public IntMatrix2(int xToX, int yToX, int xToY, int yToY)
        {
            this.xToX = xToX;
            this.yToX = yToX;
            this.xToY = xToY;
            this.yToY = yToY;
        }


        public Point Transform(Point p)
        {
            return new Point(p.X * xToX + p.Y * yToX, p.X * xToY + p.Y * yToY);
        }

        public static IntMatrix2 operator*(IntMatrix2 lhs, IntMatrix2 rhs)
        {
            IntMatrix2 output;

            // Column Major (doesn't match XNA, whoops)
            //output.xToX = lhs.xToX * rhs.xToX + lhs.yToX * rhs.xToY;
            //output.yToX = lhs.xToX * rhs.yToX + lhs.yToX * rhs.yToY;
            //output.xToY = lhs.xToY * rhs.xToX + lhs.yToY * rhs.xToY;
            //output.yToY = lhs.xToY * rhs.yToX + lhs.yToY * rhs.yToY;

            // Row Major (matches XNA)
            output.xToX = rhs.xToX * lhs.xToX + rhs.yToX * lhs.xToY;
            output.yToX = rhs.xToX * lhs.yToX + rhs.yToX * lhs.yToY;
            output.xToY = rhs.xToY * lhs.xToX + rhs.yToY * lhs.xToY;
            output.yToY = rhs.xToY * lhs.yToX + rhs.yToY * lhs.yToY;

            return output;
        }


        #region Equality and such

        public static bool operator==(IntMatrix2 lhs, IntMatrix2 rhs)
        {
            return lhs.xToX == rhs.xToX
                    && lhs.yToX == rhs.yToX
                    && lhs.xToY == rhs.xToY
                    && lhs.yToY == rhs.yToY;
        }

        public static bool operator!=(IntMatrix2 lhs, IntMatrix2 rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return xToX.GetHashCode() ^ yToX.GetHashCode() ^ xToY.GetHashCode() ^ yToY.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(!(obj is IntMatrix2))
                return false;
            return this == (IntMatrix2)obj;
        }

        public override string ToString()
        {
            return string.Format("X = | {0} {1} |  Y = | {2} {3} |", xToX, yToX, xToY, yToY);
        }

        #endregion



        public static IntMatrix2 Identity { get { return new IntMatrix2(1, 0, 0, 1); } }
        public static IntMatrix2 FlipX { get { return new IntMatrix2(-1, 0, 0, 1); } }
        public static IntMatrix2 FlipY { get { return new IntMatrix2(1, 0, 0, -1); } }

        /// <summary>Rotate 90 degrees (Clockwise in Client coordinates, Counter-Clockwise in World coordinates)</summary>
        public static IntMatrix2 Rotate90 { get { return new IntMatrix2(0, -1, 1, 0); } }
        /// <summary>Rotate 180 degrees</summary>
        public static IntMatrix2 Rotate180 { get { return new IntMatrix2(-1, 0, 0, -1); } }
        /// <summary>Rotate 270 degrees (Clockwise in Client coordinates, Counter-Clockwise in World coordinates)</summary>
        public static IntMatrix2 Rotate270 { get { return new IntMatrix2(0, 1, -1, 0); } }



        /// <summary>Return an array of all possible combinations of 90-degree rotations and flips (NOTE: Does not include the Identity transform)</summary>
        public static IntMatrix2[] GetAllRotatesAndFlips()
        {
            return new[]
            {
                FlipX,
                FlipY,
                Rotate180,
                Rotate90,
                Rotate270,
                FlipX * Rotate90,
                FlipX * Rotate270,
            };
        }


        /// <summary>Get the inverse of a matrix where that inverse can be represented as an integer matrix</summary>
        public IntMatrix2 GetIntegerInverse()
        {
            Debug.Assert(Math.Abs(xToX) <= 1);
            Debug.Assert(Math.Abs(yToX) <= 1);
            Debug.Assert(Math.Abs(xToY) <= 1);
            Debug.Assert(Math.Abs(yToY) <= 1);

            // Determinant = 1 / (ad - bc)
            int dd = xToX * yToY - yToX * xToY; // "Determinant Denominator"

            // Validate that we can use the denominator directly as the value (1/1 == 1, and 1/-1 == -1)
            if(dd != -1 && dd != 1)
            {
                Debug.Assert(false); // <- Not a usable result
                return default(IntMatrix2); // <- Oh well
            }

            var inverse = new IntMatrix2(dd * yToY, -dd * yToX, -dd * xToY, dd * xToX);

            Debug.Assert(this * inverse == Identity);
            Debug.Assert(inverse * this == Identity);

            return inverse;
        }

    }
}
