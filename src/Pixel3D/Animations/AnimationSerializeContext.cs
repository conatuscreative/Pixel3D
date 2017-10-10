using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Common;
using System.IO;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Pixel3D.Animations.Serialization;

namespace Pixel3D.Animations
{
    public class AnimationSerializeContext
    {
        public AnimationSerializeContext(BinaryWriter bw, ImageWriter imageWriter) : this(bw, imageWriter, formatVersion) { } // Default to writing current version

        public AnimationSerializeContext(BinaryWriter bw, ImageWriter imageWriter, int version)
        {
            this.Version = version;
            this.bw = bw;
            this.imageWriter = imageWriter;

            if(Version > AnimationSerializeContext.formatVersion)
                throw new Exception("Tried to save AnimationSet with a version that is too new");
            if(Version < 34)
                throw new Exception("Cannot serialize old version");

            bw.Write(Version);
            bw.Write(imageWriter != null);
        }

        public readonly BinaryWriter bw;
        public readonly ImageWriter imageWriter;


        // NOTE: ImageWriter doesn't have a "monitor" mode currently (so don't use it for change monitering)
        /// <summary>True for save change monitoring (try to avoid mutation).</summary>
        public bool monitor;

        #region Version

        /// <summary>Increment this number when anything we serialize changes</summary>
        public const int formatVersion = 38;

        public int Version { get; private set; }

        #endregion



        #region Shared Item Serializer

        internal Dictionary<T, int> SerializeSharedItems<T>(IEnumerable<T> itemCollection, Action<T, AnimationSerializeContext> serializeDelegate) where T : class
        {
            // Create a lookup that can be used to associate reference types with indicies into the table of shared objects
            Dictionary<T, int> itemLookup = new Dictionary<T, int>(ReferenceEqualityComparer<T>.Instance);

            bw.Write(itemCollection.Count());

            int i = 0;
            foreach(var item in itemCollection)
            {
                serializeDelegate(item, this);
                itemLookup.Add(item, i);
                i++;
            }

            return itemLookup;
        }

        #endregion



    }
}
