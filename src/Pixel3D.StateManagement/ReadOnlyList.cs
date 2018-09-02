using System.Collections.Generic;

namespace Pixel3D.StateManagement
{
    /// <summary>Thin wrapper around List that, unlike ReadOnlyCollection, does not allocate.</summary>
    /// <remarks>Could implement IList... but that just invites boxing.</remarks>
    public struct ReadOnlyList<T>
    {
        List<T> list;

        public ReadOnlyList(List<T> list)
        {
            this.list = list;
        }

        public int Count { get { return list.Count; } }
        public T this[int index] { get { return list[index]; } }

        // List already has a perfectly serviceable non-allocating enumerator:
        public List<T>.Enumerator GetEnumerator() { return list.GetEnumerator(); }
    }
}
