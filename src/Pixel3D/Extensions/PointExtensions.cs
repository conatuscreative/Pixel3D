using Microsoft.Xna.Framework;

namespace Pixel3D.Extensions
{
    public static class PointExtensions
    {
        public static Vector2 AsVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static Position AsPosition(this Point point)
        {
            return new Position(point.X, point.Y, 0);
        }

        public static Point AsPoint(this Position position)
        {
            return new Point(position.X, position.Y);
        }


        public static int DistanceSquared(this Point point, Point other)
        {
            return (point.X - other.X) * (point.X - other.X) + (point.Y - other.Y) * (point.Y - other.Y);
        }


        public static Point Add(this Point point, Point other)
        {
            return new Point(point.X + other.X, point.Y + other.Y);
        }

        public static Point Subtract(this Point point, Point other)
        {
            return new Point(point.X - other.X, point.Y - other.Y);
        }

        public static Point Negate(this Point point)
        {
            return new Point(-point.X, -point.Y);
        }

        public static Point FlipY(this Point point)
        {
            return new Point(point.X, -point.Y);
        }


        /// <summary>Given a point representing a pixel in a region, return the point if the region is flipped on the X axis.</summary>
        public static Point RegionFlipX(this Point value, int regionWidth)
        {
            value.X = regionWidth - 1 - value.X;
            return value;
        }

        /// <summary>Given a point representing a pixel in a region, return the point if the region is flipped on the Y axis.</summary>
        public static Point RegionFlipY(this Point value, int regionHeight)
        {
            value.Y = regionHeight - 1 - value.Y;
            return value;
        }
    }
}
