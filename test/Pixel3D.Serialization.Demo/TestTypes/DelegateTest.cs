using System.Diagnostics;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.Demo.TestTypes
{
    [SerializationRoot]
    class DelegateTest
    {
        class DelegateTestSubject
        {
            public int frobCount = 0;
            public void Frob() { frobCount++; }
            public void Set(int i) { frobCount = i; }
        }

        // NOTE: Was using System.Action for these, but then SerializableDelegate was added
        [SerializableDelegate]
        public delegate void MyAction();

        [SerializableDelegate]
        public delegate void MyAction<in T>(T obj);
        
        MyAction frob1;
        MyAction frob2;
        MyAction<int> set;
        DelegateTestSubject subject;

        public static void RunTest()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SerializeContext serializeContext = new SerializeContext(bw);

            DelegateTest testObject = new DelegateTest();
            testObject.subject = new DelegateTestSubject();
            testObject.frob1 = testObject.subject.Frob;
            testObject.frob2 = () => testObject.subject.frobCount = 100;
            testObject.set = testObject.subject.Set;
            testObject.subject.frobCount = -999;

            Field.Serialize(serializeContext, bw, ref testObject);

            BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray()));
            DeserializeContext deserializeContext = new DeserializeContext(br);

            DelegateTest result = null;
            Field.Deserialize(deserializeContext, br, ref result);

            // Tests:
            Debug.Assert(!ReferenceEquals(testObject.subject, result.subject));
            Debug.Assert(testObject.subject.frobCount == result.subject.frobCount);
            result.subject.frobCount = 0;
            result.frob1();
            Debug.Assert(result.subject.frobCount == 1);
            result.frob2();
            Debug.Assert(result.subject.frobCount == 100);
            result.set(5);
            Debug.Assert(result.subject.frobCount == 5);

            Debug.Assert(testObject.subject.frobCount == -999); // <- just to be sure
        }
    }
}
