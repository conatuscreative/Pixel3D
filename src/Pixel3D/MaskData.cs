using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.IO;

namespace Pixel3D
{
	/// <summary>A bit-packed mask, basically equivelent to Data2D[bool]</summary>
	[Serializable]
    public struct MaskData
    {
        public MaskData(uint[] packedData, int offsetX, int offsetY, int width, int height)
        {
            this.packedData = packedData;
            this.OffsetX = offsetX;
            this.OffsetY = offsetY;
            this.Width = width;
            this.Height = height;

            Debug.Assert(width == 0 || height == 0 || packedData.Length == DataWidth * height);
        }

        public MaskData(uint[] packedData, Rectangle bounds) : this(packedData, bounds.X, bounds.Y, bounds.Width, bounds.Height) { }


        /// <summary>Create a new data buffer</summary>
        public MaskData(int offsetX, int offsetY, int width, int height)
        {
            if(width == 0 || height == 0)
                this.packedData = null;
            else
                this.packedData = new uint[WidthToDataWidth(width) * height];

            this.OffsetX = offsetX;
            this.OffsetY = offsetY;
            this.Width = width;
            this.Height = height;
            
        }

        public MaskData(Rectangle bounds) : this(bounds.X, bounds.Y, bounds.Width, bounds.Height) { }

        public MaskData Clone()
        {
            uint[] packedDataClone = packedData == null ? null : (uint[]) packedData.Clone();
            return new MaskData(packedDataClone, OffsetX, OffsetY, Width, Height);
        }

	    /// <summary>Data packed into 32-bit horizontal groups. Can be null.</summary>
        public readonly uint[] packedData;
        public int OffsetX, OffsetY;
        public readonly int Width, Height;

        /// <summary>The stride of each row of data.</summary>
        public int DataWidth { get { return (Width + 31) >> 5; } }
        public static int WidthToDataWidth(int width) { return (width + 31) >> 5; }


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

        /// <summary>Verify that the underlying array is the correct size</summary>
        public bool Valid
        {
            get
            {
                if(Width == 0 || Height == 0)
                    return packedData == null;
                else
                    return packedData.Length == DataWidth * Height;
            }
        }


        /// <summary>Get a translation of the data to a different position.</summary>
        public MaskData Translated(int x, int y)
        {
            return new MaskData(packedData, OffsetX + x, OffsetY + y, Width, Height);
        }

        /// <summary>Get a translation of the data to a different position.</summary>
        public MaskData Translated(Point translation)
        {
            return new MaskData(packedData, OffsetX + translation.X, OffsetY + translation.Y, Width, Height);
        }



        public bool this[int x, int y]
        {
            get
            {
                Debug.Assert(x >= StartX && x < EndX);
                Debug.Assert(y >= StartY && y < EndY);
                x -= OffsetX;
                y -= OffsetY;
                return (packedData[(x >> 5) + y * DataWidth] & (1u << (x & 31))) != 0;
            }
            set
            {
                Debug.Assert(x >= StartX && x < EndX);
                Debug.Assert(y >= StartY && y < EndY);
                x -= OffsetX;
                y -= OffsetY;
                if(value)
                    packedData[(x >> 5) + y * DataWidth] |=  (1u << (x & 31));
                else
                    packedData[(x >> 5) + y * DataWidth] &= ~(1u << (x & 31));
            }
        }

        public bool GetOrDefault(int x, int y, bool defaultValue = false)
        {
            x -= OffsetX;
            y -= OffsetY;
            if((uint)x < (uint)Width && (uint)y < (uint)Height)
                return (packedData[(x >> 5) + y * DataWidth] & (1u << (x & 31))) != 0u;
            else
                return defaultValue;
        }


        public bool IsSetInXRange(int xStart, int xEnd, int y)
        {
            y -= OffsetY;
            if((uint)y >= (uint)Height)
                return false;

            Debug.Assert(xStart <= xEnd);
            xStart = System.Math.Max(0, xStart - OffsetX);
            xEnd = System.Math.Min(Width, xEnd - OffsetX);

            // PERF: Consider doing this with bitmasks to save some cycles
            for(int x = xStart; x < xEnd; x++)
            {
                Debug.Assert((uint)x < (uint)Width);

                if((packedData[(x >> 5) + y * DataWidth] & (1u << (x & 31))) != 0u)
                    return true;
            }

            return false;
        }



        public MaskData MakeFlippedX()
        {
            MaskData flipped = new MaskData(Bounds.FlipXIndexable());
            for(int y = StartY; y < EndY; y++) for(int x = StartX; x < EndX; x++)
            {
                flipped[-x, y] = this[x, y];
            }
            return flipped;
        }

