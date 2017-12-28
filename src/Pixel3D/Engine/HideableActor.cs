using Pixel3D.Animations;
using Pixel3D.Levels;

namespace Pixel3D.Engine
{
    public abstract class HideableActor : Actor
    {
        public bool active = false;
        public bool visible = false;

        protected HideableActor(AnimationSet animationSet) : base(animationSet) { }
        protected HideableActor(Thing thing, UpdateContext updateContext) : base(thing, updateContext) { }

        public override void RegisterPhysics(UpdateContext updateContext)
        {
            if (!active)
                return;
            base.RegisterPhysics(updateContext);
        }

        public override void Update(UpdateContext updateContext)
        {
            if (!active)
                return;
            base.Update(updateContext);
        }

        public override void Draw(DrawContext drawContext, int tag, IDrawSmoothProvider sp)
        {
            if (!visible)
                return;
            base.Draw(drawContext, tag, sp);
        }
    }
}