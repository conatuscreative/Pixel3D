using System;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.Demo.TestTypes
{
    #pragma warning disable 169 // "Never used" (ok - testing reflection)


    [SerializationRoot]
    class CustomSerializerRoot
    {
        CustomSerializeReferenceType referenceType;
        CustomSerializeValueType valueType;
    }


    
    class CustomSerializeReferenceType
    {
        int something;

        [CustomSerializer]
        public static void Serialize(SerializeContext context, BinaryWriter bw, CustomSerializeReferenceType obj)
        {
            bw.Write((Int16)obj.something);
        }

        [CustomSerializer]
        public static void Deserialize(DeserializeContext context, BinaryReader br, CustomSerializeReferenceType obj)
        {
            obj.something = br.ReadInt16();
        }
        
        // This method is not necessary if all you want is for the object to be blank (ie: initobj)
        [CustomInitializer]
        public static CustomSerializeReferenceType Initialize()
        {
            return new CustomSerializeReferenceType();
        }
    }



    struct CustomSerializeValueType
    {
        int something;

        [CustomSerializer]
        public static void Serialize(SerializeContext context, BinaryWriter bw, ref CustomSerializeValueType obj)
        {
            bw.Write((Int16)obj.something);
        }

        [CustomSerializer]
        public static void Deserialize(DeserializeContext context, BinaryReader br, ref CustomSerializeValueType obj)
        {
            obj.something = br.ReadInt16();
        }
    }
}
