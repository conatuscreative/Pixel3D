// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;

namespace Pixel3D.Levels
{
	public class Path
	{
		/// <summary>Arbitrary level properties (consumers are expected to parse the strings)</summary>
		public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();

		public bool looped;

		public List<LevelPosition> positions = new List<LevelPosition>();

		// Provided to allow parameterless construction (due to presence of deserialization constructor)
	}
}