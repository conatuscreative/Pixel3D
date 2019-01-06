// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using Microsoft.Xna.Framework;

namespace Pixel3D
{
    public static class DebugColors
    {
        #region Pretty Colours

        public static Color ColorFromHSV(float hue, float saturation, float value)
        {
            float h = hue / 60f;
            float c = value * saturation; // chroma
            float x = c * (1f - Math.Abs((h%2f) - 1f));
            float m = value - c;
            switch((int)Math.Floor(h))
            {
                case 0: return new Color(c + m, x + m, m);
                case 1: return new Color(x + m, c + m, m);
                case 2: return new Color(m, c + m, x + m);
                case 3: return new Color(m, x + m, c + m);
                case 4: return new Color(x + m, m, c + m);
                case 5: return new Color(c + m, m, x + m);
                default: return Color.Transparent;
            }
        }

        public static Color[] GetFillColors(int count)
        {
            Color[] output = new Color[count];

            const int c = 11; // <- hue count per layer
            int layers = (count / c) + 1;

            for(int i = 0; i < count; i++)
            {
                int layer = (i / layers);

                float hue = ((float)(i%c) / (float)c)  +  ((float)(i/c) / (float)layers) * (1f/(float)c); // Spiral hues

                float value = ((float)(i/c) / (float)layers);

                output[i] = ColorFromHSV(hue * 360f, 0.8f + value * 0.2f, 1f - value*0.5f);
            }

            // Shuffle:
            Random random = new Random(0); // constant seed for consistant results
            for(int i = 0; i < output.Length; i++)
            {
                int j = random.Next(i, output.Length);
                Color temp = output[i];
                output[i] = output[j];
                output[j] = temp;
            }

            return output;
        }

        #endregion

    }
}
