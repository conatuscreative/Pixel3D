using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Pixel3D.AssetManagement
{
    public static class AssetReader
    {
		static readonly Dictionary<Type, string> ExtensionRegistry = new Dictionary<Type, string>();
	    static readonly Dictionary<Type, ReadFromFile> ReadRegistry = new Dictionary<Type, ReadFromFile>();

	    public delegate object ReadFromFile(string fullPath, IAssetProvider assetProvider, GraphicsDevice graphicsDevice);

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
	        string extension;
	        if(!ExtensionRegistry.TryGetValue(type, out extension))
		        throw new InvalidOperationException("Unknown asset type");
	        return extension;

			/*
			if (type == typeof(AnimationSet))
				return ".as";
			if (type == typeof(Level))
				return ".lvl";
			if (type == typeof(Cue))
				return ".cue";
			else
                throw new InvalidOperationException("Unknown asset type");
			*/
		}

        /// <summary>Return the extension for an asset type, including leading period.</summary>
        public static string Extension<T>()
        {
            return Extension(typeof(T));
        }

        public static T Read<T>(IAssetProvider assetProvider, IServiceProvider services, string fullPath) where T : class
        {
	        ReadFromFile read;
	        if (!ReadRegistry.TryGetValue(typeof(T), out read))
		        throw new InvalidOperationException("Unknown asset type");

	        var gd = ((IGraphicsDeviceService)services.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;
			return (T)read(fullPath, assetProvider, gd);

			/*
			if (typeof(T) == typeof(AnimationSet))
            {
				var gd = ((IGraphicsDeviceService)services.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;
				return (T)(object)AnimationSet.ReadFromFile(fullPath, gd);
            }
            else if(typeof(T) == typeof(Level))
            {
                var gd = ((IGraphicsDeviceService)services.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;
                return (T)(object)Level.ReadFromFile(fullPath, assetProvider, gd);
            }
            else if(typeof (T) == typeof (Cue))
            {
                return (T)(object)Cue.ReadFromFile(fullPath);
            }
            else
            {
                throw new InvalidOperationException("Unknown asset type");
            }
			*/
        }
    }
}
