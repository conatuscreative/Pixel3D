using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

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
