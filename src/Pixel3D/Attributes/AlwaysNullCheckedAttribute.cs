using System;

namespace Pixel3D.Attributes
{
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class AlwaysNullCheckedAttribute : Attribute
	{
	}
}