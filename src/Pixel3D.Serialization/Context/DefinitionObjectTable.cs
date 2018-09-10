// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;

namespace Pixel3D.Serialization.Context
{
	public class DefinitionObjectTable
	{
		internal Dictionary<object, int> externalVisitedObjectIndices;

		internal List<object> visitedObjectTable;

		internal DefinitionObjectTable(List<object> visitedObjectTable,
			Dictionary<object, int> externalVisitedObjectIndices)
		{
			this.visitedObjectTable = visitedObjectTable;
			this.externalVisitedObjectIndices = externalVisitedObjectIndices;
		}

		public bool ContainsObject(object o)
		{
			return externalVisitedObjectIndices.ContainsKey(o);
		}
	}
}