namespace Pixel3D.UI
{
	public interface IDrawableGameMenuHost : IGameMenuHost
	{
		Position Position { get; }
		int Width { get; }
		int Height { get; }
		bool DeferLayout { get; }
	}
}