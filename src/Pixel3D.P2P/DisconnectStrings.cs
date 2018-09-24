// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Lidgren.Network;

namespace Pixel3D.P2P
{
	internal static class DisconnectStrings
	{
		// Sent by server:
		public const string Kicked = "Kicked";

		// Sent by server at join:
		public const string BadHailMessage = "Bad hail message";
		public const string BadGameVersion = "Game version mismatch";
		public const string GameFull = "Game is full";
		public const string BadSideChannelAuth = "Bad side-channel auth";

		// Sent by client:
		public const string
			BecomingHost =
				"Becoming host"; // sent to peers who were not app-connected and connected at host migration time

		public const string Leaving = "Leaving";
		public const string DisconnectedByServer = "Disconnected by server"; // you were disconnected
		public const string InternalError = "Internal error"; // programming error
		public const string DataError = "Data error"; // received bad data (indicates programming error)

		// Sent by both:
		public const string Shutdown = "Shutdown";
		public const string LidgrenTimedOut = NetConnection.ConnectionTimedOutString;


		internal static string LocaliseServerDisconnectionReason(string reason)
		{
			switch (reason)
			{
				case GameFull: return UserVisibleStrings.GameIsFull;
				case BadGameVersion: return UserVisibleStrings.GameVersionMismatch;
				case BadHailMessage: return UserVisibleStrings.ServerRejectedConnection;
				case BadSideChannelAuth: return UserVisibleStrings.ServerRejectedConnection; // <- near enough.

				case Shutdown: return UserVisibleStrings.ServerShuttingDown;
				case LidgrenTimedOut: return UserVisibleStrings.ServerTimedOut;


				case Kicked:
					// Don't say "kicked", because the server detecting a network error or being removed by the side-channel
					// is treated currently treated as a "kick" and that sounds a bit too agressive (like the server owner
					// is initiating the action)
					return UserVisibleStrings.DisconnectedByServer;

				default:
					return UserVisibleStrings.DisconnectedByServerUnknownReason;
			}
		}
	}
}