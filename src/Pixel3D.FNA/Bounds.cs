using System;
using System.IO;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Pixel3D
{
    public struct Bounds
    {
        public int startX, endX, startY, endY;

        public Bounds(int startX, int endX, int startY, int endY)
        {
            this.startX = startX;
            this.endX = endX;
            this.startY = startY;
            this.endY = endY;
        }

        public Bounds(Rectangle rectangle)
        {
            this.startX = rectangle.X;
            this.startY = rectangle.Y;
            this.endX = rectangle.X + rectangle.Width;
            this.endY = rectangle.Y + rectangle.Height;
        }

        public Rectangle AsXNARectangle()
        {
            return new Rectangle(startX, startY, Width, Height);
        }

        public static Bounds InfiniteInverse { get { return new Bounds { startX = int.MaxValue, startY = int.MaxValue, endX = int.MinValue, endY = int.MinValue }; } }


        public bool IsValid { get { return startX <= endX && startY <= endY; } }
        public bool HasPositiveArea { get { return startX < endX && startY < endY; } }


        public int Width { get { return endX - startX; } }
        public int Height { get { return endY - startY; } }
        public int Area { get { return Width * Height; } }
        public Point Center { get { return new Point(startX + Width / 2, startY + Height / 2); } }


        public int IndexInBounds(int x, int y)
        {
            Debug.Assert(x >= startX && x < endX);
            Debug.Assert(y >= startY && y < endY);
            return (x - startX) + (y - startY) * Width;
        }

        public int IndexInBoundsClamp(int x, int y)
        {
            Debug.Assert(startX < endX && startY < endY);

            if(x < startX)
                x = startX;
            else if(x >= endX)
                x = endX - 1;

            if(y < startY)
                y = startY;
            else if(y >= endY)
                y = endY - 1;

            return (x - startX) + (y - startY) * Width;
        }

        public Point PositionInBoundsByIndex(int i)
        {
            Debug.Assert(i >= 0 && i < Area);

            Point result;
            result.X = startX + i % Width;
            result.Y = startY + i / Width;
            
            return result;
        }

		public bool Intersects(Bounds other)
        {
            return !(this.endX <= other.startX || this.startX >= other.endX || this.endY <= other.startY || this.startY >= other.endY); 
        }

        public static Bounds operator+(Bounds bounds, Point p)
        {
            bounds.startX += p.X;
            bounds.endX += p.X;
            bounds.startY += p.Y;
            bounds.endY += p.Y;
            return bounds;
        }

        public static Bounds operator-(Bounds bounds, Point p)
        {
            bounds.startX -= p.X;
            bounds.endX -= p.X;
            bounds.startY -= p.Y;
            bounds.endY -= p.Y;
            return bounds;
        }

		public bool Contains(int x, int y)
        {
            return x >= startX && x < endX && y >= startY && y < endY;
        }

        public bool Contains(Bounds other)
        {
            return !(startX >= other.endX || other.startX >= endX || startY >= other.endY || other.startY >= endY);
        }

		public Bounds FlipX()
        {
            return new Bounds { startX = -endX +1, endX = -startX +1, startY = startY, endY = endY };
        }

        public Bounds MaybeFlipX(bool flipX)
        {
            return flipX ? FlipX() : this;
        }

		public Bounds Combine(Bounds other)
        {
            return new Bounds
            {
                startX = Math.Min(startX, other.startX),
                endX = Math.Max(endX, other.endX),
                startY = Math.Min(startY, other.startY),
                endY = Math.Max(endY, other.endY),
            };
        }

	    public static Bounds Intersection(Bounds a, Bounds b)
        {
            return new Bounds
            {
                startX = Math.Max(a.startX, b.startX),
                startY = Math.Max(a.startY, b.startY),
                endX = Math.Min(a.endX, b.endX),
                endY = Math.Min(a.endY, b.endY),
            };
        }

        public static Bounds Union(Bounds a, Bounds b)
        {
            return new Bounds
            {
                startX = Math.Min(a.startX, b.startX),
                startY = Math.Min(a.startY, b.startY),
                endX = Math.Max(a.endX, b.endX),
                endY = Math.Max(a.endY, b.endY),
            };
        }
		
        public static Bounds Interpolate(Bounds a, Bounds b, int p, int last)
        {
            if(p == 0)
                return a;
            if(p == last)
                return b;
            Debug.Assert(p > 0 && p < last);
            Debug.Assert(last > 0);
            
            int q = last-p;

            Bounds result;
            result.startX = ((a.startX * q) + (b.startX * p)) / last;
            result.endX = ((a.endX * q) + (b.endX * p)) / last;
            result.startY = ((a.startY * q) + (b.startY * p)) / last;
            result.endY = ((a.endY * q) + (b.endY * p)) / last;

            return result;
        }

		/// <summary>Expand to contain the pixel at the given position (position is considered to have width and height of 1)</summary>
        public Bounds ExpandToContain(Point position)
        {
            Bounds result = this;

            if(position.X < result.startX)
                result.startX = position.X;
            if(position.X >= result.endX)
                result.endX = position.X + 1;
            if(position.Y < result.startY)
                result.startY = position.Y;
            if(position.Y >= result.endY)
                result.endY = position.Y + 1;

            return result;
        }
		
        public Bounds Grow(int p)
        {
            Bounds result = this;

            result.startX -= p;
            result.endX += p;
            result.startY -= p;
            result.endY += p;

            return result;
        }

        public Bounds Grow(int x, int y)
        {
            Bounds result = this;

            result.startX -= x;
            result.endX += x;
            result.startY -= y;
            result.endY += y;

            return result;
        }
    }

	public static class BoundsExtensions
    {
        public static void Write(this BinaryWriter bw, Bounds bounds)
        {
            bw.Write(bounds.startX);
            bw.Write(bounds.endX);
            bw.Write(bounds.startY);
            bw.Write(bounds.endY);
        }

        public static Bounds ReadBounds(this BinaryReader br)
        {
            Bounds bounds;
            bounds.startX = br.ReadInt32();
            bounds.endX = br.ReadInt32();
            bounds.startY = br.ReadInt32();
            bounds.endY = br.ReadInt32();
            return bounds;
        }
    }
}