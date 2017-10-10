using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Common
{
    public class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private ReferenceEqualityComparer() { } // <- no external instancing

        static ReferenceEqualityComparer<T> _instance = new ReferenceEqualityComparer<T>();

        public static ReferenceEqualityComparer<T> Instance
        {
            get
            {
                return _instance;
            }
        }
     

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
