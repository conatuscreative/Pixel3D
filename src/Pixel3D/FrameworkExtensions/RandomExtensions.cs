namespace System
{
    public static class RandomExtensions
    {
        public static bool NextBoolean(this Random random)
        {
            return random.Next(1) != 0;
        }
    }
}
