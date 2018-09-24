// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Lidgren.Network;

namespace Pixel3D.P2P
{
	public class DiscoveredGame
	{
		internal const double DiscoveryResponseTimeout = 30; // seconds


		/// <summary>Arbitrary data provided by the application</summary>
		public byte[] applicationData;

		internal double expireTime;

		private string hostString;

		private DiscoveredGame()
		{
		}


		/// <param name="messageForTiming">Used only for timing and source data, not read from</param>
		private DiscoveredGame(NetIncomingMessage messageForTiming)
		{
			expireTime = messageForTiming.ReceiveTime + DiscoveryResponseTimeout;
			EndPoint = messageForTiming.SenderEndPoint;
			if (EndPoint == null)
				throw new InvalidOperationException();
		}

		public GameInfo GameInfo { get; private set; }


		/// <summary>The server where the game is being hosted (unique identifier for discovered servers).</summary>
		public IPEndPoint EndPoint { get; private set; }

		public string HostString => hostString ?? (hostString = EndPoint == null ? null : EndPoint.Address.ToString());

		/// <summary>0 = unknown mismatch, -1 = too old, 1 = too new.</summary>
		public int? VersionMismatch { get; internal set; }

		public bool IsFull { get; internal set; }


		public string StatusString
		{
			get
			{
				if (VersionMismatch.HasValue)
				{
					if (VersionMismatch.GetValueOrDefault() < 0)
						return "(version too old)";
					if (VersionMismatch.GetValueOrDefault() > 0)
						return "(version too new)";
					return "(version mismatch)";
				}

				if (IsFull)
					return "(full)";
				return string.Empty;
			}
		}

		public bool CanJoin => !VersionMismatch.HasValue && !IsFull;


		internal void CopyFrom(DiscoveredGame other)
		{
			GameInfo.CopyFrom(other.GameInfo);

			EndPoint = other.EndPoint;
			expireTime = other.expireTime;
			VersionMismatch = other.VersionMismatch;
			IsFull = other.IsFull;
			applicationData = other.applicationData; // <- assumed to be immutable
		}

		public static DiscoveredGame FakeGame()
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			var random = new Random();
			var name = new string(Enumerable.Repeat(chars, 14).Select(s => s[random.Next(s.Length)]).ToArray());
			var game = new DiscoveredGame();
			game.GameInfo = new GameInfo(name, false, false);
			game.EndPoint = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 65535);
			game.IsFull = true;
			return game;
		}

		internal static void WriteDiscoveryResponse(NetworkAppConfig appConfig, NetOutgoingMessage message,
			GameInfo gameInfo, bool isFull, MemoryStream applicationData)
		{
			// START: Version safe data
			// {
			message.Write(appConfig.AppId);
			message.Write(gameInfo.Name.FilterName());
			message.Write(appConfig.ApplicationVersion);
			message.WriteVariableUInt32((uint) appConfig.ApplicationSignature.Length);
			message.Write(appConfig.ApplicationSignature);
			// }
			// END: Version safe data

			message.Write(gameInfo.IsInternetGame);
			message.Write(isFull);
			message.WriteMemoryStreamAsByteArray(applicationData); // <- fill in for WriteByteArray
		}

		public static DiscoveredGame ReadFromDiscoveryResponse(NetworkAppConfig appConfig, NetIncomingMessage message)
		{
			try
			{
				// START: Version safe data
				// {

				var theirAppId = message.ReadString();
				if (theirAppId != appConfig.AppId)
					return null; // Wrong application

				var gameName = message.ReadString().FilterName();

				var theirAppVersion = message.ReadUInt16();
				var theirAppSignatureLength = (int) message.ReadVariableUInt32();

				byte[] theirAppSignature;
				if (theirAppSignatureLength < 0 ||
				    theirAppSignatureLength > NetworkAppConfig.ApplicationSignatureMaximumLength)
					theirAppSignature = null;
				else
					theirAppSignature = message.ReadBytes(theirAppSignatureLength);

				// }
				// END: Version safe data


				// Check for version mismatch
				if (theirAppVersion != appConfig.ApplicationVersion || theirAppSignature == null ||
				    !appConfig.ApplicationSignature.SequenceEqual(theirAppSignature))
				{
					var gi = new GameInfo(gameName);
					var dg = new DiscoveredGame(message);
					dg.GameInfo = gi;

					if (theirAppVersion < appConfig.ApplicationVersion)
						dg.VersionMismatch = -1;
					else if (theirAppVersion > appConfig.ApplicationVersion)
						dg.VersionMismatch = 1;
					else
						dg.VersionMismatch = 0;

					Debug.Assert(dg.VersionMismatch.HasValue);

					return dg;
				}

				// If we get here, all the versioning is correct:
				{
					var isInternetGame = message.ReadBoolean();
					var gi = new GameInfo(gameName, isInternetGame,
						false); // <- NOTE: we are assuming that the side-channel handles discovery

					var dg = new DiscoveredGame(message);
					dg.GameInfo = gi;
					dg.IsFull = message.ReadBoolean();
					dg.applicationData = message.ReadByteArray();
					return dg;
				}
			}
			catch
			{
				return null;
			}
		}
	}
}