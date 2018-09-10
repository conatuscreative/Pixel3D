// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Pixel3D.Serialization.Context
{
	public class SerializeContextWithStatistics : SerializeContext
	{
		public SerializeContextWithStatistics(BinaryWriter writer, DefinitionObjectTable definitions = null) : base(
			writer, true, definitions)
		{
			lastPosition = writer.BaseStream.Position;
			definitionObjectTable = definitions != null ? definitions.visitedObjectTable : null;
		}


		public override void Reset(BinaryWriter writer)
		{
			CheckFinished();

			base.Reset(writer);

			objectSizeTable.Clear();
			activeObject.Clear();
			rootObjects.Clear();
			linkTable.Clear();
			lastPosition = writer.BaseStream.Position;
		}


		#region Size Tracking

		private readonly List<int> objectSizeTable = new List<int>();
		private long lastPosition; // Initialized to initial stream position

		private void ApplySizeChangeTo(int id)
		{
			var newPosition = BinaryWriter.BaseStream.Position;
			objectSizeTable[id] += (int) (newPosition - lastPosition);
			lastPosition = newPosition;
		}

		#endregion


		#region Visit Stack

		private readonly Stack<int> activeObject = new Stack<int>();
		private readonly List<int> rootObjects = new List<int>();

		private void CheckFinished()
		{
			if (activeObject.Count != 0)
				throw new InvalidOperationException("Serialization visit/leave is unbalanced");
		}

		public override void VisitObject(object obj)
		{
			base.VisitObject(obj);

			var id = visitedObjectTable.Count -
			         1; // NOTE: we may visit the same object multiple times due to inheritance
			Debug.Assert(id <= objectSizeTable.Count);
			if (id == objectSizeTable.Count) // First visit, start tracking size
				objectSizeTable.Add(0);

			if (activeObject.Count > 0)
			{
				var visitFromObjectId = activeObject.Peek();
				ApplySizeChangeTo(visitFromObjectId);

				if (visitFromObjectId != id) // (skip if going through inhertance)
					AddLink((uint) visitFromObjectId, (uint) id);
			}
			else
			{
				rootObjects.Add(id);
			}

			activeObject.Push(id);
		}


		public override void LeaveObject()
		{
			base.LeaveObject();

			ApplySizeChangeTo(activeObject.Pop());
		}

		#endregion


		#region Link Tracking

		// Links are tracked by IDs with "Definition" flag (same as binary format)

		private struct Link
		{
			public uint from, to;
		}

		private readonly List<Link> linkTable = new List<Link>();

		private void AddLink(uint from, uint to)
		{
			linkTable.Add(new Link {from = from, to = to});
		}

		protected override void DidLinkObject(int id)
		{
			base.DidLinkObject(id);
			AddLink((uint) activeObject.Peek(), (uint) id);
		}

		protected override void DidLinkDefinitionObject(int id)
		{
			base.DidLinkDefinitionObject(id);
			AddLink((uint) activeObject.Peek(), (uint) id | Constants.DefinitionVisitFlag);
		}

		#endregion


		#region Object size and count report

		private class PerTypeStatistics
		{
			public int count;
			public int totalSize;
			public Type type;
		}

		/// <summary>Write a tab-separated-values output of the contents of the visited objects table, plus additional debug data</summary>
		public void WriteObjectTableReport(StreamWriter output)
		{
			CheckFinished();

			var totalSize = 0;

			var typeStatisticsLookup = new Dictionary<Type, PerTypeStatistics>();

			Debug.Assert(visitedObjectTable.Count == objectSizeTable.Count);
			for (var i = 0; i < visitedObjectTable.Count; i++)
			{
				var type = visitedObjectTable[i].GetType();
				PerTypeStatistics stats;
				if (!typeStatisticsLookup.TryGetValue(type, out stats))
				{
					stats = new PerTypeStatistics();
					stats.type = type;
					typeStatisticsLookup.Add(type, stats);
				}

				stats.count++;
				stats.totalSize += objectSizeTable[i];
				totalSize += objectSizeTable[i];
			}

			var typeStatistics = typeStatisticsLookup.Values.OrderByDescending(s => s.count)
				.ThenByDescending(s => s.totalSize).ToArray();


			// Column Headers:
			output.WriteLine(
				"Index\t.ToString()\t.GetType()\tSize\t\t\tType\tObject Count\tTotal Size\tAvg. Size / Obj");

			// Totals:
			output.WriteLine("\tVisited object count = " + visitedObjectTable.Count);
			output.WriteLine("\tReferences = " + linkTable.Count);
			output.WriteLine("\tTotal size = " + totalSize);

			// (note: the type statistics table is necessaraly shorter than the visited object table)
			for (var i = 0; i < visitedObjectTable.Count; i++)
			{
				output.Write(i);
				output.Write('\t');
				output.Write(visitedObjectTable[i].ToString().Replace('\n', '|'));
				output.Write('\t');
				output.Write(visitedObjectTable[i].GetType());
				output.Write('\t');
				output.Write(objectSizeTable[i]);

				// Extra columns:
				if (i < typeStatistics.Length)
				{
					output.Write("\t\t\t");
					output.Write(typeStatistics[i].type);
					output.Write('\t');
					output.Write(typeStatistics[i].count);
					output.Write('\t');
					output.Write(typeStatistics[i].totalSize);
					output.Write('\t');
					output.Write(typeStatistics[i].totalSize / (double) typeStatistics[i].count);
				}

				output.WriteLine();
			}
		}

		#endregion


		#region Graphviz output of serialization

		private readonly List<object> definitionObjectTable;

		/// <summary>Write out a serialization table in DOT format (suitable for passing to Graphviz)</summary>
		/// <param name="output"></param>
		public void WriteSerializationGraph(StreamWriter output)
		{
			output.WriteLine("digraph Serialization {");

			output.WriteLine();
			output.WriteLine("\t// Visited Objects:");
			for (var i = 0; i < visitedObjectTable.Count; i++)
			{
				output.Write("\tObj");
				output.Write(i);
				output.Write(" [shape=box,label=\"");
				output.Write(visitedObjectTable[i].GetType());
				output.Write("\"]");
				output.WriteLine();
			}

			output.WriteLine();


			if (definitionObjectTable != null)
			{
				// Determine which definition objects are actually in use
				var definitionObjectsInUse = new bool[definitionObjectTable.Count];
				for (var i = 0; i < linkTable.Count; i++)
				{
					var toId = linkTable[i].to;
					if ((toId & Constants.DefinitionVisitFlag) != 0)
						definitionObjectsInUse[toId & ~Constants.DefinitionVisitFlag] = true;
				}

				output.WriteLine();
				output.WriteLine("\t// Definition Objects:");
				for (var i = 0; i < definitionObjectsInUse.Length; i++)
					if (definitionObjectsInUse[i])
					{
						output.Write("\tDef");
						output.Write(i);
						output.Write(" [shape=box,label=\"");
						output.Write(definitionObjectTable[i].GetType());
						output.Write("\",style=\"filled\",color=\".7 .3 1.0\"]");
						output.WriteLine();
					}

				output.WriteLine();
			}


			output.WriteLine();
			output.WriteLine("\t// Links (" + linkTable.Count + "):");
			for (var i = 0; i < linkTable.Count; i++)
			{
				var fromId = linkTable[i].from;
				var toId = linkTable[i].to;
				Debug.Assert((fromId & Constants.DefinitionVisitFlag) == 0);

				output.Write("\tObj");
				output.Write(fromId);
				output.Write(" -> ");

				if ((toId & Constants.DefinitionVisitFlag) != 0)
				{
					output.Write("Def");
					output.Write(toId & ~Constants.DefinitionVisitFlag);
				}
				else
				{
					output.Write("Obj");
					output.Write(toId);
				}

				output.WriteLine();
			}

			output.WriteLine();

			output.WriteLine("}");
		}


		public void WriteSerializationTypeGraph(StreamWriter output, List<Type> skipTypes = null)
		{
			output.WriteLine("digraph SerializationByType {");


			output.WriteLine();
			output.WriteLine("\t// Visited Object Types:");
			var visitedObjectTypes = visitedObjectTable.Select(o => o.GetType()).Distinct();
			foreach (var type in visitedObjectTypes)
			{
				if (skipTypes != null)
					foreach (var skipType in skipTypes)
						if (skipType.IsAssignableFrom(type))
							goto skip;

				output.Write("\t\"Obj ");
				output.Write(type);
				output.Write("\" [shape=box,label=\"");
				output.Write(type);
				output.Write("\"]");
				output.WriteLine();

				skip: ;
			}

			output.WriteLine();


			if (definitionObjectTable != null)
			{
				// Determine which definition object types are actually in use
				var definitionTypes = new HashSet<Type>();
				for (var i = 0; i < linkTable.Count; i++)
				{
					var toId = linkTable[i].to;
					if ((toId & Constants.DefinitionVisitFlag) != 0)
						definitionTypes.Add(definitionObjectTable[(int) (toId & ~Constants.DefinitionVisitFlag)]
							.GetType());
				}

				output.WriteLine();
				output.WriteLine("\t// Definition Object Types:");
				foreach (var type in definitionTypes)
				{
					if (skipTypes != null)
						foreach (var skipType in skipTypes)
							if (skipType.IsAssignableFrom(type))
								goto skip;

					output.Write("\t\"Def ");
					output.Write(type);
					output.Write("\" [shape=box,label=\"");
					output.Write(type);
					output.Write("\",style=\"filled\",color=\".7 .3 1.0\"]");
					output.WriteLine();

					skip: ;
				}

				output.WriteLine();
			}


			// Links between types:
			var linksAsText = new HashSet<string>();
			var sb = new StringBuilder();
			for (var i = 0; i < linkTable.Count; i++)
			{
				var fromId = linkTable[i].from;
				var toId = linkTable[i].to;
				Debug.Assert((fromId & Constants.DefinitionVisitFlag) == 0);

				Type fromType, toType;

				sb.Append("\t\"Obj ");
				sb.Append(fromType = visitedObjectTable[(int) fromId].GetType());
				sb.Append("\" -> \"");

				if ((toId & Constants.DefinitionVisitFlag) != 0)
				{
					sb.Append("Def ");
					sb.Append(toType = definitionObjectTable[(int) (toId & ~Constants.DefinitionVisitFlag)].GetType());
				}
				else
				{
					sb.Append("Obj ");
					sb.Append(toType = visitedObjectTable[(int) toId].GetType());
				}

				sb.Append("\"");

				if (skipTypes != null)
					foreach (var skipType in skipTypes)
						if (skipType.IsAssignableFrom(fromType) || skipType.IsAssignableFrom(toType))
							goto skip;

				linksAsText.Add(sb.ToString());

				skip:
				sb.Clear();
			}

			// Only output Distinct() links (otherwise the graph gets stupid)
			output.WriteLine();
			output.WriteLine("\t// Type Links:");
			foreach (var link in linksAsText)
				output.WriteLine(link);
			output.WriteLine();

			output.WriteLine("}");
		}

		#endregion
	}
}