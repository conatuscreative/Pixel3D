// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.IO;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.AssetManagement;

namespace Pixel3D.Levels
{
	public class LevelSerializeContext
	{
		public readonly AnimationSerializeContext animationSerializeContext;

		public readonly BinaryWriter bw;
		public IAssetPathProvider assetPathProvider;

		/// <summary>
		///     True for save change monitoring (try to avoid mutation). Must set AnimationSerializeContext.monitor
		///     independently.
		/// </summary>
		public bool monitor;


		/// <summary>NOTE: parallel updates between serialize and deserialize</summary>
		public int nextRegionIndex = 0;

		public LevelSerializeContext(BinaryWriter bw, ImageWriter imageWriter, IAssetPathProvider assetPathProvider) :
			this(bw, imageWriter, assetPathProvider, formatVersion)
		{
		} // Default to writing current version

		public LevelSerializeContext(BinaryWriter bw, ImageWriter imageWriter, IAssetPathProvider assetPathProvider,
			int version)
		{
			this.bw = bw;
			this.assetPathProvider = assetPathProvider;
			Version = version;

			if (Version > formatVersion)
				throw new Exception("Tried to save Level with a version that is too new");
			if (Version < 13)
				throw new Exception("Level version too old!");

			bw.Write(Version);

			animationSerializeContext =
				new AnimationSerializeContext(bw, imageWriter); // <- writes out animation version number
		}


		#region Version

		/// <summary>Increment this number when anything we serialize changes</summary>
		public const int formatVersion = 20;

		public int Version { get; }

		#endregion
	}
}