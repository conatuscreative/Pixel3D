// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.Serialization.Generator
{
	internal static class DirectionExtensionMethods
	{
		private static readonly string[] names = {"Serialize", "Deserialize"};

		public static string Name(this Direction direction)
		{
			return names[(int) direction];
		}
	}
}