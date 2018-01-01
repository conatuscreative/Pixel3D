using System;
using System.Diagnostics;
using Pixel3D.Animations;
using Pixel3D.Collections;

namespace Pixel3D.Levels
{
    [DebuggerDisplay("thing:{AnimationSet.EditorName}")]
    public class Thing : IEditorObject, IDrawObject
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

        #region Serialize

        public void Serialize(LevelSerializeContext context)
        {
            context.WriteAnimationSet(AnimationSet);

            context.bw.Write(Position);
            context.bw.Write(FacingLeft);

            context.bw.WriteNullableString(overrideBehaviour);

            context.bw.Write(includeInNavigation);

            // Properties
            {
                context.bw.Write(properties.Count);
                foreach(var kvp in properties)
                {
                    context.bw.Write(kvp.Key);
                    context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
                }
            }
        }

        /// <summary>Deserialize into new object instance</summary>
        public Thing(LevelDeserializeContext context)
        {
            AnimationSet = context.ReadAnimationSet();

            Position = context.br.ReadPosition();
            FacingLeft = context.br.ReadBoolean();

            overrideBehaviour = context.br.ReadNullableString();

            includeInNavigation = context.br.ReadBoolean();

            // Properties
            {
                int count = context.br.ReadInt32();
                for(int i = 0; i < count; i++)
                {
                    properties.Add(context.br.ReadString(), context.br.ReadString());
                }
            }
        }

        #endregion

        
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
