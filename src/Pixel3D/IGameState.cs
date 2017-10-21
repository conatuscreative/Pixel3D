namespace Pixel3D
{
	public interface IGameState
	{
		int MaxPlayers { get; }

		Position? GetPlayerPosition(int playerIndex);
	}
}