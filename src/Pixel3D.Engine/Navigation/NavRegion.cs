using System;
using System.Diagnostics;
using Pixel3D.Engine.Maths;

namespace Pixel3D.Engine.Navigation
{
    public struct NavRegion
    {
        public int startX, startZ, endX, endZ;

        public NavRegion(int x, int z)
        {
            startX = x;
            startZ = z;
            endX = x+1;
            endZ = z+1;
        }


        public bool Contains(NavRegion other)
        {
            return this.startX <= other.startX 
                && this.endX   >= other.endX
                && this.startZ <= other.startZ
                && this.endZ   >= other.endZ;
        }

        public bool Contains(Pixel3D.Bounds other) // <- bleh.
        {
            return this.startX <= other.startX 
                && this.endX   >= other.endX
                && this.startZ <= other.startY
                && this.endZ   >= other.endY;
        }

        public bool Contains(int x, int z)
        {
            return startX <= x && x < endX
                && startZ <= z && z < endZ;
        }

        public bool ContainsWithSlop(int x, int z, int slopX, int slopZ)
        {
            int halfSlopX = slopX >> 1;
            int halfSlopZ = slopZ >> 1;

            return startX - halfSlopX <= x && x < endX + (slopX - halfSlopX)
                && startZ - halfSlopZ <= z && z < endZ + (slopZ - halfSlopZ);
        }



        // NOTE: Both primary and secondary distances are included in output,
        //       in order to get a better path out of A*. With weighting on the
        //       primary, for flavour.
        public const int PrimaryDistanceWeight = 4;
        public const int SecondaryDistanceWeight = 1;


        // TODO: PERF: Lots of calls to this method only require the distance! (use GetWeightedDistance)
        public static int GetWeightedDistanceAndClip(NavRegion from, NavRegion to, Fraction xSpeed, Fraction zSpeed, out NavRegion clipped)
        {
            int xDistance;
            if(to.endX <= from.startX) // Target is to the left
            {
                clipped.startX = to.endX-1;
                clipped.endX = to.endX;
                xDistance = from.startX - (to.endX-1);
            }
            else if(to.startX >= from.endX) // Target is to the right
            {
                clipped.startX = to.startX;
                clipped.endX = to.startX+1;
                xDistance = to.startX - (from.endX-1);
            }
            else // Intersection on X axis
            {
                clipped.startX = to.startX;
                clipped.endX = to.endX;
                xDistance = 0;
            }

            int zDistance;
            if(to.endZ <= from.startZ) // Target is in front
            {
                clipped.startZ = to.endZ-1;
                clipped.endZ = to.endZ;
                zDistance = from.startZ - (to.endZ-1);
            }
            else if(to.startZ >= from.endZ) // Target is behind
            {
                clipped.startZ = to.startZ;
                clipped.endZ = to.startZ+1;
                zDistance = to.startZ - (from.endZ-1);
            }
            else // Intersection on Z axis
            {
                clipped.startZ = to.startZ;
                clipped.endZ = to.endZ;
                zDistance = 0;
            }


            int xWeightedDistance = (xDistance * zSpeed.numerator) / zSpeed.denominator;
            int zWeightedDistance = (zDistance * xSpeed.numerator) / xSpeed.denominator;

            if(xWeightedDistance == 0 && zWeightedDistance == 0) // Intersection
            {
                clipped.startX = Math.Max(from.startX, to.startX);
                clipped.startZ = Math.Max(from.startZ, to.startZ);
                clipped.endX = Math.Min(from.endX, to.endX);
                clipped.endZ = Math.Min(from.endZ, to.endZ);
                return 0;
            }
            else if(xWeightedDistance >= zWeightedDistance) // Left or right (also corner)
            {
                int expand = Fraction.MultiplyDivideTruncate(xDistance, zSpeed, xSpeed); // (xDistance * zSpeed / xSpeed)
                int bottomEdge = from.startZ - expand;
                int topEdge = from.endZ + expand;
                clipped.startZ = Math.Max(to.startZ, bottomEdge);
                clipped.endZ = Math.Min(to.endZ, topEdge);
                return xWeightedDistance * PrimaryDistanceWeight + zWeightedDistance * SecondaryDistanceWeight;
            }
            else // Above or below
            {
                int expand = Fraction.MultiplyDivideTruncate(zDistance, xSpeed, zSpeed); // (zDistance * xSpeed / zSpeed)
                int leftEdge = from.startX - expand;
                int rightEdge = from.endX + expand;
                clipped.startX = Math.Max(to.startX, leftEdge);
                clipped.endX = Math.Min(to.endX, rightEdge);
                return zWeightedDistance * PrimaryDistanceWeight + zWeightedDistance * SecondaryDistanceWeight;
            }
        }


