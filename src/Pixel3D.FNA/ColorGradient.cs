using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pixel3D
{
    public static class ColorGradientExtension
    {
        public static readonly SortedList<int, Color> defaultHeightGradient = new SortedList<int, Color>
        {
            { -128, Color.Black },
            { -1, Color.Lerp(Color.Magenta, Color.Black, 0.5f) },

            { 0, Color.Magenta }, { 128, Color.Cyan }, { 256, Color.Yellow }, { 384, Color.Red }, { 512, Color.Blue }, { 1024, Color.White },
        };


        public static readonly SortedList<int, Color> zoneHeightGradient = new SortedList<int, Color>
        {
            { -128, Color.Black },
            { -1, Color.Lerp(Color.Yellow, Color.Black, 0.5f) },

            { 0, Color.Yellow }, { 128, Color.Orange }, { 256, Color.Red }, { 384, Color.Violet }, { 512, Color.Cyan }, { 1024, Color.White },
        };

        public static readonly SortedList<int, Color> zoneHeightGradientAlt = new SortedList<int, Color>
        {
            { -128, Color.Black },
            { -1, Color.Lerp(Color.DarkGreen, Color.Black, 0.5f) },

            { 0, Color.DarkGreen }, { 128, Color.Green }, { 256, Color.GreenYellow }, { 384, Color.YellowGreen }, { 512, Color.Yellow }, { 1024, Color.White },
        };


        public static readonly SortedList<int, Color> dangerZoneGradient = new SortedList<int, Color>
        {
            { 0, Color.Red },
            { 15, Color.Orange },
            { 30, Color.Yellow },
            { 60, Color.Lime },
            { 254, Color.Blue },
            { 255, Color.Black * 0.4f },
        };


        public static Color GetColorFromGradient(this SortedList<int, Color> gradient, int position)
        {
            if(gradient.Count == 0)
                return Color.Transparent;

            if(position < gradient.Keys[0])
                return gradient.Values[0];

            for(int i = 1; i < gradient.Keys.Count; i++)
            {
                if(position < gradient.Keys[i])
                {
                    int v = position - gradient.Keys[i-1];
                    int c = gradient.Keys[i] - gradient.Keys[i-1];

                    return Color.Lerp(gradient.Values[i-1], gradient.Values[i], (float)v / (float)c);
                }
            }

            return gradient.Values[gradient.Count-1];
        }
    }
}
