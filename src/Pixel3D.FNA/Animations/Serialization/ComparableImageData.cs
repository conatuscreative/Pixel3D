using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Xna.Framework;

namespace Pixel3D.Animations.Serialization
{
    struct ComparableImageData : IEquatable<ComparableImageData>
    {
        public ComparableImageData(Data2D<Color> source)
        {
            data = source.Data;
            width = source.Width;
            height = source.Height;

            if(data == null)
            {
                hash = 0;
                return;
            }

            Debug.Assert(data.Length == width * height);

            // Butchered https://en.wikipedia.org/wiki/Jenkins_hash_function
            {
                hash = 0;
                hash += (uint)width;
                hash += (hash << 10);
                hash ^= (hash >> 6);

                hash += (uint)height;
                hash += (hash << 10);
                hash ^= (hash >> 6);

                for(int i = 0; i < data.Length; i++)
                {
                    hash += data[i].R;
                    hash += (hash << 10);
                    hash ^= (hash >> 6);

                    hash += data[i].G;
                    hash += (hash << 10);
                    hash ^= (hash >> 6);

                    hash += data[i].B;
                    hash += (hash << 10);
                    hash ^= (hash >> 6);

                    hash += data[i].A;
                    hash += (hash << 10);
                    hash ^= (hash >> 6);
                }
                hash += (hash << 3);
                hash ^= (hash >> 11);
                hash += (hash << 15);
            }
        }


        public Color[] data;
        public int width, height;
        public uint hash;

        public override int GetHashCode()
        {
            return (int)hash;
        }

        public override bool Equals(object obj)
        {
            if(obj is ComparableImageData)
                return Equals((ComparableImageData)obj);
            else
                return false;
        }


        // Nasty data-specific version of memcmp (so .NET fixes our arrays for us)
        [DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static unsafe extern int memcmp(Color[] colors1, Color[] colors2, UIntPtr bytes);

        public bool Equals(ComparableImageData other) // from IEquatable<ComparableImageData>
        {
            if(hash != other.hash || width != other.width || height != other.height)
                return false;

            if(data == null && other.data == null)
                return true;
            if(data == null || other.data == null)
                return false;

            if(data.Length != other.data.Length)
                return false; // Hopefully the width/height check catches this...

            return 0 == memcmp(data, other.data, (UIntPtr)(data.Length * Marshal.SizeOf(typeof(Color))));
        }
    }

}