        public void SetBitwiseOrFrom(MaskData other)
        {
            int localStartX = System.Math.Max(this.StartX, other.StartX);
            int localStartY = System.Math.Max(this.StartY, other.StartY);
            int localEndX = System.Math.Min(this.EndX, other.EndX);
            int localEndY = System.Math.Min(this.EndY, other.EndY);
            
            for(int y = localStartY; y < localEndY; y++) for(int x = localStartX; x < localEndX; x++)
            {
                this[x, y] |= other[x, y];
            }
        }

        public void SetBitwiseAndFromMustBeContained(MaskData other)
        {
            Debug.Assert(other.Bounds.Contains(this.Bounds));

            int localStartX = StartX;
            int localStartY = StartY;
            int localEndX = EndX;
            int localEndY = EndY;

            for(int y = localStartY; y < localEndY; y++) for(int x = localStartX; x < localEndX; x++)
            {
                this[x, y] &= other[x, y];
            }
        }

        public MaskData BitwiseAnd(MaskData other)
        {
            Rectangle bounds = Rectangle.Intersect(this.Bounds, other.Bounds);
            if(bounds.Width == 0 || bounds.Height == 0)
                return new MaskData();

            int localStartX = bounds.X;
            int localStartY = bounds.Y;
            int localEndX = bounds.X + bounds.Width;
            int localEndY = bounds.Y + bounds.Height;

            MaskData result = new MaskData(bounds);
            for(int y = localStartY; y < localEndY; y++) for(int x = localStartX; x < localEndX; x++)
            {
                result[x, y] = this[x, y] & other[x, y];
            }

            return result.LazyCopyAutoCrop();
        }


        public uint CountBitsSet()
        {
            uint result = 0;
            for(int i = 0; i < packedData.Length; i++)
            {
                result += packedData[i].CountBitsSet();
            }
            return result;
        }


