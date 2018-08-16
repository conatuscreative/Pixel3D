using System;
using System.Collections.Generic;
using Pixel3D.Engine.Collections;
using Pixel3D.Engine.Levels;

namespace Pixel3D.Engine.Levels
{
    public class Path
    {
        // Provided to allow parameterless construction (due to presence of deserialization constructor)
        public Path() { }

        /// <summary>Arbitrary level properties (consumers are expected to parse the strings)</summary>
        public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();

        public List<LevelPosition> positions = new List<LevelPosition>();
        public bool looped;

        #region Serialization

        public virtual void Serialize(LevelSerializeContext context)
        {
            context.bw.WriteBoolean(looped);
            context.bw.Write(positions.Count);
            foreach (var position in positions)
                position.Serialize(context);

            context.bw.Write(properties.Count);
            foreach (var kvp in properties)
            {
                context.bw.Write(kvp.Key);
                context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
            }
        }


        /// <summary>Deserialize into new object instance</summary>
        public Path(LevelDeserializeContext context)
        {
            looped = context.br.ReadBoolean();
            var positionsCount = context.br.ReadInt32();
            positions = new List<LevelPosition>(positionsCount);
            for (var i = 0; i < positionsCount; i++)
                positions.Add(new LevelPosition(context));

            var count = context.br.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                properties.Add(context.br.ReadString(), context.br.ReadString());
            }
        }

        #endregion
    }
}