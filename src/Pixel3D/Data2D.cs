using System;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Pixel3D.Extensions;

namespace Pixel3D
{
    /// <summary>A wrapper around a 1D array that represents 2D data</summary>
    public struct Data2D<T>
    {
        public Data2D(T[] data, int offsetX, int offsetY, int width, int height)
        {
            this.Data = data;
            this.OffsetX = offsetX;
            this.OffsetY = offsetY;
            this.Width = width;
            this.Height = height;

            Debug.Assert((width * height == 0 && data == null) || data.Length == width * height);
        }

        public Data2D(T[] data, Rectangle bounds) : this(data, bounds.X, bounds.Y, bounds.Width, bounds.Height) { }


        /// <summary>Create a new data buffer</summary>
        public Data2D(int offsetX, int offsetY, int width, int height)
        {
            Debug.Assert(width >= 0 && height >= 0);

            this.Data = (width != 0 && height != 0) ? new T[width * height] : null;
            this.OffsetX = offsetX;
            this.OffsetY = offsetY;
            this.Width = width;
            this.Height = height;
        }

        public Data2D(Rectangle bounds) : this(bounds.X, bounds.Y, bounds.Width, bounds.Height) { }

        public Data2D<T> Clone()
        {
            return new Data2D<T>(Data != null ? (T[])Data.Clone() : null, OffsetX, OffsetY, Width, Height);
        }


        public bool HasData { get { return Width != 0 && Height != 0; } }


        public readonly T[] Data;
        public int OffsetX, OffsetY;
        public readonly int Width, Height;

        public Rectangle Bounds { get { return new Rectangle(OffsetX, OffsetY, Width, Height); } }

        /// <summary>Inclusive X-axis minimum bound</summary>
        public int StartX { get { return OffsetX; } }
        /// <summary>Inclusive Y-axis minimum bound</summary>
        public int StartY { get { return OffsetY; } }
        /// <summary>Exclusive X-axis maximum bound</summary>
        public int EndX { get { return OffsetX + Width; } }
        /// <summary>Exclusive Y-axis maximum bound</summary>
        public int EndY { get { return OffsetY + Height; } }


        /// <summary>If the data is used without an offset, give the origin within the data.</summary>
        public Point OriginInData
        {
            get { return new Point(-OffsetX, -OffsetY); }
            set { OffsetX = -value.X; OffsetY = -value.Y; }
        }



        /// <summary>Get a translation of the data to a different position.</summary>
        public Data2D<T> Translated(int x, int y)
        {
            return new Data2D<T>(Data, OffsetX + x, OffsetY + y, Width, Height);
        }

        /// <summary>Get a translation of the data to a different position.</summary>
        public Data2D<T> Translated(Point translation)
        {
            return new Data2D<T>(Data, OffsetX + translation.X, OffsetY + translation.Y, Width, Height);
        }



        public T this[int x, int y]
        {
            // (Kind of sad that these are just a smidge above the default JIT inlining threshold.)
            get
            {
                Debug.Assert(x >= StartX && x < EndX);
                Debug.Assert(y >= StartY && y < EndY);
                return Data[(x-OffsetX) + (y-OffsetY) * Width];
            }
            set
            {
                Debug.Assert(x >= StartX && x < EndX);
                Debug.Assert(y >= StartY && y < EndY);
                Data[(x-OffsetX) + (y-OffsetY) * Width] = value;
            }
        }

        public T GetOrDefault(int x, int y, T defaultValue = default(T))
        {
            x -= OffsetX;
            y -= OffsetY;
            if((uint)x < (uint)Width && (uint)y < (uint)Height)
                return Data[x + y * Width];
            else
                return defaultValue;
        }



        public void ClearWithoutChangingSize(T clearToValue = default(T))
        {
            if(Data != null)
            {
                for(int i = 0; i < Data.Length; i++)
                {
                    Data[i] = clearToValue;
                }
            }
        }




        #region Extents and Resizing



        /// <summary>Find the rectangle that contains data meeting some condition.</summary>
        /// <returns>The found rectangle, or Rectangle.Empty if no data matches.</returns>
        public Rectangle FindExtents(Func<T, bool> func)
        {
            bool found = false;
            Rectangle foundRegion = Rectangle.Empty;

            for(int y = 0; y < Height; y++) for(int x = 0; x < Width; x++)
            {
                if(func(Data[x + y * Width]))
                {
                    Rectangle r = new Rectangle(x, y, 1, 1);
                    if(!found)
                        foundRegion = r;
                    else
                        foundRegion = Rectangle.Union(foundRegion, r);
                    found = true;
                }
            }

            if(found)
            {
                foundRegion.X += OffsetX;
                foundRegion.Y += OffsetY;
            }

            return foundRegion;
        }

        

