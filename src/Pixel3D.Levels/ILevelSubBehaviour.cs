// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Pixel3D.ActorManagement;
using Pixel3D.Animations;

namespace Pixel3D.Levels
{
	public interface IGlobalLevelSubBehaviour { }

	public interface ILevelSubBehaviour
	{
		void BeforeBeginLevel(UpdateContext updateContext);
		void BeginLevelStoryTriggers(UpdateContext updateContext);
		void BeginLevel(UpdateContext updateContext, Level previousLevel, string targetSpawn);
		void BeforeUpdate(UpdateContext updateContext);
		void AfterUpdate(UpdateContext updateContext);
		void BeforeBackgroundDraw(DrawContext drawContext);
		void AfterDraw(DrawContext drawContext);

		void PlayerDidLeave(UpdateContext updateContext, int playerIndex);
		void PlayerDidJoin(UpdateContext updateContext, int playerIndex);

		void LevelWillChange(UpdateContext updateContext, LevelBehaviour nextLevelBehaviour, Level nextLevel);
	}
}