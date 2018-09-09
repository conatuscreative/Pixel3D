using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.Navigation;

namespace Pixel3D.Engine.Navigation
{
    /// <summary>Eight possible directions, maps to DirectionFlags</summary>
    public enum DirectionNumber : byte
    {
        // NOTE: Values map to DirectionFlags and should be mod 7 (3 bits) for rotation
        East      = 0,
        NorthEast = 1,
        North     = 2,
        NorthWest = 3,
        West      = 4,
        SouthWest = 5,
        South     = 6,
        SouthEast = 7,

        Count = 8,
        None = 8,
    }

    public static class DirectionNumberExtensions
    {
        public static DirectionNumber RotateCounterClockwise(this DirectionNumber v)
        {
            return (DirectionNumber)(((int)v + 1) & 7);
        }

        public static DirectionNumber RotateClockwise(this DirectionNumber v)
        {
            return (DirectionNumber)(((int)v - 1) & 7);
        }

        public static DirectionNumber Rotate180(this DirectionNumber v)
        {
            return (DirectionNumber)(((int)v + 4) & 7);
        }

        public static DirectionNumber Rotate(this DirectionNumber v, int steps)
        {
            return (DirectionNumber)(((int)v + steps) & 7);
        }


        public static DirectionFlags ToFlag(this DirectionNumber v)
        {
            return (DirectionFlags)(1u << (int)v);
        }

        public static bool IsValid(this DirectionNumber v)
        {
            Debug.Assert((int)DirectionNumber.Count == 8); // <- this is assumed.
            return ((int)v & 7) == (int)v;
        }

        public static Point ToMovement(this DirectionNumber d)
        {
            switch (d)
            {
                case DirectionNumber.East: return new Point(1, 0);
                case DirectionNumber.NorthEast: return new Point(1, 1);
                case DirectionNumber.North: return new Point(0, 1);
                case DirectionNumber.NorthWest: return new Point(-1, 1);
                case DirectionNumber.West: return new Point(-1, 0);
                case DirectionNumber.SouthWest: return new Point(-1, -1);
                case DirectionNumber.South: return new Point(0, -1);
                case DirectionNumber.SouthEast: return new Point(1, -1);

                default:
                    return Point.Zero;
            }
        }

        public static DirectionNumber FromMovement(Point movement)
        {
            Debug.Assert(Math.Abs(movement.X) <= 1 && Math.Abs(movement.Y) <= 1);

            // PERF: Could convert this to a single jump table by combining movement.X and movement.Y (as per DriveDirection)
            switch (movement.X)
            {
                case -1:
                    switch (movement.Y)
                    {
                        case -1: return DirectionNumber.SouthWest;
                        case 0: return DirectionNumber.West;
                        case 1: return DirectionNumber.NorthWest;
                    }
                    break;

                case 0:
                    switch (movement.Y)
                    {
                        case -1: return DirectionNumber.South;
                        case 0: return DirectionNumber.None;
                        case 1: return DirectionNumber.North;
                    }
                    break;

                case 1:
                    switch (movement.Y)
                    {
                        case -1: return DirectionNumber.SouthEast;
                        case 0: return DirectionNumber.East;
                        case 1: return DirectionNumber.NorthEast;
                    }
                    break;
            }

            Debug.Assert(false); // <- shouldn't happen
            return DirectionNumber.None;

        }



        public static DirectionNumber GetNextDirectionToProjection(int x, int z, DirectionNumber previous, NavRegionProjection projection)
        {
            // This incredible switch construct is so that we can jump directly to the previous value, saving us a lot of code in the general case
            // And, in particular, allowing us to avoid shuffling our large arguments between stacks, as the dopey JIT won't inline the large comparisons used here

            // The use of non-short-circuiting is a speculative optimisation against expected cost of branch mis-prediction (NOTE: inspected JIT but haven't profiled yet -AR)
            // This slightly expands our output code size (~5 icache lines total), but perhaps not a big deal due to the switch behaviour in the typical case.

            switch (previous)
            {
                default:
                    previous = 0; // Reset
                    goto case DirectionNumber.East;

                case DirectionNumber.East:
                    {
                        if ((x < projection.x.min & projection.z.min <= z & z <= projection.z.max) | (x < projection.negative.min & x < projection.positive.min))
                            return DirectionNumber.East;
                    }
                    if (previous == 0)
                        goto case DirectionNumber.West;
                    else
                        goto default;

                case DirectionNumber.West:
                    {
                        if ((x > projection.x.max & projection.z.min <= z & z <= projection.z.max) | (x > projection.negative.max & x > projection.positive.max))
                            return DirectionNumber.West;
                    }
                    if (previous == 0)
                        goto case DirectionNumber.North;
                    else
                        goto default;

                case DirectionNumber.North:
                    {
                        if ((z < projection.z.min & projection.x.min <= x & x <= projection.x.max) | (x < projection.negative.min & x > projection.positive.max))
                            return DirectionNumber.North;
                    }
                    if (previous == 0)
                        goto case DirectionNumber.South;
                    else
                        goto default;

                case DirectionNumber.South:
                    {
                        if ((z > projection.z.max & projection.x.min <= x & x <= projection.x.max) | (x > projection.negative.max & x < projection.positive.min))
                            return DirectionNumber.South;
                    }
                    if (previous == 0)
                        goto case DirectionNumber.NorthEast;
                    else
                        goto default;

                case DirectionNumber.NorthEast:
                    {
                        if ((x <= projection.x.max & z <= projection.z.max & projection.positive.min <= x & x <= projection.positive.max)
                           | (x < projection.x.min & z < projection.z.min))
                            return DirectionNumber.NorthEast;
                    }
                    if (previous == 0)
                        goto case DirectionNumber.NorthWest;
                    else
                        goto default;

                case DirectionNumber.NorthWest:
                    {
                        if ((x >= projection.x.min & z <= projection.z.max & projection.negative.min <= x & x <= projection.negative.max)
                           | (x > projection.x.max & z < projection.z.min))
                            return DirectionNumber.NorthWest;
                    }
                    if (previous == 0)
                        goto case DirectionNumber.SouthEast;
                    else
                        goto default;

                case DirectionNumber.SouthEast:
                    {
                        if ((x <= projection.x.max & z >= projection.z.min & projection.negative.min <= x & x <= projection.negative.max)
                           | (x < projection.x.min & z > projection.z.max))
                            return DirectionNumber.SouthEast;
                    }
                    if (previous == 0)
                        goto case DirectionNumber.SouthWest;
                    else
                        goto default;

                case DirectionNumber.SouthWest:
                    {
                        if ((x >= projection.x.min & z >= projection.z.min & projection.positive.min <= x & x <= projection.positive.max)
                           | (x > projection.x.max & z > projection.z.max))
                            return DirectionNumber.SouthWest;
                    }
                    if (previous == 0)
                        return 0; // Nowhere to go :(
                    else
                        goto default;
            }
        }
    }
}
