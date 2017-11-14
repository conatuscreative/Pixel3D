using Pixel3D.Animations;
using Pixel3D.Engine;

namespace Pixel3D.Levels
{
    public class LevelBehaviour
    {
        public virtual void LevelWillChange(UpdateContext updateContext, LevelBehaviour nextLevelBehaviour)
        {
            /* Handler for when the current level is about to be changed out */
        }

        public virtual void AfterUpdate(UpdateContext updateContext)
        {
            /* Handler for when the update loop has finished ticking */
        }

        public virtual void AfterDraw(DrawContext drawContext)
        {
            /* Handler for when the draw loop has finished ticking */
        }

        public virtual void BeforeBackgroundDraw(DrawContext drawContext)
        {
            /* Handler for things like drawing the movie theatre credits */
        }

        public virtual void PlayerDidLeave(UpdateContext updateContext, int playerIndex)
        {
            /* Handler for when a player has dropped (possibly in the middle of a story sequence) */
        }

        public virtual void PlayerDidJoin(UpdateContext updateContext, int playerIndex)
        {
            /* Handler for when a player has joined (may be in purgatory) */
        }

        public virtual bool TryGetShimPosition(int i, Shim shim, out Position position)
        {
            position = shim.Position; // <== gives levels an opportunity for sliding shims (or hidden shims)
            return true;
        }

        public virtual Actor SpawnThing(Thing thing, UpdateContext updateContext)
        {
            var actor = (Actor)CreateThingCache.CreateThing(thing.Behaviour, thing, updateContext);
            updateContext.GameState.actors.Add(actor);
            return actor;
        }
    }
}