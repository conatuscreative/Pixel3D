// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Lidgren.Network;

namespace Pixel3D.P2P
{
	public class GameInfo
	{
		public GameInfo(string name, bool isInternetGame, bool sideChannelAuth)
		{
			Name = name.FilterName();
			IsInternetGame = isInternetGame;
			SideChannelAuth = sideChannelAuth;
		}

		public string Name { get; private set; }
		public bool IsInternetGame { get; private set; }
		public bool SideChannelAuth { get; private set; }

		internal void CopyFrom(GameInfo other)
		{
			Name = other.Name;
			IsInternetGame = other.IsInternetGame;
			SideChannelAuth = other.SideChannelAuth;
		}


		#region Network Read/Write

		// Used by "dud" discovered game
		internal GameInfo(string name)
		{
			Name = name;
		}

		internal GameInfo(NetIncomingMessage message)
		{
			Name = message.ReadString().FilterName();
			IsInternetGame = message.ReadBoolean();
			SideChannelAuth = message.ReadBoolean();
		}

		internal void WriteTo(NetOutgoingMessage message)
		{
			message.Write(Name.FilterName());
			message.Write(IsInternetGame);
			message.Write(SideChannelAuth);
		}

		#endregion
	}
}