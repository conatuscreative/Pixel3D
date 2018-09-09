using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Diagnostics;

namespace Pixel3D.Animations.Serialization
{
    [DebuggerDisplay("ImageBundle: \"{" + nameof(Name) + "}\"")]
    public class ImageBundle : IDisposable
    {
        public string Name => manager != null ? manager.GetBundleName(bundleIndex) : "(no name)";

	    public ImageBundle() { }

        // This is hacky...
        /// <summary>Create a dummy reader for cases where we don't want to load the texture file (for tooling)</summary>
        /// <param name="imageCount">Pass in a huge value, it shouldn't matter...</param>
        public static ImageBundle CreateDummyBundle(int imageCount)
        {
            ImageBundle result = new ImageBundle();

            result.manager = null;
            result.liveIndex = 0; // <- basically alive
            result.bundleIndex = -1;

            result.textures = new Texture2D[1]; // <- just index zero
            result.imageTextureIndicies = null;
            result.imageSourceRectangles = new Rectangle[imageCount]; // <- hopefully don't care about the contents...

            return result;
        }

        public ImageBundle(Sprite sprite)
        {
            manager = null;
            liveIndex = 0; // <- basically alive
            bundleIndex = -1;

            textures = new Texture2D[] { sprite.texture };
            imageTextureIndicies = null;
            imageSourceRectangles = new Rectangle[] { sprite.sourceRectangle };
        }

        public ImageBundle(ImageBundleManager manager, int managedIndex)
        {
            this.manager = manager;
            this.bundleIndex = managedIndex;
            this.liveIndex = -1; // <- not loaded
        }
		
        public void ReadAllImagesOLD(BinaryReader br, GraphicsDevice graphicsDevice, List<TextureData> texturesData = null)
        {
            int version = br.ReadInt32();
            if (version > ImageWriter.formatVersion)
                throw new Exception("Texture file too new");

            // Read in textures:
            int textureCount = br.ReadInt32();
            if (textures == null)
                textures = new Texture2D[textureCount];
            else
                Debug.Assert(textures.Length == textureCount);

            for (int i = 0; i < textures.Length; i++)
            {
                int width = br.ReadInt32();
                int height = br.ReadInt32();
                int bytes = width * height * 4;

                // TODO: Refactor: Maybe stick this on an interface (request temp memory, submit read texture)
                if (manager != null) // <- if we have a bundle manager, use shared resources:
                {
                    Debug.Assert(manager.sharedLoadBuffer.Length >= bytes);
                    br.Read(manager.sharedLoadBuffer, 0, bytes);
                    textures[i] = manager.GetTexture(width, height);
                    textures[i].SetData(0, new Rectangle(0, 0, width, height), manager.sharedLoadBuffer, 0, bytes);
                }
                else
                {
                    byte[] data = br.ReadBytes(bytes);
                    if (graphicsDevice != null) // <- Allows us to operate in headless mode for asset packing
                    {
                        textures[i] = new Texture2D(graphicsDevice, width, height);
                        textures[i].SetData(data);
                    }
                    if (texturesData != null)
                        texturesData.Add(new TextureData(width, height, data));
                }
            }

            // Read in images (locations in textures):
            int imageCount = br.ReadInt32();
            imageTextureIndicies = new byte[imageCount];
            imageSourceRectangles = new Rectangle[imageCount];

            // TODO: Separate these loads, and make the texture index optional if we only have a single texture (common case)
            for (int i = 0; i < imageCount; i++)
            {
                imageTextureIndicies[i] = (byte)br.ReadInt32(); // <- TODO: change to a byte
                imageSourceRectangles[i] = br.ReadRectangle();
            }
        }
		
