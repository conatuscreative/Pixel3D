using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Pixel3D.Animations.Serialization;

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
            this.bundle = new ImageBundle(sprite);
            this.index = 0;
            this.origin = sprite.origin;
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
    }
}
