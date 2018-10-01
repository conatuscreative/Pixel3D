using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CRTSim
{
	public static class GraphicsDeviceExtensions
	{
		public static void LazySetupRenderTarget(this GraphicsDevice device, ref RenderTarget2D renderTarget, Point renderSize, Color? clearColor = null)
		{
			device.LazySetupRenderTarget(ref renderTarget, renderSize.X, renderSize.Y, clearColor);
		}

		public static void LazySetupRenderTarget(this GraphicsDevice device, ref RenderTarget2D renderTarget, int width, int height, Color? clearColor = null)
		{
			int maxTextureSize = device.GraphicsProfile == GraphicsProfile.Reach ? 2048 : 4096;
			width = width.Clamp(1, maxTextureSize);
			height = height.Clamp(1, maxTextureSize);

			if (renderTarget == null || renderTarget.Width != width || renderTarget.Height != height)
			{
				if (renderTarget != null)
					renderTarget.Dispose();
				renderTarget = new RenderTarget2D(device, width, height);

				if (clearColor.HasValue)
				{
					device.SetRenderTarget(renderTarget);
					device.Clear(clearColor.GetValueOrDefault());
				}
			}
		}
	}
}