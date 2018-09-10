// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Reflection;

namespace Pixel3D.Serialization.Static
{
	internal class StaticModuleTable
	{
		internal static Dictionary<Module, int> moduleToId;
		internal static List<Module> idToModule;

		/// <param name="modules">A list of modules that is sorted by a reproducible sort (order is network-sensitive)</param>
		internal static void SetModuleTable(List<Module> modules)
		{
			moduleToId = new Dictionary<Module, int>();
			idToModule = modules;

			for (var i = 0; i < modules.Count; i++) moduleToId.Add(modules[i], i);
		}
	}
}