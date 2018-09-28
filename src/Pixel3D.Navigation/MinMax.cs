// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;

namespace Pixel3D.Navigation
{
	public struct MinMax
	{
		public int min, max;

		public MinMax(int min, int max)
		{
			this.min = min;
			this.max = max;
		}


		public static bool Clip(ref MinMax current, MinMax next)
		{
			// NOTE: This solves most but not all slop issues (needs more testing)
			// NOTE: Arbitrary value - should perhaps depend on movement speed (but this is ok for both walking and running)
			const int cornerSlop = 3;

			if (next.max <= current.min)
			{
				current.max = Math.Min(current.min + cornerSlop, current.max);
				return false;
			}

			if (next.min >= current.max)
			{
				current.min = Math.Max(current.max - cornerSlop, current.min);
				return false;
			}

			if (next.max < current.max)
				current.max = next.max;
			if (next.min > current.min)
				current.min = next.min;

			return true;
		}
	}
}