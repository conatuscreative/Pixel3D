using Pixel3D.Audio;

namespace Pixel3D.Engine.Audio
{
    /// <summary>
    /// Objects that can play back ambient audio
    /// </summary>
    public interface IAmbientSoundSource
    {
        AmbientSound AmbientSound { get; }
	    Position Position { get; }
        bool FacingLeft { get; }
        AudioAABB? Bounds { get; }
    }
}