using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Pixel3D.Engine;

namespace Pixel3D.DebugRendering // <- Separate namespace, so you have to opt-into these
{
    public static class DebugRenderingExtensions
    {
        // It'd be nice to have proper debug line rendering (like what I use in Stick Ninjas)
        // Be lazy for now...


        #region Textures

        private static Texture2D arrowHead;
        private static Texture2D whitePixel;
        private static Rectangle whitePixelRectangle;

        private static void LazyGenerateTextures(GraphicsDevice device)
        {
            if(arrowHead == null)
            {
                const int size = 16;
                arrowHead = new Texture2D(device, size, size);
                Color[] data = new Color[size * size];
                for(int y = 0; y < size; y++) for(int x = 0; x < size; x++)
                {
                    int xx = x-size/2;
                    int yy = y-size/2;

                    if(xx <= 0)
                    {
                        if(Math.Abs(yy) < -xx - 2)
                            data[x + y * size] = Color.White;
                        else if(Math.Abs(yy) <= -xx)
                            data[x + y * size] = Color.Black;
                    }
                }
                arrowHead.SetData(data);

                whitePixelRectangle = new Rectangle(2, size/2, 1, 1);
            }

            if (whitePixel == null)
            {
                whitePixel = new Texture2D(device, 1, 1, false, SurfaceFormat.Color);
                whitePixel.SetData(new[] { Color.White });
            }
        }


        public static Texture2D GetWhitePixel(GraphicsDevice device)
        {
            LazyGenerateTextures(device);
            return whitePixel;
        }

        #endregion


        public static void DrawDebugWorldRectangleInset(this SpriteBatch sb, Camera camera, Rectangle worldRectangle, Color color)
        {
            LazyGenerateTextures(sb.GraphicsDevice);

            var thickness = camera.RenderContentZoom;
            var viewRectangle = camera.ContentToView(camera.WorldZeroToContent(worldRectangle));

            sb.Draw(whitePixel, new Rectangle(viewRectangle.X, viewRectangle.Y, viewRectangle.Width, thickness), color);
            sb.Draw(whitePixel, new Rectangle(viewRectangle.X, viewRectangle.Y + thickness, thickness, viewRectangle.Height - 2*thickness), color);
            sb.Draw(whitePixel, new Rectangle(viewRectangle.X + viewRectangle.Width - thickness, viewRectangle.Y + thickness, thickness, viewRectangle.Height - 2*thickness), color);
            sb.Draw(whitePixel, new Rectangle(viewRectangle.X, viewRectangle.Y + viewRectangle.Height - thickness, viewRectangle.Width, thickness), color);
        }

        public static void DrawDebugWorldRectangleOutset(this SpriteBatch sb, Camera camera, Rectangle worldRectangle, Color color)
        {
            DrawDebugWorldRectangleInset(sb, camera, new Rectangle(worldRectangle.X - 1, worldRectangle.Y - 1, worldRectangle.Width + 2, worldRectangle.Height + 2), color);
        }


        public static void DrawDebugWorldRectangleSolid(this SpriteBatch sb, Camera camera, Rectangle worldRectangle, Color color)
        {
            LazyGenerateTextures(sb.GraphicsDevice);
            var viewRectangle = camera.ContentToView(camera.WorldZeroToContent(worldRectangle));
            sb.Draw(whitePixel, viewRectangle, color);
        }




        public static void DrawDebugArrow(this SpriteBatch sb, Position from, Position to, Color color, float width = 2f)
        {
            DrawDebugDisplaySpaceArrow(sb, from.ToDisplay, to.ToDisplay, color, width);
        }


        public static void DrawDebugDisplaySpaceArrow(this SpriteBatch sb, Vector2 start, Vector2 end, Color color, float width = 2f)
        {
            LazyGenerateTextures(sb.GraphicsDevice);

            float arrowHeadSize = 0.125f * width;

            Vector2 lineVector = (end-start);
            Vector2 normal = Vector2.Normalize(lineVector);
            lineVector -= normal * (arrowHead.Width * (arrowHeadSize/2f));

            float angle = (float)Math.Atan2(normal.Y, normal.X);

            sb.Draw(arrowHead, start, whitePixelRectangle, color * 0.6f, angle, new Vector2(0.0f, 0.5f), new Vector2(lineVector.Length(), width/4f), 0, 0);

            sb.Draw(arrowHead, end, null, color, angle, new Vector2(arrowHead.Width/2, arrowHead.Height/2), new Vector2(arrowHeadSize, arrowHeadSize/2f), 0, 0);
        }





        public static void DrawDebugLine(this SpriteBatch sb, Position from, Position to, Color color, float width = 2f)
        {
            LazyGenerateTextures(sb.GraphicsDevice);

            var p1 = from.ToDisplay;
            var p2 = to.ToDisplay;
            var length = Vector2.Distance(p1, p2);
            var angle = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);

            sb.DrawWorld(whitePixel, from, null, color, angle, Vector2.Zero, false, new Vector2(length, width));
        }

        private static void DrawWorld(this SpriteBatch sb, Texture2D texture, Position position, Rectangle? sourceRectangle, Color color, float angle, Vector2 origin, bool flipX, Vector2 scale)
        {
            var effects = SpriteEffects.None;
            if (flipX)
            {
                effects = SpriteEffects.FlipHorizontally;
                var rect = sourceRectangle ?? texture.Bounds;
                origin.X = rect.Width - origin.X - 1; // -1 because the engine flips around the pixel itself, rather than the boundary between pixels
            }
            sb.Draw(texture, position.ToDisplay, sourceRectangle, color, angle, origin, scale, effects, 0);
        }
    }
}
