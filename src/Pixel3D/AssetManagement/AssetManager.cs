using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Pixel3D.AssetManagement
{
    // NOTE: IAssetPathProvider is only required so we can implement scenario saving

    public class AssetManager : IAssetProvider, IAssetPathProvider
    {
        public IServiceProvider Services { get; private set; }

        public AssetManager(IServiceProvider services, string rootDirectory)
        {
            this.Services = services;
            this.RootDirectory = rootDirectory;
        }



        #region Asset Root Directory

        string _rootDirectory;
        /// <summary>The root directory for managed assets, or null for absolute paths</summary>
        public string RootDirectory
        {
            get { return _rootDirectory; }
            set
            {
                if(loadedAssets.Count == 0)
                    _rootDirectory = CanonicaliseAssetPath(value);
                else
                    throw new InvalidOperationException("Cannot change the asset root directory after loading assets");
            }
        }

        public string GetFullPathForAssetPath(string assetPath)
        {
            if(_rootDirectory == null)
                return assetPath;
            else
                return Path.Combine(_rootDirectory, assetPath);
        }

        #endregion



        #region Managed Assets

        public static string CanonicaliseAssetPath(string assetPath)
        {
            if(assetPath == null)
                return null;
            if(assetPath.Contains('/'))
                assetPath = assetPath.Replace('/', '\\');
            if(assetPath.EndsWith("\\"))
                assetPath = assetPath.Substring(0, assetPath.Length - 1);
            if(assetPath.StartsWith("\\"))
                assetPath = assetPath.Substring(1, assetPath.Length - 1);
            return assetPath;
        }


        /// <summary>Lookup of asset path to loaded asset</summary>
        public Dictionary<string, object> loadedAssets = new Dictionary<string, object>();


        public void Insert<T>(string assetPath, T asset) where T : class
        {
            loadedAssets.Add(assetPath, asset);
        }

        /// <param name="assetPath">Path relative to the asset directory</param>
        public T Load<T>(string assetPath) where T : class
        {
            assetPath = CanonicaliseAssetPath(assetPath);

            object asset;
            if(loadedAssets.TryGetValue(assetPath, out asset)) // Check cache
            {
                return (T)asset;
            }
            else // Not found, load asset
            {
                if(Locked)
                    throw new InvalidOperationException("Asset manager has been locked, cannot load from disk.");

                var fullPath = Path.Combine(_rootDirectory, assetPath + AssetReader.Extension<T>());

                Debug.Assert(!fullPath.Contains("\\\\")); // <- corrupt file?

                T typedAsset = AssetReader.Read<T>(this, Services, fullPath);
                loadedAssets.Add(assetPath, typedAsset);
                return typedAsset;
            }
        }

        #endregion


        #region Locking

        /// <summary>True if the asset manager is locked and can no longer be used to load assets from disk.</summary>
        public bool Locked { get; private set; }

        /// <remarks>Call this when network definition data is created.</remarks>
        public void Lock()
        {
            this.Locked = true;
        }

        #endregion


        #region IAssetPathProvider (for Scenario serialization only)

        // TODO: PERF: IMPORTANT: A bunch of not-developer-tools stuff has ended up depending on this. BADLY needs a fast reverse-lookup!!!

        /// <summary>Developer tooling only. Slow!</summary>
        public string GetAssetPath<T>(T asset) where T : class
        {
            return loadedAssets.FirstOrDefault(p => ReferenceEquals(p.Value, asset)).Key;
        }

        #endregion




    }
}
