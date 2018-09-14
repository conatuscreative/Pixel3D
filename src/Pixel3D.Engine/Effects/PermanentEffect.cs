using Pixel3D.ActorManagement;
using Pixel3D.Animations;

namespace Pixel3D.Engine.Actors.Effects
{
    public abstract class PermanentEffect : SimpleEffect
    {
        protected PermanentEffect(AnimationSet animationSet) : base(animationSet) { }

        public void AlreadyHappened(UpdateContext updateContext)
        {
            currentAnimation.frame = currentAnimation.animation.FrameCount - 1;
        }

        public override void Update(UpdateContext updateContext)
        {
            TickAnimation(updateContext);
        }
    }
}