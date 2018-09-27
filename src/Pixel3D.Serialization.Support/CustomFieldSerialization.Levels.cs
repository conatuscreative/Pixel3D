using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.AssetManagement;
using Pixel3D.Extensions;
using Pixel3D.FrameworkExtensions;
using Pixel3D.Levels;
using Pixel3D.Physics;
using Path = Pixel3D.Levels.Path;

namespace Pixel3D
{
	partial class CustomFieldSerialization
	{
		#region Region

		const int beforeVersion17WorldPhysicsMaximumHeight = 10000;
		
		public static void Serialize(this Region region, LevelSerializeContext context)
		{
			SerializeRegion(context, region);
		}

		private static void SerializeRegion(LevelSerializeContext context, Region region)
		{
			Debug.Assert(!Asserts.enabled || region.mask.Valid);
			region.mask.Serialize(context.bw);
			if (context.Version >= 15)
			{
				context.bw.Write(region.startY);

				if (context.Version < 17 && region.endY != beforeVersion17WorldPhysicsMaximumHeight)
					context.bw.Write(region.endY - 1); // <- Old version had an inclusive upper bound
				else
					context.bw.Write(region.endY);
			}

			if (!context.monitor)
				region.regionIndex = context.nextRegionIndex++;
		}


		public static Region DeserializeRegion(this LevelDeserializeContext context)
		{
			var region = new Region();

			return DeserializeRegion(context, region);
		}

		private static Region DeserializeRegion(LevelDeserializeContext context, Region region)
		{
			region.mask = new MaskData(context.br, context.FastReadHack);

			if (context.Version >= 15)
			{
				region.startY = context.br.ReadInt32();
				region.endY = context.br.ReadInt32();

				if (context.Version < 17 && region.endY != beforeVersion17WorldPhysicsMaximumHeight)
					region.endY++; // <- Old version had an inclusive upper bound
			}
			else
			{
				region.startY = 0;
				region.endY = WorldPhysics.MaximumHeight;
			}

			region.regionIndex = context.nextRegionIndex++;

			return region;
		}
		

		#endregion

		#region Teleporter

		public static void Serialize(this Teleporter teleporter, LevelSerializeContext context)
		{
			if (teleporter.TargetLevel != null)
				teleporter.TargetLevel = teleporter.TargetLevel.ToLowerInvariant();
			context.bw.WriteNullableString(teleporter.TargetLevel);
			context.bw.WriteNullableString(teleporter.targetSpawn);

			if (context.Version >= 18)
				context.bw.Write(teleporter.neverSelectAtRandom);

			SerializeRegion(context, teleporter);
		}

		public static Teleporter DeserializeTeleporter(this LevelDeserializeContext context)
		{
			var teleporter = new Teleporter();

			teleporter.TargetLevel = context.br.ReadNullableString();
			if (teleporter.TargetLevel != null)
				teleporter.TargetLevel = teleporter.TargetLevel.ToLowerInvariant();
			teleporter.targetSpawn = context.br.ReadNullableString();

			if (context.Version >= 18)
				teleporter.neverSelectAtRandom = context.br.ReadBoolean();

			DeserializeRegion(context, teleporter);

			return teleporter;
		}

		#endregion

		#region Thing

		public static void Serialize(this Thing thing, LevelSerializeContext context)
		{
			context.WriteAnimationSet(thing.AnimationSet);

			context.bw.Write(thing.Position);
			context.bw.Write(thing.FacingLeft);

			context.bw.WriteNullableString(thing.overrideBehaviour);

			context.bw.Write(thing.includeInNavigation);

			// Properties:
			{
				context.bw.Write(thing.properties.Count);
				foreach (var kvp in thing.properties)
				{
					context.bw.Write(kvp.Key);
					context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
				}
			}
		}

