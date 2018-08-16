using Microsoft.Xna.Framework;

namespace Pixel3D
{
    public static class RectangleExtensions
    {
        #region Should have been operators

        public static Rectangle Add(this Rectangle rectangle, Point point)
        {
            rectangle.X += point.X;
            rectangle.Y += point.Y;
            return rectangle;
        }

        #endregion


        #region World Coordinates (to make orientation slightly less confusing...)

        /// <summary>Get the inclusive position of the bottom of this rectangle in World coordinates</summary>
        public static int WorldBottom(this Rectangle value)
        {
            return value.Y;
        }

        /// <summary>Get the exclusive position of the bottom of this rectangle in World coordinates (Y + Height)</summary>
        public static int WorldTop(this Rectangle value)
        {
            return value.Y + value.Height;
        }

        #endregion



        /// <summary>Find the union of two rectangles, disregarding rectangles having zero area.</summary>
        public static Rectangle UnionIgnoreEmpty(Rectangle value1, Rectangle value2)
        {
            if(value2.Width == 0 || value2.Height == 0)
            {
                if(value1.Width == 0 || value1.Height == 0)
                    return Rectangle.Empty;
                else
                    return value1;
            }

            if(value1.Width == 0 || value1.Height == 0)
                return value2;

            return Rectangle.Union(value1, value2);
        }

        public static bool ContainsIgnoreEmpty(this Rectangle containing, Rectangle contained)
        {
            if(containing.Width == 0 || containing.Height == 0)
                return false; // If we have zero size, we can't contain anything!
            if(containing.Width == 0 || containing.Height == 0)
                return true; // If they have zero size, assume they're degenerate and ignore

            return containing.Contains(contained);
        }



        /// <summary>Flip a rectangle on the X axis (around the centre of the origin pixel)</summary>
        /// <remarks>
        /// If <c>Rectangle bounds</c> represents some data bounds, then iterating within
        /// <c>bounds.FlipX()</c> and accessing <c>[-x, y]</c> will give the mirrored data.
        /// </remarks>
        public static Rectangle FlipXIndexable(this Rectangle value)
        {
            value.X = 1 - (value.X + value.Width);
            return value;
        }

        /// <summary>Flip a rectangle on the Y axis (around the centre of the origin pixel)</summary>
        /// <remarks>
        /// If <c>Rectangle bounds</c> represents some data bounds, then iterating within
        /// <c>bounds.FlipY()</c> and accessing <c>[x, -y]</c> will give the mirrored data.
        /// </remarks>
        public static Rectangle FlipYIndexable(this Rectangle value)
        {
            value.Y = 1 - (value.Y + value.Height);
            return value;
        }


        /// <summary>Flip a rectangle on the X axis (around the edge between the origin pixel and the pixel at X=-1)</summary>
        public static Rectangle FlipXNonIndexable(this Rectangle value)
        {
            value.X = -(value.X + value.Width);
            return value;
        }

        /// <summary>Flip a rectangle on the Y axis (around the edge between the origin pixel and the pixel at Y=-1)</summary>
        public static Rectangle FlipYNonIndexable(this Rectangle value)
        {
            value.Y = -(value.Y + value.Height);
            return value;
        }



        /// <summary>Given a rectangle in a region, return the rectangle if the region is flipped on the X axis.</summary>
        public static Rectangle RegionFlipX(this Rectangle value, int regionWidth)
        {
            value.X = regionWidth - (value.X + value.Width);
            return value;
        }

        /// <summary>Given a rectangle in a region, return the rectangle if the region is flipped on the Y axis.</summary>
        public static Rectangle RegionFlipY(this Rectangle value, int regionHeight)
        {
            value.Y = regionHeight - (value.Y + value.Height);
            return value;
        }

    }
}