        /// <summary>Read all images out of a buffer</summary>
        /// <returns>Returns the position after reading, or -1 if we did a "fast" read</returns>
        public unsafe int ReadAllImages(byte[] data, int offset, ITextureLoadHelper loadHelper)
        {
            // IMPORTANT: Uses loadHelper instead of manager so that we can load textures independently of a proper manager

            bool firstTime = (textures == null);

            if (firstTime)
            {
                Debug.Assert(imageSourceRectangles == null);

                // Read in the counts and allocate before we do `fixed(data)` to avoid allocations while fixed (better for GC)
                textures = new Texture2D[data[offset + 0]];

                int imageCount = data[offset + 1];
                imageCount |= (data[offset + 2] << 8); // <- ReadShort

                if (textures.Length > 1)
                    imageTextureIndicies = new byte[imageCount];
                imageSourceRectangles = new Rectangle[imageCount];
            }
            else
            {
                Debug.Assert(textures.Length == 1 || imageTextureIndicies != null);
                Debug.Assert(imageSourceRectangles != null);
            }

            offset += 3;

            byte[] sharedLoadBuffer = loadHelper.GetSharedLoadBuffer();
            fixed (byte* dataPointer = data, loadBuffer = sharedLoadBuffer)
            {
                byte* startReadAt = dataPointer + offset;
                byte* d = startReadAt;
                uint* pixels = (uint*)loadBuffer;

                for (int t = 0; t < textures.Length; t++)
                {
                    int width = ReadShort(d); d += 2;
                    int height = ReadShort(d); d += 2;
                    Debug.Assert(sharedLoadBuffer.Length >= width * height * 4); // <- ensure we're not about to buffer-overrun!

                    d = RCRURLEReader.Decode(d, pixels, width * height);

                    textures[t] = loadHelper.LoadTexture(width, height, sharedLoadBuffer);
                }

                // Finish up by reading image locations, if this is our first time touching the bundle:
                if (firstTime)
                {
                    if (textures.Length > 1) // <- we have texture indicies to load:
                    {
                        System.Runtime.InteropServices.Marshal.Copy((IntPtr)d, imageTextureIndicies, 0, imageTextureIndicies.Length);
                        d += imageTextureIndicies.Length;
                    }

                    // This might be nicer as a memory copy, too...
                    for (int i = 0; i < imageSourceRectangles.Length; i++)
                    {
                        imageSourceRectangles[i].X = ReadShort(d); d += 2;
                        imageSourceRectangles[i].Y = ReadShort(d); d += 2;
                        imageSourceRectangles[i].Width = ReadShort(d); d += 2;
                        imageSourceRectangles[i].Height = ReadShort(d); d += 2;
                    }

                    var bytesRead = d - startReadAt;
                    offset += (int)bytesRead;
                    return offset;
                }
                else
                {
                    return -1; // <- Because we skipped a segment, we don't know the full read length
                }
            }
        }

        private static unsafe int ReadShort(byte* buffer)
        {
            return (*buffer) | (*(buffer + 1) << 8);
        }
		
        internal bool IsCachable { get { return manager != null; } }

        ImageBundleManager manager;
        internal int bundleIndex;
        internal int liveIndex;

        public Texture2D[] textures;
        public byte[] imageTextureIndicies;  // <- NOTE: Only public so the asset packer can see it
        public Rectangle[] imageSourceRectangles;  // <- NOTE: Only public so the asset packer can see it

	    public Sprite GetSprite(int index, Point origin)
        {
            if (index == -1) // <- blank sprite
                return new Sprite();

            if (liveIndex == -1)
            {
                Debug.Assert(manager != null);
                Debug.WriteLine("Loading image bundle: " + manager.GetBundleName(bundleIndex));
                manager.MakeBundleAlive(this);
            }
            else
            {
	            manager?.LiveListTouch(liveIndex);
            }

	        Sprite result;
            result.texture = textures[imageTextureIndicies != null ? imageTextureIndicies[index] : 0];
            result.sourceRectangle = imageSourceRectangles[index];
            result.origin = origin;
            return result;
        }
		
        #region IDisposable Members

        public void Dispose()
        {
            if (manager != null)
            {
                // If there is a manager, it owns us and the textures, not you.
                Debug.Assert(false);
            }
            else
            {
                if (textures != null)
                {
                    for (int i = 0; i < textures.Length; i++)
                    {
                        if (textures[i] != null)
                        {
                            textures[i].Dispose();
                            textures[i] = null;
                        }
                    }
                }
            }
        }

        #endregion
    }
}