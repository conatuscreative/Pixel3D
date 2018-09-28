// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D
{
	/// <summary>Heightmap operations (these match methods in Heightmap)</summary>
	/// <remarks>Values are serialization sensitive!</remarks>
	public enum HeightmapOp
	{
		ClearToHeight = 0,
		SetFromFlatBaseMask = 1,
		SetFromFlatTopMask = 2,
		SetFromObliqueTopMask = 3,
		SetFromRailingMask = 4,
		SetFromFrontEdge = 5,
		SetFlatRelative = 12, // New in version 8
		SetFromSideOblique = 13, // New in version 9

		// ShadowReceiver-related instructions:

		/// <summary>For ShadowReceiver, creates from the AnimationSet heightmap</summary>
		CreateExtendedObliqueFromBase = 6,
		ExtendOblique = 7,
		FillLeft = 8,
		FillLeftFixedHeight = 9,
		FillRight = 10,
		FillRightFixedHeight = 11,
	}
}