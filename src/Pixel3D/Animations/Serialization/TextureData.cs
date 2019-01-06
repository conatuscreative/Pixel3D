// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Animations.Serialization
{
    public struct TextureData
    {
        public int width, height;
        public byte[] data;

        public TextureData(int width, int height, byte[] data)
        {
            this.width = width;
            this.height = height;
            this.data = data;
        }
    }
}
