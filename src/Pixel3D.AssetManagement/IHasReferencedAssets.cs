// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;

namespace Pixel3D.AssetManagement
{
	/// <summary>Allows management of assets that reference other assets</summary>
	public interface IHasReferencedAssets
	{
		/// <summary>Get a list of all assets referenced by this asset</summary>
		IEnumerable<object> GetReferencedAssets();

		/// <summary>Replace all instances of a given asset with another asset</summary>
		void ReplaceAsset(object search, object replace);
	}
}