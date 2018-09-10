// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.StateManagement
{
	/// <summary>State machine for types that exist within the game state</summary>
	public class StateMachine<TUpdateContext> : StateProvider
	{
		public StateMachine()
		{
			CurrentState = GetState<State>();
		}

		public MethodTable StateMethods => (MethodTable) CurrentState.methodTable;
		public State CurrentState { get; private set; }


		public override string ToString()
		{
			return string.Format("{0} ({1})", GetType().Name,
				CurrentState != null ? CurrentState.GetType().Name : "(null)");
		}

		public void SetState<TState>(TUpdateContext updateContext, bool allowStateRestart = false)
			where TState : State, new()
		{
			_DirectlySetState(GetState<TState>(), updateContext, allowStateRestart);
		}

		/// <summary>Set a state from a previously found state object. Not for general use.</summary>
		public void _DirectlySetState(State nextState, TUpdateContext updateContext, bool allowStateRestart)
		{
			if (!allowStateRestart && ReferenceEquals(CurrentState, nextState))
				return; // Don't re-enter the same state

			StateMethods.EndState?.Invoke(this, updateContext, nextState);
			var previousState = CurrentState;
			CurrentState = nextState;
			StateMethods.BeginState?.Invoke(this, updateContext, previousState);
		}


		public new class MethodTable : StateProvider.MethodTable
		{
			[AlwaysNullChecked] public Action<StateMachine<TUpdateContext>, TUpdateContext, State> BeginState;

			[AlwaysNullChecked] public Action<StateMachine<TUpdateContext>, TUpdateContext, State> EndState;
		}
	}
}