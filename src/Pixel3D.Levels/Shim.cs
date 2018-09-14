// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;
using Pixel3D.Audio;
using Pixel3D.Extensions;

namespace Pixel3D.Levels
{
	// NOTE: "sealed" isn't really a hard requirement. Need to consider what to make virtual (eg: drawing) if making unsealed.

	[DebuggerDisplay("shim:{AnimationSet.EditorName}")]
	public sealed class Shim : IGameObjectDefinition, IDrawObject, IAmbientSoundSource
	{
		public const int TagDay = 1;
		public const int TagNight = 2;

		// TODO: Convert this to null when not in use (it is hardly used at all) (could consider removing and using tag instead)
		/// <summary>Arbitrary thing properties (consumers are expected to parse the strings)</summary>
		public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();

		public string ambientSoundSource;
		public int animationNumber = -1;

		public float parallaxX;
		public float parallaxY;
		public int tag;

		public Shim(AnimationSet animationSet, Position position, bool facingLeft, float parallaxX, float parallaxY)
		{
			AnimationSet = animationSet;
			Position = position;
			FacingLeft = facingLeft;
			this.parallaxX = parallaxX;
			this.parallaxY = parallaxY;
			tag = 0;
		}

		public int DirectionX
		{
			get { return FacingLeft ? -1 : 1; }
		}

		public AABB? Bounds
		{
			get { return AnimationSet.AsAABB(Position, FacingLeft); }
		}

		Position IAmbientSoundSource.Position
		{
			get { return Position; }
		}

		public AmbientSound AmbientSound { get; set; }

		/// <summary>Implements IDrawObject.Draw</summary>
		public void Draw(DrawContext drawContext, int frameNumber, IDrawSmoothProvider sp)
		{
			AnimationSet.DefaultAnimation.Frames[frameNumber].Draw(drawContext, Position, FacingLeft, Color.White);
		}

		public AnimationSet AnimationSet { get; set; }
		public bool FacingLeft { get; set; }
		public Position Position { get; set; }

		public DrawableFrame GetDrawableFrame()
		{
			return new DrawableFrame(AnimationSet.DefaultAnimation.Frames[0], Position, FacingLeft);
		}

		#region Masks

		public TransformedMaskData GetAlphaMask()
		{
			return AnimationSet.DefaultAnimation.Frames[0].masks.GetBaseFallback()
				.GetTransformedMaskData(Position, FacingLeft);
		}

		#endregion
	}
}