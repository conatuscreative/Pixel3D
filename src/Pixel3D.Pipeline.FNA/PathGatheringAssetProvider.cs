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

		public ICollection<T> LoadAll<T>() where T : class
		{
			return YieldAssetsOfType<T>().ToArray();
		}

		private IEnumerable<T> YieldAssetsOfType<T>() where T : class
		{
			var starDotExtension = "*" + AssetReader.Extension<T>();
			var paths = Directory.GetFiles(assetRoot, starDotExtension, SearchOption.AllDirectories);
			foreach (var path in paths)
			{
				var assetPath = path
					.Replace(assetRoot, string.Empty)
					.Replace(Path.GetFileName(path), Path.GetFileNameWithoutExtension(path));
				yield return Load<T>(assetPath);
			}
		}
	}
}