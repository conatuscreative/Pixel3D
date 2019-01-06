// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pixel3D.Animations.Serialization
{
    struct ImageKey : IEquatable<ImageKey>
    {
        public ImageKey(Texture2D texture, Rectangle sourceRectangle)
        {
            this.texture = texture;
            this.sourceRectangle = sourceRectangle;
        }

        Texture2D texture;
        Rectangle sourceRectangle;


        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(texture) ^ sourceRectangle.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj is ImageKey)
                return Equals((ImageKey)obj);
            else
                return false;
        }

        public bool Equals(ImageKey other) // from IEquatable<ImageKey>
        {
            return ReferenceEquals(texture, other.texture) && sourceRectangle == other.sourceRectangle;
        }
    }
}
