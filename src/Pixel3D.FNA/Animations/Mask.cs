// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.Extensions;
using Pixel3D.FrameworkExtensions;

namespace Pixel3D.Animations
{
    public class Mask
    {
        public Mask()
        {
            // Blank default constructor because there's another constructor for deserialization
        }


        public MaskData data;


        /// <summary>True if this mask is auto-generated based on the alpha-channel of a <see cref="Cel"/> or <see cref="AnimationFrame"/>.</summary>
        /// <remak>It is not permissable to use an alpha mask for any other purpose (this greatly simplifies code for tracking and regenerating alpha masks).</remak>
        public bool isGeneratedAlphaMask;


        #region Editor Stuff

        /// <summary>Does nothing.</summary>
        [Obsolete]
        public string friendlyName { get { return string.Empty; } set { } }

        public Mask Clone()
        {
            Mask clone = new Mask();
            clone.data = data.Clone();
            clone.isGeneratedAlphaMask = isGeneratedAlphaMask;
            return clone;
        }

        #endregion


        public Rectangle Bounds { get { return data.Bounds; } }



        /// <summary>Return mask data that has been transformed by this mask view and the supplied transformation</summary>
        public TransformedMaskData GetTransformedMaskData(Point transformPosition, bool transformFlipX)
        {
            if(transformFlipX)
                transformPosition.X = -transformPosition.X; // Flip this so that it gets unflipped when resulting mask data gets flipped

            TransformedMaskData result;
            result.flipX = transformFlipX;
            result.maskData = data.Translated(transformPosition);

            return result;
        }

        /// <summary>Return mask data that has been transformed by this mask view and the supplied transformation</summary>
        public TransformedMaskData GetTransformedMaskData(Position transformPosition, bool transformFlipX)
        {
            return GetTransformedMaskData(transformPosition.ToWorldZero(), transformFlipX);
        }

        /// <summary>Return mask data that has been transformed by this mask view (simple 2D transform of the data) and then transformed as an XZ mask on an actor</summary>
        public TransformedMaskData GetTransformedMaskDataXZ(Position transformPosition, bool transformFlipX)
        {
            // NOTE: Don't take into account Y axis. This is safe, because there is a direct conversion from Y to Z in world space.
            return GetTransformedMaskData(new Position(transformPosition.X, 0, transformPosition.Z), transformFlipX);
        }

	    #region Serialization

	    public void Serialize(AnimationSerializeContext context)
	    {
		    if (context.Version < 37)
			    context.bw.WriteNullableString(string.Empty); // was friendly name

		    context.bw.Write(isGeneratedAlphaMask);

		    Debug.Assert(!Asserts.enabled || data.Valid);
		    data.Serialize(context.bw);
	    }

	    public Mask(AnimationDeserializeContext context)
	    {
		    if (context.Version < 37)
			    context.br.ReadNullableString(); // was friendly name

		    isGeneratedAlphaMask = context.br.ReadBoolean();

		    if (context.customMaskDataReader != null)
		    {
			    // NOTE: Matches MaskData deserializing constructor:
			    var rect = context.br.ReadRectangle();
			    data = new MaskData(context.customMaskDataReader.Read(MaskData.WidthToDataWidth(rect.Width) * rect.Height), rect);
		    }
		    else
		    {
			    data = new MaskData(context.br, context.fastReadHack);
		    }
	    }

	    #endregion
	}
}
