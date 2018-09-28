// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Pixel3D.Animations;

namespace Pixel3D
{
	public interface IDrawObject
    {
        void Draw(DrawContext drawContext, int tag, IDrawSmoothProvider sp);
    }
}
