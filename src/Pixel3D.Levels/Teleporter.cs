using Pixel3D.AssetManagement;
using Pixel3D.FrameworkExtensions;

namespace Pixel3D.Engine.Levels
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
    }
}
