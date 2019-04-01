// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Pixel3D.Audio;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.BuiltIn
{
	public static class SerializeIgnoreAudio
	{
		// "Ignore" serializer, as per SerializeIgnoreXNA -- we want to be able to store a ref for the definition table, but we can't deserialize a sound effect
		[CustomSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, SafeSoundEffect value) { context.VisitObject(value); context.LeaveObject(); }
		[CustomSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, SafeSoundEffect value) { throw new InvalidOperationException(); }

		// Outright block SoundEffect from serializing
		[CustomSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, SoundEffect value) { throw new InvalidOperationException(); }
		[CustomSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, SoundEffect value) { throw new InvalidOperationException(); }
	}
}