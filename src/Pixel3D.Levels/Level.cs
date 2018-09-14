// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Pixel3D.ActorManagement;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.AssetManagement;
using Pixel3D.Engine;
using Pixel3D.Engine.Collections;
using Pixel3D.Extensions;

namespace Pixel3D.Levels
{
	public class Level : IHasReferencedAssets, IEditorNameProvider
	{
		/// <summary>Arbitrary level properties (consumers are expected to parse the strings)</summary>
		public readonly OrderedDictionary<string, string> properties = new OrderedDictionary<string, string>();

		public byte animatedShimCount;

		public sbyte backgroundShimsEndIndex = -1;

		/// <summary>Class name of level behaviour class to spawn</summary>
		public string behaviourName;

		public Rectangle cameraBounds;
		public sbyte foregroundShimsStartIndex = -1;

		/// <summary>IMPORTANT: Do not use in gameplay code (not network safe)</summary>
		[NonSerialized] public string friendlyName;

		public bool isAPitLevel;

		public AnimationSet levelAnimation;
		public List<Shim> shims;

		public List<Thing> things;

		public Level()
		{
			teleporters = new List<Teleporter>();
			playerSpawns = new OrderedDictionary<string, List<Position>>();
			things = new List<Thing>();
			shims = new List<Shim>();
		}

		public Level(Sprite sprite) : this()
		{
			SetLevelAnimationFromSprite(sprite);
		}

		/// <summary>Helper access to level walkable area heightmap</summary>
		public Heightmap Walkable
		{
			get { return levelAnimation.Heightmap; }
		}

		/// <summary>Helper access to level ceiling heightmap</summary>
		public Heightmap Ceiling
		{
			get { return levelAnimation.Ceiling; }
		}

		public int MaxVisibleHeight
		{
			get { return cameraBounds.WorldTop() - Walkable.StartZ; }
		}

		public string EditorName
		{
			get { return friendlyName; }
		}

		public void SetLevelAnimationFromSprite(Sprite sprite)
		{
			levelAnimation = AnimationSet.CreateSingleSprite(sprite);
			levelAnimation.Heightmap = new Heightmap(Heightmap.Infinity);
		}

		#region Serialization

		public void RegisterImages(ImageWriter imageWriter, IAssetPathProvider assetPathProvider)
		{
			// NOTE: Using assetPathProvider to check if the item is embedded
			//       (Kind of ugly, as it duplicates the condition in LevelSerializeContext)

			if (levelAnimation != null && assetPathProvider.GetAssetPath(levelAnimation) == null)
				levelAnimation.RegisterImages(imageWriter);
			foreach (var thing in things)
				if (thing.AnimationSet != null && assetPathProvider.GetAssetPath(thing.AnimationSet) == null)
					thing.AnimationSet.RegisterImages(imageWriter);
			foreach (var shim in shims)
				if (shim.AnimationSet != null && assetPathProvider.GetAssetPath(shim.AnimationSet) == null)
					shim.AnimationSet.RegisterImages(imageWriter);
		}

		#endregion

		#region IHasReferencedAssets Members

		public IEnumerable<object> GetReferencedAssets()
		{
			yield return levelAnimation;
			foreach (var thing in things)
				yield return thing.AnimationSet;
			foreach (var shim in shims)
				yield return shim.AnimationSet;
		}

		public void ReplaceAsset(object search, object replace)
		{
			if (search is AnimationSet)
			{
				if (ReferenceEquals(levelAnimation, search))
					levelAnimation = (AnimationSet) replace;

				foreach (var thing in things)
					if (ReferenceEquals(thing.AnimationSet, search))
						thing.AnimationSet = (AnimationSet) replace;
				foreach (var shim in shims)
					if (ReferenceEquals(shim.AnimationSet, search))
						shim.AnimationSet = (AnimationSet) replace;
			}
		}

		#endregion

		#region Teleports

		/// <summary>List of outgoing teleporters</summary>
		public List<Teleporter> teleporters;

		/// <summary>
		///     Dictionary of symbols for incoming teleports to player spawns. Use Symbols.Default for the default spawn
		///     positions.
		/// </summary>
		public OrderedDictionary<string, List<Position>> playerSpawns;

		public List<Position> GetPlayerSpawns(string entrySymbol)
		{
			if (entrySymbol == null)
				entrySymbol = Symbols.Default;

			List<Position> retval;
			if (entrySymbol != null && playerSpawns.TryGetValue(entrySymbol, out retval))
				return retval;

			// NOTE: Rather than throwing a not-found exception, just use the default spawn (if it exists)
			if (playerSpawns.TryGetValue(Symbols.DefaultSpawn, out retval))
				return retval;

			// ... or just pick one basically at random
			var enumerator = playerSpawns.Values.GetEnumerator();
			enumerator.MoveNext();
			return enumerator.Current;
		}


		public Teleporter GetTeleporterByTargetLevel(string targetLevel)
		{
			foreach (var teleporter in teleporters)
				if (teleporter.TargetLevel == targetLevel)
					return teleporter;
			return null;
		}

		#endregion

		#region Geometry

		public MultiDictionary<string, Region> regions = new MultiDictionary<string, Region>();
		public MultiDictionary<string, LevelPosition> positions = new MultiDictionary<string, LevelPosition>();
		public OrderedDictionary<string, Path> paths = new OrderedDictionary<string, Path>();

		#endregion
	}
}