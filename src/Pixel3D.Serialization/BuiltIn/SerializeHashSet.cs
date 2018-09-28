// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Serialization.BuiltIn
{
	// TODO - make it possible to override built-ins, then uncomment this 

	/*
	static class SerializeHashSet
	{
	    // Because we want to know what comparer we get by default:
	    static class Sample<T>
	    {
	        internal static HashSet<T> sample = Initialize<T>();
	    }


	    [CustomSerializer]
	    public static void Serialize<T>(SerializeContext context, BinaryWriter bw, HashSet<T> hashSet)
	    {
	        // TODO: Add support for custom comparers
	        if(!hashSet.Comparer.Equals(Sample<T>.sample.Comparer))
	            throw new NotImplementedException("Serializing a custom comparer is not implemented");

	        context.VisitObject(hashSet);

	        bw.WriteSmallInt32(hashSet.Count);
	        foreach(var entry in hashSet)
	        {
	            T item = entry;
	            Field.Serialize(context, bw, ref item);
	        }

	        context.LeaveObject();
	    }

	    [CustomSerializer]
	    public static void Deserialize<T>(DeserializeContext context, BinaryReader br, HashSet<T> hashSet)
	    {
	        context.VisitObject(hashSet);

	        int count = br.ReadSmallInt32();

	        hashSet.Clear();

	        for(int i = 0; i < count; i++)
	        {
	            T item = default(T);
	            Field.Deserialize(context, br, ref item);
	            hashSet.Add(item);
	        }
	    }

	    [CustomInitializer]
	    public static HashSet<T> Initialize<T>()
	    {
	        return new HashSet<T>();
	    }
	}
	*/
}