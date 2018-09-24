using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pixel3D.Animations
{
    /// <summary>
    /// This is a compatibility shim for old code that accesses layers as a list.
    /// </summary>
    /// <remarks>
    /// About 98% of our (~360k at time of writing) frames only have a single layer.
    /// Storing a List (list pointer, Cel pointer, count) for each one wasted a *lot* of time and memory during de/serialization (both regular and network).
    /// The fact that this is O(n^2) to operate on isn't really a concern, given that most of the time n=1.
    /// </remarks>
    public struct LayersList : IList<Cel>
    {
		private readonly AnimationFrame owner;

		public LayersList(AnimationFrame owner)
        {
            this.owner = owner;
        }

        #region IList<Cel> Members

        public int IndexOf(Cel item)
        {
            int i = 0;
            Cel current = owner.firstLayer;
            while(current != null)
            {
                if(current == item)
                    return i;
                current = current.next;
                i++;
            }
            return -1;
        }

        public void Insert(int index, Cel item)
        {
            Debug.Assert(item != null);
            Debug.Assert(item.next == null);

            if(index == 0)
            {
                item.next = owner.firstLayer;
                owner.firstLayer = item;
                return;
            }
            else
            {
                int i = 1; // <- index of current.next
                Cel current = owner.firstLayer;
                while(current != null) // <- final `i` == Count
                {
                    if(i == index)
                    {
                        // Insert at `current.next`:
                        item.next = current.next;
                        current.next = item;
                        return;
                    }
                    
                    current = current.next;
                    i++;
                }
            }
            throw new ArgumentOutOfRangeException("index");
        }

        public void RemoveAt(int index)
        {
            if(owner.firstLayer != null)
            {
                if(index == 0)
                {
                    Cel remove = owner.firstLayer;
                    owner.firstLayer = owner.firstLayer.next;
                    remove.next = null; // <- null while outside of a list invariant
                    return;
                }
                else
                {
                    int i = 1; // <- index of current.next
                    Cel current = owner.firstLayer;
                    while(current.next != null) // <- final `i` == Count-1
                    {
                        if(i == index)
                        {
                            // Remove `current.next`:
                            Cel remove = current.next;
                            current.next = remove.next;
                            remove.next = null; // <- null while outside of a list invariant
                            return;
                        }

                        current = current.next;
                        i++;
                    }
                }
            }
            throw new ArgumentOutOfRangeException("index");
        }

        public Cel this[int index]
        {
            get
            {
                int i = 0; // <- index of `current`
                Cel current = owner.firstLayer;
                while(current != null)
                {
                    if(i == index)
                        return current;
                    current = current.next;
                    i++;
                }
                throw new ArgumentOutOfRangeException("index");
            }
            set
            {
                Debug.Assert(value.next == null);

                if(owner.firstLayer != null)
                {
                    if(index == 0)
                    {
                        value.next = owner.firstLayer.next;
                        owner.firstLayer.next = null; // <- being removed -- null while outside of a list invariant
                        owner.firstLayer = value;
                        return;
                    }
                    else
                    {
                        int i = 1; // <- index of current.next
                        Cel current = owner.firstLayer;
                        while(current.next != null) // <- final `i` == Count-1
                        {
                            if(i == index)
                            {
                                // Replace `current.next`:
                                value.next = current.next.next;
                                current.next.next = null; // <- being removed -- null while outside of a list invariant
                                current.next = value;
                                return;
                            }

                            current = current.next;
                            i++;
                        }
                    }
                }
                throw new ArgumentOutOfRangeException("index");
            }
        }

        #endregion

		#region ICollection<Cel> Members

        public void Add(Cel item)
        {
            Debug.Assert(item != null);
            Debug.Assert(item.next == null);

            if(owner.firstLayer == null)
            {
                owner.firstLayer = item;
            }
            else
            {
                Cel current = owner.firstLayer;
                while(current.next != null)
                    current = current.next;
                current.next = item;
            }
        }

        public void Clear()
        {
            if(owner.firstLayer != null)
                ClearHelper(owner.firstLayer);
            owner.firstLayer = null;
        }

        // NOTE: This is a bit of a waste of cycles, but we want to keep "Cel.next == null" invariant for Cels outside the list
        private static void ClearHelper(Cel item)
        {
            Cel next = item.next;
            item.next = null;
            if(next != null)
                ClearHelper(next);
        }

        public bool Contains(Cel item)
        {
            Cel current = owner.firstLayer;
            while(current != null)
            {
                if(current == item)
                    return true;
                current = current.next;
            }
            return false;
        }

        public void CopyTo(Cel[] array, int arrayIndex)
        {
            throw new NotImplementedException(); // can't be bothered.
        }

        public int Count
        {
            get
            {
                int i = 0;
                Cel current = owner.firstLayer;
                while(current != null)
                {
                    i++;
                    current = current.next;
                }
                return i;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Cel item)
        {
            Debug.Assert(item != null);

            if(owner.firstLayer == item)
            {
                owner.firstLayer = owner.firstLayer.next;
                item.next = null;
                return true;
            }
            else
            {
                Cel current = owner.firstLayer;
                while(current != null)
                {
                    if(current.next == item)
                    {
                        current.next = current.next.next;
                        item.next = null;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

		#region Compatibility with List<Cel>

        public void InsertRange(int index, IEnumerable<Cel> collection)
        {
            // Get the first thing to insert (so we can start working from a Cel as early as possible, not AnimationFrame)
            var enumerator = collection.GetEnumerator();
            if(!enumerator.MoveNext())
                return; // <- Nothing to insert
            Cel first = enumerator.Current;
            Debug.Assert(first.next == null);

            Cel last = null;
            if(index == 0)
            {
                last = owner.firstLayer;
                owner.firstLayer = first;
                goto readyToInsert;
            }
            else
            {
                int i = 1; // <- index of current.next
                Cel current = owner.firstLayer;
                while(current != null) // <- final `i` == Count
                {
                    if(i == index)
                    {
                        // Insert at `current.next`:
                        last = current.next;
                        current.next = first;
                        goto readyToInsert;
                    }

                    current = current.next;
                    i++;
                }
            }
            throw new ArgumentOutOfRangeException("index");

        readyToInsert:
            while(enumerator.MoveNext())
            {
                Debug.Assert(enumerator.Current.next == null);
                first.next = enumerator.Current;
                first = first.next;
            }

            first.next = last;
        }

        #endregion

		#region IEnumerable

        public struct Enumerator : IEnumerator<Cel>
        {
            public Enumerator(Cel next)
            {
                this.current = null;
                this.next = next;
            }

            Cel current;
            Cel next;

            public Cel Current { get { return current; } }
            object System.Collections.IEnumerator.Current { get { return current; } }

            public bool MoveNext()
            {
                if(next != null)
                {
                    current = next;
                    next = next.next;
                    return true;
                }
                return false;
            }

            public void Dispose() { }
            public void Reset() { throw new InvalidOperationException(); } // nope.
        }

        public IEnumerator<Cel> GetEnumerator()
        {
            return new Enumerator(owner.firstLayer);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(owner.firstLayer);
        }

        #endregion
    }
}

