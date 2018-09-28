// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using System.IO;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D
{
	public static class CustomSerialization
	{
		#region SpriteRef

		// Definition-only (TODO: Maybe we should care that the sprite references match up between players?)
		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref SpriteRef value) { }

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref SpriteRef value)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region Sprite

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref Sprite value)
		{
			// NOTE: Not visiting the texture object, because it could be deferred (so definitions can't know about it)
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref Sprite value)
		{
			Debug.Assert(false); // Shouldn't happen! (Can't store Sprite in game state)
			throw new InvalidOperationException();
		}

		#endregion
	}
}