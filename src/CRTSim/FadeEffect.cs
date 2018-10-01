// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CRTSim
{
    public class FadeEffect
    {
        public readonly Effect effect;
        public readonly Texture3D[] palettes;
        public readonly Vector4[] approximationValues;

        public readonly Texture3D red, darkred;
        public readonly Texture3D[] goofyPalettes;
		
        // Fade levels start at 1 and go to MaxFadeLevel (0 is no fading)
        public const int MaxFadeLevel = 5; 
		
        public FadeEffect(GraphicsDevice device, ContentManager content)
        {
            // Oh so many hard-coded paths...

            if(device.GraphicsProfile == GraphicsProfile.HiDef)
            {
                effect = content.Load<Effect>("FadeHiDef");
                palettes = new[] { null,
                        LoadFadePalette(device, @"Palettes\gs1.bin"),
                        LoadFadePalette(device, @"Palettes\gs2.bin"),
                        LoadFadePalette(device, @"Palettes\gs3.bin"),
                        LoadFadePalette(device, @"Palettes\gs4.bin"),
                        LoadFadePalette(device, @"Palettes\blk.bin") };

                red = LoadFadePalette(device, @"Palettes\red.bin");
                darkred = LoadFadePalette(device, @"Palettes\darkred.bin");

                goofyPalettes = new[] {
                        LoadFadePalette(device, @"Palettes\eight.bin"),
                        LoadFadePalette(device, @"Palettes\cga.bin"),
                        LoadFadePalette(device, @"Palettes\ega.bin"),
                        LoadFadePalette(device, @"Palettes\gb.bin") };
            }
            else
            {
                effect = content.Load<Effect>("FadeReach");
                // NOTE: These are hand-calibrated to somewhat match palette effect (-AR)
                //       (Could tweak the shader for better match by adding constrast effect. But probably not worth the effort for Reach. -AR)
                approximationValues = new[] { Color.White.ToVector4(),
                        new Color(250, 240, 250, 50).ToVector4(), // <- alpha channel = desaturate, higher than approximation of palette effect (~10), because it looks better
                        new Color(130, 140, 150, 4).ToVector4(),
                        new Color(100, 100, 100, 0).ToVector4(),
                        new Color(50, 50, 50, 0).ToVector4(),
                        Color.Black.ToVector4() };
            }
        }


        public static Texture3D LoadFadePalette(GraphicsDevice device, string path)
        {
#if !WINDOWS
            path = path.Replace('\\', '/');
#endif
            byte[] data = File.ReadAllBytes(path);

            // Figure out the size from the data length. Gracefully handle wrong-sized data (in case modders mess with it).
            int size = 1;
            while(size * size * size * 4 <= data.Length)
                size++;
            size--;
            Debug.Assert(size * size * size * 4 == data.Length); // <- Warn if the data is the wrong length

            Texture3D palette = new Texture3D(device, size, size, size, false, SurfaceFormat.Color);
            palette.SetData(data, 0, size * size * size * 4);
            return palette;
        }



        public void SetupForFadeLevel(int fadeLevel)
        {
            Debug.Assert(fadeLevel > 0 && fadeLevel <= MaxFadeLevel);

            var device = effect.GraphicsDevice;
            
            if(palettes != null)
            {
                device.Textures[1] = palettes[fadeLevel];
                device.SamplerStates[1] = SamplerState.PointClamp;
            }
            else
            {
                effect.Parameters["FadeAmount"].SetValue(approximationValues[fadeLevel]);
            }

        }


        public void SetCustomPalette(Texture3D palette, Color fallback)
        {
            SetCustomPalette(palette, fallback.ToVector4());
        }

        public void SetCustomPalette(Texture3D palette, Vector4 fallback)
        {
            var device = effect.GraphicsDevice;
            if(device.GraphicsProfile == GraphicsProfile.HiDef)
            {
                device.Textures[1] = palette;
                device.SamplerStates[1] = SamplerState.PointClamp;
            }
            else
            {
                effect.Parameters["FadeAmount"].SetValue(fallback);
            }
        }
    }
}