		/// <summary>Deserialize into new object instance</summary>
		public static Thing DeserializeThing(this LevelDeserializeContext context)
		{
			var animationSet = context.ReadAnimationSet();
			var position = context.br.ReadPosition();
			var facingLeft = context.br.ReadBoolean();

			var thing = new Thing(animationSet, position, facingLeft);
			thing.overrideBehaviour = context.br.ReadNullableString();
			thing.includeInNavigation = context.br.ReadBoolean();

			// Properties:
			int count = context.br.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				thing.properties.Add(context.br.ReadString(), context.br.ReadString());
			}

			return thing;
		}

		#endregion

		#region Shim

		public static void Serialize(this Shim shim, LevelSerializeContext context)
		{
			context.WriteAnimationSet(shim.AnimationSet);
			context.bw.Write(shim.Position);
			context.bw.Write(shim.FacingLeft);
			context.bw.Write(shim.parallaxX);
			context.bw.Write(shim.parallaxY);
			context.bw.Write(shim.animationNumber);
			context.bw.WriteNullableString(shim.ambientSoundSource);

			if (context.Version >= 14)
				context.bw.Write(shim.tag);

			if (context.Version >= 16)
			{
				context.bw.Write(shim.properties.Count);
				foreach (var kvp in shim.properties)
				{
					context.bw.Write(kvp.Key);
					context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
				}
			}
		}

		public static Shim DeserializeShim(this LevelDeserializeContext context)
		{
			var animationSet = context.ReadAnimationSet();
			var position = context.br.ReadPosition();
			var facingLeft = context.br.ReadBoolean();
			var parallaxX = context.br.ReadSingle();
			var parallaxY = context.br.ReadSingle();

			var shim = new Shim(animationSet, position, facingLeft, parallaxX, parallaxY);
			
			shim.animationNumber = context.br.ReadInt32();
			shim.ambientSoundSource = context.br.ReadNullableString();

			if (context.Version >= 14)
				shim.tag = context.br.ReadInt32();

			if (context.Version >= 16)
			{
				int count = context.br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					shim.properties.Add(context.br.ReadString(), context.br.ReadString());
				}
			}

