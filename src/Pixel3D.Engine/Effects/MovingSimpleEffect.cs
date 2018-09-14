using Pixel3D.ActorManagement;
using Pixel3D.Animations;

namespace Pixel3D.Engine.Actors.Effects
{
    /// <summary>This exists primaraly so I don't have to edit animations to remove gameplay motion. Best avoid using deliberately.</summary>
    public class MovingSimpleEffect : SimpleEffect
    {
        public MovingSimpleEffect(AnimationSet animationSet, UpdateContext updateContext, string animationTag, Position position, bool facingLeft)
                : base(animationSet, updateContext, animationTag, position, facingLeft)
        {
        }


        public override void Update(UpdateContext updateContext)
        {
            base.Update(updateContext);

            var positionDelta = currentAnimation.PositionDeltaThisTick();
            if(facingLeft)
                positionDelta.X = -positionDelta.X;
            position += positionDelta;
        }
    }
}
