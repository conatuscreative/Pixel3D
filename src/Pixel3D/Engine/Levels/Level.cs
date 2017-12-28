using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.AssetManagement;
using Pixel3D.Collections;
using Pixel3D.Extensions;
using Pixel3D.Levels;
using Pixel3D.Serialization;
using Path = Pixel3D.Engine.Levels.Path;

namespace Pixel3D.Engine.Levels
{
    public class Level : IHasReferencedAssets, IEditorNameProvider
    {
        private Level()
        {
            teleporters = new List<Teleporter>();
            playerSpawns = new OrderedDictionary<string, List<Position>>();
            things = new List<Thing>();
            shims = new List<Shim>();
        }

        /// <summary>IMPORTANT: Do not use in gameplay code (not network safe)</summary>
        [SerializationIgnore]
        public string friendlyName;

        /// <summary>Class name of level behaviour class to spawn</summary>
        public string behaviourName;

        /// <summary>Arbitrary level properties (consumers are expected to parse the strings)</summary>
        public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();

        /// <summary>Helper access to level walkable area heightmap</summary>
        public Heightmap Walkable { get { return levelAnimation.Heightmap; } }

        /// <summary>Helper access to level ceiling heightmap</summary>
        public Heightmap Ceiling { get { return levelAnimation.Ceiling; } }

        public Rectangle cameraBounds;

        public int MaxVisibleHeight { get { return cameraBounds.WorldTop() - Walkable.StartZ; } }

        public bool isAPitLevel;

        public AnimationSet levelAnimation;

        #region IHasReferencedAssets Members

        public IEnumerable<object> GetReferencedAssets()
        {
            yield return levelAnimation;
            foreach(var thing in things)
                yield return thing.AnimationSet;
            foreach (var shim in shims)
                yield return shim.AnimationSet;
        }

        public void ReplaceAsset(object search, object replace)
        {
            if(search is AnimationSet)
            {
                if(ReferenceEquals(levelAnimation, search))
                    levelAnimation = (AnimationSet)replace;

                foreach(var thing in things)
                {
                    if(ReferenceEquals(thing.AnimationSet, search))
                        thing.AnimationSet = (AnimationSet)replace;
                }
                foreach (var shim in shims)
                {
                    if (ReferenceEquals(shim.AnimationSet, search))
                        shim.AnimationSet = (AnimationSet)replace;
                }
            }
        }

        #endregion

        
        #region Serialization

        public void RegisterImages(ImageWriter imageWriter, IAssetPathProvider assetPathProvider)
        {
            // NOTE: Using assetPathProvider to check if the item is embedded
            //       (Kind of ugly, as it duplicates the condition in LevelSerializeContext)

            if(levelAnimation != null && assetPathProvider.GetAssetPath(levelAnimation) == null)
                levelAnimation.RegisterImages(imageWriter);
            foreach(var thing in things)
                if(thing.AnimationSet != null && assetPathProvider.GetAssetPath(thing.AnimationSet) == null)
                    thing.AnimationSet.RegisterImages(imageWriter);
            foreach(var shim in shims)
                if(shim.AnimationSet != null && assetPathProvider.GetAssetPath(shim.AnimationSet) == null)
                    shim.AnimationSet.RegisterImages(imageWriter);
        }

