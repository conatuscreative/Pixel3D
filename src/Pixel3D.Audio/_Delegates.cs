using System;
using System.IO;

namespace Pixel3D.Audio
{
	public delegate int GetMaxPlayers();
	public delegate PitchPan WorldToAudio(object owner, int x, int y, int z);
	public delegate AudioPosition? GetPlayerAudioPosition(object owner, int playerIndex);
	public delegate SafeSoundEffect CreateSoundEffectFromStream(Stream stream);
	public delegate SafeSoundEffect CreateSoundEffectFromFile(string path);
	public delegate IDisposable CreateSoundEffectInstance(object owner);
	public delegate string GetString(IDisposable owner);
	public delegate void SetString(IDisposable owner, string value);
	public delegate bool GetBoolean(IDisposable owner);
	public delegate void SetBoolean(IDisposable owner, bool value);
	public delegate float GetSingle(IDisposable owner);
	public delegate void SetSingle(IDisposable owner, float value);
	public delegate SoundState GetSoundState(IDisposable owner);
	public delegate void SetState(IDisposable owner, SoundState value);
	public delegate TimeSpan GetTimeSpan(IDisposable owner);
	public delegate void SetTimeSpan(IDisposable owner, TimeSpan value);
	public delegate bool IsAudioDeviceAvailable();
	public delegate bool PlaySoundEffect(IDisposable owner, float volume, float pitch, float pan);
	public delegate void PlaySoundEffectInstance(IDisposable owner);
	public delegate void StopSoundEffectInstance(IDisposable owner);
	public delegate void PauseSoundEffectInstance(IDisposable owner);
	public delegate void SetFadePitchPan(SafeSoundEffectInstance owner, float volume, float pitch, float pan);
	public delegate void ReportExpectedCue(string context, params object[] args);
	public delegate void ReportMissingCue(string name, object debugContext);
}
