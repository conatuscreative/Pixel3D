using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Audio
{
    /// <summary>Because XNA sucks, it will crash when attempting to create a sound if there is no audio hardware. We must wrap, because we need an object for serialization.</summary>
    public class SafeSoundEffect
    {
        public SafeSoundEffect() { }
        public SafeSoundEffect(SoundEffect soundEffect)
        {
            this.soundEffect = soundEffect;
            
        }

        /// <summary>The underlying sound effect (can be null)</summary>
        public SoundEffect soundEffect;




        #region Network Serialization

        // "Ignore" serializer, as per SerializeIgnoreXNA -- we want to be able to store a ref for the definition table, but we can't deserialize a sound effect
        [CustomSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, SafeSoundEffect value) { context.VisitObject(value); context.LeaveObject(); }
        [CustomSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, SafeSoundEffect value) { throw new InvalidOperationException(); }
        // Outright block SoundEffect from serializing
        [CustomSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, SoundEffect value) { throw new InvalidOperationException(); }
        [CustomSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, SoundEffect value) { throw new InvalidOperationException(); }

        #endregion



        #region Wrapper

        public bool Play()
        {
            return soundEffect != null && soundEffect.Play(_sfxVolume, 0, 0);
        }

        public bool Play(float volume, float pitch, float pan)
        {
            return soundEffect != null && soundEffect.Play(volume * _sfxVolume, pitch, pan);
        }

        public bool Play(FadePitchPan fpp)
        {
            return soundEffect != null && soundEffect.Play(fpp.fade * _sfxVolume, fpp.pitch, fpp.pan);
        }

        /// <summary>Create an instance of the sound effect (can return null)</summary>
        public SoundEffectInstance CreateInstance()
        {
            return soundEffect != null ? soundEffect.CreateInstance() : null;
        }

        public static SafeSoundEffect FromStream(Stream stream)
        {
            if (!AudioDevice.Available)
                return new SafeSoundEffect();
            return new SafeSoundEffect(SoundEffect.FromStream(stream));
        }

        public static SafeSoundEffect FromFile(string path)
        {
            if(!AudioDevice.Available)
                return new SafeSoundEffect();
            using(var fs = File.OpenRead(path))
            {
                return new SafeSoundEffect(SoundEffect.FromStream(fs));
            }
        }


        #endregion



        #region Instance Pool (For SoundEffectManager only)

        // This is basically so that SoundEffectManager doesn't need to have a Dictionary lookup.
        // NOTE: Thread-safety is predicated on being inside the SoundEffectManager lock!!
        // NOTE: Network serialization cannot get at `instancePool` and cannot overwrite it (due to custom serializer, and it having no deserialize path)

        // TODO: Should probably expire old, unused instances
        private readonly List<SoundEffectInstance> instancePool = new List<SoundEffectInstance>();

        /// <summary>IMPORTANT: We assume you will fully set the Volume, Pitch and Pan properties. We assume you never set IsLooped!</summary>
        public SoundEffectInstance SoundEffectManager_GetInstance()
        {
            if(instancePool.Count == 0)
                return CreateInstance();
            else
            {
                var instance = instancePool[instancePool.Count-1];
                instancePool.RemoveAt(instancePool.Count-1);
                return instance;
            }
        }

        /// <summary>IMPORTANT: We assume you stopped the instance...</summary>
        public void SoundEffectManager_ReturnInstance(SoundEffectInstance instance)
        {
            // NOTE: We cannot check if the sound is really stopped, because of the way threading works in the XNA sound library (ie: in a dumb way.)
            Debug.Assert(instance.IsLooped == false);
            instancePool.Add(instance);
        }

        #endregion



        /// <summary>How long is this sound effect in frames at a given pitch (NOTE: this value is not network-safe)</summary>
        public int DurationInFrames(float pitch)
        {
            if(soundEffect == null)
                return 1; // <- oh well;

            // Making a reasonably safe assumption about how XNA pitch-bending works here:
            double seconds = soundEffect.Duration.TotalSeconds;
            seconds = seconds / System.Math.Pow(2.0, pitch); // <- pitch bend changes duration

            return (int)System.Math.Ceiling(seconds * 60);
        }



        #region Static Methods

        public static float MasterVolume
        {
            get { return AudioDevice.Available ? SoundEffect.MasterVolume : 0f; }
            set
            {
                if(AudioDevice.Available && SoundEffect.MasterVolume != value) // <- avoid touching native sound engine if we'd just set the same value
                    SoundEffect.MasterVolume = value;
            }
        }


        private static float _sfxVolume = 1.0f;

        public static float SoundEffectVolume
        {
            get { return _sfxVolume; }
            set { _sfxVolume = value; }
        }

        #endregion


        
    }
}
