using Pixel3D.Animations;

namespace Pixel3D.Engine
{
    public interface IFullScreenEffect
    {
        void DrawFullScreenEffect(Camera camera, DrawContext context, GameState readOnlyGameState, IDrawSmoothProvider sp);
    }
}