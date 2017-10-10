using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pixel3D
{
    public struct AABB
    {
        /// <summary>Create a new AABB. Nominally an "inclusive" bounds (min and max positions are considered inside).</summary>
        /// <param name="min">The bottom front left position (in standard coordinates)</param>
        /// <param name="max">The top back right position (in standard coordinates)</param>
        public AABB(Position min, Position max)
        {
            this.min = min;
            this.max = max;
        }

        public AABB(int left, int right, int bottom, int top, int front, int back)
        {
            this.min = new Position(left, bottom, front);
            this.max = new Position(right, top, back);
        }

        public Position min, max;


        #region Corners

        public Position BottomFrontLeft  { get { return min; } }
        public Position BottomBackLeft   { get { return new Position(min.X, min.Y, max.Z); } }
        public Position BottomFrontRight { get { return new Position(max.X, min.Y, min.Z); } }
        public Position BottomBackRight  { get { return new Position(max.X, min.Y, max.Z); } }
        public Position TopFrontLeft     { get { return new Position(min.X, max.Y, min.Z); } }
        public Position TopBackLeft      { get { return new Position(min.X, max.Y, max.Z); } }
        public Position TopFrontRight    { get { return new Position(max.X, max.Y, min.Z); } }
        public Position TopBackRight     { get { return max; } }

        #endregion


        #region Faces

        public int Left   { get { return min.X; } }
        public int Right  { get { return max.X; } }
        public int Bottom { get { return min.Y; } }
        public int Top    { get { return max.Y; } }
        public int Front  { get { return min.Z; } }
        public int Back   { get { return max.Z; } }

        #endregion


        #region Operators

        public static AABB operator +(AABB aabb, Position offset)
        {
            return new AABB(aabb.min + offset, aabb.max + offset);
        }

        #endregion



        public void FlipXInPlace()
        {
            int tempX = -min.X;
            min.X = -max.X;
            max.X = tempX;
        }

        public bool Contains(Position position)
        {
            return position.X >= min.X && position.X <= max.X
                && position.Y >= min.Y && position.Y <= max.Y
                && position.Z >= min.Z && position.Z <= max.Z;
        }

        public bool Intersects(AABB other)
        {
            return !(this.max.X < other.min.X || this.min.X > other.max.X)
                    && !(this.max.Y < other.min.Y || this.min.Y > other.max.Y)
                    && !(this.max.Z < other.min.Z || this.min.Z > other.max.Z);
        }

        public int DistanceSquaredTo(Position position)
        {
            int xDistance = 0;
            if(position.X < min.X)
                xDistance = min.X - position.X;
            else if(position.X > max.X)
                xDistance = position.X - max.X;

            int yDistance = 0;
            if(position.Y < min.Y)
                yDistance = min.Y - position.Y;
            else if(position.Y > max.Y)
                yDistance = position.Y - max.Y;

            int zDistance = 0;
            if(position.Z < min.Z)
                zDistance = min.Z - position.Z;
            else if(position.Z > max.Z)
                zDistance = position.Z - max.Z;

            return xDistance * xDistance + yDistance * yDistance + zDistance * zDistance;
        }

    }


    public static class AABBExtensions
    {
        public static void Write(this BinaryWriter bw, AABB aabb)
        {
            bw.Write(aabb.min);
            bw.Write(aabb.max);
        }

        public static AABB ReadAABB(this BinaryReader br)
        {
            AABB aabb;
            aabb.min = br.ReadPosition();
            aabb.max = br.ReadPosition();
            return aabb;
        }
    }
}