			return shim;
		}

		#endregion

		#region Path

		public static void Serialize(this Path path, LevelSerializeContext context)
		{
			context.bw.WriteBoolean(path.looped);
			context.bw.Write(path.positions.Count);
			foreach (var position in path.positions)
				position.Serialize(context);

			context.bw.Write(path.properties.Count);
			foreach (var kvp in path.properties)
			{
				context.bw.Write(kvp.Key);
				context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
			}
		}

		public static Path DeserializePath(this LevelDeserializeContext context)
		{
			var path = new Path();

			path.looped = context.br.ReadBoolean();
			var positionsCount = context.br.ReadInt32();
			path.positions = new List<LevelPosition>(positionsCount);
			for (var i = 0; i < positionsCount; i++)
				path.positions.Add(new LevelPosition(context));

			var count = context.br.ReadInt32();
			for (var i = 0; i < count; i++)
				path.properties.Add(context.br.ReadString(), context.br.ReadString());

			return path;
		}

		#endregion

		#region Level

		public static void Serialize(this Level level, LevelSerializeContext context)
		{
			context.bw.WriteNullableStringNonBlank(level.friendlyName);
			context.bw.WriteNullableStringNonBlank(level.behaviourName);

			if (context.bw.WriteBoolean(level.levelAnimation != null))
				context.WriteAnimationSet(level.levelAnimation);

			// Properties
			{
				context.bw.Write(level.properties.Count);
				foreach (var kvp in level.properties)
				{
					context.bw.Write(kvp.Key);
					context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
				}
			}

			// Teleporters
			{
				context.bw.Write(level.teleporters.Count);
				foreach (var teleporter in level.teleporters)
					teleporter.Serialize(context);
			}

			// Player Spawns
			{
				context.bw.Write(level.playerSpawns.Count);
				foreach (var item in level.playerSpawns)
				{
					context.bw.Write(item.Key); // string
					context.bw.Write(item.Value.Count);
					foreach (var position in item.Value)
						context.bw.Write(position);
				}
			}

			// Things
			{
				context.bw.Write(level.things.Count);
				foreach (var thing in level.things)
					thing.Serialize(context);
			}

			// Geometry
			{
				// Regions
				{
					context.bw.Write(level.regions.Count);
					foreach (var region in level.regions)
					{
						context.bw.Write(region.Key);
						context.bw.Write(region.Count());
						foreach (var area in region)
						{
							area.Serialize(context);
						}
					}
				}

				// Paths
				{
					context.bw.Write(level.paths.Count);
					foreach (var kvp in level.paths)
					{
						context.bw.Write(kvp.Key);
						kvp.Value.Serialize(context);
					}
				}

				// Positions
				{
					context.bw.Write(level.positions.Count);
					foreach (var cluster in level.positions)
					{
						context.bw.Write(cluster.Key);
						context.bw.Write(cluster.Count());
						foreach (LevelPosition levelPosition in cluster)
						{
							levelPosition.Serialize(context);
						}
					}
				}
			}

			// Shims
			{
				context.bw.Write(level.shims.Count);
				foreach (var shim in level.shims)
				{
					shim.Serialize(context);
				}

				context.bw.Write(level.backgroundShimsEndIndex);
				context.bw.Write(level.foregroundShimsStartIndex);
				context.bw.Write(level.animatedShimCount);
			}

			// Camera Bounds
			context.bw.Write(level.cameraBounds);

			if (context.Version >= 20)
				context.bw.Write(level.isAPitLevel);
		}

		public static Level DeserializeLevel(this LevelDeserializeContext context)
		{
			var level = new Level();

			return DeserializeLevel(context, level);
		}

		public static Level DeserializeLevel(this LevelDeserializeContext context, Level level)
		{
			level.friendlyName = context.br.ReadNullableString();
			level.behaviourName = context.br.ReadNullableString();

			if (context.br.ReadBoolean())
				level.levelAnimation = context.ReadAnimationSet();

			// Properties
			{
				int count = context.br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					level.properties.Add(context.br.ReadString(), context.br.ReadString());
				}
			}

			// Teleporters
			{
				int teleportersCount = context.br.ReadInt32();
				level.teleporters = new List<Teleporter>(teleportersCount);
				for (int i = 0; i < teleportersCount; i++)
					level.teleporters.Add(context.DeserializeTeleporter());
			}

			// Player Spawns
			{
				int playerSpawnsCount = context.br.ReadInt32();
				level.playerSpawns = new OrderedDictionary<string, List<Position>>(playerSpawnsCount);
				for (int i = 0; i < playerSpawnsCount; i++)
				{
					string name = context.br.ReadString();
					int positionsCount = context.br.ReadInt32();
					List<Position> positions = new List<Position>(positionsCount);
					for (int j = 0; j < positionsCount; j++)
						positions.Add(context.br.ReadPosition());
					level.playerSpawns.Add(name, positions);
				}
			}

			// Things
			{
				int thingsCount = context.br.ReadInt32();
				level.things = new List<Thing>(thingsCount);
				for (int i = 0; i < thingsCount; i++)
					level.things.Add(context.DeserializeThing());
			}

			// Geometry
			{
				// Regions
				{
					int count = context.br.ReadInt32();
					level.regions = new MultiDictionary<string, Region>();
					for (int i = 0; i < count; i++)
					{
						var regionKey = context.br.ReadString();
						var areaCount = context.br.ReadInt32();
						for (var j = 0; j < areaCount; j++)
						{
							level.regions.Add(regionKey, context.DeserializeRegion());
						}
					}
				}

				// Paths
				{
					int count = context.br.ReadInt32();
					level.paths = new OrderedDictionary<string, Path>(count);
					for (int i = 0; i < count; i++)
					{
						level.paths.Add(context.br.ReadString(), context.DeserializePath());
					}
				}

				// Positions
				{
					int count = context.br.ReadInt32();
					level.positions = new MultiDictionary<string, LevelPosition>();
					for (int i = 0; i < count; i++)
					{
						var positionKey = context.br.ReadString();
						var pointCount = context.br.ReadInt32();
						for (var j = 0; j < pointCount; j++)
						{
							level.positions.Add(positionKey, new LevelPosition(context));
						}
					}
				}
			}

			// Shims
			{
				int count = context.br.ReadInt32();
				level.shims = new List<Shim>(count);
				for (int i = 0; i < count; i++)
				{
					level.shims.Add(context.DeserializeShim());
				}

				level.backgroundShimsEndIndex = context.br.ReadSByte();
				level.foregroundShimsStartIndex = context.br.ReadSByte();
				level.animatedShimCount = context.br.ReadByte();
			}

			// Camera Bounds
			level.cameraBounds = context.br.ReadRectangle();

			if (context.Version >= 20)
				level.isAPitLevel = context.br.ReadBoolean();

			return level;
		}

		/// <summary>Check that an AnimationSet round-trips through serialization cleanly</summary>
		public static void RoundTripCheck(this Level level, GraphicsDevice graphicsDevice, IAssetProvider assetProvider, IAssetPathProvider assetPathProvider, bool useExternalImages)
		{
			// Serialize a first time
			MemoryStream firstMemoryStream = new MemoryStream();
			BinaryWriter firstBinaryWriter = new BinaryWriter(firstMemoryStream);
			ImageWriter firstImageWriter = null;
			if (useExternalImages)
			{
				firstImageWriter = new ImageWriter();
				level.RegisterImages(firstImageWriter, assetPathProvider);
				firstImageWriter.WriteOutAllImages(firstMemoryStream);
			}
			LevelSerializeContext firstSerializeContext = new LevelSerializeContext(firstBinaryWriter, firstImageWriter, assetPathProvider);
			level.Serialize(firstSerializeContext);
			byte[] originalData = firstMemoryStream.ToArray();

			// Then deserialize that data
			BinaryReader br = new BinaryReader(new MemoryStream(originalData));
			ImageBundle imageBundle = null;
			if (useExternalImages)
			{
				var helper = new SimpleTextureLoadHelper(graphicsDevice);
				imageBundle = new ImageBundle();
				br.BaseStream.Position = imageBundle.ReadAllImages(originalData, (int)br.BaseStream.Position, helper);
			}
			LevelDeserializeContext deserializeContext = new LevelDeserializeContext(br, imageBundle, assetProvider, graphicsDevice);
			Level deserialized = deserializeContext.DeserializeLevel();

			// Then serialize that deserialized data and see if it matches
			MemoryCompareStream secondMemoryStream = new MemoryCompareStream(originalData);
			BinaryWriter secondBinaryWriter = new BinaryWriter(secondMemoryStream);
			ImageWriter secondImageWriter = null;
			if (useExternalImages)
			{
				secondImageWriter = new ImageWriter();
				deserialized.RegisterImages(secondImageWriter, assetPathProvider);
				secondImageWriter.WriteOutAllImages(secondMemoryStream);
			}
			LevelSerializeContext secondSerializeContext = new LevelSerializeContext(secondBinaryWriter, secondImageWriter, assetPathProvider);
			deserialized.Serialize(secondSerializeContext);

			// Clean-up:
			if (imageBundle != null)
				imageBundle.Dispose();
		}

		/// <summary>IMPORTANT: Must match with calls to RegisterImages</summary>
		public static void WriteAnimationSet(this LevelSerializeContext context, AnimationSet animationSet)
		{
			string name = context.assetPathProvider.GetAssetPath(animationSet);

			Debug.Assert(name == null || !name.StartsWith("\\"));

			if (name != null)
			{
				// Write a reference
				context.bw.Write(true);
				context.bw.Write(name);
			}
			else
			{
				// Embed the animation
				context.bw.Write(false);
				animationSet.Serialize(context.animationSerializeContext);
			}
		}

		public static AnimationSet ReadAnimationSet(this LevelDeserializeContext context)
		{
			bool externalReference = context.br.ReadBoolean();
			if (externalReference)
			{
				return context.assetProvider.Load<AnimationSet>(context.br.ReadString());
			}
			else
			{
				return context.animationDeserializeContext.DeserializeAnimationSet();
			}
		}

		#endregion
	}
}
