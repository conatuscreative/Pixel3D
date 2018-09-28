// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;
using Pixel3D.Animations;

namespace Pixel3D
{
	public static class HeightmapInstructionExtensions
	{
		#region Serialization

		public static void Serialize(this List<HeightmapInstruction> instructions, AnimationSerializeContext context)
		{
			context.bw.Write(instructions != null);
			if (instructions != null)
			{
				context.bw.Write(instructions.Count);
				for (var i = 0; i < instructions.Count; i++)
					instructions[i].Serialize(context);
			}
		}

		public static List<HeightmapInstruction> DeserializeHeightmapInstructions(this AnimationDeserializeContext context)
		{
			if (context.br.ReadBoolean())
			{
				int count = context.br.ReadInt32();
				var instructions = new List<HeightmapInstruction>(count);
				for (var i = 0; i < count; i++)
					instructions.Add(new HeightmapInstruction(context));
				return instructions;
			}
			return null;
		}

		#endregion
	}
}