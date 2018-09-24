using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pixel3D.Extensions
{
    public static class WorldSpriteExtensions
    {
        public static void DrawWorld(this SpriteBatch sb, Texture2D texture, Position position, Rectangle sourceRectangle, Color color, Vector2 origin, bool flipX, Vector2 scale)
        {
            if(texture == null)
                return;

            SpriteEffects effects = SpriteEffects.None;
            if(flipX)
            {
                effects = SpriteEffects.FlipHorizontally;
                origin.X = sourceRectangle.Width - origin.X - 1; // -1 because the engine flips around the pixel itself, rather than the boundary between pixels
            }

            sb.Draw(texture, position.ToDisplay(), sourceRectangle, color, 0, origin, scale, effects, 0);
        }
        
        public static void DrawWorld(this SpriteBatch sb, Sprite sprite, Position position, Color color, bool flipX)
        {
            if(sprite.texture != null)
                DrawWorld(sb, sprite.texture, position, sprite.sourceRectangle, color, sprite.DrawOrigin, flipX, Vector2.One);
        }

        public static void DrawWorld(this SpriteBatch sb, Sprite sprite, Position position, Color color, bool flipX, float scale = 1f)
        {
            if(sprite.texture != null)
                DrawWorld(sb, sprite.texture, position, sprite.sourceRectangle, color, sprite.DrawOrigin, flipX, new Vector2(scale, scale));
        }

    }
}
