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