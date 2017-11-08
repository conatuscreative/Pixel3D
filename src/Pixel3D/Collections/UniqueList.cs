using System.Collections.Generic;
using System.IO;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Collections
{
    /// <summary>IMPORTANT: This is a serialization optimisation where we assume the stored list is not referenced elsewhere and is never null.</summary>
    /// <remarks>Could implement IList... but that just invites boxing.</remarks>
    public struct UniqueList<T>
    {
        List<T> list;

        public UniqueList(List<T> list)
        {
            this.list = list;
        }

        public static UniqueList<T> Create()
        {
            return new UniqueList<T>(new List<T>());
        }

        public List<T> GetList() // Escape hatch
        {
            return this.list;
        }


        #region Pass Through to List<T>

        public int Count { get { return list.Count; } }
        public T this[int index] { get { return list[index]; } set { list[index] = value; } }

        public void Clear()
        {
            list.Clear();
        }

        public void AddRange(UniqueList<T> otherList)
        {
            this.list.AddRange(otherList.list);
        }

        public void Add(T slot)
        {
            list.Add(slot);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        public void Remove(T item)
        {
            list.Remove(item);
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }


        // List already has a perfectly serviceable non-allocating enumerator:
        public List<T>.Enumerator GetEnumerator() { return list.GetEnumerator(); }

        #endregion

    }



    public static class UniqueListSerialiation
    {
        #region Serialization

        [CustomSerializer]
        public static void SerializeField<T>(SerializeContext context, BinaryWriter bw, ref UniqueList<T> value)
        {
            var list = value.GetList();
            context.AssertUnique(list);

            bw.WriteSmallInt32(list.Count);
            for(int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                Field.Serialize(context, bw, ref item);
            }
        }

        [CustomSerializer]
        public static void DeserializeField<T>(DeserializeContext context, BinaryReader br, ref UniqueList<T> value)
        {
            int count = br.ReadSmallInt32();
            var list = new List<T>(count);
            value = new UniqueList<T>(list);

            for(int i = 0; i < count; i++)
            {
                T item = default(T);
                Field.Deserialize(context, br, ref item);
                list.Add(item);
            }
        }

        #endregion
    }

}
