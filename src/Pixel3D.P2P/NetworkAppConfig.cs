// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;

namespace Pixel3D.P2P
{
	public class NetworkAppConfig
	{
		public const int ApplicationSignatureMaximumLength = 32;

		/// <param name="appId">A very short string to identify the application</param>
		/// <param name="knownPorts">List of ports used by the application</param>
		/// <param name="version">
		///     The protocol version of the application. NOTE: Application is responsible for bumping this is the
		///     P2P layer protocol changes!
		/// </param>
		/// <param name="signature">Signature of the application (for version compatibility check)</param>
		public NetworkAppConfig(string appId, int[] knownPorts, ushort version, byte[] signature)
		{
			if (knownPorts.Length == 0)
				throw new ArgumentOutOfRangeException("knownPorts", "Must specify at least one known port");

			if (signature == null)
				signature = new byte[0];

			if (signature.Length > ApplicationSignatureMaximumLength)
				throw new ArgumentException("Application signature too long", "signature");


			AppId = appId;
			KnownPorts = knownPorts;
			ApplicationVersion = version;
			ApplicationSignature = signature;
		}

		internal int[] KnownPorts { get; }

		internal string AppId { get; }
		internal ushort ApplicationVersion { get; }
		internal byte[] ApplicationSignature { get; }
	}
}