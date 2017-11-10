namespace Pixel3D.Engine
{
    public static class TagSetExtensions
    {
        public static TagSet Flip(this TagSet @this)
        {
            int count = @this.Count;
            for (int i = 0; i < count; i++)
            {
                string tag = @this[i];
                if (tag == Symbols.Left || tag == Symbols.Right || tag == Symbols.Front || tag == Symbols.Back)
                    return FlipHelper(@this, i);
            }
            return @this;
        }

        private static TagSet FlipHelper(TagSet @this, int index)
        {
            string[] tags = @this.ToArray();
            for (int i = index; i < tags.Length; i++)
            {
                if (tags[i] == Symbols.Left)
                    tags[i] = Symbols.Right;
                else if (tags[i] == Symbols.Right)
                    tags[i] = Symbols.Left;
                else if (tags[i] == Symbols.Back)
                    tags[i] = Symbols.Front;
                else if (tags[i] == Symbols.Front)
                    tags[i] = Symbols.Back;
            }
            return new TagSet(tags);
        }

        public static TagSet MaybeFlip(this TagSet @this, bool shouldFlip)
        {
            return shouldFlip ? Flip(@this) : @this;
        }
    }
}