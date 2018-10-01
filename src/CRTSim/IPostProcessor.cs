using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CRTSim
{
	public interface IPostProcessor
	{
		GraphicsDevice GraphicsDevice { get; }
		ContentManager Content { get; }
		void UpdateCameraWindows();

		int PostProcessingCount { get; }
		void SetPostProcessTo(sbyte index);
	}
}