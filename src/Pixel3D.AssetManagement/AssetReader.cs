using System;
using System.Collections.Generic;

namespace Pixel3D.AssetManagement
{
	public static class AssetReader
    {
	    public delegate object ReadFromFile(string fullPath, IAssetProvider assetProvider, object serviceObject);
	    public delegate object ServiceObjectProvider(IServiceProvider services);

		private static readonly Dictionary<Type, string> ExtensionRegistry = new Dictionary<Type, string>();
	    private static readonly Dictionary<Type, ReadFromFile> ReadRegistry = new Dictionary<Type, ReadFromFile>();

		public static ServiceObjectProvider serviceObjectProvider;

	    public static void Clear()
	    {
		    ExtensionRegistry.Clear();
			ReadRegistry.Clear();
	    }

	    public static void Add<T>(string extension, ReadFromFile read)
	    {
		    ExtensionRegistry.Add(typeof(T), extension);
		    ReadRegistry.Add(typeof(T), read);
	    }

		/// <summary>Return the extension for an asset type, including leading period.</summary>
		public static string Extension(Type type)
        {
	        if(!ExtensionRegistry.TryGetValue(type, out var extension))
		        throw new InvalidOperationException("Unknown asset type");
	        return extension;
		}

        /// <summary>Return the extension for an asset type, including leading period.</summary>
        public static string Extension<T>()
        {
            return Extension(typeof(T));
        }

        public static T Read<T>(IAssetProvider assetProvider, IServiceProvider services, string fullPath) where T : class
        {
	        if (!ReadRegistry.TryGetValue(typeof(T), out var read))
		        throw new InvalidOperationException("Unknown asset type");
	        var serviceObject = serviceObjectProvider(services);
			return (T)read(fullPath, assetProvider, serviceObject);
        }
    }
}