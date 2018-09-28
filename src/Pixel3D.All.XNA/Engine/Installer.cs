// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;
using Pixel3D.AssetManagement;
using Pixel3D.Audio;
using Pixel3D.LoopRecorder;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;
using SoundState = Pixel3D.Audio.SoundState;

namespace Pixel3D.Engine
{
	/// <summary>
	/// Links independent Pixel3D engine components to FNA. Should be called once from an external game project.
	/// </summary>
	public static class Installer
	{
		public static void Install(int maxPlayers)
		{
			AssetReader.serviceObjectProvider = services => ((IGraphicsDeviceService)services.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;

			InstallAudioSystem(maxPlayers);			
		}

		private static void InstallAudioSystem(int maxPlayers)
		{
            AudioSystem.worldToAudio = (owner, x, y, z) =>
            {
                var camera = (Camera)owner;
                var position = camera.WorldToAudio(new Position(x, y, z));
                return new PitchPan(position.X, position.Y);
            };

            AudioSystem.getMaxPlayers = () => maxPlayers;
            AudioSystem.audioDeviceCheck = AudioDeviceCheck;
            AudioSystem.getMasterVolume = owner => SoundEffect.MasterVolume;
            AudioSystem.setMasterVolume = (owner, value) => SoundEffect.MasterVolume = value;

            AudioSystem.getPlayerAudioPosition = (owner, playerIndex) =>
            {
                var gameState = (IGameState)owner;
                var position = gameState.GetPlayerPosition(playerIndex);
                return position;
            };

            //
            // SoundEffectInstance:
            {
                AudioSystem.createSoundEffectInstance = owner => owner == null ? null : ((SoundEffect) owner).CreateInstance();

                AudioSystem.playSoundEffectInstance = owner => ((SoundEffectInstance) owner).Play();
                AudioSystem.stopSoundEffectInstance = owner => ((SoundEffectInstance) owner).Stop();

                AudioSystem.getIsLooped = owner => ((SoundEffectInstance) owner).IsLooped;
                AudioSystem.setIsLooped = (owner, value) => ((SoundEffectInstance) owner).IsLooped = value;

                AudioSystem.getIsDisposed = owner => ((SoundEffectInstance)owner).IsDisposed;

                AudioSystem.getVolume = owner => ((SoundEffectInstance) owner).Volume;
                AudioSystem.setVolume = (owner, value) => ((SoundEffectInstance) owner).Volume = value;

                AudioSystem.getPitch = owner => ((SoundEffectInstance) owner).Pitch;
                AudioSystem.setPitch = (owner, value) => ((SoundEffectInstance) owner).Pitch = value;

                AudioSystem.getPan = owner => ((SoundEffectInstance) owner).Pan;
                AudioSystem.setPan = (owner, value) => ((SoundEffectInstance) owner).Pan = value;

                AudioSystem.getSoundState = owner => (SoundState) ((SoundEffectInstance) owner).State;

                AudioSystem.getDuration = owner => ((SoundEffect) owner).Duration;

                AudioSystem.setFadePitchPan = (owner, fade, pitch, pan) =>
                {
                    owner.Volume = fade;
                    owner.Pitch = pitch;
                    owner.Pan = pan;
                };
            }

            //
            // SoundEffect:
            AudioSystem.playSoundEffect = (owner, volume, pitch, pan) => ((SoundEffect)owner).Play(volume, pitch, pan);
            AudioSystem.createSoundEffectFromStream = stream => new SafeSoundEffect(SoundEffect.FromStream(stream));
            AudioSystem.createSoundEffectFromFile = path =>
            {
                using (var fs = File.OpenRead(path))
                {
                    return new SafeSoundEffect(SoundEffect.FromStream(fs));
                }
            };

            //
            // Audio Diagnostics:
            AudioSystem.reportMissingCue = (name, debugContext) =>
            {
                string s = debugContext as string;
                var provider = debugContext as IEditorNameProvider;

                string c;
                if (s != null)
                    c = s;
                else if (provider != null)
                    c = provider.GetType().Name + ": " + provider.EditorName;
                else if (debugContext != null)
                    c = debugContext.ToString();
                else
                    c = "[no context]";

                string message = "Missing cue \"" + name + "\" (context: {c})";
                Debug.WriteLine(message);
                Log.Current.Warn(message);
            };
            AudioSystem.reportExpectedCue = (context, args) =>
            {
                AnimationSet animationSet = args[0] as AnimationSet;
                Animation animation = args[1] as Animation;
                int frame = (int)args[2];

                string format;
                if (animation == null)
                    format = "Expected cue for \"{0}\" on AnimationSet = {1}";
                else if (frame == -1)
                    format = "Expected cue for \"{0}\" on Animation = {2} (AnimationSet = {1})";
                else
                    format = "Expected cue for \"{0}\" on Frame = {3} (AnimationSet = {1}, Animation = {2})";

                string message = string.Format(format, context, animationSet == null ? "???" : animationSet.friendlyName,
                    animation == null ? "???" : animation.friendlyName, frame);
                Debug.WriteLine(message);
                Log.Current.Warn(message);
            };
		}

		private static bool AudioDeviceCheck()
		{
			try
			{
				SoundEffect.MasterVolume = 1f;
				// The above line should throw an exception if there is no audio device
				return true;
			}
			catch (NoAudioHardwareException)
			{
				Debug.WriteLine("No audio hardware available");
				return false;
			}
			catch (Exception e)
			{
				Debug.WriteLine("Exception during audio device testing. XNA or something under it doing something dumb.");
				Log.Current.WarnException("Exception during audio device testing", e);
				return false;
			}
		}
	}
}