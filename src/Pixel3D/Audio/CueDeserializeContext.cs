using System;
using System.IO;

namespace Pixel3D.Audio
{
    public partial class CueDeserializeContext
    {
        public CueDeserializeContext(BinaryReader br)
        {
            this.br = br;
            Version = br.ReadInt32();
            if (Version > CueSerializeContext.formatVersion)
                throw new Exception("Tried to load Cue with a version that is too new");
        }

        public readonly BinaryReader br;

        public int Version { get; private set; }

        public Sound ReadSound()
        {
            return new Sound(this);
        }
    }
}