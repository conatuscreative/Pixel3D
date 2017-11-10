using System.Collections.Generic;
using System.Diagnostics;
using Pixel3D.Animations;
using Pixel3D.Levels;
using Pixel3D.Physics;
using Pixel3D.Sorting;

namespace Pixel3D.Engine
{
    public class Actor : StateMachine, IHasDrawableFrame, IDrawObject
    {
        public Actor(AnimationSet animationSet)
        {
            this.animationSet = animationSet;
            this.currentAnimation = new AnimationPlayer(animationSet.DefaultAnimation);
        }

        public Actor(Thing thing, UpdateContext updateContext)
        {
            this.animationSet = thing.AnimationSet;
            this.position = thing.Position;
            this.facingLeft = thing.FacingLeft;

            this.currentAnimation = new AnimationPlayer(animationSet.DefaultAnimation);
        }


        #region State Machine Methods

        public new MethodTable StateMethods
        {
            get { return (MethodTable) CurrentState.methodTable; }
        }

        public new class MethodTable : StateMachine.MethodTable
        {
            public delegate void AnimationTriggersDelegate(Actor self, List<string> triggerList, UpdateContext updateContext);
            public AnimationTriggersDelegate AnimationTriggers = null;

            public delegate void UpdateDelegate(Actor self, UpdateContext updateContext);

            public UpdateDelegate Update = null;
        }

        #endregion

        
        #region Update and Gameplay

        public virtual void Update(UpdateContext updateContext)
        {
            TickAnimation(updateContext);

            StateMethods.Update(this, updateContext);
        }

        public virtual void RegisterPhysics(UpdateContext updateContext)
        {
            // NOTE: Default behaviour is to register the actor as static.
            //       Derived classes that want to be movers should not call back to base, but should register as a mover instead.

            if(animationSet.Heightmap != null)
                updateContext.physics.AddStatic(this, new HeightmapView(animationSet.Heightmap, position, facingLeft));
        }

        #endregion


        #region Connection (holding / held relationships)

        // This is so the game state can track what actors are connected to each other,
        // so that it can manage teleporting, people leaving the game, etc.

        // For the time being, we are just a double-linked list. Could be upgraded to a tree,
        // but probably unnecessary.

        public virtual Actor OutgoingConnection { get { return null; } }
        public virtual Actor IncomingConnection { get { return null; } }


        // IMPORTANT: Outgoing connections are broken first, in case the holder wants to apply some
        //            kind of effect to the held actor (eg: throwing it away)

        public void ExternalBreakOutgoingConnection(UpdateContext updateContext)
        {
            var oc = OutgoingConnection;
            if(oc != null)
            {
                // NOTE: By convention, the outgoing connection gets broken first.
                InternalBreakOutgoingConnection(updateContext);
                oc.InternalBreakIncomingConnection(updateContext);
                
                Debug.Assert(OutgoingConnection == null);
            }
        }

        public void ExternalBreakIncomingConnection(UpdateContext updateContext)
        {
            var ic = IncomingConnection;
            if(ic != null)
            {
                // NOTE: By convention, the outgoing connection gets broken first.
                ic.InternalBreakOutgoingConnection(updateContext);
                InternalBreakIncomingConnection(updateContext);

                Debug.Assert(IncomingConnection == null);
            }
        }


        protected virtual void InternalBreakOutgoingConnection(UpdateContext updateContext) { }

        protected virtual void InternalBreakIncomingConnection(UpdateContext updateContext) { }

        #endregion


        #region Positioning

        public Position position;

        public bool facingLeft;

        public int DirectionX
        {
            get { return facingLeft ? -1 : 1; }
        }

        public virtual bool SetFacingFromMotion(int motionX)
        {
            // If motionX == 0, don't change direction
            if (motionX < 0)
            {
                var previous = facingLeft;
                facingLeft = true;
                return previous != facingLeft;
            }
            if (motionX > 0)
            {
                var previous = facingLeft;
                facingLeft = false;
                return previous != facingLeft;
            }
            return false;
        }

        /// <summary>Get the Y position of the ground for this actor.</summary>
        public int GroundHeight(WorldPhysics physics)
        {
            var cpi = new CharacterPhysicsInfo(animationSet, this);
            return CharacterPhysics.GroundHeight(ref cpi, physics, position);
        }

        #endregion
        

        #region Context

        public virtual TagSet AddContextTo(TagSet tagSet)
        {
            return tagSet;
        }

        #endregion

        
        #region Rendering

        // NOTE: Two versions of this method in case we want to play with not smoothing for sorting (so positions are always physically possible)
        //       while still smoothing out actual drawing positions.

        public Position GetRenderPosition(IDrawSmoothProvider smoothProvider) // <- DRAWING
        {
            if(smoothProvider == null) // <- Due to Sauna.cs drawing stuff from the UI, this is possible.
                return position;

            return position + smoothProvider.GetOffset(this);
        }

        public Position GetRenderPosition(ISortSmoothProvider smoothProvider) // <- SORTING
        {
            // NOTE: Visually things work better if we do not smooth the sort (so that all sorted positions are physically plausable)
            return position;
        }


