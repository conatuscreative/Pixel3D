using System;

namespace Pixel3D.StateManagement
{
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class AlwaysNullCheckedAttribute : Attribute
	{
	}
}