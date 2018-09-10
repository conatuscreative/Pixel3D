using System;
using System.Collections.Generic;

namespace Pixel3D.Serialization.Demo.TestTypes
{
    struct SimpleStruct
    {
        int member;

        public SimpleStruct(int value) { this.member = value; }
    }

    class SimpleClass
    {
        int member;

        public SimpleClass(int value) { this.member = value; }

        public override bool Equals(object obj)
        {
            if(obj.GetType() == typeof(SimpleClass))
            {
                return (member == ((SimpleClass)obj).member);
            }

            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }


    [SerializationRoot]
    class ContainerTypes
    {
        List<SimpleStruct> list1 = new List<SimpleStruct>();
        List<List<SimpleClass>> nestedList = new List<List<SimpleClass>>();

        SimpleStruct[] array1 = new SimpleStruct[3];
        SimpleClass[] array2 = new SimpleClass[3];
    }
}
