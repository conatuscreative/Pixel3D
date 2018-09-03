using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Pixel3D.Animations;
using System.IO;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D
{
    // This tag-matching is inspired by Valve's "Dynamic Dialog" system.

    public class TagLookup<T> : IEnumerable<KeyValuePair<TagSet, T>>
    {
        const int defaultCapacity = 8;

        int count;

        /// <summary>Performance optimisation to skip over rules with more than one tag</summary>
        int countOfMultiTagRules;

        // Lookup of data stable-sorted by tag count
        TagSet[] rules;
        T[] values;
        
        public int Count { get { return count; } }
        public int Capacity { get { return rules.Length; } }

        public void Clear()
        {
            count = 0;
            countOfMultiTagRules = 0;
        }


        public TagLookup() : this(defaultCapacity) { }

        public TagLookup(int capacity)
        {
            count = 0;
            countOfMultiTagRules = 0;
            rules = new TagSet[capacity];
            values = new T[capacity];
        }

		#region Value Collection

        public struct ValueCollection : IEnumerable<T>
        {
            public ValueCollection(TagLookup<T> owner)
            {
                this.owner = owner;
            }

            TagLookup<T> owner;

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator(owner);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count { get { return owner.count; } }

            public T this[int i] { get { return owner.values[i]; } }

            public void EditorReplaceValue(int i, T value)
            {
                owner.values[i] = value;
            }

            public struct Enumerator : IEnumerator<T>
            {
                int i;
                T current;
                TagLookup<T> owner;

                public Enumerator(TagLookup<T> owner)
                {
                    i = 0;
                    current = default(T);
                    this.owner = owner;
                }

                public void Reset()
                {
                    i = 0;
                    current = default(T);
                }

                public void Dispose() { }
                public T Current { get { return current; } }
                object IEnumerator.Current { get { return current; } }

                public bool MoveNext()
                {
                    if(i < owner.Count)
                    {
                        current = owner.values[i];
                        i++;
                        return true;
                    }
                    return false;
                }
            }
        }

        public ValueCollection Values { get { return new ValueCollection(this); } }

        #endregion

		#region Rules Collection

        public struct RuleCollection : IEnumerable<TagSet>
        {
            public RuleCollection(TagLookup<T> owner)
            {
                this.owner = owner;
            }

            TagLookup<T> owner;

            public IEnumerator<TagSet> GetEnumerator()
            {
                return new Enumerator(owner);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }


            public int Count { get { return owner.count; } }

            public TagSet this[int i] { get { return owner.rules[i]; } }

            public int IndexOf(TagSet tagSet)
            {
                for(int i = 0; i < owner.count; i++)
                    if(owner.rules[i].IsEqual(tagSet))
                        return i;
                return -1;
            }


            public struct Enumerator : IEnumerator<TagSet>
            {
                int i;
                TagSet current;
                TagLookup<T> owner;

                public Enumerator(TagLookup<T> owner)
                {
                    i = 0;
                    current = null;
                    this.owner = owner;
                }

                public void Reset()
                {
                    i = 0;
                    current = null;
                }

                public void Dispose() { }
                public TagSet Current { get { return current; } }
                object IEnumerator.Current { get { return current; } }

                public bool MoveNext()
                {
                    if(i < owner.Count)
                    {
                        current = owner.rules[i];
                        i++;
                        return true;
                    }
                    return false;
                }
            }
        }

        public RuleCollection Rules { get { return new RuleCollection(this); } }

        #endregion

		#region IEnumerable (KeyValuePair)

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TagSet, T>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TagSet, T>>
        {
            int i;
            KeyValuePair<TagSet, T> current;
            TagLookup<T> owner;

            public Enumerator(TagLookup<T> owner)
            {
                i = 0;
                current = default(KeyValuePair<TagSet, T>);
                this.owner = owner;
            }

            public void Reset()
            {
                i = 0;
                current = default(KeyValuePair<TagSet, T>);
            }

            public void Dispose() { }
            public KeyValuePair<TagSet, T> Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }

            public bool MoveNext()
            {
                if(i < owner.Count)
                {
                    current = new KeyValuePair<TagSet, T>(owner.rules[i], owner.values[i]);
                    i++;
                    return true;
                }
                return false;
            }
        }

        #endregion

		public void Add(TagSet rule, T value)
        {
            // Expand if necessary
            if(count == rules.Length)
            {
                int newSize = Math.Max(8, rules.Length * 2); // <- Required to handle 0-length rule sets (can come from deserialize)
                Array.Resize(ref rules, newSize);
                Array.Resize(ref values, newSize);
            }

            // Find the insertion point
            // Search backwards so that when we get pre-sorted data we are O(1)
            // Also maintain stability (items that sort the same are kept in insertion order)
            int i = count;
            while(i > 0)
            {
                if(rule.Count <= rules[i-1].Count)
                    break; // Found insertion point
                i--;
            }

            // Make space at the insertion point
            Array.Copy(rules, i, rules, i+1, count - i);
            Array.Copy(values, i, values, i+1, count - i);
            count++;

            // Insert the new data
            rules[i] = rule;
            values[i] = value;

            // Because we are sorted, we can treat this as a count:
            if(rule.Count > 1)
                countOfMultiTagRules++;
        }

	    public void RemoveAt(int i)
        {
            if(i < 0 || i >= count)
                throw new ArgumentOutOfRangeException();

            // Because we are sorted, we can treat this as a count:
            if(rules[i].Count > 1)
                countOfMultiTagRules--;

            // Move the contents of the arrays back over the removed value
            Array.Copy(rules, i+1, rules, i, count - i - 1);
            Array.Copy(values, i+1, values, i, count - i - 1);

            // We now have one fewer item in our collection
            count--;

            // Clear the value off the end of the collection (just so it's not affecting GC)
            rules[count] = null;
            values[count] = default(T);
        }

		#region Lookup by Context

        public T this[string context]
        {
            get
            {
                if(context == null)
                    return GetBaseFallback();

                for(int i = countOfMultiTagRules; i < count; i++)
                {
                    if(rules[i].Count == 0 || (rules[i].Count == 1 && rules[i][0] == context))
                    {
                        return values[i];
                    }
                }
                Debug.Assert(!HasBaseFallback); // Should always match something if we have a base fallback.
                return default(T);
            }
        }

        public bool TryGetBestValue(string context, out T value)
        {
            if(context == null)
            {
                value = GetBaseFallback();
                return HasBaseFallback;
            }

            for(int i = countOfMultiTagRules; i < count; i++)
            {
                if(rules[i].Count == 0 || (rules[i].Count == 1 && rules[i][0] == context))
                {
                    value = values[i];
                    return true;
                }
            }
            Debug.Assert(!HasBaseFallback); // Should always match something if we have a base fallback.
            value = default(T);
            return false;
        }


        /// <summary>
        /// Find the best matching value for a given context.
        /// A matching value is one who's tags (rules) are a subset of the given context.
        /// The best value is the one with the largest number of rules.
        /// In case of a tie, the best value is the first ordinally.
        /// Returns default(T) if no rules are matched (will never happen if HasBaseFallback is true)
        /// </summary>
        public T this[TagSet context]
        {
            get
            {
                if(context.Count == 0)
                    return GetBaseFallback();
                else if(context.Count == 1)
                    return this[context[0]];

                // Use the sorted-by-rule-count property so that the first matched rule is the best one.
                for(int i = 0; i < count; i++)
                {
                    if(context.IsSupersetOf(rules[i]))
                    {
                        return values[i];
                    }
                }
                Debug.Assert(!HasBaseFallback); // Should always match something if we have a base fallback.
                return default(T);
            }
            // NOTE: Do not add a 'set' to this indexer, it does not make sense ("context" is different to "rule", even though both are TagSet)
        }


        public bool TryGetBestValue(TagSet context, out T value)
        {
            if(context.Count == 0)
            {
                value = GetBaseFallback();
                return HasBaseFallback;
            }
            else if(context.Count == 1)
                return TryGetBestValue(context[0], out value);

            // Use the sorted-by-rule-count property so that the first matched rule is the best one.
            for(int i = 0; i < count; i++)
            {
                if(context.IsSupersetOf(rules[i]))
                {
                    value = values[i];
                    return true;
                }
            }
            Debug.Assert(!HasBaseFallback); // Should always match something if we have a base fallback.
            value = default(T);
            return false;
        }


        /// <summary>Editor support</summary>
        public bool TryGetBestValueWithIndex(TagSet context, out T value, out int index)
        {
            // Use the sorted-by-rule-count property so that the first matched rule is the best one.
            for(int i = 0; i < count; i++)
            {
                if(context.IsSupersetOf(rules[i]))
                {
                    value = values[i];
                    index = i;
                    return true;
                }
            }
            Debug.Assert(!HasBaseFallback); // Should always match something if we have a base fallback.
            value = default(T);
            index = -1;
            return false;
        }


        /// <summary>
        /// Find one of the best matching values for a given context, selecting between multiple choices if they exist.
        /// A matching value is one who's tags (rules) are a subset of the given context.
        /// A best value is the one with the largest number of rules.
        /// In the case that there are multiple "best" matches, select the one at choiceIndex mod the number of options.
        /// </summary>
        public bool TryGetBestValueChoice(TagSet context, int choiceIndex, out T value)
        {
            // Try to find the first rule set that matches
            int first;
            for(first = 0; first < count; first++)
                if(context.IsSupersetOf(rules[first]))
                    break;

            if(first == count) // Got to the end without finding anything
            {
                value = default(T);
                return false;
            }

            int matchTagCount = rules[first].Count;

            // Optimisation: Oppertunistically avoid a re-walk if all matches are consecutive (common case)
            int lastMatch = first;

            // Find the last rule that could match and count the number of matches in between
            int matches = 1;
            int end;
            for(end = first + 1; end < count; end++)
            {
                if(rules[end].Count < matchTagCount)
                    break; // Don't match smaller rules

                if(context.IsSupersetOf(rules[end]))
                {
                    lastMatch = end;
                    matches++;
                }
            }

            // At this point we know the number of valid matches, and we can select one
            choiceIndex = (choiceIndex % matches);
            
            // If all matching elements are consecutive, we can immediately return
            if((lastMatch - first) + 1 == matches)
            {
                value = values[first + choiceIndex];
            }
            else // Otherwise we must re-walk (will early-out once we hit our option)
            {
                // WARNING: Reusing local variable 'first' as indexer here!
                while(choiceIndex > 0) // NOTE: choiceIndex = 0 will skip this loop, immediately giving the 'first' element (its rules always match)
                {
                    first++;
                    if(context.IsSupersetOf(rules[first]))
                        choiceIndex--; // Decrement our way through options we're not choosing
                }

                value = values[first];
            }

            return true;
        }

        #endregion

        /// <summary>Get the value with an empty tagset, or the default(T) if none exists</summary>
        public T GetBaseFallback()
        {
            // Rules are sorted by the number of tags,
            // so the final one will be the empty one (if it exists)
            if(Count > 0 && rules[Count-1].Count == 0)
                return values[Count-1];
            else
                return default(T); // <- caller can figure out what to do.
        }

        /// <summary>Has an empty rule (will always match)</summary>
        public bool HasBaseFallback
        {
            get { return Count > 0 && rules[Count-1].Count == 0; }
        }

        public void TryRemoveBaseFallBack()
        {
            if(HasBaseFallback)
                RemoveAt(Count-1);
        }

		#region Serialization

        // NOTE: Pass-through the animation serializer to a simple binary serializer (the format of `TagLookup` is *really* stable, and some folks need to directly serialize us)

        public void Serialize(AnimationSerializeContext context, Action<T> serializeValue)
        {
            Serialize(context.bw, serializeValue);
        }

        /// <summary>Deserialize into new object instance</summary>
        public TagLookup(AnimationDeserializeContext context, Func<T> deserializeValue) : this(context.br, deserializeValue)
        {
        }


        public void Serialize(BinaryWriter bw, Action<T> serializeValue)
        {
            bw.Write(Count);
            for(int i = 0; i < Count; i++)
                rules[i].Serialize(bw);
            for(int i = 0; i < Count; i++)
                serializeValue(values[i]);
        }

        /// <summary>Deserialize into new object instance</summary>
        public TagLookup(BinaryReader br, Func<T> deserializeValue)
        {
            count = br.ReadInt32();

            rules = new TagSet[count];
            for(int i = 0; i < rules.Length; i++)
            {
                rules[i] = new TagSet(br);
                if(rules[i].Count > 1)
                    countOfMultiTagRules++;
            }

            values = new T[count];
            for(int i = 0; i < values.Length; i++)
                values[i] = deserializeValue();
        }

        #endregion

		#region Editor Specific

        public TagSet GetRule(int i)
        {
            return rules[i];
        }

        /// <summary> The editor has the index, and wants to lookup the position dynamically </summary>
        public T GetValue(int i)
        {
            return values[i];
        }

        /// <summary> The editor has the index, and wants to edit the attachment in place, vs. remove and re-add </summary>
        public void SetValue(int i, T value)
        {
            values[i] = value;
        }

        /// <summary> The editor wants to do an exact match, not a best match, and receive the index if it hits (for preserving selections between frames) </summary>
        public bool TryGetExactValue(TagSet context, out T value, out int index)
        {
            // Use the sorted-by-rule-count property so that the first matched rule is the best one.
            for (int i = 0; i < count; i++)
            {
                var rule = rules[i];
                if (context.IsEqual(rule))
                {
                    value = values[i];
                    index = i;
                    return true;
                }
            }
            value = default(T);
            index = -1;
            return false;
        }

        #endregion

		internal void NetworkSerializeHelper(SerializeContext context, BinaryWriter bw)
        {
            for(int i = 0; i < count; i++)
                Field.Serialize(context, bw, ref values[i]);
        }
    }
}

