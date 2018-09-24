// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using Lidgren.Network;
using Pixel3D.P2P;

namespace Pixel3D.Network.Test
{
	internal class SimpleNetworkGame : INetworkApplication
	{
		public const ushort ProtocolVersion = 0;

		private readonly P2PNetwork network;

		private readonly Random random = new Random();

		public SimpleNetworkGame(P2PNetwork network)
		{
			this.network = network;
		}


		MemoryStream INetworkApplication.GetDiscoveryData()
		{
			return null;
		}

		void INetworkApplication.Shutdown()
		{
		}


		private string FormatPlayerData(PeerInfo peerInfo)
		{
			var playerData = peerInfo.PlayerData;
			if (playerData != null)
			{
				for (var i = 0; i < playerData.Length; i++)
					Debug.Assert(playerData[i] == (byte) i);
				return " [" + playerData.Length + "]";
			}

			return string.Empty;
		}


		public void Update()
		{
			if (!network.IsApplicationConnected)
				return;

			try
			{
				foreach (var remotePeer in network.RemotePeers)
				{
					NetIncomingMessage message;
					while ((message = remotePeer.ReadMessage()) != null)
						try
						{
							WriteInputMessage((char) message.ReadUInt16(), remotePeer.PeerInfo, false);
						}
						catch (Exception e)
						{
							network.NetworkDataError(remotePeer, e);
						}
				}
			}
			catch (NetworkDisconnectionException)
			{
			}
		}

		public void HandleKeyPress(ConsoleKeyInfo keyPress)
		{
			if (!network.IsApplicationConnected)
				return;

			if ((keyPress.Modifiers == 0 || keyPress.Modifiers == ConsoleModifiers.Shift) && keyPress.KeyChar != 0)
			{
				var message = network.CreateMessage();
				message.Write(keyPress.KeyChar);
				network.Broadcast(message, NetDeliveryMethod.ReliableOrdered, 1);

				WriteInputMessage(keyPress.KeyChar, network.LocalPeerInfo, true);
			}
		}


		private static void WriteInputMessage(char c, PeerInfo peerInfo, bool local)
		{
			Debug.Assert(peerInfo.InputAssignment != 0);

			var color = ConsoleColor.DarkMagenta; // Multi-input, not yet fully supported
			switch (peerInfo.InputAssignment)
			{
				case InputAssignment.Player1:
					color = ConsoleColor.Red;
					break;
				case InputAssignment.Player2:
					color = ConsoleColor.Blue;
					break;
				case InputAssignment.Player3:
					color = ConsoleColor.DarkGreen;
					break;
				case InputAssignment.Player4:
					color = ConsoleColor.DarkYellow;
					break;
			}

			var fgc = Console.ForegroundColor;
			var bgc = Console.BackgroundColor;

			if (local)
				Console.ForegroundColor = ConsoleColor.White;
			Console.Write(peerInfo.PlayerName + ": ");

			Console.BackgroundColor = color;
			Console.ForegroundColor = ConsoleColor.White;

			Console.Write(' ');
			Console.Write(c);
			Console.Write(' ');

			Console.ForegroundColor = fgc;
			Console.BackgroundColor = bgc;

			Console.WriteLine(" -- #" + peerInfo.ConnectionId + ", input=" +
			                  peerInfo.InputAssignment.GetFirstAssignedPlayerIndex());
		}


		#region INetworkApplication Server

		void INetworkApplication.JoinOnServer(RemotePeer remotePeer, NetOutgoingMessage joinMessage,
			NetOutgoingMessage connectedMessage)
		{
			var v = random.Next();
			Console.WriteLine("Welcome " + remotePeer.PeerInfo + " = " + v + " (on server)" +
			                  FormatPlayerData(remotePeer.PeerInfo));

			joinMessage.Write(v);
			connectedMessage.Write(v);
		}

		void INetworkApplication.StartOnServer()
		{
			Console.WriteLine("Started game as " + network.LocalPeerInfo + FormatPlayerData(network.LocalPeerInfo));
			Console.Title = "P2P Test - Port " + network.PortNumber + " (SERVER)";
		}

		void INetworkApplication.LeaveOnServer(RemotePeer remotePeer, NetOutgoingMessage message)
		{
			var v = random.Next();
			Console.WriteLine("Goodbye " + remotePeer.PeerInfo + " = " + v + " (on server)" +
			                  FormatPlayerData(remotePeer.PeerInfo));

			message.Write(v);
		}

		void INetworkApplication.HostMigrationBecomeHost(NetOutgoingMessage message)
		{
			var v = random.Next();
			Console.WriteLine("Host Migration = " + v + " (on server)");
			message.Write(v);

			Console.Title = "P2P Test - Port " + network.PortNumber + " (SERVER)";
		}

		#endregion


		#region INetworkApplication Client

		void INetworkApplication.JoinOnClient(RemotePeer remotePeer, NetIncomingMessage message)
		{
			int v;
			try
			{
				v = message.ReadInt32();
			}
			catch (Exception e)
			{
				throw new ProtocolException("", e);
			}

			Console.WriteLine("Welcome " + remotePeer.PeerInfo + " = " + v + " (on client)" +
			                  FormatPlayerData(remotePeer.PeerInfo));
		}


		void INetworkApplication.LeaveOnClient(RemotePeer remotePeer, NetIncomingMessage message)
		{
			int v;
			try
			{
				v = message.ReadInt32();
			}
			catch (Exception e)
			{
				throw new ProtocolException("", e);
			}

			Console.WriteLine("Goodbye " + remotePeer.PeerInfo + " = " + v + " (on client)" +
			                  FormatPlayerData(remotePeer.PeerInfo));
		}


		void INetworkApplication.ConnectedOnClient(NetIncomingMessage message)
		{
			int v;
			try
			{
				v = message.ReadInt32();
			}
			catch (Exception e)
			{
				throw new ProtocolException("", e);
			}

			Console.WriteLine("Connected as " + network.LocalPeerInfo + " = " + v + " (on client)" +
			                  FormatPlayerData(network.LocalPeerInfo));

			if (network.RemotePeers.Count == 0)
			{
				Console.WriteLine("(no one else online)");
			}
			else
			{
				Console.WriteLine("Already online:");
				foreach (var peer in network.RemotePeers)
					Console.WriteLine(peer.PeerInfo + FormatPlayerData(peer.PeerInfo));
			}
		}


		void INetworkApplication.HostMigrationChangeHost(RemotePeer newHost, NetIncomingMessage message)
		{
			int v;
			try
			{
				v = message.ReadInt32();
			}
			catch (Exception e)
			{
				throw new ProtocolException("", e);
			}

			Console.WriteLine("Host Migration to " + newHost.PeerInfo + " = " + v + " (on client)" +
			                  FormatPlayerData(newHost.PeerInfo));
		}

		#endregion
	}
}