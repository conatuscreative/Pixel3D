// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pixel3D.Audio
{
	public static class MusicManager
	{
		private const float fadeTime = 0.8f; // seconds
		private const float sufficientFadeOutToStart = 0.2f;
		private const float sufficientFadeOutToFadeIn = 0.75f;

		public const float DefaultVolume = 0.7f;

		// Public methods are thread-safe (must lock)
		private static readonly object lockObject = new object();
		private static float _volume = DefaultVolume; // <- good default (keeps the sound effects above the music)

		public static float Volume
		{
			get
			{
				lock (lockObject)
				{
					return _volume;
				}
			}
			set
			{
				lock (lockObject)
				{
					_volume = value;
					if (!AudioDevice.Available)
						return;

					foreach(var a in activeMusic)
						if(a.instance != null)
							a.instance.Volume = a.fade.StepNES() * _volume;

					foreach (var f in fadingOutMusic)
						f.instance.Volume = f.fade.StepNES() * _volume;
				}
			}
		}


		private static AudioPackage _musicSource;
		public static AudioPackage MusicSource
		{
			get
			{
				lock(lockObject)
				{
					return _musicSource;
				}
			}
			set
			{
				lock(lockObject)
				{
					_musicSource = value;
				}
			}
		}


		/// <summary>Volume stepping like a NES (16 levels)</summary>
		public static float StepNES(this float value)
		{
			return (float) (Math.Round(value * 16.0) / 16.0);
		}


		#region Audio Stream Pool

		private static Stack<IStreamingAudio> streamingAudioPool = new Stack<IStreamingAudio>();

		private static void StopAndReturnToPool(IStreamingAudio instance)
		{
			instance.Close();
			streamingAudioPool.Push(instance);
		}

		private static IStreamingAudio CreateEmptyStreamingAudio()
		{
			IStreamingAudio instance;
			if(streamingAudioPool.Count > 0)
				instance = streamingAudioPool.Pop();
			else
				instance = AudioSystem.createEmptyStreamingAudio();
			return instance;
		}

		#endregion


		#region Active Music

		/// <param name="synchronise">Start music immediately, don't wait for fade</param>
		public static void SetMenuMusic(string musicPath, bool loop = true, bool synchronise = false)
		{
			lock (lockObject)
			{
				SetMusic(musicPath, 0, loop, synchronise);
			}
		}

		/// <param name="synchronise">Start music immediately, don't wait for fade</param>
		public static void SetGameMusic(string musicPath, bool loop = true, bool synchronise = false)
		{
			lock (lockObject)
			{
				SetMusic(musicPath, 1, loop, synchronise);
			}
		}

		private struct ActiveMusic
		{
			public string musicPath; // <- used to indicate "exists" ("has value")
			public IStreamingAudio instance;
			public float fade;
			public bool loop;
			public bool synchronise;
		}

		// Active music at different priorities (lower index is higher priority)
		private static readonly ActiveMusic[] activeMusic = new ActiveMusic[2];

		private static int BestPriority
		{
			get
			{
				int i;
				for (i = 0; i < activeMusic.Length; i++)
					if (activeMusic[i].musicPath != null)
						break;
				return i;
			}
		}

		private static void SetMusic(string musicPath, int priority, bool loop, bool synchronise)
		{
			if (!AudioDevice.Available)
				return;

			// ReSharper disable once PossibleUnintendedReferenceComparison
			if (activeMusic[priority].musicPath == musicPath)
				return; // Already playing this song

			// Get rid of music currently set at this level (possibly with a fade-out, if it is still playing)
			if (activeMusic[priority].musicPath != null)
			{
				if (activeMusic[priority].instance != null)
				{
					if (activeMusic[priority].fade == 0)
						StopAndReturnToPool(activeMusic[priority].instance);
					else
						fadingOutMusic.Add(new FadingOutMusic
						{
							fade = activeMusic[priority].fade,
							instance = activeMusic[priority].instance
						});
				}

				activeMusic[priority] = default(ActiveMusic);
			}
			
			if (musicPath == null)
				return; // Nothing to play

			if(!_musicSource.Contains(musicPath))
			{
				Debug.Assert(false, "Music package is missing this path: " + musicPath);
				return;
			}

			activeMusic[priority].musicPath = musicPath;
			activeMusic[priority].loop = loop;
			activeMusic[priority].synchronise = synchronise;

			if (synchronise && priority >= BestPriority)
				DoFastFade(); // Make the most of the synced music opening

			if (synchronise)
				StartPlaying(priority,
					priority >= BestPriority
						? 1f
						: 0f); // <- synced music starts immediately (even if it is at zero volume)
			else if (CanStartSong(priority, sufficientFadeOutToStart))
				StartPlaying(priority);
		}

		private static unsafe void StartPlaying(int priority, float fade = 1f)
		{
			var entry = _musicSource.GetEntryByPath(activeMusic[priority].musicPath);
			if(!entry.Valid)
				return;
			
			var instance = CreateEmptyStreamingAudio();
			instance.Open(entry.VorbisStart, entry.VorbisEnd, entry.LoopStart);
			
			activeMusic[priority].instance = instance;
			activeMusic[priority].fade = fade;

			instance.Volume = _volume * fade;
			instance.IsLooped = activeMusic[priority].loop;
			instance.Play();
		}

		private static bool CanStartSong(int priority, float sufficientFade)
		{
			if (fadingOutMusic.Count > 0 && fadingOutMusic[fadingOutMusic.Count - 1].fade > sufficientFade)
				return false;

			for (var i = 0; i < activeMusic.Length; i++)
				if (activeMusic[i].musicPath != null)
				{
					if (i < priority)
						return false; // never start playing something when we have a higher priority to play
					if (i > priority)
						if (activeMusic[i].fade > sufficientFade)
							return false; // waiting to fade out
				}

			return true;
		}

		public static void Update(TimeSpan elapsedTime)
		{
			if (!AudioDevice.Available)
				return;

			lock (lockObject)
			{
				var seconds = (float) elapsedTime.TotalSeconds;

				UpdateFadeOuts(seconds);

				// Fade in/out the active music:
				var bestPriority = activeMusic.Length;
				for (var i = 0; i < activeMusic.Length; i++) // highest to lowest priority
					if (activeMusic[i].musicPath != null)
					{
						if (i < bestPriority)
							bestPriority = i;


						if (i == bestPriority)
						{
							if (activeMusic[i].instance == null)
							{
								// NOTE: synced music does not get a chance to restart (should never happen anyway)
								if (!activeMusic[i].synchronise && CanStartSong(i, sufficientFadeOutToStart))
									StartPlaying(i);
							}
							else if (activeMusic[i].fade < 1f)
							{
								if (activeMusic[i].fade == 0f)
								{
									if (CanStartSong(i, sufficientFadeOutToFadeIn))
									{
										if (activeMusic[i].loop) // NOTE: only looping music gets paused when silent
											activeMusic[i].instance.Play(); // unpause
									}
									else
									{
										continue;
									}
								}

								activeMusic[i].fade += seconds / fadeTime; // fade in
								if (activeMusic[i].fade > 1f)
									activeMusic[i].fade = 1f;

								activeMusic[i].instance.Volume = _volume * activeMusic[i].fade.StepNES();
							}
						}


						if (i > bestPriority)
							if (activeMusic[i].instance != null)
								if (activeMusic[i].fade > 0f)
								{
									activeMusic[i].fade -= seconds / fadeTime; // fade out
									if (activeMusic[i].fade <= 0f)
									{
										activeMusic[i].fade = 0f;

										activeMusic[i].instance.Volume = 0f;

										if (activeMusic[i].loop) // NOTE: only looping music gets paused when silent
											activeMusic[i].instance.Pause();
									}
									else
									{
										activeMusic[i].instance.Volume = _volume * activeMusic[i].fade.StepNES();
									}
								}
					}
			}
		}

		#endregion


		#region Fading Out Music

		private struct FadingOutMusic
		{
			public float fade;
			public IStreamingAudio instance;
			public bool fastFade;
		}

		// This acts as a queue
		private static readonly List<FadingOutMusic> fadingOutMusic = new List<FadingOutMusic>();

		private static void DoFastFade()
		{
			for (var i = 0; i < fadingOutMusic.Count; i++)
			{
				var f = fadingOutMusic[i];
				f.fastFade = true;
				fadingOutMusic[i] = f;
			}
		}

		private static void UpdateFadeOuts(float seconds)
		{
			for (var i = 0; i < fadingOutMusic.Count;) // NOTE: in-loop removal
			{
				var f = fadingOutMusic[i];

				var fadeOutBy = seconds / fadeTime;
				if (f.fastFade)
					fadeOutBy *= 3;
				f.fade -= fadeOutBy;

				if (f.fade <= 0)
				{
					StopAndReturnToPool(f.instance);
					fadingOutMusic.RemoveAt(i);
					continue;
				}

				f.instance.Volume = _volume * f.fade.StepNES();
				fadingOutMusic[i] = f;

				i++;
			}
		}

		#endregion

	}
}