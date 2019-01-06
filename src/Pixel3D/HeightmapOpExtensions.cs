// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D
{
	public static class HeightmapOpExtensions
	{
		public static bool IsShadowReceiverOperation(this HeightmapOp op)
		{
			return op == HeightmapOp.CreateExtendedObliqueFromBase ||
			       op == HeightmapOp.ExtendOblique ||
			       op == HeightmapOp.FillLeft ||
			       op == HeightmapOp.FillLeftFixedHeight ||
			       op == HeightmapOp.FillRight ||
			       op == HeightmapOp.FillRightFixedHeight;
		}
	}
}