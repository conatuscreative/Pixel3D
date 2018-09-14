using Pixel3D.Animations;
using Pixel3D.Engine.Collections;
using Pixel3D.StateManagement;

namespace Pixel3D.Engine.Levels
{
	public class LevelBehaviour
    {
        public ReadOnlyList<ILevelSubBehaviour> subBehaviours;

        public virtual void BeginLevel(UpdateContext updateContext, Level previousLevel, string targetSpawn)
        {
            /* Handler for when the level is initialized */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.BeginLevel(updateContext, previousLevel, targetSpawn);
            }
        }

        public virtual void BeforeUpdate(UpdateContext updateContext)
        {
            /* Handler for when the update loop is about to tick */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.BeforeUpdate(updateContext);
            }
        }

        public virtual void LevelWillChange(UpdateContext updateContext, LevelBehaviour nextLevelBehaviour, Level nextLevel)
        {
            /* Handler for when the current level is about to be changed out */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.LevelWillChange(updateContext, nextLevelBehaviour, nextLevel);
            }
        }

        public virtual void AfterUpdate(UpdateContext updateContext)
        {
            /* Handler for when the update loop has finished ticking */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.AfterUpdate(updateContext);
            }
        }

        public virtual void AfterDraw(DrawContext drawContext)
        {
            /* Handler for when the draw loop has finished ticking */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.AfterDraw(drawContext);
            }
        }

        public virtual void BeforeBackgroundDraw(DrawContext drawContext)
        {
            /* Handler for things like drawing the movie theatre credits */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.BeforeBackgroundDraw(drawContext);
            }
        }

        public virtual void PlayerDidLeave(UpdateContext updateContext, int playerIndex)
        {
            /* Handler for when a player has dropped (possibly in the middle of a story sequence) */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.PlayerDidLeave(updateContext, playerIndex);
            }
        }

        public virtual void PlayerDidJoin(UpdateContext updateContext, int playerIndex)
        {
            /* Handler for when a player has joined (may be in purgatory) */

            foreach (var subBehaviour in subBehaviours)
            {
                subBehaviour.PlayerDidJoin(updateContext, playerIndex);
            }
        }

        public virtual bool TryGetShimPosition(int i, Shim shim, out Position position)
        {
            position = shim.Position; // <== gives levels an opportunity for sliding shims (or hidden shims)
            return true;
        }

        public virtual Actor SpawnThing(Thing thing, UpdateContext updateContext)
        {
            var actor = CreateThingCache.CreateThing(thing.Behaviour, thing, updateContext);
            updateContext.GameState.actors.Add(actor);
            actor.DidSpawn(updateContext);
            return actor;
        }

        public T GetSubBehaviour<T>() where T : LevelSubBehaviour
        {
            foreach (var subBehaviour in subBehaviours)
            {
                if (!(subBehaviour is T))
                    continue;
                return subBehaviour as T;
            }
            return null;
        }
    }
}