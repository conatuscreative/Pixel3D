// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Pixel3D
{
    // This data structure is millstone. One day, it will go far, far away.
	
    /// <summary>Immutable set of tags for matching in TagLookup</summary>
    public class TagSet : IEnumerable<string>
    {
        public static readonly TagSet Empty = new TagSet();
        
		/// <summary>IMPORTANT: The TagSet takes ownership of the passed array!</summary>
        public TagSet(params string[] tags)
        {
            // NOTE: The mutable version used to remove duplicates, but it shouldn't actually matter, so let's just ignore the problem.

            Array.Sort(tags, StringComparer.InvariantCulture); // PERF: Should be Ordinal, but requires a complex data rewrite
            Array.Reverse(tags);
            
            this.tags = tags;
        }

        public TagSet(TagSet source, params string[] tags)
        {
            string[] allTags = new string[source.tags.Length + tags.Length];
            Array.Copy(source.tags, allTags, source.tags.Length);
            Array.Copy(tags, 0, allTags, source.tags.Length, tags.Length);
            Array.Sort(allTags, StringComparer.InvariantCulture); // PERF: Should be Ordinal, but requires a complex data rewrite
            Array.Reverse(allTags);

            this.tags = allTags;
        }

        // Non-params version for performance:
        public TagSet(TagSet source, string tag1)
        {
            string[] allTags = new string[source.tags.Length + 1];
            Array.Copy(source.tags, allTags, source.tags.Length);
            allTags[source.tags.Length] = tag1;
            Array.Sort(allTags, StringComparer.InvariantCulture); // PERF: Should be Ordinal, but requires a complex data rewrite
            Array.Reverse(allTags);

            tags = allTags;
        }

        // Non-params version for performance:
        public TagSet(TagSet source, string tag1, string tag2)
        {
            string[] allTags = new string[source.tags.Length + 2];
            Array.Copy(source.tags, allTags, source.tags.Length);
            allTags[source.tags.Length+0] = tag1;
            allTags[source.tags.Length+1] = tag2;
            Array.Sort(allTags, StringComparer.InvariantCulture); // PERF: Should be Ordinal, but requires a complex data rewrite
            Array.Reverse(allTags);

            tags = allTags;
        }
		
        private string[] tags;
        public int Count { get { return tags.Length; } }

		/// <summary>Get a copy of the underlying tag array</summary>
        public string[] ToArray()
        {
            string[] result = new string[tags.Length];
            Array.Copy(tags, result, tags.Length);
            return result;
        }

		public string this[int index]
        {
            get { return tags[index]; }
        }

		public override string ToString()
        {
            if(Count == 0)
                return "*";
            return string.Join(", ", tags, 0, Count);
        }
		
        public bool Contains(string tag)
        {
            for(int i = 0; i < Count; i++)
            {
                if(tags[i] == tag)
                    return true;
            }
            return false;
        }

        /// <summary> Editor use; wants to match "ru" with "run"</summary>
        public bool IsFuzzySupersetOf(TagSet subset)
        {
            // Take advantage of sorted property to search linearly
            int subsetIndex = 0, thisIndex = 0;
            while (subsetIndex < subset.Count)
            {
                while (thisIndex < Count)
                {
                    var left = subset.tags[subsetIndex];
                    var right = tags[thisIndex]; 
                    
                    // truncate to search length for "fuzzy"
                    if (left.Length <= right.Length)
                        right = right.Substring(0, left.Length);

                    var comparison = string.Compare(left, right, StringComparison.InvariantCulture); // PERF: Should be Ordinal, but requires a complex data rewrite
                    thisIndex++;

                    if (comparison == 0)
                        goto nextSubsetItem; // This item matched, go to the next one
                    if (comparison > 0)
                        return false; // This item comes after the given one (and all subsequent ones will as well - we'll never find a match)
                }

                // If we get to here, we ran out of superset items to check against
                return false;

            nextSubsetItem:
                subsetIndex++;
            }

            // If we get to here, all subset items were matched
            return true;
        }

		public bool IsSupersetOf(TagSet subset)
        {
            // Take advantage of sorted property to search linearly
            int subsetIndex = 0, thisIndex = 0;
            while(subsetIndex < subset.Count)
            {
                while(thisIndex < Count)
                {
                    int comparison = string.Compare(subset.tags[subsetIndex], tags[thisIndex], StringComparison.InvariantCulture); // PERF: Should be Ordinal, but requires a complex data rewrite
                    thisIndex++;

                    if(comparison == 0)
                        goto nextSubsetItem; // This item matched, go to the next one
                    if(comparison > 0)
                        return false; // This item comes after the given one (and all subsequent ones will as well - we'll never find a match)
                }

                // If we get to here, we ran out of superset items to check against
                return false;

            nextSubsetItem:
                subsetIndex++;
            }

            // If we get to here, all subset items were matched
            return true;
        }

	    public bool IsEqual(TagSet other)
        {
            if (other == null)
                return false;
            if(Count != other.Count)
                return false;
            for(int i = 0; i < Count; i++)
            {
                if(tags[i] != other.tags[i])
                    return false;
            }

            return true;
        }
		
        #region Object Overrides

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this, obj))
                return true;
            return IsEqual(obj as TagSet);
        }

        public override int GetHashCode()
        {
            int hash = Count;
            for(int i = 0; i < Count; i++)
            {
                hash ^= tags[i].GetHashCode();
            }
            return hash;
        }

        #endregion

		#region IEnumerable

        public struct Enumerator : IEnumerator<string>
        {
            int i;
            string current;
            TagSet tagSet;

            public Enumerator(TagSet tagSet)
            {
                i = 0;
                current = null;
                this.tagSet = tagSet;
            }

            public void Reset()
            {
                i = 0;
                current = null;
            }

            public void Dispose() { }
            public string Current { get { return current; } }
            object IEnumerator.Current { get { return current; } }

            public bool MoveNext()
            {
                if(i < tagSet.Count)
                {
                    current = tagSet.tags[i];
                    i++;
                    return true;
                }
                return false;
            }
        }


        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

		#endregion

		#region Serialize

	    public void Serialize(BinaryWriter bw)
        {
            bw.Write(Count);
            for(int i = 0; i < Count; i++)
            {
                bw.Write(tags[i]);
            }
        }

        public TagSet(BinaryReader br)
        {
            int count = br.ReadInt32();
            tags = new string[count];
            for(int i = 0; i < tags.Length; i++)
            {
                tags[i] = br.ReadString();
            }
        }

        #endregion
    }
}
