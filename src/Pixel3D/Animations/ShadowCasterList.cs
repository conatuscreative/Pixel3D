// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Extensions;

namespace Pixel3D.Animations
{
    public class ShadowCasterList
    {
        struct StartEnd { public int start, end; }

        /// <summary>Bounds for culling casters on the X axis (can't cull on Y/Z because we don't know the height the shadow will draw at)</summary>
        List<StartEnd> shadowCasterXBounds = new List<StartEnd>();


        private struct ShadowCaster
        {
            public List<ShadowLayer> shadows;

            public int physicsStartX, physicsEndX;

            public Position position;
            public Position shadowOffset; // <- NOTE: Pre-flipped!
            public bool flipX;

            public Color color;
        }

        List<ShadowCaster> shadowCasters = new List<ShadowCaster>();

        List<int> shadowLayers = new List<int>();


        public void Clear()
        {
            shadowCasterXBounds.Clear();
            shadowCasters.Clear();
            shadowLayers.Clear();
        }



        public void AddShadowCaster(List<ShadowLayer> shadows, Bounds shadowBounds,
                int physicsStartX, int physicsEndX, Position position, Position shadowOffset, bool flipX)
        {
            AddShadowCaster(shadows, shadowBounds, physicsStartX, physicsEndX, position, shadowOffset, flipX, Color.White, 0);
        }

        public void AddShadowCaster(List<ShadowLayer> shadows, Bounds shadowBounds,
                int physicsStartX, int physicsEndX, Position position, Position shadowOffset, bool flipX,
                Color color, int layer)
        {
            // NOTE: Right now we take physicsStartX/physicsEndX for character physics
            //       If we start doing shadows with heightmap physics, we'll need to take a heightmap as well

            StartEnd xBounds = flipX ?
                    new StartEnd { start = shadowBounds.startX, end = shadowBounds.endX } :
                    new StartEnd { start = -(shadowBounds.endX-1), end = -(shadowBounds.startX-1) };
            xBounds.start += position.X + shadowOffset.X;
            xBounds.end += position.X + shadowOffset.X;

            ShadowCaster shadowCaster = new ShadowCaster
            {
                shadows = shadows,

                physicsStartX = physicsStartX,
                physicsEndX = physicsEndX,

                position = position,
                shadowOffset = shadowOffset,
                flipX = flipX,

                color = color,
            };

            if(flipX)
                shadowCaster.shadowOffset.X = -shadowCaster.shadowOffset.X;


            // Determine insertion point:
            int i;
            for(i = shadowLayers.Count; i > 0; --i)
            {
                if(shadowLayers[i-1] <= layer)
                    break;
            }

            shadowLayers.Insert(i, layer);
            shadowCasterXBounds.Insert(i, xBounds);
            shadowCasters.Insert(i, shadowCaster);
        }


        public void DrawShadowReceiver(DrawContext context, Sprite whitemask, ShadowReceiver shadowReceiver, Position shadowReceiverPosition, bool shadowReceiverFlipX)
        {
            Bounds receiverBounds = new Bounds(whitemask.WorldSpaceBounds).MaybeFlipX(shadowReceiverFlipX) + shadowReceiverPosition.ToWorldZero();

            bool startedReceivingShadows = false;

            // For each shadow caster:
            Debug.Assert(shadowCasterXBounds.Count == shadowCasters.Count);
            for(int i = 0; i < shadowCasterXBounds.Count; i++)
            {
                StartEnd casterWorldXBounds = shadowCasterXBounds[i];
                if(casterWorldXBounds.start >= receiverBounds.endX || receiverBounds.startX >= casterWorldXBounds.end)
                    continue; // Horizontally out of range

                var shadowCaster = shadowCasters[i];

                // Determine the ground height of the shadow:
                int groundHeight;
                if(shadowReceiver.heightmap.HasData)
                {
                    var heightmapView = new HeightmapView(shadowReceiver.heightmap, shadowReceiverPosition, shadowReceiverFlipX);
                    int startX = shadowCaster.position.X + shadowCaster.physicsStartX;
                    int endX = shadowCaster.position.X + shadowCaster.physicsEndX;
                    if(endX <= startX)
                        endX = startX + 1; // <- whoops our shadow caster has no width... quick fix-up for now...

                    groundHeight = heightmapView.GetHeightForShadow(startX, endX, shadowCaster.position.Z, shadowReceiver.heightmapExtendDirection);
                }
                else
                {
                    groundHeight = shadowReceiver.heightmap.DefaultHeight + shadowReceiverPosition.Y;
                }

                int shadowDifference = shadowCaster.position.Y - groundHeight;


                // NOTE: shadowOffset is pre-flipped
                Position shadowPosition = new Position(shadowCaster.position.X, groundHeight, shadowCaster.position.Z) + shadowCaster.shadowOffset;


                if(shadowCaster.shadows[0].startHeight > shadowDifference)
                    continue; // Out of range

                int shadowIndex = 0;
                while(shadowIndex + 1 < shadowCaster.shadows.Count)
                {
                    if(shadowCaster.shadows[shadowIndex + 1].startHeight > shadowDifference)
                        break;
                    shadowIndex++;
                }
                Sprite shadowSprite;
                if(!shadowCaster.shadows[shadowIndex].shadowSpriteRef.ResolveBestEffort(out shadowSprite))
                    continue;

                
                // Determine whether the shadow overlaps the whitemask
                Bounds shadowBounds = new Bounds(shadowSprite.WorldSpaceBounds).MaybeFlipX(shadowCaster.flipX) + shadowPosition.ToWorldZero();
                Bounds worldZeroDrawBounds = Bounds.Intersection(receiverBounds, shadowBounds);
                if(!worldZeroDrawBounds.HasPositiveArea)
                    continue;

                if(!startedReceivingShadows)
                {
                    startedReceivingShadows = true;
                    context.SetupShadowReceiver(whitemask, shadowReceiver, shadowReceiverPosition, shadowReceiverFlipX);
                }


                // Drawing with clipping:
                {
                    Rectangle clipRect = new Rectangle // <- Region within the source rectangle (NOTE: initially with Y+ up, we fix later)
                    {
                        X = worldZeroDrawBounds.startX - shadowBounds.startX,
                        Y = worldZeroDrawBounds.startY - shadowBounds.startY,
                        Width = worldZeroDrawBounds.endX - worldZeroDrawBounds.startX,
                        Height = worldZeroDrawBounds.endY - worldZeroDrawBounds.startY,
                    };

                    // Flipping:
                    if(shadowCaster.flipX)
                        clipRect.X = shadowSprite.sourceRectangle.Width - (clipRect.X + clipRect.Width); // RegionFlipX
                    // IMPORTANT: Convert clipRect from Y+ Up coordinates to correct Texel coordinates
                    clipRect.Y = shadowSprite.sourceRectangle.Height - (clipRect.Y + clipRect.Height); // RegionFlipY

                    // Turn it into a source rectangle within the texture:
                    clipRect.X += shadowSprite.sourceRectangle.X;
                    clipRect.Y += shadowSprite.sourceRectangle.Y;
                    
                    Vector2 displayPosition = new Vector2(worldZeroDrawBounds.startX, -(worldZeroDrawBounds.startY + clipRect.Height));
                    // NOTE: don't need to consider origin, because it's built into the draw bounds
                    context.SpriteBatch.Draw(shadowSprite.texture, displayPosition, clipRect, shadowCaster.color, 0f, Vector2.Zero, 1,
                            shadowCaster.flipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
                }
            }


            if(startedReceivingShadows)
                context.TeardownShadowReceiver();
        }

    }
}