        public static int GetWeightedDistance(int fromX, int fromZ, NavRegion to, Fraction xSpeed, Fraction zSpeed)
        {
            int xDistance;
            if(to.endX <= fromX) // Target is to the left
            {
                xDistance = fromX - (to.endX-1);
            }
            else if(to.startX > fromX) // Target is to the right
            {
                xDistance = to.startX - fromX;
            }
            else // Intersection on X axis
            {
                xDistance = 0;
            }

            int zDistance;
            if(to.endZ <= fromZ) // Target is in front
            {
                zDistance = fromZ - (to.endZ-1);
            }
            else if(to.startZ >= fromZ) // Target is behind
            {
                zDistance = to.startZ - fromZ;
            }
            else // Intersection on Z axis
            {
                zDistance = 0;
            }


            int xWeightedDistance = (xDistance * zSpeed.numerator) / zSpeed.denominator;
            int zWeightedDistance = (zDistance * xSpeed.numerator) / xSpeed.denominator;

            if(xWeightedDistance >= zWeightedDistance) // Left or right or intersection
            {
                return xWeightedDistance * PrimaryDistanceWeight + zWeightedDistance * SecondaryDistanceWeight;
            }
            else // Above or below
            {
                return zWeightedDistance * PrimaryDistanceWeight + zWeightedDistance * SecondaryDistanceWeight;
            }
        }

        public static int GetWeightedDistance(int fromX, int fromZ, int toX, int toZ, Fraction xSpeed, Fraction zSpeed)
        {
            int xDistance = Math.Abs(toX - fromX);
            int zDistance = Math.Abs(toZ - fromZ);

            int xWeightedDistance = (xDistance * zSpeed.numerator) / zSpeed.denominator;
            int zWeightedDistance = (zDistance * xSpeed.numerator) / xSpeed.denominator;

            if(xWeightedDistance >= zWeightedDistance) // Left or right or intersection
            {
                return xWeightedDistance * PrimaryDistanceWeight + zWeightedDistance * SecondaryDistanceWeight;
            }
            else // Above or below
            {
                return zWeightedDistance * PrimaryDistanceWeight + zWeightedDistance * SecondaryDistanceWeight;
            }
        }


        #region Projection

        static int ProjectToXAlignedAxis(int axisY, int fromX, int fromY, Fraction xSpeed, Fraction ySpeed)
        {
            Debug.Assert(ySpeed.numerator != 0);
            return fromX - Fraction.MultiplyDivideTruncate((fromY - axisY), xSpeed, ySpeed);

            // Old integer version:
            //return fromX - ((fromY - axisY) * xSpeed) / ySpeed; // <- NOTE: This seems to do the right thing as far as rounding is concerned
        }


        public MinMax GetPositiveProjectionToXAlignedAxis(int axisY, Fraction xSpeed, Fraction ySpeed)
        {
            int min = ProjectToXAlignedAxis(axisY, startX, endZ-1, +xSpeed, ySpeed);
            int max = ProjectToXAlignedAxis(axisY, endX-1, startZ, +xSpeed, ySpeed);
            return new MinMax(min, max);
        }

        public MinMax GetNegativeProjectionToXAlignedAxis(int axisY, Fraction xSpeed, Fraction ySpeed)
        {
            int min = ProjectToXAlignedAxis(axisY, startX, startZ, -xSpeed, ySpeed);
            int max = ProjectToXAlignedAxis(axisY, endX-1, endZ-1, -xSpeed, ySpeed);
            return new MinMax(min, max);
        }

        public MinMax GetXMinMax()
        {
            return new MinMax { min = startX, max = endX-1 };
        }

        public MinMax GetZMinMax()
        {
            return new MinMax { min = startZ, max = endZ-1 };
        }

        #endregion

    }
}
