using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations;
using Pixel3D.Extensions;
using Pixel3D.FrameworkExtensions;

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

		#region Serialization

		public void Serialize(AnimationSerializeContext context)
		{
			if (sourceRectangle.Width > 2048 || sourceRectangle.Height > 2048)
				throw new InvalidOperationException("Cannot handle textures larger than 2048"); // Due to Reach support

			if (context.imageWriter != null)
			{
				int index = context.imageWriter.GetImageIndex(texture, sourceRectangle);
				context.bw.Write(index);

				if (index >= 0)
					context.bw.Write(origin);
			}
			else // In-place sprite
			{
				if (texture == null || sourceRectangle.Width == 0 || sourceRectangle.Height == 0)
				{
					context.bw.Write(0); // Writing 0 width indicates blank texture (no need to write height)
				}
				else
				{
					var data = new byte[sourceRectangle.Width * sourceRectangle.Height * 4];
					texture.GetData<byte>(0, sourceRectangle, data, 0, data.Length);

					// Only write the size (we intentionally lose the source rectangle's position)
					context.bw.Write(sourceRectangle.Width);
					context.bw.Write(sourceRectangle.Height);

					context.bw.Write(data);

					context.bw.Write(origin);
				}
			}
		}

		public Sprite(AnimationDeserializeContext context) : this()
		{
			// IMPORTANT: This method is compatible with SpriteRef's deserializer
			if (context.imageBundle != null)
			{
				int index = context.br.ReadInt32();
				if (index == -1)
				{
					texture = null;
					sourceRectangle = default(Rectangle);
					origin = default(Point);
				}
				else
				{
					context.imageBundle.GetSprite(index, context.br.ReadPoint());
				}
			}
			else // In place sprite
			{
				int width = context.br.ReadInt32();
				if (width == 0) // A blank texture
				{
					sourceRectangle = Rectangle.Empty;
					texture = null;
					origin = default(Point);
				}
				else
				{
					int height = context.br.ReadInt32();
					sourceRectangle = new Rectangle(0, 0, width, height);
					byte[] data = context.br.ReadBytes(width * height * 4);

					if (context.GraphicsDevice != null) // <- Allow loading headless
					{
						texture = new Texture2D(context.GraphicsDevice, width, height);
						((Texture2D)texture).SetData(data);
					}
					else
					{
						texture = null;
					}
					origin = context.br.ReadPoint();
				}
			}
		}

		#endregion
	}
}
