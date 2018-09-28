// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.IO;

namespace Pixel3D.Serialization.Context
{
	public class DeserializeContext
	{
		private readonly List<object> definitionObjectTable;
		private readonly List<object> visitedObjectTable;

		public DeserializeContext(BinaryReader reader, DefinitionObjectTable definitions = null)
		{
			BinaryReader = reader;

			visitedObjectTable = new List<object>();

			if (definitions != null)
				definitionObjectTable = definitions.visitedObjectTable;
			else
				definitionObjectTable = new List<object>();
		}

        public BinaryReader BinaryReader { get; private set; }

		public void Reset()
		{
			BinaryReader.BaseStream.Position = 0;
			visitedObjectTable.Clear();
		}

		/// <summary>
		///     Call at the beginning of deserialization of the base-most type (note: this is different to what
		///     SerializeContext requires)
		/// </summary>
		public void VisitObject(object obj)
		{
			visitedObjectTable.Add(obj);
		}

		public bool Walk<T>(ref T obj) where T : class
		{
			var visitedObjectIndex = BinaryReader.ReadUInt32();
			if (visitedObjectIndex == Constants.VisitNull)
			{
				obj = null;
				return false;
			}

			if (visitedObjectIndex == Constants.FirstVisit) return true; // Caller should walk into the object

			if ((visitedObjectIndex & Constants.DefinitionVisitFlag) != 0)
			{
				var index = (int) (visitedObjectIndex & ~Constants.DefinitionVisitFlag);
				obj = (T) definitionObjectTable[index];
				return false;
			}

			obj = (T) visitedObjectTable[(int) visitedObjectIndex];
			return false;
		}

#if DEBUG
		/// <summary>
		///     Feedback from the generated serializer about what it's doing (in case a memory-compare mismatch fires
		///     somewhere inside, for example)
		/// </summary>
		public string DebugLastTrace { get; private set; }

		public void DebugTrace(string trace)
		{
			DebugLastTrace = trace;
		}
#endif
	}
}