using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using System.Collections.Generic;
using Pixel3D.Engine;

namespace Pixel3D.Animations
{
    public class DrawContext
    {
        public DrawContext(SpriteBatch spriteBatch, Effect spriteEffect, Effect shadowMaskEffect, FadeEffect fadeEffect, Effect doctorEffect, SpriteFont standardFont, SpriteFont thinFontJP, Texture2D whitePixel)
        {
            this.sb = spriteBatch;
            this.spriteEffect = spriteEffect;
            this.shadowMaskEffect = shadowMaskEffect;
            this.fadeEffect = fadeEffect;
            this.doctorEffect = doctorEffect;
            this.standardFont = standardFont;
            this.thinFontJP = thinFontJP;
            this.whitePixel = whitePixel;

            rasterizerState = new RasterizerState
            {
                CullMode = CullMode.CullCounterClockwiseFace,
                ScissorTestEnable = true,
            };
        }


        /// <summary>Temporary working space for word wrap</summary>
        public readonly StringBuilder working = new StringBuilder();
        /// <summary>Temporary output space for word wrap</summary>
        public readonly StringBuilder output = new StringBuilder();


        protected SpriteBatch sb;
        Effect spriteEffect, shadowMaskEffect;
        FadeEffect fadeEffect;
        Effect doctorEffect;
        Effect ntscEffect;

        public RasterizerState rasterizerState;


        #region SpriteBatch

        public SpriteBatch SpriteBatch { get { return sb; } }

        public GraphicsDevice GraphicsDevice
        {
            get { return sb.GraphicsDevice; }
        }