        public virtual SmoothingIdentifiers GetSmoothingIdentifier()
        {
            return new SmoothingIdentifiers { id = int.MinValue, type = null };
        }

        public virtual void RegisterToDraw(SortedDrawList sortedDrawList, GameState readOnlyGameState, ISortSmoothProvider sp)
        {
            sortedDrawList.Add(this, 0, GetRenderPosition(sp), facingLeft, animationSet, currentAnimation.animation);
        }

        public virtual void Draw(DrawContext drawContext, int tag, IDrawSmoothProvider sp)
        {
            currentAnimation.CurrentFrame.Draw(drawContext, GetRenderPosition(sp), facingLeft);
        }

        public virtual void RegisterShadow(ShadowCasterList shadowCasterList, Definitions definitions, IDrawSmoothProvider sp)
        {
            if(animationSet.shadowLayers != null)
            {
                shadowCasterList.AddShadowCaster(animationSet.shadowLayers, animationSet.cachedShadowBounds,
                        animationSet.physicsStartX, animationSet.physicsEndX,
                        GetRenderPosition(sp), CurrentFrame.shadowOffset, facingLeft);
            }
        }


        // Editor:
        DrawableFrame IHasDrawableFrame.GetDrawableFrame()
        {
            return new DrawableFrame(currentAnimation.CurrentFrame, position, facingLeft);
        }

        #endregion

        
        #region Animation

        public AnimationSet animationSet;
        public AnimationPlayer currentAnimation;

        public AnimationFrame CurrentFrame
        {
            get { return currentAnimation.CurrentFrame; }
        }

        public void TickAnimation(UpdateContext updateContext)
        {
            // Don't tick the animation if our animation has already been changed this frame
            // (either externally by another actor earlier in the update order, or by ourselves - eg - changing moves)
            bool externalAnimationChanged = (updateContext.activeActorIndex >= 0) &&
                        ((updateContext.activeActorIndex >= updateContext.initialAnimationStates.Count)
                      || (updateContext.initialAnimationStates[updateContext.activeActorIndex] != currentAnimation));
            if(externalAnimationChanged)
                return;

            currentAnimation.Tick();

            if(currentAnimation.tick == 0) // If we animate, and we're attached, reattach (if you move yourself later, that's your own problem)
            {
                DeferredAttachment da;
                if(updateContext.deferredAttachments.TryGetValue(this, out da))
                    SetAttachedPosition(da.owner, da.outgoingAttachment);
            }

            AnimationDidChange(updateContext);
        }

        /// <param name="animationTags">Tags for the animation. Will NOT have context added.</param>
        public void SetAnimationSingleTag(string animationTag, UpdateContext updateContext)
        {
            var animation = animationSet[animationTag];
            currentAnimation.SetWithoutRestart(animation);
            AnimationDidChange(updateContext);
        }

        /// <param name="animationTags">Tags for the animation. Will have context added.</param>
        public void SetAnimation(TagSet animationTags, UpdateContext updateContext)
        {
            SetAnimationNoCues(animationTags, updateContext);
            AnimationDidChange(updateContext);
        }

        public void SetAnimationNoCues(TagSet animationTags, UpdateContext updateContext)
        {
            animationTags = AddContextTo(animationTags);
            var animation = animationSet[animationTags];
            currentAnimation.SetWithoutRestart(animation);
        }

        /// <param name="animationTags">Tags for the animation. Will have context added.</param>
        public void ResetAnimation(TagSet animationTags, UpdateContext updateContext)
        {
            animationTags = AddContextTo(animationTags);
            var animation = animationSet[animationTags];
            currentAnimation = new AnimationPlayer(animation);
            AnimationDidChange(updateContext);
        }

        public void ResetAnimation(UpdateContext updateContext, string symbol)
        {
            ResetAnimation(new TagSet(symbol), updateContext);
        }

        /// <summary>Direct animation change. Only use for effects.</summary>
        public void ResetAnimation(Animation animation, UpdateContext updateContext)
        {
            currentAnimation = new AnimationPlayer(animation);
            AnimationDidChange(updateContext);
        }

        /// <remarks>Helper method.</remarks>
        protected void AnimationDidChange(UpdateContext updateContext)
        {
            var triggers = currentAnimation.TriggersThisTick();
            if (triggers != null)
                StateMethods.AnimationTriggers(this, triggers, updateContext);

            var cue = updateContext.Definitions.GetCue(currentAnimation.CueThisTick(), this);
            if(cue != null)
                updateContext.PlayCueWorld(cue, this);
        }

        #endregion


        #region Attachment

