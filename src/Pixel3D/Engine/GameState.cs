namespace Pixel3D.Engine
{
    public abstract class GameState : IGameState
    {
        public abstract int MaxPlayers { get; }
        public abstract Position? GetPlayerPosition(int playerIndex);
    }
}