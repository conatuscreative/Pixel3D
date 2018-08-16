using System.Collections.Generic;
using System.Linq;

namespace Pixel3D.Engine.Collections
{
    public static class MutliDictionaryExtensions
    {
        // This exists almost entirely to stop Edit and Continue complaining about lambdas...
        public static IEnumerable<TValue> SelectMany<TKey, TValue>(this MultiDictionary<TKey, TValue> multiDictionary)
        {
            return multiDictionary.SelectMany(g => g);
        }
    }
}
