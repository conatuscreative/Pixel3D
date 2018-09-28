// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Diagnostics;

namespace Pixel3D.Animations
{
    [DebuggerDisplay("Shadow from height = {startHeight}")]
    public struct ShadowLayer
    {
        public ShadowLayer(int startHeight, SpriteRef shadowSpriteRef)
        {
            this.startHeight = startHeight;
            this.shadowSpriteRef = shadowSpriteRef;
        }

        /// <summary>The minimum (inclusive) height where this shadow sprite gets used</summary>
        public int startHeight;
        public SpriteRef shadowSpriteRef;
    }
}
