using Pixel3D.Engine.Collections;

namespace Pixel3D.Engine.Levels
{
    public class LevelPosition
    {
        public Position position;

        /// <summary>Arbitrary level properties (consumers are expected to parse the strings)</summary>
        public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();

        // Provided to allow parameterless construction (due to presence of deserialization constructor)
        public LevelPosition() { }

        #region Serialization

        public virtual void Serialize(LevelSerializeContext context)
        {
            context.bw.Write(position);
            context.bw.Write(properties.Count);
            foreach (var kvp in properties)
            {
                context.bw.Write(kvp.Key);
                context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
            }
        }

        /// <summary>Deserialize into new object instance</summary>
        public LevelPosition(LevelDeserializeContext context)
        {
            Deserialize(context);
        }

        public void Deserialize(LevelDeserializeContext context)
        {
            position = context.br.ReadPosition();
            int count = context.br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                properties.Add(context.br.ReadString(), context.br.ReadString());
            }
        }

        #endregion
    }
}