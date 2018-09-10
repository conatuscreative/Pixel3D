// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Pixel3D.Serialization.Context
{
	public class SerializeContext
	{
		/// <summary>Optionally build the visited object table (for reconstructing from definitions on deserailization)</summary>
		protected readonly List<object> visitedObjectTable;

		private readonly Dictionary<object, int> definitionVisitedObjectIndices;


		private object lastObjectSerialized;

		/// <summary>Still need to assign IDs, even if we're not building the full table</summary>
		private int visitedObjectCount;


		// Allow object graphs to be reconstructed:
		// (MUST use ReferenceEqualityComparer!)
		private readonly Dictionary<object, int> visitedObjectIndices;


		public SerializeContext(BinaryWriter writer, bool fillObjectTable = false,
			DefinitionObjectTable definitions = null)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			BinaryWriter = writer;

			if (definitions != null)
				definitionVisitedObjectIndices = definitions.externalVisitedObjectIndices;
			else
				definitionVisitedObjectIndices =
					new Dictionary<object, int>(ReferenceEqualityComparer<object>.Instance); // Empty for easy code

			visitedObjectIndices = new Dictionary<object, int>(ReferenceEqualityComparer<object>.Instance);

			// Only create this if asked:
			visitedObjectTable = fillObjectTable ? new List<object>() : null;
		}

        public BinaryWriter BinaryWriter { get; private set; }


		public DefinitionObjectTable GetAsDefinitionObjectTable()
		{
			if (visitedObjectTable == null)
				throw new InvalidOperationException("Cannot get definition object table unless fillObjectTable is set");
			return new DefinitionObjectTable(visitedObjectTable, visitedObjectIndices);
		}


		public virtual void Reset(BinaryWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			visitedObjectCount = 0;
			if (visitedObjectTable != null)
				visitedObjectTable.Clear();

			visitedObjectIndices.Clear();

			lastObjectSerialized = null;
		}


		/// <summary>
		///     Must be called at the start of the serialization of an object.
		///     Should be called at each level of an inheritance hierarchy (ie: call before calling the serializer method for a
		///     base type).
		/// </summary>
		public virtual void VisitObject(object obj)
		{
			// Ignore multiple visits on a single object instance (could be traversing an inheritance hierarchy)
			if (ReferenceEquals(obj, lastObjectSerialized))
				return;
			lastObjectSerialized = obj;

			// This will throw an exception if we visit the same object twice (desired behaviour)
			visitedObjectIndices.Add(obj, visitedObjectCount++);

			if (visitedObjectTable != null)
				visitedObjectTable.Add(obj);
		}

		public virtual void LeaveObject()
		{
			// Do nothing (derived types might want to gather statistics)
		}


		public bool Walk(object obj)
		{
#if DEBUG
			Debug.Assert(obj == null || !uniqueObjects.Contains(obj));
#endif

			int key;
			if (obj == null)
			{
				BinaryWriter.Write(Constants.VisitNull);
				return false;
			}

			if (visitedObjectIndices.TryGetValue(obj, out key))
			{
				DidLinkObject(key);
				BinaryWriter.Write((uint) key);
				return false;
			}

			if (definitionVisitedObjectIndices.TryGetValue(obj, out key))
			{
				DidLinkDefinitionObject(key);
				BinaryWriter.Write((uint) key | Constants.DefinitionVisitFlag);
				return false;
			}

			BinaryWriter.Write(Constants.FirstVisit);
			return true; // Caller should walk into the object
		}


		// Extension points for statistics:
		protected virtual void DidLinkObject(int id)
		{
		}

		protected virtual void DidLinkDefinitionObject(int id)
		{
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


		#region Unique Object Check

#if DEBUG
		private readonly HashSet<object>
			uniqueObjects = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
#endif

		[Conditional("DEBUG")]
		public void AssertUnique(object obj)
		{
#if DEBUG
			Debug.Assert(obj != null);
			Debug.Assert(!visitedObjectIndices.ContainsKey(obj));
			Debug.Assert(uniqueObjects.Add(obj));
#endif
		}

		#endregion
	}
}