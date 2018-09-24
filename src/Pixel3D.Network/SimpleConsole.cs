// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pixel3D.Network
{
	public class SimpleConsole
	{
		private double currentTime;

		private readonly List<Line> lines = new List<Line>();

		public void Update(double elapsed)
		{
#if Retail
            return;
#endif
			currentTime += elapsed;
		}

		public void WriteLine(string text)
		{
#if Retail
            return;
#endif
			WriteLine(text, Color.Black);
		}

		public void WriteLine(string text, Color color)
		{
#if Retail
            return;
#endif
			Debug.WriteLine(text);
			foreach (var s in text.Split('\n')) lines.Add(new Line {text = s, timeAdded = currentTime, color = color});
		}


		public void Draw(SpriteBatch sb, SpriteFont font, Texture2D whitePixel, bool noFade)
		{
#if Retail
            return;
#endif
			var fadeStartTime = currentTime - 2;
			const double fadeOutTime = 0.5;
			var fadeEndTime = fadeStartTime - fadeOutTime;

			var ks = Keyboard.GetState();

			var cursor = new Vector2(2, sb.GraphicsDevice.Viewport.Height - font.LineSpacing);

			sb.Begin();

			// Aid readability:
			if (noFade) sb.Draw(whitePixel, sb.GraphicsDevice.Viewport.Bounds, Color.White * 0.75f);

			// Draw console text
			for (var i = lines.Count - 1; i >= 0; i--)
			{
				// If we're too far up the page
				if (cursor.Y + font.LineSpacing < 0)
					break;

				// If we're too old
				if (!noFade && lines[i].timeAdded < fadeEndTime)
					break;

				var alpha = 1f;
				if (!noFade && lines[i].timeAdded < fadeStartTime)
					alpha = 1.0f - (float) ((fadeStartTime - lines[i].timeAdded) / fadeOutTime);

				var size = font.MeasureString(lines[i].text);
				sb.Draw(whitePixel, cursor - new Vector2(2, 0), null, Color.White * 0.8f * alpha, 0, Vector2.Zero,
					size + new Vector2(4, 0), 0, 0);
				sb.DrawString(font, lines[i].text, cursor, lines[i].color * alpha);

				cursor.Y -= font.LineSpacing;
			}

			sb.End();
		}

		private struct Line
		{
			public string text;
			public double timeAdded;
			public Color color;
		}
	}
}