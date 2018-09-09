using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Pixel3D.Animations;
using System.Diagnostics;

namespace Pixel3D
{
    public struct Sprite
    {
        public Sprite(Texture2D texture) : this(texture, texture.Bounds, new Point(0, texture.Height-1)) { }
        public Sprite(Texture2D texture, Rectangle sourceRectangle) : this(texture, sourceRectangle, new Point(0, texture.Height - 1)) { }
        public Sprite(Texture2D texture, Point origin) : this(texture, texture.Bounds, origin) { }

        public Sprite(Texture2D texture, Rectangle sourceRectangle, Point origin)
        {
            Debug.Assert(sourceRectangle.Width <= 2048 && sourceRectangle.Height <= 2048); // Always require Reach-sized textures

            this.texture = texture;
            this.sourceRectangle = sourceRectangle;
            this.origin = origin;
        }
        
        public Texture2D texture;
        public Rectangle sourceRectangle;
        /// <summary>The origin pixel in Texture space. Refers to the centre of the pixel.</summary>
        public Point origin;


        /// <summary>Draw origin is the bottom-left of a pixel (to make various transformations easier)</summary>
        public Vector2 DrawOrigin { get { return new Vector2(origin.X, origin.Y + 1); } }

        /// <summary>Draw origin for when the sprite is being flipped on its X axis</summary>
        /// <remarks>"Width - OriginX - 1", the -1 is because we flip around the origin pixel's centre.</remarks>
        public Vector2 DrawOriginFlipX { get { return new Vector2(Width - origin.X - 1, origin.Y + 1); } }

        public float Width { get { return sourceRectangle.Width; } }
        public float Height { get { return sourceRectangle.Height; } }



        public Data2D<Color> GetData()
        {
            if(texture == null)
                return new Data2D<Color>();

            Color[] data = new Color[sourceRectangle.Width * sourceRectangle.Height];
            texture.GetData(0, sourceRectangle, data, 0, data.Length);

            return new Data2D<Color>(data, -origin.X, -origin.Y, sourceRectangle.Width, sourceRectangle.Height);
        }

        public Data2D<Color> GetWorldSpaceData()
        {
            return GetData().CopyFlipY();
        }

        public MaskData GetAlphaMask()
        {
            return GetWorldSpaceData().CreateMask(Color.Transparent, true);
        }

        public Rectangle WorldSpaceBounds
        {
            get { return new Rectangle(-origin.X, -origin.Y, sourceRectangle.Width, sourceRectangle.Height).FlipYIndexable(); }
        }



        public static bool operator ==(Sprite a, Sprite b)
        {
            return ReferenceEquals(a.texture, b.texture)
                    && a.sourceRectangle == b.sourceRectangle
                    && a.origin == b.origin;
        }

        public static bool operator !=(Sprite a, Sprite b)
        {
            return !(a == b);
        }

        
    }
}
