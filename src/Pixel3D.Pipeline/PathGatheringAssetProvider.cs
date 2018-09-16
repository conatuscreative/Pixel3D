// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pixel3D.AssetManagement;

namespace Pixel3D.Pipeline
{
	public class PathGatheringAssetProvider : IAssetProvider
	{
		public readonly HashSet<string> assetPaths = new HashSet<string>();
		private readonly string assetRoot;

		public PathGatheringAssetProvider(string assetRoot)
		{
			this.assetRoot = assetRoot;
		}

		public T Load<T>(string assetPath) where T : class
		{
			assetPath = AssetManager.CanonicaliseAssetPath(assetPath);
			assetPaths.Add(assetPath + AssetReader.Extension<T>());
			return null;
		}

		public IEnumerable<T> LoadAll<T>() where T : class
		{
			var paths = Directory.GetFiles(assetRoot, "*." + AssetReader.Extension<T>(), SearchOption.AllDirectories);

			var fileNamePaths = paths.Select(x =>
				x.Replace(Path.GetPathRoot(x), "").Replace(Path.GetFileName(x), Path.GetFileNameWithoutExtension(x)));

			foreach (var assetPath in fileNamePaths)
				yield return Load<T>(assetPath);
		}
	}
}