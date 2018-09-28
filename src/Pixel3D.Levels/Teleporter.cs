// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Pixel3D.AssetManagement;

namespace Pixel3D.Levels
{
	public class Teleporter : Region, IEditorNameProvider
	{
		/// <summary>Set to true if you never want this teleporter to appear in random (or nearest/furthest) selections</summary>
		public bool neverSelectAtRandom;

		/// <summary>The asset path of the level to teleport to, or null for no teleport</summary>
		private string targetLevel;

		/// <summary>The target symbol the spawn points to use</summary>
		public string targetSpawn;

		// Provided to allow parameterless construction (due to presence of deserialization constructor)

		public string TargetLevel
		{
			get { return targetLevel; }
			set { targetLevel = value == null ? null : AssetManager.CanonicaliseAssetPath(value); }
		}


		public string EditorName
		{
			get { return string.Format("{0}_{1}", TargetLevel, targetSpawn); }
		}
	}
}