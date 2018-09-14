// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Diagnostics;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;

namespace Pixel3D.Levels
{
	[DebuggerDisplay("thing:{AnimationSet.EditorName}")]
	public class Thing : IGameObjectDefinition, IDrawObject
	{
		/// <summary>Arbitrary thing properties (consumers are expected to parse the strings)</summary>
		public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();


		public bool includeInNavigation;


		/// <summary>Optional name of the thing (intended for use by the LevelBehaviour)</summary>
		public string name;

		/// <summary>Use a behaviour not specified in the AnimationSet</summary>
		public string overrideBehaviour;

		public Thing(AnimationSet animationSet, Position position, bool facingLeft)
		{
			AnimationSet = animationSet;
			Position = position;
			FacingLeft = facingLeft;
		}


		/// <summary>The behaviour to use when spawning this Thing</summary>
		public string Behaviour
		{
			get { return overrideBehaviour ?? AnimationSet.behaviour ?? "Prop"; }
		}


		public AnimationSet AnimationSet { get; set; }
		public Position Position { get; set; }
		public bool FacingLeft { get; set; }


		#region ISortedDrawable Members (allows Things to be drawn in the editor)

		public void Draw(DrawContext drawContext, int tag, IDrawSmoothProvider sp)
		{
			AnimationSet.DefaultAnimation.Frames[0].Draw(drawContext, Position, FacingLeft);
		}

		public DrawableFrame GetDrawableFrame()
		{
			return new DrawableFrame(AnimationSet.DefaultAnimation.Frames[0], Position, FacingLeft);
		}

		#endregion
	}
}