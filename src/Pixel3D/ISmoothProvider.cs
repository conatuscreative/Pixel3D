using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This network-related nonsense is needed in this layer because we have to pass it through draw sorting
// And putting it in a dumb namspace is so I don't have to add `using` to ALL THE THINGS.

namespace Pixel3D.Animations
{
    public interface IDrawSmoothProvider
    {
        Position GetOffset(object owner);
    }
}

namespace Pixel3D.Sorting
{
    public interface ISortSmoothProvider
    {
        Position GetOffset(object owner);
    }
}

