// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Reflection;

namespace Pixel3D.Serialization.Discovery.ReadIL
{
	internal static class ResolveToStringExtensions
	{
		// TODO: one day it would be nice to spit out something that ILASM might accept.

		public static string GetLocalNameFor(this MethodBase fromMethod, MemberInfo memberInfo)
		{
			var declaringAssemblyString = memberInfo.Module != fromMethod.Module
				? "[" + memberInfo.Module.Assembly.GetName().Name + "]"
				: string.Empty;

			var declaringTypeString =
				memberInfo.DeclaringType != null && memberInfo.DeclaringType != fromMethod.DeclaringType
					? memberInfo.DeclaringType + ":: "
					: string.Empty;

			return declaringAssemblyString + declaringTypeString + memberInfo;
		}


		public static string ResolveMethodToString(this MethodBase method, int metadataToken)
		{
			return method.GetLocalNameFor(method.Module.ResolveMethod(metadataToken));
		}

		public static string ResolveFieldToString(this MethodBase method, int metadataToken)
		{
			return method.GetLocalNameFor(method.Module.ResolveField(metadataToken));
		}

		public static string ResolveTypeToString(this MethodBase method, int metadataToken)
		{
			return method.GetLocalNameFor(method.Module.ResolveType(metadataToken));
		}
	}
}