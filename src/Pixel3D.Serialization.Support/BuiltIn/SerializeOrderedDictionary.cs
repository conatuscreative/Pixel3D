// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.BuiltIn
{
    static class SerializeOrderedDictionary
    {
        // Because we want to know what comparer we get by default:
        static class Sample<TKey, TValue>
        {
            internal static OrderedDictionary<TKey, TValue> sample = Initialize<TKey, TValue>();
        }


        [CustomSerializer]
        public static void Serialize<TKey, TValue>(SerializeContext context, BinaryWriter bw, OrderedDictionary<TKey, TValue> dictionary)
        {
            // TODO: Add support for custom comparers
            if (!dictionary.Comparer.Equals(Sample<TKey, TValue>.sample.Comparer))
                throw new NotImplementedException("Serializing a custom comparer is not implemented");

            context.VisitObject(dictionary);

            bw.WriteSmallInt32(dictionary.Count);

            foreach (var item in dictionary)
            {
                TKey key = item.Key;
                TValue value = item.Value;
                Field.Serialize(context, bw, ref key);
                Field.Serialize(context, bw, ref value);
            }

            context.LeaveObject();
        }

        [CustomSerializer]
        public static void Deserialize<TKey, TValue>(DeserializeContext context, BinaryReader br, OrderedDictionary<TKey, TValue> dictionary)
        {
            context.VisitObject(dictionary);

            int count = br.ReadSmallInt32();

            dictionary.Clear();

            for (int i = 0; i < count; i++)
            {
                TKey key = default(TKey);
                TValue value = default(TValue);
                Field.Deserialize(context, br, ref key);
                Field.Deserialize(context, br, ref value);
                dictionary.Add(key, value);
            }
        }

        [CustomInitializer]
        public static OrderedDictionary<TKey, TValue> Initialize<TKey, TValue>()
        {
            return new OrderedDictionary<TKey, TValue>();
        }
    }
}