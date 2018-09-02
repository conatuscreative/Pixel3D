using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;
using Pixel3D.StateManagement;

namespace Pixel3D.Engine.Serialization
{
	public static class SerializeIgnoreStateManagement
	{
		#region Network Serialization

		// Don't want to serialize into the "State" object (mostly during type discovery)
		// because it will always be a definition object that is manually created using
		// "AllStateInstances". Instead, just link up the reference by calling "Walk".

		[CustomFieldSerializer]
		public static void SerializeField(SerializeContext context, BinaryWriter bw, StateProvider.State value)
		{
			context.Walk(value);
		}

		[CustomFieldSerializer]
		public static void DeserializeField(DeserializeContext context, BinaryReader br, ref StateProvider.State value)
		{
			context.Walk(ref value);
		}

		#endregion
	}
}
