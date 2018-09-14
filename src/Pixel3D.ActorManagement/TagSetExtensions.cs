// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.ActorManagement
{
	public static class TagSetExtensions
	{
		public static TagSet Flip(this TagSet @this)
		{
			var count = @this.Count;
			for (var i = 0; i < count; i++)
			{
				var tag = @this[i];
				if (tag == Symbols.Left || tag == Symbols.Right || tag == Symbols.Front || tag == Symbols.Back)
					return FlipHelper(@this, i);
			}

			return @this;
		}

		private static TagSet FlipHelper(TagSet @this, int index)
		{
			var tags = @this.ToArray();
			for (var i = index; i < tags.Length; i++)
				if (tags[i] == Symbols.Left)
					tags[i] = Symbols.Right;
				else if (tags[i] == Symbols.Right)
					tags[i] = Symbols.Left;
				else if (tags[i] == Symbols.Back)
					tags[i] = Symbols.Front;
				else if (tags[i] == Symbols.Front)
					tags[i] = Symbols.Back;
			return new TagSet(tags);
		}

		public static TagSet MaybeFlip(this TagSet @this, bool shouldFlip)
		{
			return shouldFlip ? Flip(@this) : @this;
		}
	}
}