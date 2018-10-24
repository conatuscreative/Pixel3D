// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.ActorManagement;
using Pixel3D.AssetManagement;
using Pixel3D.Audio;
using Pixel3D.LoopRecorder;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D.Engine
{
	/// <summary>
	/// Links independent Pixel3D engine components to FNA. Should be called once from an external game project.
	/// </summary>
	public static class Installer
	{
		public static void Install<TGameState>(int maxPlayers)
		{
			AssetReader.serviceObjectProvider = services => ((IGraphicsDeviceService)services.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;

			InstallAudioSystem(maxPlayers);

			InstallLoopSystem<TGameState>();
		}

		private static void InstallLoopSystem<TGameState>()
		{
			LoopSystem<TGameState>.serialize = (BinaryWriter bw, ref TGameState gameState, object userData) =>
			{
				var serializeContext = new SerializeContext(bw, false, (DefinitionObjectTable) userData);
				Field.Serialize(serializeContext, bw, ref gameState);
			};
			LoopSystem<TGameState>.deserialize = (BinaryReader br, ref TGameState gameState, object userData) =>
			{
				var deserializeContext = new DeserializeContext(br, (DefinitionObjectTable) userData);
				Field.Deserialize(deserializeContext, br, ref gameState);
			};
		}

		private static void InstallAudioSystem(int maxPlayers)
		{
			AudioSystem.getMaxPlayers = () => maxPlayers;

			AudioSystem.getPlayerAudioPosition = (owner, playerIndex) =>
			{
				var gameState = (IGameState) owner;
				var position = gameState.GetPlayerPosition(playerIndex);
				return position;
			};
		}
	}
}