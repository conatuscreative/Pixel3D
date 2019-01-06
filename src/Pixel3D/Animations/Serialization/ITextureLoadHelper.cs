// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Microsoft.Xna.Framework.Graphics;

namespace Pixel3D.Animations.Serialization
{
    public interface ITextureLoadHelper
    {
        /// <summary>Returns a shared load buffer that is big enough to load any texture we might load</summary>
        byte[] GetSharedLoadBuffer();

        Texture2D LoadTexture(int width, int height, byte[] buffer);
    }
}
