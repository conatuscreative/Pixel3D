// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Net;
using System.Threading;
using Pixel3D.P2P;

namespace Pixel3D.Network.Test
{
	internal class Program
	{
		private static void PrintNetworkMenu(Discovery discovery)
		{
			Console.WriteLine("---- NETWORK MENU ----");
			Console.WriteLine();

			if (discovery.Items.Count == 0)
				Console.WriteLine("Finding LAN games...");
			else
				for (var i = 0; i < discovery.Items.Count && i < 10; i++)
				{
					Console.Write("[");
					Console.Write((char) ('0' + i));
					Console.Write("] - ");
					var status = discovery.Items[i].StatusString;
					Console.WriteLine(discovery.Items[i].GameInfo.Name + " - " + discovery.Items[i].EndPoint +
					                  (status == "" ? "" : " - " + status));
				}

			Console.WriteLine();
			Console.WriteLine("[F1] - Start LAN Game");
			Console.WriteLine("[F2] - Start Internet Game");
			Console.WriteLine("[F3] - Specify Host");

			Console.WriteLine();
			Console.WriteLine("----------------------");
		}

		private static void Main(string[] args)
		{
			Console.Title = "P2P Test";
			const int msPerFrame = 16;

			#region Startup mode select

			Console.WriteLine("Connection Testing Program. Escape to exit.");
			Console.WriteLine();
			Console.WriteLine("[Z] = Toggle bad network simulation");
			Console.WriteLine("[Q] = Add player data");
			Console.WriteLine("[space] = Begin");
			Console.WriteLine();

			var badNetworkSimPreset = 0;
			byte[] localPlayerData = null;

			while (true)
			{
				var keyInfo = Console.ReadKey(true);
				var key = keyInfo.Key;

				if (key == ConsoleKey.Escape)
					return;

				if (key == ConsoleKey.Spacebar)
					break;

				if (key == ConsoleKey.Q)
				{
					if ((keyInfo.Modifiers & ConsoleModifiers.Shift) == 0)
					{
						Array.Resize(ref localPlayerData, localPlayerData == null ? 1 : localPlayerData.Length + 1);
						localPlayerData[localPlayerData.Length - 1] = (byte) (localPlayerData.Length - 1);
					}
					else if (localPlayerData != null && localPlayerData.Length > 0)
					{
						Array.Resize(ref localPlayerData, localPlayerData.Length - 1);
					}

					Console.WriteLine("Player data length = " + localPlayerData.Length);
				}

				if (key == ConsoleKey.Z)
				{
					badNetworkSimPreset = BadNetworkSimulation.NextPreset(badNetworkSimPreset);
					Console.WriteLine(BadNetworkSimulation.GetPreset(badNetworkSimPreset).ToString());
				}
			}

			#endregion

			#region Network Game Menu (and LAN Discovery)

			var appConfig = new NetworkAppConfig("P2P Test", new[] {20001, 20002, 20003, 20004, 20005, 20006},
				SimpleNetworkGame.ProtocolVersion, null);

			var network = new P2PNetwork(appConfig, new ConsoleLogHandler(),
				BadNetworkSimulation.GetPreset(badNetworkSimPreset));
			SimpleNetworkGame networkGame;

			Console.Title = "P2P Test - Port " + network.PortNumber;

			network.StartDiscovery();
			network.Discovery.OnItemsChanged += PrintNetworkMenu; // Will keep reprinting menu during discovery

			// Print initial menu directly:
			PrintNetworkMenu(network.Discovery);

			while (true)
			{
				// Poll network
				network.Update();

				// Handle Input
				if (Console.KeyAvailable)
				{
					var keyPress = Console.ReadKey(true);

					// Exit
					if (keyPress.Key == ConsoleKey.Escape)
						goto Shutdown;

					// Select a discovered server
					if (keyPress.Key >= ConsoleKey.D0 && keyPress.Key <= ConsoleKey.D9)
					{
						var selection = keyPress.Key - ConsoleKey.D0;
						if (selection >= 0 && selection < network.Discovery.Items.Count &&
						    network.Discovery.Items[selection].CanJoin)
						{
							var discoveredGame = network.Discovery.Items[selection];
							network.StopDiscovery();

							Console.WriteLine();
							Console.WriteLine("Connecting to game: " + discoveredGame.GameInfo.Name);
							network.ConnectToGame(networkGame = new SimpleNetworkGame(network), "Player",
								localPlayerData, discoveredGame.EndPoint, 0, 0);
							break;
						}
					}

					// Start an Internet or LAN game:
					var internet = false;
					if (keyPress.Key == ConsoleKey.F1 || (internet = keyPress.Key == ConsoleKey.F2))
					{
						network.StopDiscovery();

						Console.WriteLine();
						Console.WriteLine(internet ? "Starting Internet Game..." : "Starting LAN Game...");
						network.StartGame(networkGame = new SimpleNetworkGame(network), "Player", localPlayerData,
							"Default Game", internet, false, 0);
						break;
					}

					// Join a specific game host:
					if (keyPress.Key == ConsoleKey.F3)
					{
						Console.WriteLine();
						Console.WriteLine("Enter host to connect to:");
						var userHost = Console.ReadLine();

						IPEndPoint endpoint = null;
						try
						{
							endpoint = network.ParseAndResolveEndpoint(userHost);
						}
						catch
						{
						}

						if (endpoint == null)
						{
							Console.WriteLine("Failed to parse or resolve endpoint: \"" + endpoint + "\"");
						}
						else
						{
							network.StopDiscovery();
							Console.WriteLine("Connecting to game at " + endpoint);
							network.ConnectToGame(networkGame = new SimpleNetworkGame(network), "Player",
								localPlayerData, endpoint, 0, 0);
							break;
						}
					}
				}

				Thread.Sleep(msPerFrame);
			}

			#endregion

			#region Run Network

			while (true)
			{
				// Poll network
				network.Update();
				networkGame.Update();

				// Handle Input
				if (Console.KeyAvailable)
				{
					var keyPress = Console.ReadKey(true);

					// Exit
					if (keyPress.Key == ConsoleKey.Escape)
						goto Shutdown;

					networkGame.HandleKeyPress(keyPress);
				}

				Thread.Sleep(msPerFrame);
			}

			#endregion

			#region Shutdown

			Shutdown:
			network.Shutdown().Wait(1500);

			#endregion
		}
	}
}