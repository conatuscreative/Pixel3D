using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;

namespace Pixel3D
{
	public static class FileReadWrite
	{
		#region File Read/Write

		public static void WriteToFile(this AnimationSet animationSet, string path)
		{
			ImageWriter imageWriter = new ImageWriter();
			animationSet.RegisterImages(imageWriter);
			string texturePath = Path.ChangeExtension(path, ".tex");

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

			using (var stream = File.Create(path))
			{
				using (var zip = new GZipStream(stream, CompressionMode.Compress, true))
				{
					using (var bw = new BinaryWriter(zip))
					{
						animationSet.Serialize(new AnimationSerializeContext(bw, imageWriter));
					}
				}
			}
		}

		public static AnimationSet ReadFromFile(string path, GraphicsDevice graphicsDevice)
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
						var deserializeContext = new AnimationDeserializeContext(br, imageBundle, graphicsDevice);
						return deserializeContext.DeserializeAnimationSet();
					}
				}
			}
		}

		#endregion
	}
}
