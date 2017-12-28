using System.Collections.Generic;
using Pixel3D.Network.Rollback.Input;

namespace Pixel3D.Engine
{
    public abstract class GameState : IGameState
    {
        public readonly ushort[] cueStates;
        public readonly Definitions definitions;

        public abstract int MaxPlayers { get; }
        public virtual Definitions Definitions { get { return definitions; } }
        public abstract Position? GetPlayerPosition(int playerIndex);

        // Host derived content
        public byte language = 0;

        #region // DEBUG ONLY (not in an #if region, so that loops are compatible between builds)
        public bool forceDoorsOpen;
        #endregion // DEBUG ONLY 

        public GameState(Definitions definitions)
        {
            this.definitions = definitions;
            cueStates = new ushort[definitions.cuesWithIds];
        }

        // can be used for duty cycling (i.e recover 2 hp every 15 ticks) (not suitable for timers)
        public int frameCounter;

        public virtual void Update(UpdateContext updateContext, MultiInputState currentRawInput)
        {
            frameCounter++;
        }

        #region Actor Lists

        /// <summary>List of actors that currently exist in the world</summary>
        public readonly List<Actor> actors = new List<Actor>();

        #endregion


    }
}