        public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, Vector2? origin = null, float scale = 1f)
        {
            sb.DrawString(spriteFont, text, position, color, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, Vector2? origin = null, float scale = 1f)
        {
            sb.DrawString(spriteFont, text, position, color, 0f, origin ?? Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public void DrawWorld(Sprite sprite, Position position, Color color, bool flipX)
        {
            sb.DrawWorld(sprite, position, color, flipX);
        }

        public void DrawWorld(Texture2D texture, Position position, Rectangle sourceRectangle, Color color, Vector2 origin, bool flipX, Vector2 scale)
        {
            sb.DrawWorld(texture, position, sourceRectangle, color, origin, flipX, scale);
        }

        public void DrawPixel(Rectangle destinationRectangle, Color color)
        {
            sb.Draw(whitePixel, destinationRectangle, color);
        }

        public void DrawPixel(Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        {
            sb.Draw(whitePixel, position, null, color, rotation, origin, scale, effects, layerDepth);
        }

        public void DrawWorldPixel(Position position, Color color, Vector2 origin, bool flipX, Vector2 scale)
        {
            sb.DrawWorld(whitePixel, position, whitePixel.Bounds, color, origin, flipX, scale);
        }

        #endregion

        public SpriteFont standardFont;
        public SpriteFont thinFontJP;

        public readonly Texture2D whitePixel;
        private readonly Texture2D ntscPaletteLut;


        // Debugging:
        public bool debugDrawShadowReceivers, debugDrawShadowReceiverCulling;


        // "Batch" state:
        /// <summary>NOTE: Only exists when in a drawing begin/end block (there is a camera to draw on)</summary>
        public Camera Camera { get; protected set; }
        ShadowCasterList shadowCasterList;


        public void Begin(Camera camera, ShadowCasterList shadowCasterList)
        {
            if(fadeLevelStack.Count > 0)
                throw new InvalidOperationException("Cannot begin batch while still in fade block");
            if(this.Camera != null)
                throw new InvalidOperationException("Cannot begin while already in batch");

            this.Camera = camera;
            this.shadowCasterList = shadowCasterList;

            CommonBeginSpriteBatch();
        }

        public void End()
        {
            if(fadeLevelStack.Count > 0)
                throw new InvalidOperationException("Cannot end batch while still in fade block");
            if(this.Camera == null)
                throw new InvalidOperationException("Cannot end if not in batch");

            this.Camera = null;
            this.shadowCasterList = null;

            sb.End();
        }



        #region Doctor Effect

        Random random = new Random();

        public static Color ColorFromHSV(float hue, float saturation, float value)
        {
            float h = hue / 60f;
            float c = value * saturation; // chroma
            float x = c * (1f - Math.Abs((h%2f) - 1f));
            float m = value - c;
            switch((int)Math.Floor(h))
            {
                case 0: return new Color(c + m, x + m, m);
                case 1: return new Color(x + m, c + m, m);
                case 2: return new Color(m, c + m, x + m);
                case 3: return new Color(m, x + m, c + m);
                case 4: return new Color(x + m, m, c + m);
                case 5: return new Color(c + m, m, x + m);
                default: return Color.Transparent;
            }
        }

        /// <summary>NOTE: This has not been written for compatibility with other drawing modes!</summary>
        public void BeginDoctorEffect()
        {
            sb.End();

            // First attempt at a glitch effect (because sampling from a texture with the correct positioning is hard)
            doctorEffect.Parameters["doctorColor"].SetValue(ColorFromHSV((float)(random.NextDouble() * 360), 1, 1).ToVector4());

            sb.Begin(0, null, SamplerState.PointClamp, null, rasterizerState, doctorEffect, Camera.ViewMatrix);
        }

        /// <summary>NOTE: This has not been written for compatibility with other drawing modes!</summary>
        public void EndDoctorEffect()
        {
            sb.End();
            sb.GraphicsDevice.Textures[1] = null;
            CommonBeginSpriteBatch();
        }

        #endregion


        private void CommonBeginSpriteBatch()
        {
            Effect effect;
            if(fadeLevelStack.Count > 0)
            {
                fadeEffect.SetupForFadeLevel(fadeLevelStack.Peek());
                effect = fadeEffect.effect;
            }
            else
            {
                effect = spriteEffect;
            }

            sb.Begin(0, null, SamplerState.PointClamp, null, rasterizerState, effect, Camera.ViewMatrix);
        }


        Stack<int> fadeLevelStack = new Stack<int>();
        

        public void BeginFade(int fadeLevel)
        {
            if(this.Camera == null)
                throw new InvalidOperationException("Cannot begin a fade block while not in a batch");

            fadeLevelStack.Push(fadeLevel);

            // Ugly end-begin sequence. Oh well.
            sb.End();
            CommonBeginSpriteBatch();
        }

        public void EndFade(int fadeLevel)
        {
            fadeLevelStack.Pop();

            // Ugly end-begin sequence. Oh well.
            sb.End();
            CommonBeginSpriteBatch();
        }


        Rectangle? oldScissorRectangle;

        /// <summary>Start a scissor region in the current camera, given some Display bounds</summary>
        public void BeginScrollBox(Rectangle displayBounds, bool debugBounds = false)
        {
            if(oldScissorRectangle.HasValue)
                throw new InvalidOperationException("Already in scroll box, cannot start a new one"); // Nesting not currently supported

            Rectangle bounds = Camera.ContentToRender(Camera.DisplayToContent(displayBounds));

            sb.End();
            oldScissorRectangle = sb.GraphicsDevice.ScissorRectangle;
            bounds = Rectangle.Intersect(oldScissorRectangle.Value, bounds);
            sb.GraphicsDevice.ScissorRectangle = bounds;
            CommonBeginSpriteBatch();

            if(debugBounds)
            {
                sb.Draw(whitePixel, new Rectangle(-2000, -2000, 4000, 4000), Color.Magenta);
                sb.Draw(whitePixel, displayBounds, Color.Red);
            }
        }

        public void EndScrollBox()
        {
            if(!oldScissorRectangle.HasValue)
                throw new InvalidOperationException("Cannot end a scroll box while not in one");

            sb.End();

            sb.GraphicsDevice.ScissorRectangle = oldScissorRectangle.Value;
            oldScissorRectangle = null;

            CommonBeginSpriteBatch();
        }



        public void DrawShadowReceiver(Sprite whitemask, ShadowReceiver shadowReceiver, Position position, bool flipX, float debugOpacity = 0.7f)
        {
            if(Camera == null)
                throw new InvalidOperationException("Cannot draw shadows outside of batch");

            if(debugDrawShadowReceivers)
                sb.DrawWorld(whitemask, position, Color.White * debugOpacity, flipX);

            if(shadowCasterList == null)
                return; // Nothing to do

            shadowCasterList.DrawShadowReceiver(this, whitemask, shadowReceiver, position, flipX);
        }

        public void DrawDisplayRectangleInset(Rectangle rectangle, Color color)
        {
            const int thickness = 1;

            DrawPixel(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawPixel(new Rectangle(rectangle.X, rectangle.Y + thickness, thickness, rectangle.Height - 2 * thickness), color);
            DrawPixel(new Rectangle(rectangle.X + rectangle.Width - thickness, rectangle.Y + thickness, thickness, rectangle.Height - 2 * thickness), color);
            DrawPixel(new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - thickness, rectangle.Width, thickness), color);
        }

        public void DrawWorldRectangleInset(Rectangle rectangle, Color color)
        {
            var displayRectangle = new Rectangle(rectangle.X, -rectangle.Y - rectangle.Height, rectangle.Width, rectangle.Height);
            DrawDisplayRectangleInset(displayRectangle, color);
        }

        public void DrawFilledScreen(Color color)
        {
            Rectangle bounds = Camera.DisplayBounds;

            DrawFilledRectangle(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Width, bounds.Height), color, 0);
        }

        public void DrawFilledRectangle(Vector2 location, Vector2 size, Color color, float angle)
        {
            sb.Draw(whitePixel, location, null, color, angle, Vector2.Zero, size, SpriteEffects.None, 0);
        }

        public void SetupShadowReceiver(Sprite whitemask, ShadowReceiver shadowReceiver, Position position, bool flipX)
        {
            if(debugDrawShadowReceiverCulling)
                sb.DrawWorld(whitemask, position, Color.White, flipX);


            // Setup matrix for drawing shadow sprites (matches SpriteBatch)
            shadowMaskEffect.Parameters["ViewProjectMatrix"].SetValue(Camera.ViewProjectMatrix);

            // Setup shadow mask:
            sb.GraphicsDevice.Textures[1] = whitemask.texture;
            sb.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;

            // Setup matrix for calculating texture coordinates within the shadow mask (converts spritesheet corner positions in Display space to UVs)
            Matrix maskTextureMatrix;
            if(!flipX)
            {
                Vector2 drawOriginInSheet = new Vector2(whitemask.sourceRectangle.X + whitemask.origin.X, whitemask.sourceRectangle.Y + whitemask.origin.Y + 1); // <- equivelent to Sprite.DrawOrigin
                Vector2 maskTopLeftDisplay = position.ToDisplay - drawOriginInSheet;
                maskTextureMatrix = Matrix.CreateTranslation(new Vector3(-maskTopLeftDisplay, 0))
                        * Matrix.CreateScale(1f / whitemask.texture.Width, 1f / whitemask.texture.Height, 1f);
            }
            else
            {
                Vector2 drawOriginInSheetFlipX = new Vector2(whitemask.texture.Width - (whitemask.sourceRectangle.X + whitemask.origin.X) - 1, whitemask.sourceRectangle.Y + whitemask.origin.Y + 1); // <- equivelent to Sprite.DrawOriginFlipX
                Vector2 maskTopLeftDisplay = position.ToDisplay - drawOriginInSheetFlipX;
                maskTextureMatrix = Matrix.CreateTranslation(new Vector3(-maskTopLeftDisplay, 0))
                        * Matrix.CreateScale(1f / whitemask.texture.Width, 1f / whitemask.texture.Height, 1f)
                        * Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateTranslation(1, 0, 0); // <- equivalent to SpriteEffects.FlipHorizontally
            }

            shadowMaskEffect.Parameters["MaskTextureMatrix"].SetValue(maskTextureMatrix);


            if(!debugDrawShadowReceiverCulling)
            {
                // Break out of the active sprite batch to draw shadows using the mask effect:
                sb.End();
                sb.Begin(0, null, SamplerState.PointClamp, null, rasterizerState, shadowMaskEffect, Camera.ViewMatrix);
            }
        }

        public void TeardownShadowReceiver()
        {
            if(!debugDrawShadowReceiverCulling)
            {
                // We now return you to your regularly scheduled sprite batch...
                sb.End();
                sb.Begin(0, null, SamplerState.PointClamp, null, rasterizerState, spriteEffect, Camera.ViewMatrix);
            }
        }

        
    }
}