        public virtual void Serialize(LevelSerializeContext context)
        {
            context.bw.WriteNullableStringNonBlank(friendlyName);
            context.bw.WriteNullableStringNonBlank(behaviourName);

            if(context.bw.WriteBoolean(levelAnimation != null))
                context.WriteAnimationSet(levelAnimation);

            // Properties
            {
                context.bw.Write(properties.Count);
                foreach(var kvp in properties)
                {
                    context.bw.Write(kvp.Key);
                    context.bw.Write(kvp.Value ?? string.Empty); // (null value should probably be blocked by editor, but being safe...)
                }
            }

            // Teleporters
            {
                context.bw.Write(teleporters.Count);
                foreach(var teleporter in teleporters)
                    teleporter.Serialize(context);
            }

            // Player Spawns
            {
                context.bw.Write(playerSpawns.Count);
                foreach(var item in playerSpawns)
                {
                    context.bw.Write(item.Key); // string
                    context.bw.Write(item.Value.Count);
                    foreach(var position in item.Value)
                        context.bw.Write(position);
                }
            }

            // Things
            {
                context.bw.Write(things.Count);
                foreach(var thing in things)
                    thing.Serialize(context);
            }

            // Geometry
            {
                // Regions
                {
                    context.bw.Write(regions.Count);
                    foreach (var region in regions)
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
                    context.bw.Write(paths.Count);
                    foreach (var kvp in paths)
                    {
                        context.bw.Write(kvp.Key);
                        kvp.Value.Serialize(context);
                    }
                }

                // Positions
                {
                    context.bw.Write(positions.Count);
                    foreach (var cluster in positions)
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
                context.bw.Write(shims.Count);
                foreach (var shim in shims)
                {
                    shim.Serialize(context);   
                }

                context.bw.Write(backgroundShimsEndIndex);
                context.bw.Write(foregroundShimsStartIndex);
                context.bw.Write(animatedShimCount);
            }

            // Camera Bounds
            context.bw.Write(cameraBounds);

            if(context.Version >= 20)
                context.bw.Write(isAPitLevel);
        }


        /// <summary>Deserialize into new object instance</summary>
        public Level(LevelDeserializeContext context)
        {
            Deserialize(context);
        }

        private void Deserialize(LevelDeserializeContext context)
        {
            friendlyName = context.br.ReadNullableString();
            behaviourName = context.br.ReadNullableString();

            if (context.br.ReadBoolean())
                levelAnimation = context.ReadAnimationSet();

            // Properties
            {
                int count = context.br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    properties.Add(context.br.ReadString(), context.br.ReadString());
                }
            }

            // Teleporters
            {
                int teleportersCount = context.br.ReadInt32();
                teleporters = new List<Teleporter>(teleportersCount);
                for (int i = 0; i < teleportersCount; i++)
                    teleporters.Add(new Teleporter(context));
            }

            // Player Spawns
            {
                int playerSpawnsCount = context.br.ReadInt32();
                playerSpawns = new OrderedDictionary<string, List<Position>>(playerSpawnsCount);
                for (int i = 0; i < playerSpawnsCount; i++)
                {
                    string name = context.br.ReadString();
                    int positionsCount = context.br.ReadInt32();
                    List<Position> positions = new List<Position>(positionsCount);
                    for (int j = 0; j < positionsCount; j++)
                        positions.Add(context.br.ReadPosition());
                    playerSpawns.Add(name, positions);
                }
            }

            // Things
            {
                int thingsCount = context.br.ReadInt32();
                things = new List<Thing>(thingsCount);
                for (int i = 0; i < thingsCount; i++)
                    things.Add(new Thing(context));
            }

            // Geometry
            {
                // Regions
                {
                    int count = context.br.ReadInt32();
                    regions = new MultiDictionary<string, Region>();
                    for (int i = 0; i < count; i++)
                    {
                        var regionKey = context.br.ReadString();
                        var areaCount = context.br.ReadInt32();
                        for (var j = 0; j < areaCount; j++)
                        {
                            regions.Add(regionKey, new Region(context));
                        }
                    }
                }

                // Paths
                {
                    int count = context.br.ReadInt32();
                    paths = new OrderedDictionary<string, Path>(count);
                    for (int i = 0; i < count; i++)
                    {
                        paths.Add(context.br.ReadString(), new Path(context));
                    }
                }

                // Positions
                {
                    int count = context.br.ReadInt32();
                    positions = new MultiDictionary<string, LevelPosition>();
                    for (int i = 0; i < count; i++)
                    {
                        var positionKey = context.br.ReadString();
                        var pointCount = context.br.ReadInt32();
                        for (var j = 0; j < pointCount; j++)
                        {
                            positions.Add(positionKey, new LevelPosition(context));
                        }
                    }
                }
            }

            // Shims
            {
                int count = context.br.ReadInt32();
                shims = new List<Shim>(count);
                for (int i = 0; i < count; i++)
                {
                    shims.Add(new Shim(context));
                }

                backgroundShimsEndIndex = context.br.ReadSByte();
                foregroundShimsStartIndex = context.br.ReadSByte();
                animatedShimCount = context.br.ReadByte();
            }

            // Camera Bounds
            cameraBounds = context.br.ReadRectangle();

            if (context.Version >= 20)
                isAPitLevel = context.br.ReadBoolean();
        }