        /// <summary>Get a view of an outgoing attachment point for a given animation at frame 0</summary>
        /// <param name="animationContext">The context to select the animation (frame 0 is used)</param>
        /// <param name="outgoingAttachmentContext">The context to select the outgoing attachment point</param>
        public OutgoingAttachmentView GetOutgoingAttachment(string animationContext, string outgoingAttachmentContext, List<OutgoingAttachmentAttempt> trackOutgoingAttachments)
        {
            var animation = animationSet[animationContext];
            var animationFrame = animation.Frames[0];
            var outgoingAttachment = animationFrame.outgoingAttachments[outgoingAttachmentContext];

            var result = new OutgoingAttachmentView(outgoingAttachment, position + currentAnimation.PositionDeltaThisTick(), facingLeft);

            if(trackOutgoingAttachments != null)
                trackOutgoingAttachments.Add(new OutgoingAttachmentAttempt(animation, result));

#if DEBUG
            if(!result.IsValid)
            {
                Debug.WriteLine("MISSING OUTGOING ATTACHMENT: AnimationSet = \"" + animationSet.friendlyName + "\", Animation Context = \"" + animationContext.ToString()
                        + "\", Found Animation = \"" + animation.friendlyName + "\", Outgoing Attachment Context = \"" + outgoingAttachmentContext.ToString() + "\"");
            }
#endif

            return result;
        }

        /// <summary>
        /// Get the world position of an matching incoming attachment point, for an outgoing attachment point, if a valid one exists.
        /// Does NOT consider if the attachment point is in-range! (Probably prefer <see cref="Character.GrabRangeHelper"/>)
        /// </summary>
        public bool GetIncomingAttachment(ref OutgoingAttachmentView outgoingAttachmentView, out Position incomingAttachment, List<IncomingAttachmentAttempt> trackIncomingAttachments)
        {
            if(!outgoingAttachmentView.IsValid)
            {
                incomingAttachment = this.position;
                return false;
            }

            var animationContext = outgoingAttachmentView.attachment.targetAnimationContext.MaybeFlip(outgoingAttachmentView.facingLeft != facingLeft);
            var attachmentContext = outgoingAttachmentView.attachment.targetAttachmentContext.MaybeFlip(outgoingAttachmentView.facingLeft != facingLeft);
            
            var animation = animationSet[animationContext];
            var animationFrame = animation.Frames[0];
            if (animationFrame.incomingAttachments.TryGetBestValue(attachmentContext, out incomingAttachment))
            {
                // Transform to world space:
                if (facingLeft)
                    incomingAttachment.X = -incomingAttachment.X; // FlipX
                incomingAttachment += position;

                if(trackIncomingAttachments != null)
                {
                    bool inRange = outgoingAttachmentView.attachRange.Contains(incomingAttachment);
                    trackIncomingAttachments.Add(new IncomingAttachmentAttempt(outgoingAttachmentView, animation, incomingAttachment, inRange));
                }

                return true;
            }

            return false;
        }

       

        /// <summary>Set position to be attached by a given outgoing attachment. Does not set animation.</summary>
        public void SetAttachedPosition(Actor owner, OutgoingAttachment outgoingAttachment)
        {
            var tagFlip = owner.facingLeft != this.facingLeft;

            var attachmentContext = outgoingAttachment.targetAttachmentContext.MaybeFlip(tagFlip);
            var incomingPosition = this.currentAnimation.CurrentFrame.incomingAttachments[attachmentContext].MaybeFlipX(this.facingLeft);
            var outgoingPosition = outgoingAttachment.position.MaybeFlipX(owner.facingLeft);

            this.position = owner.position + (outgoingPosition - incomingPosition);
        }

        #endregion

        
        #region Masks

        /// <summary>
        /// Get a mask for your own animation - assuming you are currently in the middle of your Update method.
        /// This method incoroporates gameplay motion in the animation, which gives a better estimation of where the
        /// mask will appear on-screen. Note that this is only an estimate: gameplay motion can be blocked, animations can change, etc.
        /// </summary>
        public TransformedMaskData GetOwnMask(string maskContext)
        {
            // NOTE: This is a bit of a hack that lets us get a more accurate estimate of where the mask will appear on screen on this frame
            //       because we do the actual position change at the end of the frame, after all the logic that might have used the mask has run,
            //       (such logic may change the animation, so we don't display it anyway).
            var p = position;
            if(currentAnimation.tick == 0)
                p += currentAnimation.CurrentFrame.positionDelta.MaybeFlipX(facingLeft);
            return CurrentFrame.masks[maskContext].GetTransformedMaskData(p, facingLeft);
        }

        public TransformedMaskData GetMask(string maskContext)
        {
            return CurrentFrame.masks[maskContext].GetTransformedMaskData(position, facingLeft);
        }

        public TransformedMaskData GetMask(AnimationFrame frame, string maskContext)
        {
            return frame.masks[maskContext].GetTransformedMaskData(position, facingLeft);
        }

        public TransformedMaskData GetAlphaMask()
        {
            return CurrentFrame.masks.GetBaseFallback().GetTransformedMaskData(position, facingLeft);
        }


        // Masks in the XZ plane (represents an infinite volume extruded along the Y axis)

        public TransformedMaskData GetMaskXZ(TagSet maskContext)
        {
            return GetMaskXZ(CurrentFrame, maskContext);
        }

        public TransformedMaskData GetMaskXZ(AnimationFrame frame, TagSet maskContext)
        {
            return frame.masks[maskContext].GetTransformedMaskDataXZ(position, facingLeft);
        }


        #endregion
    }
}
