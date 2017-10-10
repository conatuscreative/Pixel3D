using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Pixel3D.Animations.Serialization
{
    public class SimpleTextureLoadHelper : ITextureLoadHelper
    {
        public SimpleTextureLoadHelper(byte[] sharedLoadBuffer, GraphicsDevice graphicsDevice)
        {
            this.sharedLoadBuffer = sharedLoadBuffer;
            this.graphicsDevice = graphicsDevice;
        }

        public SimpleTextureLoadHelper(GraphicsDevice graphicsDevice)
        {
            this.sharedLoadBuffer = new byte[ImageBundleManager.LoadBufferSize];
            this.graphicsDevice = graphicsDevice;
        }

        byte[] sharedLoadBuffer;
        GraphicsDevice graphicsDevice;


        public byte[] GetSharedLoadBuffer()
        {
            return sharedLoadBuffer;
        }

        public Texture2D LoadTexture(int width, int height, byte[] buffer)
        {
            var texture = new Texture2D(graphicsDevice, width, height);
            texture.SetData(0, new Rectangle(0, 0, width, height), buffer, 0, width * height * 4);
            return texture;
        }

    }
}


