// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Pixel3D.ActorManagement
{
    /// <summary>
    /// A dictionary that allows multiple pairs with the same key.
    /// </summary>
    public class MultiDictionary<TKey, TValue> : ILookup<TKey, TValue>
    {
        readonly OrderedDictionary<TKey, List<TValue>> dict;

        public MultiDictionary()
        {
            dict = new OrderedDictionary<TKey, List<TValue>>();
        }

        public MultiDictionary(IEqualityComparer<TKey> comparer)
        {
            dict = new OrderedDictionary<TKey, List<TValue>>(comparer);
        }

        public bool TryGetValue(TKey key, out List<TValue> value)
        {
            return dict.TryGetValue(key, out value);
        }

        public void Add(TKey key, TValue value)
        {
            List<TValue> valueList;
            if (!dict.TryGetValue(key, out valueList))
            {
                valueList = new List<TValue>();
                dict.Add(key, valueList);
            }
            valueList.Add(value);
        }

        public OrderedDictionary<TKey, List<TValue>>.KeyCollection Keys
        {
            get { return dict.Keys; }
        }

        public void AddAll(TKey key, IEnumerable<TValue> values)
        {
            List<TValue> valueList;
            if(!dict.TryGetValue(key, out valueList))
            {
                valueList = new List<TValue>();
                dict.Add(key, valueList);
            }
            foreach(var value in values)
                valueList.Add(value);
        }

        public bool RemoveAll(TKey key)
        {
            return dict.Remove(key);
        }

        public bool Remove(TKey key, TValue value)
        {
            List<TValue> valueList;
            if (dict.TryGetValue(key, out valueList))
            {
                if (valueList.Remove(value))
                {
                    if (valueList.Count == 0)
                        dict.Remove(key);
                    return true;
                }
            }
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public bool ContainsValue(TKey key, TValue value, IEqualityComparer<TValue> comparer = null)
        {
            List<TValue> list;
            return dict.TryGetValue(key, out list) &&
                   list.Contains(value, comparer ?? EqualityComparer<TValue>.Default);
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                return dict.SelectMany(kvp => kvp.Value);
            }
        }

        public void Clear()
        {
            dict.Clear();
        }

#if NET_4_5
		public IReadOnlyList<TValue> this[TKey key] {
#else
        public List<TValue> this[TKey key]
        {
#endif
            get
            {
                List<TValue> list;
                if (dict.TryGetValue(key, out list))
                    return list;
                else
                    return EmptyList;
            }
        }

        private static readonly List<TValue> EmptyList = new List<TValue>();

        public int Count
        {
            get { return dict.Count; }
        }

        IEnumerable<TValue> ILookup<TKey, TValue>.this[TKey key]
        {
            get { return this[key]; }
        }

        bool ILookup<TKey, TValue>.Contains(TKey key)
        {
            return dict.ContainsKey(key);
        }

        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            foreach (var pair in dict)
                yield return new Grouping(pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        sealed class Grouping : IGrouping<TKey, TValue>
        {
            readonly TKey key;
            readonly List<TValue> values;

            public Grouping(TKey key, List<TValue> values)
            {
                this.key = key;
                this.values = values;
            }

            public TKey Key
            {
                get { return key; }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return values.GetEnumerator();
            }
        }

    }
}