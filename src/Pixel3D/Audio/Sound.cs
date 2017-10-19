namespace Pixel3D.Audio
{
    public class Sound
    {
        public string path;

        #region Serialization

        public void Serialize(CueSerializeContext context)
        {
            context.bw.Write(path);
        }

        /// <summary>Deserialize into new object instance</summary>
        public Sound(CueDeserializeContext context)
        {
            path = context.br.ReadString();
        }

        public Sound() { }

        #endregion

        #region Editor

        public bool muted;

        #endregion
    }
}