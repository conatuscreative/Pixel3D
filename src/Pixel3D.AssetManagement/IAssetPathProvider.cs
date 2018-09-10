// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

namespace Pixel3D.AssetManagement
{
	public interface IAssetPathProvider
	{
		/// <summary>Get the path for an asset, or null if the asset is not managed by this provider</summary>
		string GetAssetPath<T>(T asset) where T : class;
	}
}