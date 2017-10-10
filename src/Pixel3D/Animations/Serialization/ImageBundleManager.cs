using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using System.Threading;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Animations.Serialization
{
    public class ImageBundleManager : IDisposable, ITextureLoadHelper
    {
        private GraphicsDevice graphicsDevice;


        int[] offsets; // <- has an extra offset for EOF
        string[] names;
        Dictionary<string, int> indexLookupByName;
        int maxTextureSize = 0;

        byte[] texturePackageData;

        ImageBundle[] bundles; // <- NOTE: Lazy-filled in GetBundle


        public ImageBundleManager(string texturesPackagePath, byte[] magicNumber, GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            using(FileStream texturesFile = File.OpenRead(texturesPackagePath))
            {
                // Read Magic:
                for(int i = 0; i < magicNumber.Length; i++)
                    if(texturesFile.ReadByte() != magicNumber[i])
                        throw new Exception("Textures package is corrupt");

                using(var br = new BinaryReader(new GZipStream(texturesFile, CompressionMode.Decompress, true)))
                {
                    //
                    // Read Header:

                    int count = br.ReadInt32();
                    offsets = new int[count + 1];
                    names = new string[count];
                    indexLookupByName = new Dictionary<string, int>(count);
                    bundles = new ImageBundle[count];

                    offsets[0] = 0;
                    for(int i = 0; i < count; i++)
                    {
                        offsets[i+1] = br.ReadInt32(); // <- stored as trailing offsets (into the texture data chunk)
                        maxTextureSize = Math.Max(maxTextureSize, offsets[i+1] - offsets[i]);
                    }

                    for(int i = 0; i < count; i++)
                    {
                        string name = br.ReadString();
                        names[i] = name;
                        indexLookupByName.Add(name, i);
                    }


                    //
                    // Read the entire texture package into memory:
                    // (Turns out we can decode into textures in sub-frame times, so if we just get rid of the disk delay, we can avoid a lot of hassle)
                    int finalOffset = offsets[offsets.Length - 1]; // <- size of the entire package body
                    Debug.WriteLine("Total size of in-memory texture package = {0:0.00}MB", (finalOffset / (1024.0 * 1024.0)));
                    texturePackageData = br.ReadBytes(finalOffset);
                }
            }


            //
            // A few other things to initialize:

            for(int i = 0; i < idleTextures.Length; i++)
                idleTextures[i] = new Stack<Texture2D>();

            sharedLoadBuffer = new byte[LoadBufferSize];
        }



        #region ITextureLoadHelper

        public const int LoadBufferSize = 2048 * 2048 * 4; // <- Large enough for our largest texture
        internal byte[] sharedLoadBuffer;

        public byte[] GetSharedLoadBuffer()
        {
            return sharedLoadBuffer;
        }

        public Texture2D LoadTexture(int width, int height, byte[] buffer)
        {
            var texture = GetTexture(width, height);
            texture.SetData(0, new Rectangle(0, 0, width, height), buffer, 0, width * height * 4);
            return texture;
        }

        #endregion




        #region Texture Bucketing and Reuse

        // We don't want too many buckets, because we want textures to be able to share space
        // But we need enough levels of buckets that we're not wasting huge amounts of texture memory for tiny textures

        public const int TextureBucketCount = 3;

        /// <summary>Determine which bucket a texture belongs to</summary>
        public static int ClassifyTexture(int width, int height)
        {
            // NOTE: We prioritise narrow, tall textures, because that is what the sprite packer produces!
            //       The values here have been determined by eyeballing the various sprite sheets.

            if(width <= 128 && height <= 128) // <- 128x128 (64kb) -- Various tiny decorations, most weapons, etc
                return 0;
            else if(width <= 256) // <- 256x2048 (2mb) -- Vast majority of characters (tall), also a few slightly wider decorations
                return 1;
            else // <- 2048x2048 (16mb) -- Everything else - mostly levels, menus, cutscenes, and some large characters
                return 2;
        }

        private static Texture2D AllocateTexture(GraphicsDevice device, int bucket)
        {
            switch(bucket)
            {
                case 0:
                    return new Texture2D(device, 128, 128);
                case 1:
                    return new Texture2D(device, 256, 2048);
                case 2:
                    return new Texture2D(device, 2048, 2048);
                default:
                    Debug.Assert(false); // <- you have a bug
                    throw new InvalidOperationException();
            }
        }


        // Textures that have been reclaimed (if we reclaim a bundle with multiple textures, here is where we put the spares):
        readonly Stack<Texture2D>[] idleTextures = new Stack<Texture2D>[TextureBucketCount];


        readonly int[] textureCounters = new int[TextureBucketCount];

        public string GetStatistics()
        {
            long textureBytes = 128*128*4*(long)textureCounters[0] + 256*2048*4*(long)textureCounters[1] + 2048*2048*4*(long)textureCounters[2];

            return string.Format("Textures in buckets:\n 128x128: {0}\n 256x2048: {1}\n 2048x2048: {2}\nTotal texture memory: {3} bytes ({4}MB)",
                    textureCounters[0], textureCounters[1], textureCounters[2],
                    textureBytes, textureBytes / (1024*1024));
        }



        // Live list:
        struct BundleState
        {
            public byte timeOut; // <- is 255 frames sufficient granularity for expiry? (probably is.)
            public byte bucketFlags;
        }

        // Because we do a "find a bundle to evict" search rarely, we simply do a linear search for eviction.
        // When we evict someone, we have to swap its live index for someone we're not evicting (remove unordered).
        const int defaultLiveListCount = 32;
        int liveCount;
        BundleState[] liveListState = new BundleState[defaultLiveListCount];
        ImageBundle[] liveListBundles = new ImageBundle[defaultLiveListCount];


        private void LiveListEnsureCapacity()
        {
            Debug.Assert(liveCount <= liveListState.Length);
            Debug.Assert(liveListState.Length == liveListBundles.Length);

            if(liveCount == liveListState.Length)
            {
                Array.Resize(ref liveListState, liveListState.Length * 2);
                Array.Resize(ref liveListBundles, liveListBundles.Length * 2);
            }
        }


        public void PresentedFrame()
        {
            for(int i = 0; i < liveCount; i++)
                if(liveListState[i].timeOut < byte.MaxValue)
                    liveListState[i].timeOut++;
        }


        internal void RegisterAlive(ImageBundle bundle)
        {
            Debug.Assert(bundle.liveIndex == -1);

            if(bundle.textures.Length == 0)
                return; // <- skip bundles without textures, as they can never be reclaimed, and would just waste space in the live list

            byte bucketFlags = 0;
            for(int i = 0; i < bundle.textures.Length; i++)
            {
                int classification = ClassifyTexture(bundle.textures[i].Width, bundle.textures[i].Height);
                bucketFlags |= (byte)(1u << classification);
            }

            LiveListEnsureCapacity();
            liveListState[liveCount] = new BundleState { timeOut = 0, bucketFlags = bucketFlags };
            liveListBundles[liveCount] = bundle;
            bundle.liveIndex = liveCount;

            liveCount++;
        }

        internal void LiveListTouch(int liveIndex)
        {
            liveListState[liveIndex].timeOut = 0;
        }

        private void LiveListEvict(int liveIndex)
        {
            Debug.Assert(liveIndex >= 0 && liveIndex < liveCount);

            ImageBundle bundle = liveListBundles[liveIndex];
            Debug.Assert(bundle.liveIndex == liveIndex);

            Debug.WriteLine("Evicting image bundle: " + GetBundleName(bundle.bundleIndex));

            // Recover the textures from the bundle being evicted
            for(int i = 0; i < bundle.textures.Length; i++)
            {
                int classification = ClassifyTexture(bundle.textures[i].Width, bundle.textures[i].Height);
                idleTextures[classification].Push(bundle.textures[i]);
                bundle.textures[i] = null;
            }

            // Move another live bundle into its slot (unordered removal):
            liveCount--;
            liveListBundles[liveIndex] = liveListBundles[liveCount];
            liveListBundles[liveIndex].liveIndex = liveIndex;
            liveListBundles[liveCount] = null; // <- clear so the GC doesn't see it
            liveListState[liveIndex] = liveListState[liveCount];

            // Set the bundle as dead:
            // NOTE: Must do this AFTER the above removal, as we may swap with ourself
            bundle.liveIndex = -1;
        }


        /// <summary>Get a texture with the given minimum dimentions</summary>
        internal Texture2D GetTexture(int minimumWidth, int minimumHeight)
        {
            int bucket = ClassifyTexture(minimumWidth, minimumHeight);

            // First, see if we can just grab one directly:
            var idleBucket = idleTextures[bucket];
            if(idleBucket.Count > 0)
                return idleBucket.Pop();

            // Otherwise evict someone:
            uint bucketBit = (1u << bucket);
            const int untouchedFramesBeforeEviction = 4; // <- Want to be pretty sure we won't stall the GPU (and must be at least 1, so we are not activly using it!)
            int oldestTime = untouchedFramesBeforeEviction - 1; // (want "greater than or equals")
            int bestIndex = -1;
            

            for(int i = 0; i < liveCount; i++)
            {
                if((liveListState[i].bucketFlags & bucketBit) != 0 && liveListState[i].timeOut > oldestTime)
                {
                    oldestTime = liveListState[i].timeOut;
                    bestIndex = i;
                }
            }

            // Found one to evict:
            if(bestIndex != -1)
            {
                Debug.Assert(oldestTime >= untouchedFramesBeforeEviction);
                LiveListEvict(bestIndex);
                Debug.Assert(idleBucket.Count > 0); // <- actually resulted in a usable texture
                return idleBucket.Pop();
            }


            // Didn't find anyone:
            textureCounters[bucket]++;
#if DEBUG
            Debug.Write("Allocating texture. Current buckets: ");
            for(int i = 0; i < textureCounters.Length; i++)
            {
                if(i != 0)
                    Debug.Write(" | ");
                Debug.Write(textureCounters[i]);
            }
            Debug.WriteLine("");
#endif

            return AllocateTexture(graphicsDevice, bucket);
        }


        public void Dispose()
        {
            // Too lazy to implement inheritable dispose pattern
            while(liveCount > 0)
                LiveListEvict(liveCount - 1);

            for(int i = 0; i < idleTextures.Length; i++)
                while(idleTextures[i].Count > 0)
                    idleTextures[i].Pop().Dispose();
        }

        #endregion






        public ImageBundle GetBundle(string name)
        {
            int index = indexLookupByName[name];

            // NOTE: Interlocked is not really necessary now, but maybe someone will do something weird in the future...
            if(bundles[index] == null)
                Interlocked.CompareExchange(ref bundles[index], new ImageBundle(this, index), null);

            return bundles[index];
        }

        internal string GetBundleName(int index)
        {
            return names[index];
        }



        /// <summary>Sanity check that nothing is loading textures during multi-threaded startup (we aren't that thread safe!)</summary>
        bool canLoadTextures = false;
        public void SetCanLoadTextures() { canLoadTextures = true; }


        internal void MakeBundleAlive(ImageBundle bundle)
        {
            Debug.Assert(canLoadTextures);
            Debug.Assert(bundle.liveIndex == -1);
            
            // Super-speedy:
            int finalOffset = bundle.ReadAllImages(texturePackageData, offsets[bundle.bundleIndex], this);
            Debug.Assert(finalOffset == -1 || finalOffset == offsets[bundle.bundleIndex+1]); // <- Read the expected number of bytes!

            // https://www.youtube.com/watch?v=xos2MnVxe-c
            RegisterAlive(bundle);
        }



        #region Network Serializer Block

        [CustomFieldSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, ImageBundleManager value) { throw new InvalidOperationException(); }
        [CustomFieldSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, ref ImageBundleManager value) { throw new InvalidOperationException(); }

        #endregion




    }
}
