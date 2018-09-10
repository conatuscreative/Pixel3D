using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Serialization.Demo.TestTypes
{
    [SerializationRoot]
    class TreeTest
    {
        public TreeTest(int value)
        {
            this.value = value;
        }

        public readonly int value;
        public readonly List<TreeTest> other = new List<TreeTest>();

        public override bool Equals(object obj)
        {
            TreeTest tt = obj as TreeTest;
            if(tt != null)
            {
                if(tt.value == value)
                {
                    if(other.Count == tt.other.Count)
                    {
                        for(int i = 0; i < other.Count; i++)
                        {
                            if(!other[i].Equals(tt.other[i]))
                                return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }


        public static void RunTest()
        {
            TreeTest input = new TreeTest(43);
            TreeTest common = new TreeTest(66);
            common.other.Add(new TreeTest(100));
            input.other.Add(common);
            input.other.Add(common);
            input.other.Add(common);

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SerializeContext serializeContext = new SerializeContext(bw);

            Field.Serialize(serializeContext, bw, ref input);


            BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray()));
            DeserializeContext deserializeContext = new DeserializeContext(br);

            TreeTest result = null;
            Field.Deserialize(deserializeContext, br, ref result);

            Debug.Assert(input.Equals(result));
            foreach(var item in result.other)
            {
                // Check the graph works:
                Debug.Assert(ReferenceEquals(item, result.other[0]));
            }
        }



        public static void RunTestWithDefinitions()
        {
            TreeTest definition = new TreeTest(66);
            TreeTest definition2 = new TreeTest(100);
            definition.other.Add(definition2);

            TreeTest input = new TreeTest(43);
            input.other.Add(definition);
            input.other.Add(definition);
            input.other.Add(definition);

            var definitionTable = GetDefinitionTable(definition);

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SerializeContext serializeContext = new SerializeContext(bw, false, definitionTable);

            Field.Serialize(serializeContext, bw, ref input);


            BinaryReader br = new BinaryReader(new MemoryStream(ms.ToArray()));
            DeserializeContext deserializeContext = new DeserializeContext(br, definitionTable);

            TreeTest result = null;
            Field.Deserialize(deserializeContext, br, ref result);


            Debug.Assert(input.Equals(result));
            foreach(var item in result.other)
            {
                // Check that we refer back to definition objects
                Debug.Assert(ReferenceEquals(item, definition)); 
            }
            Debug.Assert(ReferenceEquals(definition.other[0], definition2));
        }


        public static DefinitionObjectTable GetDefinitionTable(TreeTest definition)
        {
            BinaryWriter bw = new BinaryWriter(Stream.Null);
            SerializeContext serializeContext = new SerializeContext(bw, true);

            Field.Serialize(serializeContext, bw, ref definition);

            return serializeContext.GetAsDefinitionObjectTable();
        }
    }


    
}

