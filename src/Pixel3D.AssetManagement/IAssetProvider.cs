// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;

namespace Pixel3D.AssetManagement
{
	public interface IAssetProvider
	{
		T Load<T>(string assetPath) where T : class;
		ICollection<T> LoadAll<T>() where T : class;
	}
}