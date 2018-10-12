// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Pixel3D.Audio
{
	public struct SoundBank
	{
		public OrderedDictionary<string, int> lookup;
		public SafeSoundEffect[] sounds;

		public int Count { get { return sounds.Length; } }


		public SafeSoundEffect this[string name]
		{
			get
			{
				int i;
				if(lookup.TryGetValue(name, out i))
					return sounds[i];
				else
					return null;
			}
		}

		public static SoundBank CreateEmpty(AudioPackage input)
		{
			// IMPORTANT: The sound bank is definition data. We must return the same result no matter whether or not we have an audio device!
			//            (SafeSoundEffect serializes as reference only, so we don't need to care about its contents.)

			SoundBank result;
			result.lookup = input.lookup;
			result.sounds = new SafeSoundEffect[input.Count];
			for(int i = 0; i < result.sounds.Length; i++)
			{
				result.sounds[i] = new SafeSoundEffect();
			}

			return result;
		}

		public unsafe void FillFrom(AudioPackage source)
		{
			Debug.Assert(source.Count == Count);
			Debug.Assert(ReferenceEquals(source.lookup, lookup));

			source.FillSoundEffectArray(sounds);
		}
		
	}


}
