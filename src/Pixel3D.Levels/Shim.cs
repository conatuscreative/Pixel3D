using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;
using Pixel3D.Audio;
using Pixel3D.Extensions;

namespace Pixel3D.Engine.Levels
{
    // NOTE: "sealed" isn't really a hard requirement. Need to consider what to make virtual (eg: drawing) if making unsealed.

    [DebuggerDisplay("shim:{AnimationSet.EditorName}")]
    public sealed class Shim : IGameObjectDefinition, IDrawObject, IAmbientSoundSource
    {
        public AnimationSet AnimationSet { get; set; }

        public AABB? Bounds { get { return AnimationSet.AsAABB(Position, FacingLeft); } }
        Position IAmbientSoundSource.Position { get { return Position; } }
		public bool FacingLeft { get; set; }
	    public Position Position { get; set; }
		
		public float parallaxX;
        public float parallaxY;
        public int animationNumber = -1;
        public string ambientSoundSource;
        public int tag;

        public const int TagDay = 1;
        public const int TagNight = 2;
		
        // TODO: Convert this to null when not in use (it is hardly used at all) (could consider removing and using tag instead)
        /// <summary>Arbitrary thing properties (consumers are expected to parse the strings)</summary>
        public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();
		
	    public Shim(AnimationSet animationSet, Position position, bool facingLeft, float parallaxX, float parallaxY)
        {
            this.AnimationSet = animationSet;
            this.Position = position;
            this.FacingLeft = facingLeft;
            this.parallaxX = parallaxX;
            this.parallaxY = parallaxY;
            this.tag = 0;
        }

        public AmbientSound AmbientSound { get; set; }

        public int DirectionX
        {
            get { return FacingLeft ? -1 : 1; }
        }

        #region Masks

        public TransformedMaskData GetAlphaMask()
        {
            return AnimationSet.DefaultAnimation.Frames[0].masks.GetBaseFallback()
                .GetTransformedMaskData(Position, FacingLeft);
        }

        #endregion
		
        public DrawableFrame GetDrawableFrame()
        {
            return new DrawableFrame(AnimationSet.DefaultAnimation.Frames[0], Position, FacingLeft);
        }
		
        /// <summary>Implements IDrawObject.Draw</summary>
        public void Draw(DrawContext drawContext, int frameNumber, IDrawSmoothProvider sp)
        {
            AnimationSet.DefaultAnimation.Frames[frameNumber].Draw(drawContext, Position, FacingLeft, Color.White);
        }
    }
}