        public string OutputImageAsDebugString()
        {
            StringBuilder sb = new StringBuilder();
            for(int y = EndY - 1; y >= StartY; y--) // <- Reverse because these are typically in world space (Y is up)
            {
                for(int x = StartX; x < EndX; x++)
                {
                    sb.Append(this[x, y] ? 'X' : '_');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }


        #region Bit Shifting

        // For high-performance mask comparisons:

        public MaskData CopyAndExpandForBitShift()
        {
            if(Width % 32 == 1)
            {
                // We can be a direct copy, because bit-shifting 31 times will not shift any data off the end of the image
                uint[] copyData = (uint[])packedData.Clone();
                return new MaskData(copyData, OffsetX, OffsetY, Width + 31, Height);
            }
            else
            {
                // In this case we need to expand because we will eventually shift into a new 32-bit column:
                int dataWidth = DataWidth;
                int newDataWidth = dataWidth + 1;
                Debug.Assert(newDataWidth == MaskData.WidthToDataWidth(Width + 31));
                uint[] copyData = new uint[newDataWidth * Height];

                for(int y = 0; y < Height; y++)
                {
                    Array.Copy(packedData, dataWidth * y, copyData, newDataWidth * y, dataWidth);
                }

                return new MaskData(copyData, OffsetX, OffsetY, Width + 31, Height);
            }
        }

        /// <summary>This does not affect the position of the logical image data (although it does shift the packed data to the right, spatially)</summary>
        public void BitShiftLeftInPlace()
        {
            int dataWidth = this.DataWidth;
            for(int y = Height - 1; y >= 0; y--)
            {
                packedData[(y+1)*dataWidth - 1] <<= 1;

                for(int x = dataWidth - 2; x >= 0; x--)
                {
                    int i = y * dataWidth + x;
                    packedData[i + 1] |= (packedData[i] >> 31);
                    packedData[i] <<= 1;
                }
            }

            OffsetX--; // <- Doesn't change the logical data
        }

        #endregion



        #region Modify Bounds

        /// <summary>
        /// Copy the data from one region to another (without moving it).
        /// Any existing data outside the new boundary is lost.
        /// </summary>
        public MaskData CopyWithNewBounds(Rectangle newBounds)
        {
            MaskData copy = new MaskData(newBounds);

            int startX = System.Math.Max(copy.StartX, this.StartX);
            int startY = System.Math.Max(copy.StartY, this.StartY);
            int endX = System.Math.Min(copy.EndX, this.EndX);
            int endY = System.Math.Min(copy.EndY, this.EndY);

            for(int y = startY; y < endY; y++) for(int x = startX; x < endX; x++)
            {
                copy[x, y] = this[x, y];
            }

            return copy;
        }


        /// <summary>Expand the size of the data to allow writing to the specified area. Copies the data only if required.</summary>
        public MaskData LazyCopyExpandToContain(Rectangle newBounds)
        {
            if(Bounds.ContainsIgnoreEmpty(newBounds))
                return this; // We're already the right size

            newBounds = RectangleExtensions.UnionIgnoreEmpty(Bounds, newBounds);
            return CopyWithNewBounds(newBounds);
        }


        public Rectangle FindExtents()
        {
            Rectangle foundRegion = Rectangle.Empty;

            for(int y = StartY; y < EndY; y++) for(int x = StartX; x < EndX; x++)
            {
                if(this[x, y])
                    foundRegion = RectangleExtensions.UnionIgnoreEmpty(foundRegion, new Rectangle(x, y, 1, 1)); // TODO: PERF: This is horribly inefficient
            }

            return foundRegion;
        }

        public MaskData LazyCopyAutoCrop()
        {
            Rectangle autoCropBounds = FindExtents();
            if(Bounds != autoCropBounds)
                return CopyWithNewBounds(autoCropBounds);
            else
                return this;
        }

        #endregion



        #region Drawing

        public static void DrawPixel(ref MaskData data, int x, int y, bool setTo)
        {
            if(setTo)
                data = data.LazyCopyExpandToContain(new Rectangle(x, y, 1, 1));

            if(data.Bounds.Contains(x, y))
                data[x, y] = setTo;
        }

        public static void DrawLine(ref MaskData data, int x1, int y1, int x2, int y2, bool setTo)
        {
            data = data.LazyCopyExpandToContain(Rectangle.Union(new Rectangle(x1, y1, 1, 1), new Rectangle(x2, y2, 1, 1)));

            int x = x1;
            int y = y1;

            int dx = x2 - x1;
            int dy = y2 - y1;
            int x_inc = (dx < 0) ? -1 : 1;
            int l = System.Math.Abs(dx);
            int y_inc = (dy < 0) ? -1 : 1;
            int m = System.Math.Abs(dy);
            int dx2 = l << 1;
            int dy2 = m << 1;

            if((l >= m))
            {
                int err_1 = dy2 - l;
                for(int i = 0; i < l; i++)
                {
                    data[x, y] = setTo;
                    if(err_1 > 0)
                    {
                        y += y_inc;
                        err_1 -= dx2;
                    }
                    err_1 += dy2;
                    x += x_inc;
                }
            }
            else
            {
                int err_1 = dx2 - m;
                for(int i = 0; i < m; i++)
                {
                    data[x, y] = setTo;
                    if(err_1 > 0)
                    {
                        x += x_inc;
                        err_1 -= dy2;
                    }
                    err_1 += dx2;
                    y += y_inc;
                }
            }

            data[x, y] = setTo;
        }

        public static void DrawFloodFill(MaskData data, int x, int y, bool setTo)
        {
            // http://csharphelper.com/blog/2014/09/write-a-graphical-floodfill-method-in-c/

            if(!data.Bounds.Contains(x, y))
                return;
            if(data[x, y] == setTo)
                return;

            var points = new Stack<Point>();
            points.Push(new Point(x, y));
            data[x, y] = setTo;

            while(points.Count > 0)
            {
                var pt = points.Pop();
                if(pt.X > data.StartX)   CheckPoint(data, points, pt.X - 1, pt.Y, setTo);
                if(pt.Y > data.StartY)   CheckPoint(data, points, pt.X, pt.Y - 1, setTo);
                if(pt.X < data.EndX - 1) CheckPoint(data, points, pt.X + 1, pt.Y, setTo);
                if(pt.Y < data.EndY - 1) CheckPoint(data, points, pt.X, pt.Y + 1, setTo);
            }
        }

        private static void CheckPoint(MaskData data, Stack<Point> points, int x, int y, bool setTo)
        {
            if(data[x, y] != setTo)
            {
                points.Push(new Point(x, y));
                data[x, y] = setTo;
            }
        }

        #endregion



        #region Serialization

        // IMPORTANT: Because both levels and animation sets can serialize these, we don't have a version number!
        //            (Could pass a custom one as a parameter, or use multiple methods, if the need arises...)

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(Bounds);

            if(packedData != null)
            {
                for(int i = 0; i < packedData.Length; i++)
                {
                    bw.Write(packedData[i]);
                }
            }
        }

        public MaskData(BinaryReader br, bool fastReadHack) : this(br.ReadRectangle())
        {
            Debug.Assert(Valid);

            if(packedData != null)
            {
                if(!fastReadHack)
                {
                    for(int i = 0; i < packedData.Length; i++)
                    {
                        packedData[i] = br.ReadUInt32();
                    }
                }
                else // FAST READ!
                {
                    int bytesToRead = packedData.Length * 4;
                    br.ReadBytes(bytesToRead);
                }
            }
        }

        #endregion


    }
}
