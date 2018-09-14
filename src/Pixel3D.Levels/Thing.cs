using System.Diagnostics;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;

namespace Pixel3D.Engine.Levels
{
    [DebuggerDisplay("thing:{AnimationSet.EditorName}")]
    public class Thing : IGameObjectDefinition, IDrawObject
    { 
        public Thing(AnimationSet animationSet, Position position, bool facingLeft)
        {
            this.AnimationSet = animationSet;
            this.Position = position;
            this.FacingLeft = facingLeft;
        }


        public AnimationSet AnimationSet { get; set; }
        public Position Position { get; set; }
        public bool FacingLeft { get; set; }

        
        /// <summary>Optional name of the thing (intended for use by the LevelBehaviour)</summary>
        public string name;

        /// <summary>Use a behaviour not specified in the AnimationSet</summary>
        public string overrideBehaviour;

        /// <summary>Arbitrary thing properties (consumers are expected to parse the strings)</summary>
        public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();


        /// <summary>The behaviour to use when spawning this Thing</summary>
        public string Behaviour { get { return overrideBehaviour ?? AnimationSet.behaviour ?? "Prop"; } }


        public bool includeInNavigation;

        
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
