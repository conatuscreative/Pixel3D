using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pixel3D.Animations;

namespace Pixel3D
{
    public interface IDrawObject
    {
        void Draw(DrawContext drawContext, int tag, IDrawSmoothProvider sp);
    }
}
