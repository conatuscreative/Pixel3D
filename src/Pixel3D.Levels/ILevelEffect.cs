// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using CRTSim;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.ActorManagement;

namespace Pixel3D.Levels
{
    public interface ILevelEffect
    {
        void LevelEffect(GameState gameState, RenderTarget2D inputRenderTarget, SpriteBatch sb, FadeEffect fadeEffect,
            int fadeLevel, bool disabled);
    }
}