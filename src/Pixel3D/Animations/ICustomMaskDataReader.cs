using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pixel3D.Animations
{
    public interface ICustomMaskDataReader
    {
        uint[] Read(int length);
    }
}
