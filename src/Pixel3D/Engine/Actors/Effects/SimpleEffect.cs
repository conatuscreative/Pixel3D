using Pixel3D.Animations;

namespace Pixel3D.Engine.Actors.Effects
{
    // Base classes in hierarchy:
    //
    // SimpleEffect
    //   + MovingSimpleEffect
    //   + PermanentEffect
    //   + CombatEffect
    //      + DelayedGroundCombatEffect
    //

    /// <summary>Stateless, self-destructing visual effect.</summary>
    public class SimpleEffect : Actor
    {
        /// <summary>NOTE: Caller is responsible for setting position and then calling SetAnimation or AnimationDidChange</summary>
        public SimpleEffect(AnimationSet animationSet) : base(animationSet) { }

        public SimpleEffect(AnimationSet animationSet, UpdateContext updateContext, string animationTag, Position position, bool facingLeft)
                : base(animationSet)
        {
            this.facingLeft = facingLeft;
            this.position = position;
            SetAnimationSingleTag(animationTag, updateContext);
        }

        // TODO: Consider bringing the CombatEffect constructors up to this level






        public void SetPositionAndFacingFor(Actor owner, int offsetX, int offsetY)
        {
            this.facingLeft = owner.facingLeft;
            this.position = owner.position + new Position(offsetX * (owner.DirectionX), offsetY, 0);
        }

        public override void Update(UpdateContext updateContext)
        {
            TickAnimation(updateContext);

            if(currentAnimation.Done)
                updateContext.Destroy(this);
        }



        #region Legacy Creation Methods:
        
        // TODO: This should be removed eventually, and just set the animation directly during creation,
        //       but we need to set the position BEFORE we set the animation, in case any cues get played on frame zero (by SetAnimation)
        //       because we don't want those cues to play at the wrong audio location!
        public virtual TagSet TagSet { get { return TagSet.Empty; } }

        /// <summary>Spawn an effect at an XY positon relative to the owner</summary>
        public virtual void Initialize(UpdateContext updateContext, Actor owner, int offsetX, int offsetY, bool? facingLeft)
        {
            if (owner != null)
            {
                SetRelativePosition(owner, offsetX, offsetY);
                this.facingLeft = facingLeft ?? owner.facingLeft;
            }
            else
                this.facingLeft = facingLeft ?? false;

            SetAnimation(TagSet, updateContext);
        }

        protected void SetRelativePosition(Actor owner, int offsetX, int offsetY)
        {
            var op = owner == null ? Position.Zero : owner.position;

            position = new Position(
                op.X + offsetX,
                op.Y + offsetY,
                op.Z);
        }

        #endregion

    }
}