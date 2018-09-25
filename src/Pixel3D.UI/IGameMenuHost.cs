// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.


using Microsoft.Xna.Framework;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;
using Pixel3D.Audio;
using Pixel3D.Strings;

namespace Pixel3D.UI
{
	public interface IGameMenuHost
	{
		// Display:
		TransitionDirection? Transition { get; }
		bool HasBackground { get; }
		bool TryGetTitle(DrawContext drawContext, out string title, out Color color);
		bool ShouldDrawCaret(int itemIndex);

		// Shuttling:
		void Update(IReadOnlyContext context, Definitions definitions);

		// Paging:
		int ItemsPerPage { get; }
		int GetCount(Definitions definitions);
		bool IsCircular { get; }

		// Item Selection:
		bool UseLeftRightNavigation { get; }
		bool UseLeftRightSelection { get; }
		bool UseDefaultPagingInput { get; }

		Position DrawItemAt(int itemIndex, ref Position itemPosition, Position offset, DrawContext drawContext, bool blink);
		bool SelectItemAt(int itemIndex, IReadOnlyContext context, Definitions definitions, IAudioPlayer audioPlayer, ILocalizationProvider loc, int playerIndex);
		bool CancelItemAt(int itemIndex, IReadOnlyContext context, Definitions definitions, IAudioPlayer audioPlayer, ILocalizationProvider loc, int playerIndex);
		bool OnNextItem(IReadOnlyContext context, int previousIndex, IAudioPlayer audioPlayer);
		bool OnPreviousItem(IReadOnlyContext context, int previousIndex, IAudioPlayer audioPlayer);

		// Mouse Simulation:
		Rectangle? GetItemHitZone(Position start, int itemIndex);
	}
}