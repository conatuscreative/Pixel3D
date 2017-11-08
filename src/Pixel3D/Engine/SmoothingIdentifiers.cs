using System;

namespace Pixel3D.Engine
{
    public struct SmoothingIdentifiers
    {
        // Don't need equality check methods because SmoothingManager does the right thing. (And no one will break it, right?)
        public Type type;
        public int id;
    }
}