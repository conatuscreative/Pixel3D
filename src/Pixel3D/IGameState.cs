namespace Pixel3D
{
	public interface IGameState
	{
		int MaxPlayers { get; set; }

		Position? GetPlayerPosition(int playerIndex);
	}
}