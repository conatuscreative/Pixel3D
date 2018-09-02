using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Pixel3D.Audio
{
	internal class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private ReferenceEqualityComparer() { } // <- no external instancing

	    public static ReferenceEqualityComparer<T> Instance { get; } = new ReferenceEqualityComparer<T>();


	    public bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
