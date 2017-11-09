namespace Pixel3D.Engine
{
	public interface IGameState
	{
		int MaxPlayers { get; }
	    Position? GetPlayerPosition(int playerIndex);
	}
}