// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using Microsoft.Xna.Framework;
using Pixel3D.Extensions;

namespace Pixel3D
{
    public class PanCamera
    {
        Point worldPanTarget;
        
        /// <summary>Depends on global Input</summary>
        public void Update(Camera camera)
        {
            if(Input.MiddleMouseWentDown)
            {
                SetWorldPanTarget(camera, Input.MousePosition);
            }
            else if(Input.IsMiddleMouseDown)
            {
                Pan(camera, Input.MousePosition);
            }
        }

        public void SetWorldPanTarget(Camera camera, Point mousePosition)
        {
            worldPanTarget = camera.ScreenToWorldZero(mousePosition);
        }

        public void Pan(Camera camera, Point mousePosition)
        {
            Point current = camera.ScreenToWorldZero(mousePosition);
            Point delta = worldPanTarget.Subtract(current);
            camera.WorldTarget += new Position(delta.X, delta.Y, 0);
        }

        public void Reset(Camera camera)
        {
            camera.WorldTarget = Position.Zero;
        }
    }
}
