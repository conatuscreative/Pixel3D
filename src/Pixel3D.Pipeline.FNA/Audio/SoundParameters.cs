// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pixel3D.Pipeline.Audio
{
	public struct SoundParameters
	{
		public int loopStart, loopLength;

		public static SoundParameters TryReadFromFile(string path, int sampleCount)
		{
			var result = new SoundParameters();

			if (File.Exists(path))
			{
				var lines = new Queue<string>(File.ReadLines(path).Where(l => !l.Trim().StartsWith("#")));

				while (lines.Count > 0)
				{
					string command;
					switch (command = lines.Dequeue().ToLowerInvariant().Trim())
					{
						case "loop start":
							if (!int.TryParse(lines.Dequeue().Trim(), out result.loopStart) || result.loopStart < 0 ||
							    result.loopStart >= sampleCount)
								throw new Exception("ERROR: Bad loop start value (in file " + path + ")");
							break;

						case "loop length":
							if (!int.TryParse(lines.Dequeue().Trim(), out result.loopLength) || result.loopLength < 0 ||
							    result.loopLength >= sampleCount)
								throw new Exception("ERROR: Bad loop length value (in file " + path + ")");
							break;

						case "loop end": // must come after loop start
						{
							int loopEnd;
							if (!int.TryParse(lines.Dequeue().Trim(), out loopEnd))
								throw new Exception("ERROR: Bad loop end value (in file " + path + ")");
							result.loopLength = loopEnd - result.loopStart;
						}
							break;

						case "":
							break;

						default:
							Console.WriteLine("Unknown sound parameter \"" + command + "\" (in file " + path + ")");
							break;
					}
				}
			}

			if (result.loopStart < 0 || result.loopStart >= sampleCount)
				throw new Exception("ERROR: Bad loop start position (in file " + path + ")");

			if (result.loopLength < 0 || result.loopLength + result.loopStart > sampleCount)
				throw new Exception("ERROR: Bad loop end position (in file " + path + ")");

			// XNA doesn't do this for us...
			if (result.loopStart != 0 && result.loopLength == 0) result.loopLength = sampleCount - result.loopStart;

			return result;
		}
	}
}