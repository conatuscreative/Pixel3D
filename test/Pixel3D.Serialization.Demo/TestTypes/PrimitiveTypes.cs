using System;

namespace Pixel3D.Serialization.Demo.TestTypes
{
    #pragma warning disable 649 // "Never assigned" (ok - testing reflection)

    [SerializationRoot]
    struct PrimitiveTypes
    {
        public int @int;
        public uint @uint;
        public short @short;
        public ushort @ushort;
        public sbyte @sbyte;
        public byte @byte;
        public float @float;
        public double @double;
        public ulong @ulong;
        public long @long;
        public char @char;
        public bool @bool;

        // Ignored by the serializer:
        public IntPtr intPtr;
        public UIntPtr uintPtr;
        public object @object; // (technically not primitive)

        // Handled by built-in field serializer:
        public string @string; // (technically not primitive)
    }
}
