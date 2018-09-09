using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pixel3D.Extensions
{
    // Extension methods for Data2D<Color>
    public static class ColorDataExtensions
    {
        /// <summary>Create a sprite from client-oriented color data</summary>
        public static Sprite MakeSprite(this Data2D<Color> data, GraphicsDevice device)
        {
            Debug.Assert(data.Width > 0 && data.Height > 0);

            Texture2D texture;
            if(device != null) // <- allows loading headless
            {
                texture = new Texture2D(device, data.Width, data.Height);
                texture.SetData(data.Data);
            }
            else
                texture = null;

            return new Sprite(texture, data.OriginInData);
        }

        /// <summary>Create a sprite from world-oriented color data</summary>
        public static Sprite MakeSpriteFromWorld(this Data2D<Color> data, GraphicsDevice device)
        {
            Debug.Assert(data.Width > 0 && data.Height > 0);

            var clientOrientedData = data.CopyFlipY();

            Texture2D texture = new Texture2D(device, clientOrientedData.Width, clientOrientedData.Height);
            texture.SetData(clientOrientedData.Data);
            return new Sprite(texture, clientOrientedData.OriginInData);
        }



        /// <summary>Find the rectangle containing pixels of the given colour (or not containing pixels of the given colour, if inverse is true)</summary>
        public static Rectangle FindTrimBounds(this Data2D<Color> data, Color color, bool inverse = false)
        {
            if(data.Data == null)
                return Rectangle.Empty;

            // NOTE: Bounds are found on non-offset data, and offset is applied at the end

            int top = 0;

            // Search downwards to find top row with a matching pixel...
            for (; top < data.Height; top++)
                for (int x = 0; x < data.Width; x++)
                    if ((data.Data[x + top*data.Width] == color) != inverse)
                        goto foundTop;
            return Rectangle.Empty; // No matching data found

            foundTop:

            int bottom = data.Height - 1, left = 0, right = data.Width - 1;

            // Search upwards to find bottom row with a matching pixel...
            for (; bottom > top; bottom--) // <- (exit if we reach the 'top' row: 1px tall)
                for (int x = 0; x < data.Width; x++)
                    if ((data.Data[x + bottom*data.Width] == color) != inverse)
                        goto foundBottom;

            foundBottom:

            // Search left to find left column with a matching pixel...
            for (; left < data.Width; left++)
                for (int y = top; y <= bottom; y++)
                    if ((data.Data[left + y*data.Width] == color) != inverse)
                        goto foundLeft;

            foundLeft:

            // Search right to find right column with a matching pixel...
            for (; right > left; right--) // <- (exit if we reach the 'left' column: 1px wide)
                for (int y = top; y <= bottom; y++)
                    if ((data.Data[right + y*data.Width] == color) != inverse)
                        goto foundRight;

            foundRight:

            return new Rectangle(left + data.OffsetX, top + data.OffsetY, right - left + 1, bottom - top + 1);
        }


        public static Sprite AutoTrim(this Sprite sprite, GraphicsDevice device)
        {
            var data = sprite.GetData();
            return data.CopyWithNewBounds(data.FindTrimBounds(Color.Transparent, true)).MakeSprite(device);
        }



        /// <summary>Convert color data to a trimmed 1-bit mask</summary>
        public static MaskData CreateMask(this Data2D<Color> data, Color color, bool inverse = false)
        {
            Rectangle trimBounds = data.FindTrimBounds(color, inverse);

            // TODO: Avoid this copy...
            Data2D<Color> trimData = data.CopyWithNewBounds(trimBounds);

            MaskData mask = new MaskData(trimData.Bounds);
            for(int y = trimData.StartY; y < trimData.EndY; y++) for (int x = trimData.StartX; x < trimData.EndX; x++)
            {
                mask[x, y] = ((trimData[x, y] == color) != inverse);
            }

            return mask;
        }


        public static Data2D<Color> CreateColorData(this MaskData mask, Color color)
        {
            var data = new Data2D<Color>(mask.Bounds);
            for(var y = data.StartY; y < data.EndY; y++) for(var x = data.StartX; x < data.EndX; x++)
            {
                data[x, y] = mask[x, y] ? color : Color.Transparent;
            }
       
            return data;
        }
    }
}
