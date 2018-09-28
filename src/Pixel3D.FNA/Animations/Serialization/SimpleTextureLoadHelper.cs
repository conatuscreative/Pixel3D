// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
            sharedLoadBuffer = new byte[ImageBundleManager.LoadBufferSize];
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