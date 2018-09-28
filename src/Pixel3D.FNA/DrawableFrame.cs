// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Pixel3D.Animations;

namespace Pixel3D
{
    // NOTE: This is basically for debug views
    public struct DrawableFrame
    {
        public DrawableFrame(AnimationFrame animationFrame, Position position, bool flipX)
        {
            this.animationFrame = animationFrame;
            this.position = position;
            this.flipX = flipX;
        }

        public AnimationFrame animationFrame;
        public Position position;
        public bool flipX;
    }
}
