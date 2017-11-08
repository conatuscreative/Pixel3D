using System;
using System.Diagnostics;
using System.IO;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.AssetManagement;

namespace Pixel3D.Levels
{
    public partial class LevelSerializeContext
    {
        public LevelSerializeContext(BinaryWriter bw, ImageWriter imageWriter, IAssetPathProvider assetPathProvider) : this(bw, imageWriter, assetPathProvider, formatVersion) { } // Default to writing current version

        public LevelSerializeContext(BinaryWriter bw, ImageWriter imageWriter, IAssetPathProvider assetPathProvider, int version)
        {
            this.bw = bw;
            this.assetPathProvider = assetPathProvider;
            this.Version = version;

            if(Version > LevelSerializeContext.formatVersion)
                throw new Exception("Tried to save Level with a version that is too new");
            if(Version < 13)
                throw new Exception("Level version too old!");
            
            bw.Write(Version);

            animationSerializeContext = new AnimationSerializeContext(bw, imageWriter); // <- writes out animation version number
        }

        public readonly BinaryWriter bw;

        /// <summary>True for save change monitoring (try to avoid mutation). Must set AnimationSerializeContext.monitor independently.</summary>
        public bool monitor;


        #region Version

        /// <summary>Increment this number when anything we serialize changes</summary>
        public const int formatVersion = 20;

        public int Version { get; private set; }

        #endregion


        /// <summary>NOTE: parallel updates between serialize and deserialize</summary>
        public int nextRegionIndex = 0;

        public readonly AnimationSerializeContext animationSerializeContext;
        IAssetPathProvider assetPathProvider;

        /// <summary>IMPORTANT: Must match with calls to RegisterImages</summary>
        public void WriteAnimationSet(AnimationSet animationSet)
        {
            string name = assetPathProvider.GetAssetPath(animationSet);

            Debug.Assert(name == null || !name.StartsWith("\\"));

            if(name != null)
            {
                // Write a reference
                bw.Write(true);
                bw.Write(name);
            }
            else
            {
                // Embed the animation
                bw.Write(false);
                animationSet.Serialize(animationSerializeContext);
            }
        }

    }
}