        /// <summary>
        /// Copy the data from one region to another (without moving it).
        /// Any existing data outside the new boundary is lost.
        /// Any area not covered by the original boundary is filled with <paramref name="defaultData"/>.
        /// </summary>
        public Data2D<T> CopyWithNewBounds(Rectangle newBounds, T defaultData = default(T))
        {
            if(newBounds.Width == 0 || newBounds.Height == 0)
                return new Data2D<T>();

            T[] newData = new T[newBounds.Width * newBounds.Height];

            for(int newY = 0; newY < newBounds.Height; newY++)
            {
                int y = newY + newBounds.Y - OffsetY;
                for(int newX = 0; newX < newBounds.Width; newX++)
                {
                    int x = newX + newBounds.X - OffsetX;

                    if((uint)x < (uint)Width && (uint)y < (uint)Height)
                        newData[newX + newY * newBounds.Width] = Data[x + y * Width];
                    else
                        newData[newX + newY * newBounds.Width] = defaultData;
                }
            }

            return new Data2D<T>(newData, newBounds);
        }


        /// <summary>Expand the size of the data to allow writing to the specified area. Copies the data only if required.</summary>
        public Data2D<T> LazyCopyExpandToContain(Rectangle newBounds, T defaultData = default(T))
        {
            if(Bounds.ContainsIgnoreEmpty(newBounds))
                return this; // We're already the right size

            newBounds = RectangleExtensions.UnionIgnoreEmpty(Bounds, newBounds);
            return CopyWithNewBounds(newBounds, defaultData);
        }


        #endregion



        #region Flip

        /// <summary>Copy and flip the data around the row at Y=0 (that row does not change)</summary>
        public Data2D<T> CopyFlipY()
        {
            if(Data == null)
                return new Data2D<T>();

            T[] flippedData = new T[Data.Length];
            for(int y = 0; y < Height; y++)
            {
                int flippedY = Height - y - 1;
                Array.Copy(Data, y * Width, flippedData, flippedY * Width, Width);
            }
            return new Data2D<T>(flippedData, Bounds.FlipYIndexable());
        }

        /// <summary>Copy and flip the data around the row at X=0 (that row does not change)</summary>
        public void FlipXInPlace()
        {
            for(int y = 0; y < Height; y++)
                Array.Reverse(Data, y * Width, Width);
            OffsetX = 1 - (OffsetX + Width); // Rectangle.FlipXIndexable()
        }

        #endregion



        #region Transform

        /// <summary>Perform a transform of the data around its origin, using the given transform matrix</summary>
        public Data2D<T> Transform(IntMatrix2 matrix)
        {
            // Local corners (inclusive)
            Point TL = new Point(OffsetX, OffsetY);
            Point TR = new Point(OffsetX + Width - 1, OffsetY);
            Point BL = new Point(OffsetX, OffsetY + Height - 1);
            Point BR = new Point(OffsetX + Width - 1, OffsetY + Height - 1);
            
            // Transformed corners:
            Point cornerA = matrix.Transform(TL);
            Point cornerB = matrix.Transform(TR);
            Point cornerC = matrix.Transform(BL);
            Point cornerD = matrix.Transform(BR);

            // Convert to rectangle:
            int minX = System.Math.Min(System.Math.Min(cornerA.X, cornerB.X), System.Math.Min(cornerC.X, cornerD.X));
            int minY = System.Math.Min(System.Math.Min(cornerA.Y, cornerB.Y), System.Math.Min(cornerC.Y, cornerD.Y));
            int maxX = System.Math.Max(System.Math.Max(cornerA.X, cornerB.X), System.Math.Max(cornerC.X, cornerD.X));
            int maxY = System.Math.Max(System.Math.Max(cornerA.Y, cornerB.Y), System.Math.Max(cornerC.Y, cornerD.Y));
            Rectangle outputBounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);

            // Now transform the data:
            Data2D<T> transformed = new Data2D<T>(outputBounds);

            for(int y = StartY; y < EndY; y++) for(int x = StartX; x < EndX; x++)
            {
                // INLINED: IntMatrix2.Transform
                int outX = x * matrix.xToX + y * matrix.yToX;
                int outY = x * matrix.xToY + y * matrix.yToY;

                // INLINED: indexer operator (x2)
                transformed.Data[(outX-transformed.OffsetX) + (outY-transformed.OffsetY) * transformed.Width]
                        = this.Data[(x-OffsetX) + (y-OffsetY) * Width];
            }

            return transformed;
        }


        #endregion

    }
}
