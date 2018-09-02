using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.Animations;
using Pixel3D.Audio;
using Pixel3D.Engine.Audio;

namespace Pixel3D.Engine.Levels
{
    // NOTE: "sealed" isn't really a hard requirement. Need to consider what to make virtual (eg: drawing) if making unsealed.

    [DebuggerDisplay("shim:{AnimationSet.EditorName}")]
    public sealed class Shim : IEditorObject, IDrawObject, IAmbientSoundSource
    {
        public AnimationSet AnimationSet { get; set; }
        AudioPosition IAmbientSoundSource.Position => Position.AsAudioPosition();
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
        public readonly Collections.OrderedDictionary<string, string> properties = new Collections.OrderedDictionary<string, string>();
		
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

        #region Serialization

        public void Serialize(LevelSerializeContext context)
        {
            context.WriteAnimationSet(AnimationSet);
            context.bw.Write(Position);
            context.bw.Write(FacingLeft);
            context.bw.Write(parallaxX);
            context.bw.Write(parallaxY);
            context.bw.Write(animationNumber);
            context.bw.WriteNullableString(ambientSoundSource);

            if(context.Version >= 14)
                context.bw.Write(tag);

            if (context.Version >= 16)
            {
                context.bw.Write(properties.Count);
                foreach (var kvp in properties)
                {
                    context.bw.Write(kvp.Key);
                    context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
                }
            }
        }

        public Shim(LevelDeserializeContext context)
        {
            AnimationSet = context.ReadAnimationSet();
            Position = context.br.ReadPosition();
            FacingLeft = context.br.ReadBoolean();
            parallaxX = context.br.ReadSingle();
            parallaxY = context.br.ReadSingle();
            animationNumber = context.br.ReadInt32();
            ambientSoundSource = context.br.ReadNullableString();

            if (context.Version >= 14)
                tag = context.br.ReadInt32();

            if (context.Version >= 16)
            {
                int count = context.br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    properties.Add(context.br.ReadString(), context.br.ReadString());
                }
            }
        }

        #endregion

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