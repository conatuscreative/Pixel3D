// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Microsoft.Xna.Framework;

namespace Pixel3D
{
    public class CubeCornerColors
    {
        public CubeCornerColors() { } // <- allow default construction

        public CubeCornerColors(Color primaryColor)
        {
            Color secondaryColor = Color.Lerp(Color.Black, primaryColor, 0.5f);

            BottomFrontLeft  = primaryColor;
            BottomBackLeft   = secondaryColor;
            BottomFrontRight = primaryColor;
            BottomBackRight  = secondaryColor;
            TopFrontLeft     = primaryColor;
            TopBackLeft      = secondaryColor;
            TopFrontRight    = primaryColor;
            TopBackRight     = secondaryColor;
        }


        public Color BottomFrontLeft  { get; set; }
        public Color BottomBackLeft   { get; set; }
        public Color BottomFrontRight { get; set; }
        public Color BottomBackRight  { get; set; }
        public Color TopFrontLeft     { get; set; }
        public Color TopBackLeft      { get; set; }
        public Color TopFrontRight    { get; set; }
        public Color TopBackRight     { get; set; }
    }
}
