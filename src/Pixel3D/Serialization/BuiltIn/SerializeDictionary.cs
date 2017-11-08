using System;
using System.Collections.Generic;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.BuiltIn
{
    static class SerializeDictionary
    {
        [CustomSerializer]
        public static void Serialize<TKey, TValue>(SerializeContext context, BinaryWriter bw, Dictionary<TKey, TValue> dictionary)
        {
            throw new InvalidOperationException("This collection is not network safe; use OrderedDictionary instead");
        }

        [CustomSerializer]
        public static void Deserialize<TKey, TValue>(DeserializeContext context, BinaryReader br, Dictionary<TKey, TValue> dictionary)
        {
            throw new InvalidOperationException("This collection is not network safe; use OrderedDictionary instead");
        }

        [CustomInitializer]
        public static Dictionary<TKey, TValue> Initialize<TKey, TValue>()
        {
            throw new InvalidOperationException("This collection is not network safe; use OrderedDictionary instead");
        }


        [CustomSerializer]
        public static void Serialize<T>(SerializeContext context, BinaryWriter bw, HashSet<T> hashSet)
        {
            throw new InvalidOperationException("This collection is not network safe");
        }

        [CustomSerializer]
        public static void Deserialize<T>(DeserializeContext context, BinaryReader br, HashSet<T> hashSet)
        {
            throw new InvalidOperationException("This collection is not network safe");
        }

        [CustomInitializer]
        public static HashSet<T> Initialize<T>()
        {
            throw new InvalidOperationException("This collection is not network safe");
        }
    }
}