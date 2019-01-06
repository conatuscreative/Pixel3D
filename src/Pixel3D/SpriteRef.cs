// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.FrameworkExtensions;

namespace Pixel3D
{
	/// <summary>Reference to a <see cref="Sprite"/> stored in a <see cref="ImageBundle"/></summary>
	public struct SpriteRef
    {
	    public ImageBundle bundle;
	    public int index;
	    public Point origin; // <- Stored here because it makes sprite de-duplication simple
		
        /// <summary>
        /// Create a reference to the given sprite in a dummy sprite bundle.
        /// NOTE: Expects the texture of the sprite to be either immutable or unshared (won't get uncached or modified)
        /// </summary>
        public SpriteRef(Sprite sprite)
        {
            bundle = new ImageBundle(sprite);
            index = 0;
            origin = sprite.origin;
        }
		
        /// <summary>
        /// Resolve this sprite reference, if it is already loaded. Otherwise queue it for loading.
        /// IMPORTANT: The result of this method is not network-safe! Use for rendering only.
        /// </summary>
        public bool ResolveBestEffort(out Sprite sprite)
        {
            // This API is capable of handling complicated threading scenarios where
            // the texture might not be ready, and we might have to pre-warm a cache and all
            // other kinds of nonsense. As it turns out, loading is far more reliable on the,
            // main thread, and far simpler if we load just-in-time, and - as it turns out -
            // we are fast enough that this will work.

            sprite = bundle.GetSprite(index, origin); // <- implicitly touches the bundle and makes it immediately ready to draw
            return true;
        }


        /// <summary>Resolve this sprite reference, or fail if the sprite is cachable at all (do not use except in editor / tools).</summary>
        public Sprite ResolveRequire()
        {
            if(bundle.IsCachable)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            return bundle.GetSprite(index, origin);
        }

	    #region Serialization

	    public void Serialize(AnimationSerializeContext context)
	    {
		    // Bundling is handled by registering images, keyed on the sprite itself, so we just pass-through:
		    ResolveRequire().Serialize(context);
	    }

	    public SpriteRef(AnimationDeserializeContext context)
	    {
		    // IMPORTANT: This method is compatible with Sprite's deserializer
		    
		    if (context.imageBundle != null)
		    {
			    bundle = context.imageBundle;
			    // NOTE: AssetTool depends on us not actually resolving the sprite during load

			    index = context.br.ReadInt32();
			    if (index != -1)
				    origin = context.br.ReadPoint();
			    else
				    origin = default(Point);
		    }
		    else // In place sprite
		    {
			    var sprite = new Sprite(context); // Pass through

			    bundle = new ImageBundle(sprite);
			    index = 0;
			    origin = sprite.origin;
		    }
	    }

	    #endregion
	}
}
