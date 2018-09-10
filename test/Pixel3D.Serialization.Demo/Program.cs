using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Pixel3D.Serialization.Context;
using Pixel3D.Serialization.Generator;
using Pixel3D.Serialization.Demo.TestTypes;

namespace Pixel3D.Serialization.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create our own serialization assembly on the fly:
            Assembly[] assemblies = new[] { Assembly.GetAssembly(typeof(Program)) };

            Serializer.BeginStaticInitialize(assemblies, Type.EmptyTypes, true);
            Serializer.EndStaticInitialize();

            
            using(GeneratorReports reports = new GeneratorReports("Serializer - Assembly Test"))
            {
                GeneratorResult result = Serializer.GenerateAssembly(assemblies, Type.EmptyTypes, reports, "__Serializer");
                
                // Do assembly verification
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\PEVerify.exe";
                    p.StartInfo.Arguments = "__Serializer.dll /il /verbose "
                            //+ "/hresult " // Output error codes
                            + "/ignore="
                            + "0x8013187D,0x8013187C," // ignore method and field visiblity
                            + "0x801318F3," // "Type load failed" - generally (always?) caused by private nested classes failing visiblity
                            + "0x80131884"; // ignore initonly field writes (these don't happen with DynamicMethod, because we are in-type and that seems to satisfy the JIT)
                    p.StartInfo.RedirectStandardOutput = true;
                    p.Start();
                    File.WriteAllText(reports.Directory + @"\PEVerify.txt", p.StandardOutput.ReadToEnd());
                    p.WaitForExit();
                }
            }

            using(GeneratorReports reports = new GeneratorReports("Serializer - Dynamic Method Test"))
            {
                GeneratorResult result = Serializer.GenerateDynamicMethods(assemblies, Type.EmptyTypes, reports);
            }


            // Serialization methods, and thereby the serialization assembly itself,
            // get cached in static fields that are initialized by the CLR at JIT time.
            // So any method that accesses serialization must be JITted *after* the above
            // assembly generation is completed. In theory this method doesn't get JITted
            // until it is called...
            Run();
        }
        


        static void Run()
        {
            // TODO: One day these should be converted to proper unit tests...

            TestPrimitiveType();

            TestWithDispatch(new BaseClass());
            TestWithDispatch(new DerivedClassA());
            TestWithDispatch(new DerivedClassB());
            TestWithDispatch(new DerivedClassC());

            TreeTest.RunTest();
            TreeTest.RunTestWithDefinitions();

            TestArrays();

            TestManyTypes();

            DelegateTest.RunTest();
        }

        
        static void TestPrimitiveType()
        {
            PrimitiveTypes subject = new PrimitiveTypes()
            {
                @bool = true,
                @float = 3.14159f,
                @double = 9.999,
                @char = 'q',
                @int = 42,
                @sbyte = 1
            };

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SerializeContext serializeContext = new SerializeContext(bw);

            Field.Serialize(serializeContext, bw, ref subject);


            BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray()));
            DeserializeContext deserializeContext = new DeserializeContext(br);

            PrimitiveTypes result = default(PrimitiveTypes);
            Field.Deserialize(deserializeContext, br, ref result);

            Debug.Assert(object.Equals(result, subject));
        }



        static void TestWithDispatch(BaseClass input)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SerializeContext serializeContext = new SerializeContext(bw);

            Field.Serialize(serializeContext, bw, ref input);


            BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray()));
            DeserializeContext deserializeContext = new DeserializeContext(br);

            BaseClass result = null;
            Field.Deserialize(deserializeContext, br, ref result);

            Debug.Assert(input.GetType() == result.GetType());
        }


        static void TestArrays()
        {
            int[] intArray = { 1, 2, 3, 4 };
            SimpleClass[] classArray = { new SimpleClass(1), new SimpleClass(2), new SimpleClass(3) };
            SimpleStruct[] structArray = { new SimpleStruct(1), new SimpleStruct(2), new SimpleStruct(3) };
            int[,] int2DArray = { { 1, 2 }, { 3, 4 } };


            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SerializeContext serializeContext = new SerializeContext(bw);

            Field.Serialize(serializeContext, bw, ref intArray);
            Field.Serialize(serializeContext, bw, ref classArray);
            Field.Serialize(serializeContext, bw, ref structArray);
            Field.Serialize(serializeContext, bw, ref int2DArray);


            BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray()));
            DeserializeContext deserializeContext = new DeserializeContext(br);

            int[] intArrayResult = null;
            SimpleClass[] classArrayResult = null;
            SimpleStruct[] structArrayResult = null;
            int[,] int2DArrayResult = null;

            Field.Deserialize(deserializeContext, br, ref intArrayResult);
            Field.Deserialize(deserializeContext, br, ref classArrayResult);
            Field.Deserialize(deserializeContext, br, ref structArrayResult);
            Field.Deserialize(deserializeContext, br, ref int2DArrayResult);


            Debug.Assert(intArray.SequenceEqual(intArrayResult));
            Debug.Assert(classArray.SequenceEqual(classArrayResult));
            Debug.Assert(structArray.SequenceEqual(structArrayResult));

            Debug.Assert(FlattenArray(int2DArray).SequenceEqual(FlattenArray(int2DArrayResult)));
        }

        static T[] FlattenArray<T>(T[,] array)
        {
            T[] newArray = new T[array.GetLength(0) * array.GetLength(1)];
            Buffer.BlockCopy(array, 0, newArray, 0, newArray.Length);
            return newArray;
        }



        static void TestManyTypes()
        {
            TestType(typeof(Program));
            TestType(typeof(PrimitiveTypes));
        }

        static void TestType(Type type)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SerializeContext serializeContext = new SerializeContext(bw);

            Field.Serialize(serializeContext, bw, ref type);

            BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray()));
            DeserializeContext deserializeContext = new DeserializeContext(br);

            Type result = null;
            Field.Deserialize(deserializeContext, br, ref result);

            Debug.Assert(type == result);
        }

    }
}
