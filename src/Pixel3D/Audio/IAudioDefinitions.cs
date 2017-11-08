namespace Pixel3D.Audio
{
	public interface IAudioDefinitions
	{
	    Cue GetCue(string name, object debugContext);
        SafeSoundEffect GetSound(Cue cue, int index);
	}
}