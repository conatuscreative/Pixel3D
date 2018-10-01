using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CRTSim
{
	public class DisplayPostProcessConfiguration
	{
		// TODO: Add a flag to indicate that the name is localized, and do a lookup for it in the string table
		public string name;

		//
		// Various effects and their parameters:
		
		public bool paletteEffect;
		public string palettePath;

		public bool analogueEffect;
		public float analogueSharp = 0.3f;
		public Vector4 analoguePersistence = new Vector4(0.7f, 0.525f, 0.42f, 0.0f);
		public float analogueBleed = 0.4f;
		public float analogueArtifacts = 0.2f;

		public ScreenEffect screenEffect;
		
		// TODO: Could put this in a config file
		public static readonly List<DisplayPostProcessConfiguration> builtIn = new List<DisplayPostProcessConfiguration>
		{
			new DisplayPostProcessConfiguration { name = "CRT", analogueEffect = true, screenEffect = ScreenEffect.CRT },
		    
			new DisplayPostProcessConfiguration { name = "LCD1", paletteEffect = true, palettePath = "Palettes/gb.bin", analogueEffect = false, screenEffect = ScreenEffect.LCD },
			new DisplayPostProcessConfiguration { name = "LCD2", paletteEffect = true, palettePath = "Palettes/gb.bin", analogueEffect = true, screenEffect = ScreenEffect.CRT },
			new DisplayPostProcessConfiguration { name = "LCD3", paletteEffect = true, palettePath = "Palettes/gb.bin", analogueEffect = false, screenEffect = ScreenEffect.None },

			new DisplayPostProcessConfiguration { name = "CGA1", paletteEffect = true, palettePath = "Palettes/cga.bin", analogueEffect = true, screenEffect = ScreenEffect.CRT },
			new DisplayPostProcessConfiguration { name = "CGA2", paletteEffect = true, palettePath = "Palettes/cga.bin", analogueEffect = false, screenEffect = ScreenEffect.None },

			new DisplayPostProcessConfiguration { name = "EGA1", paletteEffect = true, palettePath = "Palettes/ega.bin", analogueEffect = true, screenEffect = ScreenEffect.CRT },
			new DisplayPostProcessConfiguration { name = "EGA2", paletteEffect = true, palettePath = "Palettes/ega.bin", analogueEffect = false, screenEffect = ScreenEffect.None },

			//new DisplayPostProcessConfiguration { name = "test", paletteEffect = true, palettePath = "Palettes/eight.bin" }, // <- test not having a screen effect
			//new DisplayPostProcessConfiguration { name = "test2", analogueEffect = true }, // <- test not having a screen effect
		};
	}
}