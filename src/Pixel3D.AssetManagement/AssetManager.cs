// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Pixel3D.AssetManagement
{
	public class AssetManager : IAssetProvider, IAssetPathProvider
	{
		public AssetManager(IServiceProvider services, string rootDirectory)
		{
			Services = services;
			RootDirectory = rootDirectory;
		}

		public IServiceProvider Services { get; private set; }

		#region IAssetPathProvider

		public string GetAssetPath<T>(T asset) where T : class
		{
		    string assetPath;
			loadedAssetPaths.TryGetValue(asset, out assetPath);
			return assetPath;
		}

		#endregion

		#region Asset Root Directory

		private string rootDirectory;

		/// <summary>The root directory for managed assets, or null for absolute paths</summary>
		public string RootDirectory
		{
		    get
		    {
                return rootDirectory;
		    }
			set
			{
				if (loadedAssets.Count == 0)
					rootDirectory = CanonicaliseAssetPath(value);
				else
					throw new InvalidOperationException("Cannot change the asset root directory after loading assets");
			}
		}

		#endregion

		#region Managed Assets

		public static string CanonicaliseAssetPath(string assetPath)
		{
			if (assetPath == null)
				return null;
			if (assetPath.Contains('/'))
				assetPath = assetPath.Replace('/', '\\');
			if (assetPath.EndsWith("\\"))
				assetPath = assetPath.Substring(0, assetPath.Length - 1);
			if (assetPath.StartsWith("\\"))
				assetPath = assetPath.Substring(1, assetPath.Length - 1);
			return assetPath;
		}

		/// <summary>Lookup of asset path to loaded asset</summary>
		private readonly Dictionary<string, object> loadedAssets = new Dictionary<string, object>();

		private readonly Dictionary<object, string> loadedAssetPaths = new Dictionary<object, string>();

		public void Insert<T>(string assetPath, T asset) where T : class
		{
			loadedAssets.Add(assetPath, asset);
			loadedAssetPaths.Add(asset, assetPath);
		}

		/// <param name="assetPath">Path relative to the asset directory</param>
		public T Load<T>(string assetPath) where T : class
		{
			assetPath = CanonicaliseAssetPath(assetPath);

            object asset;
			if (loadedAssets.TryGetValue(assetPath, out asset)) // Check cache
				return (T) asset;

			if (Locked)
				throw new InvalidOperationException("Asset manager has been locked, cannot load from disk.");

			var fullPath = Path.Combine(rootDirectory, assetPath + AssetReader.Extension<T>());

			Debug.Assert(!fullPath.Contains("\\\\")); // <- corrupt file?
			var typedAsset = AssetReader.Read<T>(this, Services, fullPath);
			loadedAssets.Add(assetPath, typedAsset);
			loadedAssetPaths.Add(typedAsset, assetPath);
			return typedAsset;
		}

		public ICollection<T> LoadAll<T>() where T : class
		{
			return YieldAssetsOfType<T>().ToArray();
		}

		private IEnumerable<T> YieldAssetsOfType<T>() where T : class
		{
			//
			// Load from packages (already loaded):
			if (rootDirectory == null)
			{
				Debug.Assert(Locked && loadedAssets.Count > 0);
				foreach (var asset in loadedAssets)
				{
					var typed = asset.Value as T;
					if (typed != null)
						yield return typed;
				}

				yield break;
			}

			//
			// Load from loose assets on disk:
			var starDotExtension = "*" + AssetReader.Extension<T>();
			var filePaths = Directory.GetFiles(rootDirectory, starDotExtension, SearchOption.AllDirectories);
			var assetPaths = filePaths.Select(filePath => filePath.Replace(RootDirectory, "").Replace(Path.GetFileName(filePath), Path.GetFileNameWithoutExtension(filePath)));

			foreach (var assetPath in assetPaths)
				yield return Load<T>(assetPath);
		}

		#endregion

		#region Locking

		/// <summary>True if the asset manager is locked and can no longer be used to load assets from disk.</summary>
		public bool Locked { get; private set; }

		/// <remarks>Call this when network definition data is created.</remarks>
		public void Lock()
		{
			Locked = true;
		}

		#endregion
	}
}