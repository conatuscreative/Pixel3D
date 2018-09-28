// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.IO;
using System.Reflection;
using Pixel3D.Serialization.Discovery.ReadIL;

namespace Pixel3D.Serialization.Discovery
{
	internal struct DelegateUsageInternal
	{
		public MethodBase instantiatingMethod;
		public long instantiationILOffset;

		public bool targetTypeKnown;
		public Type targetType;
		public MethodInfo delegateMethod;
		public Type delegateType;


		public void WriteInfo(StreamWriter writer, bool writeInstantiation = false)
		{
			var delegateTargetName = targetTypeKnown
				? (targetType != null ? instantiatingMethod.GetLocalNameFor(targetType) : "(null)")
				: "*** UNKNOWN TARGET! ***";
			var delegateMethodName = delegateMethod != null
				? instantiatingMethod.GetLocalNameFor(delegateMethod)
				: "*** UNKNOWN METHOD! ***";
			var delegateTypeName = instantiatingMethod.GetLocalNameFor(delegateType);

			if (writeInstantiation)
				writer.WriteLine("  at IL offset " + instantiationILOffset + " in method " + instantiatingMethod +
				                 " in type " + instantiatingMethod.DeclaringType);

			writer.WriteLine("    T = " + delegateTargetName);
			writer.WriteLine("    M = " + delegateMethodName);
			writer.WriteLine("    D = " + delegateTypeName);
		}
	}
}