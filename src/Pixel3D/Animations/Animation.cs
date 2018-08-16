using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Common;
using Microsoft.Xna.Framework;
using Pixel3D.Serialization;

namespace Pixel3D.Animations
{
	[DebuggerDisplay("animation:{friendlyName} ({FrameCount} frames) looped:{isLooped}")]
    public class Animation : IEditorNameProvider
    {
        public Animation(bool isLooped = true)
        {
            this.isLooped = isLooped;
            Frames = new List<AnimationFrame>();
        }


        /// <summary>The number of frames in this animation</summary>
        [Browsable(false)]
        public int FrameCount { get { return Frames.Count; } }

        public bool isLooped;
        
        /// <summary>The drawable bounds of this animation (Regenerated at save time, use in the game, not the editor)</summary>
        public Bounds cachedBounds;

        public Bounds GetBoundsInWorld(Position position, bool flipX)
        {
            return (flipX ? cachedBounds.FlipX() : cachedBounds) + position.ToWorldZero;
        }


        public List<AnimationFrame> Frames { get; private set; }


        /// <summary>Get the maximum world space bounds of all frames in this animation. EDITOR ONLY!</summary>
        public Rectangle CalculateGraphicsBounds()
        {
            Rectangle maxBounds = Rectangle.Empty;
            foreach(var frame in Frames)
            {
                maxBounds = RectangleExtensions.UnionIgnoreEmpty(maxBounds, frame.CalculateGraphicsBounds());
            }
            return maxBounds;
        }


        /// <summary>EDITOR ONLY!</summary>
        public Rectangle GetSoftRenderBounds(bool accountForGameplayMotion)
        {
            Rectangle output = Rectangle.Empty;
            Position accumulatedMotion = Position.Zero;
            foreach(var frame in Frames)
            {
                Rectangle bounds = frame.GetSoftRenderBounds();

                if(accountForGameplayMotion)
                {
                    accumulatedMotion += frame.positionDelta;

                    Point offset = accumulatedMotion.ToWorldZero.FlipY(); // Flip because we're outputting in texture space
                    bounds.X += offset.X;
                    bounds.Y += offset.Y;
                }

                output = RectangleExtensions.UnionIgnoreEmpty(output, bounds);
            }
            return output;
        }

        public string cue;

        public bool preventDropMotion;

        #region Serialize

        public void Serialize(AnimationSerializeContext context)
        {
            context.bw.Write(isLooped);
            context.bw.WriteNullableString(friendlyName);

            context.bw.Write(Frames.Count);
            for(int i = 0; i < Frames.Count; i++)
            {
                Frames[i].Serialize(context);
            }


            if(!context.monitor)
                cachedBounds = new Bounds(CalculateGraphicsBounds());
            if(context.Version >= 35)
                context.bw.Write(cachedBounds);


            context.bw.Write(isShared);

            context.bw.WriteNullableString(cue);

            context.bw.WriteBoolean(preventDropMotion);
        }
        
        /// <summary>Deserialize into new object instance</summary>
        public Animation(AnimationDeserializeContext context)
        {
            isLooped = context.br.ReadBoolean();
            friendlyName = context.br.ReadNullableString();
            
            int frameCount = context.br.ReadInt32();
            Frames = new List<AnimationFrame>(frameCount);
            for(int i = 0; i < frameCount; i++)
            {
                Frames.Add(new AnimationFrame(context));
            }

            if(context.Version >= 35)
                cachedBounds = context.br.ReadBounds();
            // NOTE: Had to remove call to CalculateGraphicsBounds for old sprites (because we can't get that data at load time in the engine). Time to do a full rewrite.

            isShared = context.br.ReadBoolean();

            cue = context.br.ReadNullableString();

            preventDropMotion = context.br.ReadBoolean();
        }

        #endregion



        #region Editor Stuff

        /// <summary>IMPORTANT: Do not use in gameplay code (not network safe)</summary>
        [SerializationIgnore]
        public string friendlyName;

        public string EditorName { get { return friendlyName; } }

        /// <summary>EDITOR ONLY. Surrogate property for tag-setting in the UI; do not carry over!</summary>
        [SerializationIgnore]
        public string tags;

        /// <summary> Tells the editor the animation is shared; for batch sync operations </summary>
        public bool isShared;




        public void AdjustFrameOrigin(int frameNumber, Point p)
        {
            var frame = Frames[frameNumber];

            Position pos = new Position(p.X, p.Y, 0);

            if(frameNumber != 0 || this.isLooped) // <- This is almost always what we want...
                frame.positionDelta += pos;

            frame.shadowOffset.X -= p.X;

            foreach(var mask in frame.masks.Values)
            {
                mask.data.OffsetX -= p.X;
                mask.data.OffsetY -= p.Y;
            }

            if(frame.incomingAttachments != null)
            {
                var values = frame.incomingAttachments.Values;
                for(int i = 0; i < values.Count; i++)
                {
                    Position ia = values[i];
                    ia.X -= p.X;
                    ia.Y -= p.Y;
                    values.EditorReplaceValue(i, ia);
                }
            }

            if(frame.outgoingAttachments != null)
            {
                foreach(var oa in frame.outgoingAttachments.Values)
                {
                    oa.position.X -= p.X;
                    oa.position.Y -= p.Y;
                }
            }

            foreach(var cel in frame.layers)
            {
                var sprite = cel.spriteRef.ResolveRequire();
                sprite.origin = new Point(sprite.origin.X + p.X, sprite.origin.Y - p.Y);
                cel.spriteRef = new SpriteRef(sprite);
            }


            int nextFrame = frameNumber + 1;
            if(nextFrame >= this.FrameCount && this.isLooped)
            {
                nextFrame = 0;
            }
            if(nextFrame < this.FrameCount)
            {
                // Adjust the next frame to make it line up...
                this.Frames[nextFrame].positionDelta -= pos;
            }
        }

        #endregion



        public static Animation CreateSingleSprite(Sprite sprite)
        {
            Animation animation = new Animation();
            Cel cel = new Cel(sprite);
            animation.Frames.Add(new AnimationFrame(cel, 1));
            return animation;
        }

        public HashSet<Cel> AllCels()
        {
            HashSet<Cel> cels = new HashSet<Cel>(ReferenceEqualityComparer<Cel>.Instance);

            foreach (var frame in this.Frames)
            {
                foreach (var cel in frame.layers)
                {
                    Debug.Assert(cel != null); // <- Shouldn't have null cels?
                    cels.Add(cel);
                }
            }

            return cels;
        }
    }
}
