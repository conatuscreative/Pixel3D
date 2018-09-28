// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.AssetManagement
{
	public enum AssetClassification
	{
		/// <summary>Asset is embedded (not managed)</summary>
		Embedded,

		/// <summary>Asset is managed</summary>
		Managed,

		/// <summary>Asset is managed, but is outside the given asset root directory</summary>
		OutOfPath,

		/// <summary>Asset is managed, but has an unexpected extension</summary>
		BadExtension,

		/// <summary>
		///     Asset could not be found at load time (and is being represented by a fake "missing" asset that retains path
		///     info.)
		/// </summary>
		Missing
	}
}