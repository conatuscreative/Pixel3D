using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations.Serialization;
using Pixel3D.AssetManagement;
using Pixel3D.Engine.Levels;

namespace Pixel3D
{
	partial class FileReadWrite
	{
		public static Level ReadFromFile(string path, IAssetProvider assetProvider, GraphicsDevice graphicsDevice)
		{
			string texturePath = Path.ChangeExtension(path, ".tex");
			ImageBundle imageBundle = null;
			if (File.Exists(texturePath))
			{
#if false // OLD FORMAT
                using(var stream = File.OpenRead(texturePath))
                {
                    using(var unzip = new GZipStream(stream, CompressionMode.Decompress, true))
                    {
                        using(var br = new BinaryReader(unzip))
                        {
                            imageBundle = new ImageBundle();
                            imageBundle.ReadAllImagesOLD(br, graphicsDevice);
                        }
                    }
                }
#else
#if !WINDOWS
				texturePath = texturePath.Replace('\\', '/');
#endif
				byte[] data = File.ReadAllBytes(texturePath);
				if (data[0] != 0)
					throw new Exception("Bad version number");

				var helper = new SimpleTextureLoadHelper(graphicsDevice);
				imageBundle = new ImageBundle();
				imageBundle.ReadAllImages(data, 1, helper);
#endif
			}

			using (var stream = File.OpenRead(path))
			{
				using (var unzip = new GZipStream(stream, CompressionMode.Decompress, true))
				{
					using (var br = new BinaryReader(unzip))
					{
						var deserializeContext = new LevelDeserializeContext(br, imageBundle, assetProvider, graphicsDevice);
						return deserializeContext.DeserializeLevel();
					}
				}
			}
		}

		public static void WriteToFile(this Level level, string path, IAssetPathProvider assetPathProvider)
		{
			// Write out textures...
			ImageWriter imageWriter = new ImageWriter();
			level.RegisterImages(imageWriter, assetPathProvider);
			string texturePath = System.IO.Path.ChangeExtension(path, ".tex");

#if false // OLD FORMAT
            using(var stream = File.Create(texturePath))
            {
                using(var zip = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    using(var bw = new BinaryWriter(zip))
                    {
                        imageWriter.WriteOutAllImagesOLD(bw);
                    }
                }
            }
#else
			MemoryStream ms = new MemoryStream();
			ms.WriteByte(0); // <- version
			imageWriter.WriteOutAllImages(ms);
			ms.Position = 0;
			File.WriteAllBytes(texturePath, ms.ToArray());
#endif

			// Write out Level:
			using (var stream = File.Create(path))
			{
				using (var zip = new GZipStream(stream, CompressionMode.Compress, true))
				{
					using (var bw = new BinaryWriter(zip))
					{
						level.Serialize(new LevelSerializeContext(bw, imageWriter, assetPathProvider));
					}
				}
			}
		}
	}
}
