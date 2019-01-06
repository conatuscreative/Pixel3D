// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations.Serialization;

namespace Pixel3D.Animations
{
    public class AnimationDeserializeContext
    {
        public AnimationDeserializeContext(BinaryReader br, ImageBundle imageBundle, GraphicsDevice graphicsDevice)
        {
            this.br = br;
            this.imageBundle = imageBundle;

            Version = br.ReadInt32();
            if(Version > AnimationSerializeContext.formatVersion)
                throw new Exception("Tried to load AnimationSet with a version that is too new");
            if(Version < 34)
                throw new Exception("Animation version too old!");

            if(br.ReadBoolean() != (imageBundle != null))
                throw new Exception("External image state mismatch");

            GraphicsDevice = graphicsDevice;
        }

        public readonly BinaryReader br;
        public readonly ImageBundle imageBundle;

        public int Version { get; private set; }
        /// <summary>NOTE: May be null if we're loading headless</summary>
        public GraphicsDevice GraphicsDevice { get; private set; }


        /// <summary>Used to speed up asset packing. Use with extreme care (number of bytes read must match EXACTLY). Produces assets unusable for gameplay.</summary>
        public bool fastReadHack;

        /// <summary>Used for externally packing masks in the asset packer.</summary>
        public ICustomMaskDataReader customMaskDataReader;
		
        #region Shared Item Deserializer

        internal T[] DeserializeSharedItems<T>(Func<AnimationDeserializeContext, T> deserializeDelegate)
        {
            int count = br.ReadInt32();
            T[] sharedItems = new T[count];

            for(int i = 0; i < sharedItems.Length; i++)
            {
                sharedItems[i] = deserializeDelegate(this);
            }

            return sharedItems;
        }

        #endregion
    }

}
