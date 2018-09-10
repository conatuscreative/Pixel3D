// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Reflection;

namespace Pixel3D.Serialization.BuiltIn.DelegateHandling
{
	internal class DelegateTypeInfo
	{
		private readonly Dictionary<MethodInfo, int> methodIdLookup;
		internal List<DelegateMethodInfo> methodInfoList;

		/// <param name="methodInfoList">Must be in network-safe order!</param>
		internal DelegateTypeInfo(List<DelegateMethodInfo> methodInfoList)
		{
			this.methodInfoList = methodInfoList;
			methodIdLookup = new Dictionary<MethodInfo, int>();

			for (var i = 0; i < methodInfoList.Count; i++) methodIdLookup.Add(methodInfoList[i].method, i);
		}

		internal int GetIdForMethod(MethodInfo methodInfo)
		{
			return methodIdLookup[methodInfo];
		}

		internal DelegateMethodInfo GetMethodInfoForId(int methodId)
		{
			return methodInfoList[methodId];
		}
	}
}