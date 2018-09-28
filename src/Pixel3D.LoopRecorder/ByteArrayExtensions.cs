// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.IO;
using System.Text;

namespace Pixel3D.LoopRecorder
{
	public static class ByteArrayExtensions
	{
		public static void WriteLoopWithComment(this BinaryWriter loopWriter, byte[] saveState, Value128 definitionHash,
			string comment)
		{
			loopWriter.Write((byte) 'l');
			loopWriter.Write((byte) 'o');
			loopWriter.Write((byte) 'o');
			loopWriter.Write((byte) 'p');
			loopWriter.Write((byte) ' ');
			loopWriter.Write(Encoding.ASCII.GetBytes(comment));
			loopWriter.Write((byte) ' ');
			loopWriter.Write((byte) 0); // <- nul terminated string
			loopWriter.Write(definitionHash.v1);
			loopWriter.Write(definitionHash.v2);
			loopWriter.Write(definitionHash.v3);
			loopWriter.Write(definitionHash.v4);
			loopWriter.Write(saveState.Length);
			loopWriter.Write(saveState);
		}
	}
}