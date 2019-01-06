// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Microsoft.Xna.Framework;
using Pixel3D.Extensions;

namespace Pixel3D
{
    public struct TransformedMaskData
    {
        public TransformedMaskData(MaskData maskData, bool flipX)
        {
            this.maskData = maskData;
            this.flipX = flipX;
        }

        public MaskData maskData;

        /// <summary>True if the mask is logically transformed around X = 0 (index with [-x, y])</summary>
        public bool flipX;

		public Rectangle Bounds
        {
            get { return flipX ? maskData.Bounds.FlipXIndexable() : maskData.Bounds; }
        }
		
        public bool this[int x, int y]
        {
            get { return maskData[flipX ? -x : x, y]; }
            set { maskData[flipX ? -x : x, y] = value; }
        }

        public bool GetOrDefault(int x, int y, bool defaultValue = false)
        {
            return maskData.GetOrDefault(flipX ? -x : x, y, defaultValue);
        }
		
        public static bool Collide(TransformedMaskData a, TransformedMaskData b)
        {
            Rectangle aRect = a.Bounds;
            Rectangle bRect = b.Bounds;
            Rectangle intersection = Rectangle.Intersect(aRect, bRect);

            for(int iy = 0; iy < intersection.Height; iy++) for(int ix = 0; ix < intersection.Width; ix++)
            {
                int x = ix + intersection.X;
                int y = iy + intersection.Y;
                if(a[x, y] & b[x, y])
                {
                    return true; // Masks collide
                }
            }

            return false;
        }

        public bool CollidesWith(TransformedMaskData other)
        {
            return Collide(this, other);
        }
    }
}