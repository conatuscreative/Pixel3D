// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;

namespace Pixel3D.ActorManagement
{
	public abstract class GameState : IGameState
	{
		/// <summary>List of actors that currently exist in the world</summary>
		public readonly List<Actor> actors = new List<Actor>();

		public readonly ushort[] cueStates;
		public readonly Definitions definitions;

		// can be used for duty cycling (i.e recover 2 hp every 15 ticks) (not suitable for timers)
		public int frameCounter;

		// Host derived content
		public byte language = 0;

		public GameState(Definitions definitions)
		{
			this.definitions = definitions;
			cueStates = new ushort[definitions.cuesWithIds];
		}

		public virtual Definitions Definitions
		{
			get { return definitions; }
		}

		public abstract int MaxPlayers { get; }
		public abstract Position? GetPlayerPosition(int playerIndex);

		public virtual void Update(UpdateContext updateContext, MultiInputState currentRawInput)
		{
			frameCounter++;
		}
	}
}