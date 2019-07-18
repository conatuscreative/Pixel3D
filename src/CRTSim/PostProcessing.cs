using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CRTSim
{
	public class PostProcessing
	{
		private readonly IPostProcessor processor;

		private readonly Dictionary<string, Palette> loadedPalettes = new Dictionary<string, Palette>();
		private Palette postProcessCurrentPalette;

		private Effect analogueEffect, crtEffect, lcdEffect;
		private Texture2D analogueArtifacts, crtMask, lcdMask;

		private bool analogueOddFrame;
		private RenderTarget2D analogueEven, analogueOdd;
		private RenderTarget2D upScaleRenderTarget;

		public PostProcessing(IPostProcessor processor)
		{
			this.processor = processor;
		}

		#region Display Post-Process

		private readonly List<DisplayPostProcessConfiguration> postProcessEffectsList = DisplayPostProcessConfiguration.builtIn; // <- TODO: Load these from a file?

		public DisplayPostProcessConfiguration postProcess;

		public void SetPostProcessTo(sbyte index)
		{
			if (index == -1 || postProcessEffectsList.Count == 0 || processor.GraphicsDevice.GraphicsProfile == GraphicsProfile.Reach)
				postProcess = null;
			else if (postProcess == null)
				postProcess = postProcessEffectsList[0];
			else
				postProcess = postProcessEffectsList[index];

			processor.UpdateCameraWindows();
			LoadPostProcessEffects();
		}

		public void NextPostProcessConfig()
		{
			if (postProcessEffectsList.Count == 0 || processor.GraphicsDevice.GraphicsProfile == GraphicsProfile.Reach)
				postProcess = null;
			else if (postProcess == null)
				postProcess = postProcessEffectsList[0];
			else
			{
				int index = postProcessEffectsList.IndexOf(postProcess) + 1;
				if (index == postProcessEffectsList.Count)
					postProcess = null;
				else
					postProcess = postProcessEffectsList[index];
			}

			processor.UpdateCameraWindows();
			LoadPostProcessEffects();
		}

		public void PreviousPostProcessConfig()
		{
			if (postProcessEffectsList.Count == 0 || processor.GraphicsDevice.GraphicsProfile == GraphicsProfile.Reach)
				postProcess = null;
			else if (postProcess == null)
				postProcess = postProcessEffectsList[postProcessEffectsList.Count - 1];
			else
			{
				int index = postProcessEffectsList.IndexOf(postProcess);
				if (index == 0) // was first
					postProcess = null;
				else if (index == -1) // was unknown (treat as null)
					postProcess = postProcessEffectsList[postProcessEffectsList.Count - 1];
				else
					postProcess = postProcessEffectsList[index - 1];
			}

			processor.UpdateCameraWindows();
			LoadPostProcessEffects();
		}

		private void LoadPostProcessEffects()
		{
			// Just load post-process required files on-the-fly

			if (postProcess == null)
				return;

			postProcessCurrentPalette = null;
			if (postProcess.paletteEffect && postProcess.palettePath != null)
			{
				if (!loadedPalettes.TryGetValue(postProcess.palettePath, out postProcessCurrentPalette))
				{
					try
					{
						var palette = Palette.Load(processor.GraphicsDevice, postProcess.palettePath);
						loadedPalettes.Add(postProcess.palettePath, palette);
						postProcessCurrentPalette = palette;
					}
					catch (Exception e)
					{
						Trace.TraceError("Failed to load palette: " + e);
					}
				}
			}

			if (postProcess.analogueEffect)
			{
				analogueEffect = processor.Content.Load<Effect>("Display/analogue");
				analogueArtifacts = processor.Content.Load<Texture2D>("Display/artifacts");

				analogueEffect.Parameters["Tuning_Sharp"].SetValue(postProcess.analogueSharp);
				analogueEffect.Parameters["Tuning_Persistence"].SetValue(postProcess.analoguePersistence);
				analogueEffect.Parameters["Tuning_Bleed"].SetValue(postProcess.analogueBleed);
				analogueEffect.Parameters["Tuning_Artifacts"].SetValue(postProcess.analogueArtifacts);
				analogueEffect.Parameters["NTSCArtifactTex"].SetValue(analogueArtifacts);
				analogueEffect.Parameters["NTSCLerp"].SetValue(0.5f);
			}
			else
			{
				// Lazy way to force-clear these when changing effects
				if (analogueEven != null)
					analogueEven.Dispose();
				analogueEven = null;
				if (analogueOdd != null)
					analogueOdd.Dispose();
				analogueOdd = null;
			}


			if (postProcess.screenEffect == ScreenEffect.CRT)
			{
				crtEffect = processor.Content.Load<Effect>("Display/crt");
				crtMask = processor.Content.Load<Texture2D>("Display/crtmask");
			}

			if (postProcess.screenEffect == ScreenEffect.LCD)
			{
				lcdEffect = processor.Content.Load<Effect>("Display/lcd");
				lcdMask = processor.Content.Load<Texture2D>("Display/lcdmask");
			}
		}

		#endregion

		public void Render(
			int renderContentZoom,
			Point renderSize,
			FadeEffect fadeEffect,
			SpriteBatch sb, 
			GraphicsDevice graphicsDevice,
			ref RenderTarget2D inputRenderTarget, 
			ref RenderTarget2D outputRenderTarget)
		{
			Viewport vp = graphicsDevice.Viewport;

			if (postProcess != null && postProcess.paletteEffect && postProcessCurrentPalette != null)
			{
				SwapRenderTargets(ref outputRenderTarget, ref inputRenderTarget);
				graphicsDevice.SetRenderTarget(outputRenderTarget);

				fadeEffect.SetCustomPalette(postProcessCurrentPalette.palette, Color.White);
				sb.Begin(0, null, SamplerState.PointClamp, null, null, fadeEffect.effect);
				sb.Draw(inputRenderTarget, Vector2.Zero, Color.White);
				sb.End();
			}

			if (postProcess != null && postProcess.analogueEffect)
			{
				analogueOddFrame = !analogueOddFrame;

				graphicsDevice.LazySetupRenderTarget(ref analogueEven, renderSize, Color.Black);
				graphicsDevice.LazySetupRenderTarget(ref analogueOdd, renderSize, Color.Black);

				var analoguePrevious = analogueOddFrame ? analogueEven : analogueOdd;
				var analogueCurrent = analogueOddFrame ? analogueOdd : analogueEven;

				graphicsDevice.SetRenderTarget(analogueCurrent);
				graphicsDevice.Clear(Color.Black);

				analogueEffect.Parameters["RcpScrWidth"].SetValue(new Vector2(1f / outputRenderTarget.Width, 0));
				analogueEffect.Parameters["RcpScrHeight"].SetValue(new Vector2(0, 1f / outputRenderTarget.Height));

				analogueEffect.Parameters["curFrameMap"].SetValue(outputRenderTarget);
				analogueEffect.Parameters["prevFrameMap"].SetValue(analoguePrevious);

				sb.Begin(0, null, null, null, null, analogueEffect);
				sb.Draw(outputRenderTarget, Vector2.Zero, Color.White);
				sb.End();

				// NOTE: This fixes a bug in XNA where it cannot deal with recreating render targets that are set on an effect, when it loses the graphics device
				//       (And we can't deal with the full-reload fallback path that XNA triggers when it fails)
				analogueEffect.Parameters["curFrameMap"].SetValue((Texture2D)null);
				analogueEffect.Parameters["prevFrameMap"].SetValue((Texture2D)null);

				outputRenderTarget = analogueCurrent; // <- IMPORTANT: Can't do any swaps of input/output targets from here on (this effect depends on bouncing these!)
			}

			ScreenEffect screenEffect = postProcess != null ? postProcess.screenEffect : ScreenEffect.None;
			switch (screenEffect)
			{
				default:
				case ScreenEffect.None:
				{
					graphicsDevice.SetRenderTarget(null);
					graphicsDevice.Clear(Color.Cyan);
					sb.Begin(0, null, SamplerState.PointClamp, null, null, null);
					sb.Draw(outputRenderTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, renderContentZoom, 0, 0);
					sb.End();
					break;
				}

				case ScreenEffect.CRT:
				{
					// Up-scale
					graphicsDevice.LazySetupRenderTarget(ref upScaleRenderTarget, outputRenderTarget.Width * 2, outputRenderTarget.Height * 2);
					graphicsDevice.SetRenderTarget(upScaleRenderTarget);
					sb.Begin(0, null, SamplerState.PointClamp, null, null);
					sb.Draw(outputRenderTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2, 0, 0);
					sb.End();

					graphicsDevice.SetRenderTarget(null);
					graphicsDevice.Clear(Color.Black);

					graphicsDevice.Textures[1] = upScaleRenderTarget;
					graphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;
					graphicsDevice.Textures[2] = crtMask; // <- NOTE: Should be mipped!
					graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;
					crtEffect.Parameters["textureSize"].SetValue(new Vector2(outputRenderTarget.Width, outputRenderTarget.Height));
					crtEffect.Parameters["ViewProjection"].SetValue(Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1));

					double targetAspect = (double)outputRenderTarget.Width / (double)outputRenderTarget.Height; // <- double may be overkill
					double screenAspect = (double)vp.Width / (double)vp.Height;
					Vector2 aspectScale = screenAspect > targetAspect ? new Vector2((float)(screenAspect / targetAspect), 1f) : new Vector2(1f, (float)(targetAspect / screenAspect));
					crtEffect.Parameters["aspectScale"].SetValue(aspectScale * 2.2f);

					sb.Begin(0, null, SamplerState.PointClamp, null, null, crtEffect);
					sb.Draw(outputRenderTarget, vp.Bounds, Color.White);
					sb.End();
					break;
				}

				case ScreenEffect.LCD:
				{
					graphicsDevice.SetRenderTarget(null);
					graphicsDevice.Clear(postProcessCurrentPalette != null ? postProcessCurrentPalette.white : Color.Black);

					graphicsDevice.Textures[1] = lcdMask; // <- NOTE: Should be mipped!
					graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
					lcdEffect.Parameters["textureSize"].SetValue(new Vector2(outputRenderTarget.Width, outputRenderTarget.Height));

					int scaleX = vp.Width / outputRenderTarget.Width;
					int scaleY = vp.Height / outputRenderTarget.Height;
					int scale = Math.Max(1, Math.Min(scaleX, scaleY));
					int width = outputRenderTarget.Width * scale;
					int height = outputRenderTarget.Height * scale;
					Rectangle destination = new Rectangle(vp.Width / 2 - width / 2, vp.Height / 2 - height / 2, width, height);

					sb.Begin(0, null, SamplerState.PointClamp, null, null, lcdEffect);
					sb.Draw(outputRenderTarget, destination, Color.White);
					sb.End();
					break;
				}
			}
		}

		public static void SwapRenderTargets(ref RenderTarget2D one, ref RenderTarget2D two)
		{
			RenderTarget2D temp = one;
			one = two;
			two = temp;
		}
	}
}