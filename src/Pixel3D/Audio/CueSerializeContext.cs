using System.IO;

namespace Pixel3D.Audio
{
    public partial class CueSerializeContext
    {
        public CueSerializeContext(BinaryWriter bw) : this(bw, formatVersion) { } // Default to writing current version

        public CueSerializeContext(BinaryWriter bw, int version)
        {
            this.bw = bw;
            this.Version = version;

            bw.Write(Version);
        }

        public readonly BinaryWriter bw;

        #region Version

        /// <summary>Increment this number when anything we serialize changes</summary>
        public const int formatVersion = 3;

        public int Version { get; private set; }

        #endregion

        public void WriteSound(Sound sound)
        {
            sound.Serialize(this);
        }
    }
}