using System.Globalization;
using Pixel3D.Animations;

namespace Pixel3D
{
	public class HeightmapInstruction
    {
        public HeightmapInstruction()
        {
            // Blank constructor (because we have a deserialize constructor)
        }

        /// <summary> Shallow copy constructor for editor </summary>
        public HeightmapInstruction(HeightmapInstruction copy)
        {
            Operation = copy.Operation;
            Mask = copy.Mask;

            Height = copy.Height;
            ObliqueDirection = copy.ObliqueDirection;
            FrontEdgeDepth = copy.FrontEdgeDepth;
            Depth = copy.Depth;
            Slope = copy.Slope;
            Offset = copy.Offset;
        }

        public HeightmapOp Operation { get; set; }
        public Mask Mask { get; set; }

        // Possible arguments:
        public byte Height { get; set; }
        public Oblique ObliqueDirection  { get; set; }
        public int FrontEdgeDepth  { get; set; }
        public int Depth  { get; set; }
        public int Slope { get; set; }
        public int Offset { get; set; }
        
        /// <param name="heightmap">The heightmap to apply the instruction to</param>
        /// <param name="baseHeightmap">When creating ShadowReceiver heightmaps, the AnimationSet heightmap</param>
        public void Process(Heightmap heightmap, Heightmap baseHeightmap)
        {
            switch(Operation)
            {
                case HeightmapOp.ClearToHeight:
                    heightmap.ClearToHeight(Height);
                    break;
                case HeightmapOp.SetFromFlatBaseMask:
                    heightmap.SetFromFlatBaseMask(Mask.data, Height);
                    break;
                case HeightmapOp.SetFromFlatTopMask:
                    heightmap.SetFromFlatTopMask(Mask.data, Height);
                    break;
                case HeightmapOp.SetFromObliqueTopMask:
                    heightmap.SetFromObliqueTopMask(Mask.data, FrontEdgeDepth, ObliqueDirection);
                    break;
                case HeightmapOp.SetFromRailingMask:
                    heightmap.SetFromRailingMask(Mask.data);
                    break;
                case HeightmapOp.SetFromFrontEdge:
                    heightmap.SetFromFrontEdge(Mask.data, FrontEdgeDepth, Depth, ObliqueDirection, Slope, Offset);
                    break;
                case HeightmapOp.SetFlatRelative:
                    heightmap.SetFlatRelative(Mask.data, Height, Offset);
                    break;
                case HeightmapOp.SetFromSideOblique:
                    heightmap.SetFromObliqueSide(Mask.data, ObliqueDirection, Offset);
                    break;
                case HeightmapOp.CreateExtendedObliqueFromBase:
                    {
                        var temp = baseHeightmap.CreateExtendedOblique(ObliqueDirection);
                        heightmap.heightmapData = temp.heightmapData; // <- suck out its brains
                        heightmap.DefaultHeight = temp.DefaultHeight;
                    }
                    break;
                case HeightmapOp.ExtendOblique:
                    {
                        var temp = heightmap.CreateExtendedOblique(ObliqueDirection);
                        heightmap.heightmapData = temp.heightmapData; // <- suck out its brains
                        heightmap.DefaultHeight = temp.DefaultHeight;
                    }
                    break;
                case HeightmapOp.FillLeft:
                    heightmap.FillLeft(null);
                    break;
                case HeightmapOp.FillLeftFixedHeight:
                    heightmap.FillLeft(Height);
                    break;
                case HeightmapOp.FillRight:
                    heightmap.FillRight(null);
                    break;
                case HeightmapOp.FillRightFixedHeight:
                    heightmap.FillRight(Height);
                    break;
            }
        }

        #region Editor

        /// <summary> Represents a friendly, pre-editing view of the values for an instruction </summary>
        public override string ToString()
        {
            string formatString;
            switch(Operation)
            {
                case HeightmapOp.ClearToHeight:
                case HeightmapOp.SetFromFlatBaseMask:
                case HeightmapOp.SetFromFlatTopMask:
                    formatString = "Height = {4}";
                    break;

                case HeightmapOp.SetFromObliqueTopMask:
                    formatString = "FrontEdgeDepth = {0}, Oblique = {1}";
                    break;
                case HeightmapOp.SetFromRailingMask:
                    formatString = "(mask-only)";
                    break;

                case HeightmapOp.SetFromFrontEdge:
                    formatString = "FrontEdgeDepth = {0}, Depth = {2}, Oblique = {1}, Slope = {3}, Offset = {5}";
                    break;

                case HeightmapOp.SetFlatRelative:
                    formatString = "Height = {4}, Offset = {5}";
                    break;

                case HeightmapOp.SetFromSideOblique:
                    formatString = "Oblique = {1}, Offset = {5}";
                    break;

                case HeightmapOp.CreateExtendedObliqueFromBase:
                case HeightmapOp.ExtendOblique:
                    formatString = "Oblique = {1}";
                    break;

                case HeightmapOp.FillLeft:
                    formatString = "(no parameters)";
                    break;
                case HeightmapOp.FillLeftFixedHeight:
                    formatString = "Height = {4}";
                    break;
                case HeightmapOp.FillRight:
                    formatString = "(no parameters)";
                    break;
                case HeightmapOp.FillRightFixedHeight:
                    formatString = "Height = {4}";
                    break;

                default:
                    formatString = "Unknown Operation";
                    break;
            }

            return string.Format(formatString,
                    FrontEdgeDepth, // 0
                    ObliqueDirection, // 1
                    Depth, // 2
                    Slope, // 3
                    Height == Heightmap.Infinity ? "Infinity" : Height.ToString(CultureInfo.InvariantCulture), // 4
                    Offset); // 5
        }


        public bool RequiresMask
        {
            get
            {
                switch(Operation)
                {
                    case HeightmapOp.ClearToHeight:
                    case HeightmapOp.FillLeft:
                    case HeightmapOp.FillRight:
                    case HeightmapOp.FillLeftFixedHeight: 
                    case HeightmapOp.FillRightFixedHeight:
                    case HeightmapOp.CreateExtendedObliqueFromBase:
                    case HeightmapOp.ExtendOblique:
                        return false;

                    default:
                        return true;
                }
            }
        }

		#endregion

	    #region Serialization

	    // NOTE: Because heightmap instructions will eventually only be stored in the unoptimised data
	    //       we can be a little bit lazy and just serialize all arguments, even if they are not actually
	    //       used by the given op-code.

	    // NOTE: Masks are not shared for HeightmapInstruction (otherwise we get a nasty circular dependency through ShadowReceiver)

	    public void Serialize(AnimationSerializeContext context)
	    {
		    context.bw.Write((int)Operation);

		    context.bw.Write(Height);
		    context.bw.Write(ObliqueDirection);
		    context.bw.Write(FrontEdgeDepth);
		    context.bw.Write(Depth);
		    context.bw.Write(Slope);
		    context.bw.Write(Offset);

		    context.bw.Write(Mask != null);

		    if (Mask != null)
			    Mask.Serialize(context);
	    }

	    /// <summary>Deserialize into a new object instance</summary>
	    public HeightmapInstruction(AnimationDeserializeContext context)
	    {
		    Operation = (HeightmapOp) context.br.ReadInt32();
		    Height = context.br.ReadByte();
		    ObliqueDirection = context.br.ReadOblique();
		    FrontEdgeDepth = context.br.ReadInt32();
		    Depth = context.br.ReadInt32();
		    Slope = context.br.ReadInt32();
		    Offset = context.br.ReadInt32();

			Mask = context.br.ReadBoolean() ? new Mask(context) : null;
	    }

	    #endregion
	}
}
