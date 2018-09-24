using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Pixel3D
{
    public class Fader
    {
        public readonly FadeEffect fadeEffect;

        RenderTarget2D fadeBuffer;
        Viewport fadeEffectOriginalViewport;
        
        private readonly GraphicsDevice device;
        private readonly ContentManager content;
        private readonly SpriteBatch sb;

        public Fader(GraphicsDevice device, ContentManager content, SpriteBatch spriteBatch, FadeEffect fadeEffect)
        {
            this.device = device;
            this.content = content;
            this.sb = spriteBatch;
            this.fadeEffect = fadeEffect;
        }


        public void StartFadeEffect(int fadeLevel)
        {
            // TODO: Convert from using Viewport to pass content size (convert to new camera system)
            //       (This will allow us to render without scaling in the render target)
            var vp = device.Viewport;
            fadeEffectOriginalViewport = vp;

            // TODO: The post-process effect should probably happen at zoom=1, and the result gets scaled up
            //       (Avoid problems with reach profile on huge monitors, improve performance)

            int maxTextureSize = device.GraphicsProfile == GraphicsProfile.Reach ? 2048 : 4096;
            int width = Math.Min(maxTextureSize, vp.Width);
            int height = Math.Min(maxTextureSize, vp.Height);

            if (fadeBuffer == null || fadeBuffer.Width != width || fadeBuffer.Height != height)
            {
                if (fadeBuffer != null)
                    fadeBuffer.Dispose();
                fadeBuffer = new RenderTarget2D(device, width, height);
            }

            device.SetRenderTarget(fadeBuffer);
        }


        public void EndFadeEffect(int fadeLevel)
        {
            device.SetRenderTarget(null);
            device.Viewport = fadeEffectOriginalViewport;

            device.Clear(Color.Black); // <- TODO Fix up unnecessary clears

            fadeEffect.SetupForFadeLevel(fadeLevel);

            sb.Begin(0, null, SamplerState.PointClamp, null, null, fadeEffect.effect);
            sb.Draw(fadeBuffer, Vector2.Zero, Color.White);
            sb.End();
        }

    }
}