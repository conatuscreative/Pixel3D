using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.AssetManagement;

namespace Pixel3D.Engine.Levels
{
    public class LevelDeserializeContext
    {
        public LevelDeserializeContext(BinaryReader br, ImageBundle imageBundle, IAssetProvider assetProvider, GraphicsDevice device)
        {
            this.br = br;
            this.assetProvider = assetProvider;
            
            Version = br.ReadInt32();

            if(Version > LevelSerializeContext.formatVersion)
                throw new Exception("Tried to load Level with a version that is too new");
            if(Version < 13)
                throw new Exception("Level version too old!");

            animationDeserializeContext = new AnimationDeserializeContext(br, imageBundle, device); // <- Reads out animation set version
        }

        public readonly BinaryReader br;

        public int Version { get; private set; }


        /// <summary>NOTE: parallel updates between serialize and deserialize</summary>
        public int nextRegionIndex = 0;

	    public readonly AnimationDeserializeContext animationDeserializeContext;
	    public readonly IAssetProvider assetProvider;

        /// <summary>Used to speed up asset packing. Use with extreme care (number of bytes read must match EXACTLY). Produces assets unusable for gameplay.</summary>
        public bool FastReadHack
        {
            get
            {
                return animationDeserializeContext.fastReadHack;
            }
            set
            {
                animationDeserializeContext.fastReadHack = value;
            }
        }

        /// <summary>Used for externally packing masks in the asset packer.</summary>
        public ICustomMaskDataReader CustomMaskDataReader
        {
            get
            {
                return animationDeserializeContext.customMaskDataReader;
            }
            set
            {
                animationDeserializeContext.customMaskDataReader = value;
            }
        }
    }
}