        /// <summary>Check that an AnimationSet round-trips through serialization cleanly</summary>
        public void RoundTripCheck(GraphicsDevice graphicsDevice, IAssetProvider assetProvider, IAssetPathProvider assetPathProvider, bool useExternalImages)
        {
            // Serialize a first time
            MemoryStream firstMemoryStream = new MemoryStream();
            BinaryWriter firstBinaryWriter = new BinaryWriter(firstMemoryStream);
            ImageWriter firstImageWriter = null;
            if(useExternalImages)
            {
                firstImageWriter = new ImageWriter();
                this.RegisterImages(firstImageWriter, assetPathProvider);
                firstImageWriter.WriteOutAllImages(firstMemoryStream);
            }
            LevelSerializeContext firstSerializeContext = new LevelSerializeContext(firstBinaryWriter, firstImageWriter, assetPathProvider);
            Serialize(firstSerializeContext);
            byte[] originalData = firstMemoryStream.ToArray();

            // Then deserialize that data
            BinaryReader br = new BinaryReader(new MemoryStream(originalData));
            ImageBundle imageBundle = null;
            if(useExternalImages)
            {
                var helper = new SimpleTextureLoadHelper(graphicsDevice);
                imageBundle = new ImageBundle();
                br.BaseStream.Position = imageBundle.ReadAllImages(originalData, (int)br.BaseStream.Position, helper);
            }
            LevelDeserializeContext deserializeContext = new LevelDeserializeContext(br, imageBundle, assetProvider, graphicsDevice);
            Level deserialized = new Level(deserializeContext);

            // Then serialize that deserialized data and see if it matches
            MemoryCompareStream secondMemoryStream = new MemoryCompareStream(originalData);
            BinaryWriter secondBinaryWriter = new BinaryWriter(secondMemoryStream);
            ImageWriter secondImageWriter = null;
            if(useExternalImages)
            {
                secondImageWriter = new ImageWriter();
                deserialized.RegisterImages(secondImageWriter, assetPathProvider);
                secondImageWriter.WriteOutAllImages(secondMemoryStream);
            }
            LevelSerializeContext secondSerializeContext = new LevelSerializeContext(secondBinaryWriter, secondImageWriter, assetPathProvider);
            deserialized.Serialize(secondSerializeContext);

            // Clean-up:
            if(imageBundle != null)
                imageBundle.Dispose();
        }

        #endregion


        #region File Read/Write

        public void WriteToFile(string path, IAssetPathProvider assetPathProvider)
        {
            // Write out textures...
            ImageWriter imageWriter = new ImageWriter();
            this.RegisterImages(imageWriter, assetPathProvider);
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
                        Serialize(new LevelSerializeContext(bw, imageWriter, assetPathProvider));
                    }
                }
            }
        }

        public static Level ReadFromFile(string path, IAssetProvider assetProvider, GraphicsDevice graphicsDevice)
        {
            string texturePath = System.IO.Path.ChangeExtension(path, ".tex");
            ImageBundle imageBundle = null;
            if(File.Exists(texturePath))
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
                if(data[0] != 0)
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
                        return new Level(deserializeContext);
                    }
                }
            }
        }

        #endregion

        public string EditorName { get { return friendlyName; } }

        private const string ArenaPropertyName = "Arena";
        private const string ArenaNotVisiblePropertyName = "ArenaVisible";

        public bool IsDefinedAsAnArena
        {
            get
            {
                bool isArena;
                bool.TryParse(properties.GetString(ArenaPropertyName), out isArena);
                if (!isArena)
                    return false;
                bool isArenaVisible;
                if (!bool.TryParse(properties.GetString(ArenaNotVisiblePropertyName), out isArenaVisible))
                    isArenaVisible = true;
                return isArenaVisible;
            }
        }

        public Level(Sprite sprite) : this()
        {
            SetLevelAnimationFromSprite(sprite);
        }

        public void SetLevelAnimationFromSprite(Sprite sprite)
        {
            levelAnimation = AnimationSet.CreateSingleSprite(sprite);
            levelAnimation.Heightmap = new Heightmap(Heightmap.Infinity);
        }

        #region Teleports

        /// <summary>List of outgoing teleporters</summary>
        public List<Teleporter> teleporters;

        /// <summary>Dictionary of symbols for incoming teleports to player spawns. Use Symbols.Default for the default spawn positions.</summary>
        public OrderedDictionary<string, List<Position>> playerSpawns;

        public List<Position> GetPlayerSpawns(string entrySymbol)
        {
            if(entrySymbol == null)
                entrySymbol = Symbols.Default;

            List<Position> retval;
            if(entrySymbol != null && playerSpawns.TryGetValue(entrySymbol, out retval))
                return retval;

            // NOTE: Rather than throwing a not-found exception, just use the default spawn (if it exists)
            if(playerSpawns.TryGetValue(Symbols.DefaultSpawn, out retval))
                return retval;

            // ... or just pick one basically at random
            var enumerator = playerSpawns.Values.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current;
        }


        public Teleporter GetTeleporterByTargetLevel(string targetLevel)
        {
            foreach(var teleporter in teleporters)
            {
                if(teleporter.TargetLevel == targetLevel)
                    return teleporter;
            }
            return null;
        }

        #endregion

        #region Geometry

        public MultiDictionary<string, Region> regions = new MultiDictionary<string, Region>();
        public MultiDictionary<string, LevelPosition> positions = new MultiDictionary<string, LevelPosition>();
        public OrderedDictionary<string, Path> paths = new OrderedDictionary<string, Path>();

        #endregion

        public List<Thing> things;
        public List<Shim> shims;

        public sbyte backgroundShimsEndIndex = -1;
        public sbyte foregroundShimsStartIndex = -1;
        public byte animatedShimCount;
        
    }
}