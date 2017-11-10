using System;
using Pixel3D.AssetManagement;

namespace Pixel3D.Levels
{
    public class Teleporter : Region, IEditorNameProvider
    {
        // Provided to allow parameterless construction (due to presence of deserialization constructor)
        public Teleporter() { }

        /// <summary>The asset path of the level to teleport to, or null for no teleport</summary>
        private string targetLevel;
        public string TargetLevel
        {
            get { return targetLevel; }
            set { targetLevel = value == null ? null : AssetManager.CanonicaliseAssetPath(value); }
        }

        /// <summary>The target symbol the spawn points to use</summary>
        public string targetSpawn;


        public string EditorName
        {
            get { return string.Format("{0}_{1}", TargetLevel, targetSpawn); }
        }

        /// <summary>Set to true if you never want this teleporter to appear in random (or nearest/furthest) selections</summary>
        public bool neverSelectAtRandom;



        #region Serialization

        public override void Serialize(LevelSerializeContext context)
        {
            if (TargetLevel != null)
                TargetLevel = TargetLevel.ToLowerInvariant();
            context.bw.WriteNullableString(TargetLevel);
            context.bw.WriteNullableString(targetSpawn);

            if(context.Version >= 18)
                context.bw.Write(neverSelectAtRandom);

            base.Serialize(context);
        }


        /// <summary>Deserialize into new object instance</summary>
        public Teleporter(LevelDeserializeContext context)
        {
            TargetLevel = context.br.ReadNullableString();
            if (TargetLevel != null)
                TargetLevel = TargetLevel.ToLowerInvariant();
            targetSpawn = context.br.ReadNullableString();

            if(context.Version >= 18)
                neverSelectAtRandom = context.br.ReadBoolean();

            base.Deserialize(context);
        }

        #endregion

        
    }
}
