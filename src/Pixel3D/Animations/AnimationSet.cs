using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.IO;
using Common;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;
using Pixel3D.Animations.Serialization;
using Pixel3D.Serialization;

namespace Pixel3D.Animations
{
    public class AnimationSet : IEditorNameProvider
    {
        public AnimationSet()
        {
            animations = new TagLookup<Animation>();
        }

	    /// <summary>IMPORTANT: Do not use in gameplay code (not network safe)</summary>
        [NonSerialized]
        public string friendlyName;

        public string EditorName => friendlyName;

	    /// <summary>Class name of default class to spawn for this AnimationSet</summary>
        public string behaviour;

        /// <summary>General purpose cue; used for ambient sounds, boss sounds, level animation sounds, etc. </summary>
        public string cue;

		#region Animations

        /// <summary>Animations without associated TagSets (but we still want to store them)</summary>
        public List<Animation> unusedAnimations = null;
        public readonly TagLookup<Animation> animations;

        public IEnumerable<Animation> AllAnimations()
        {
            return unusedAnimations == null ? animations.Values : animations.Values.Concat(unusedAnimations);
        }

        /// <summary>Get an animation given a context</summary>
        /// <remarks>(Convenience pass-through)</remarks>
        public Animation this[TagSet context]
        {
            get { return animations[context]; }
        }

        /// <summary>Get an animation given a context</summary>
        /// <remarks>(Convenience pass-through)</remarks>
        public Animation this[string context]
        {
            get { return animations[context]; }
        }

        #endregion
		
        /// <summary>Texture space origin to line up all imported images by their top-left corners (for editor use)</summary>
        public Point importOrigin;
		
        #region Shadows

        public List<ShadowLayer> shadowLayers;

        /// <summary>The maximum draw boundary of the shadow (calculated to save time)</summary>
        public Bounds cachedShadowBounds;


        public void SortShadowLayers()
        {
            if(shadowLayers == null)
                return;
            // So lazy:
            shadowLayers = shadowLayers.OrderBy(sl => sl.startHeight).ToList();
        }

        public bool CheckShadowLayerOrder()
        {
            if(shadowLayers == null)
                return true;

            for(int i = 1; i < shadowLayers.Count; i++)
            {
                if(shadowLayers[i-1].startHeight >= shadowLayers[i].startHeight)
                    return false;
            }

            return true;
        }

        public void RecalculateCachedShadowBounds()
        {
            cachedShadowBounds = Bounds.InfiniteInverse;

            if(shadowLayers != null)
            {
                foreach(var shadowLayer in shadowLayers)
                {
                    var shadowSprite = shadowLayer.shadowSpriteRef.ResolveRequire();
                    if(shadowSprite.texture != null)
                    {
                        var bounds = new Bounds(shadowSprite.WorldSpaceBounds);
                        cachedShadowBounds = cachedShadowBounds.Combine(bounds);
                    }
                }
            }

            if(!cachedShadowBounds.IsValid)
                cachedShadowBounds = default(Bounds);
        }

        #endregion
		
        #region Physics Bounds

        // StartX, StartZ, EndX and Height may be manually set for "flat" objects (no heightmap)
        // and are auto-set for objects with heightmaps. EndZ is *always* auto-set.
        // FlatDirection is only used for generating Z-sort data for objects without heightmaps.
        public int physicsStartX = 0, physicsEndX = 1, physicsStartZ = 0, physicsEndZ = 0, physicsHeight = 0;
        public int coplanarPriority = 0;
        public Oblique flatDirection; // <- "straight" really means "flat" here
        


        /// <summary>Returns the physics as an AABB (who's bounds are fully inclusive), with at least 1 pixel of volume</summary>
        public AABB GetPhysicsAsAABB()
        {
            AABB result = new AABB(physicsStartX, System.Math.Max(physicsStartX, physicsEndX - 1),
                    0, System.Math.Max(0, physicsHeight),
                    physicsStartZ, System.Math.Max(physicsStartZ, physicsEndZ - 1));

            return result;
        }


        public void AutoGeneratePhysicsAndDepthBounds()
        {
            if(Heightmap != null)
                RegeneratePhysicsAndDepthBounds();
            else if(animations.Rules.SelectMany(r => r).Contains("Walk")) // Probably a character animation
                SetPhysicsForCharacter();
            else
                SetPhysicsForFlat();
        }


        public void RegeneratePhysicsAndDepthBounds()
        {
            if(Heightmap != null)
            {
                physicsStartX = Heightmap.StartX;
                physicsEndX = Heightmap.EndX;
                physicsStartZ = Heightmap.StartZ;
                physicsEndZ = Heightmap.EndZ;

                // Levels don't get a height set:
                physicsHeight = Heightmap.IsObjectHeightmap ? Heightmap.GetMaxHeight() : 0;
            }

            RegenerateDepthBounds();
        }

        public const int defaultCharacterStartX = -8;
        public const int defaultCharacterEndX = 9;
        public const int defaultCharacterHeight = 32;

        public void SetPhysicsForCharacter()
        {
            if(Heightmap != null)
                return;

            // Default values for a character (based on default CharacterPhysics)
            physicsStartX = defaultCharacterStartX;
            physicsEndX = defaultCharacterEndX;
            physicsStartZ = 0;
            physicsEndZ = 0;
            physicsHeight = defaultCharacterHeight;
            flatDirection = Oblique.Straight;

            RegenerateDepthBounds();
        }

        public void SetPhysicsForFlat()
        {
            if(Heightmap != null)
                return;

            // Default values for a static prop
            if (HasDefaultAnimation)
            {
                var bounds = DefaultAnimation.CalculateGraphicsBounds();
                physicsStartX = bounds.X;
                physicsEndX = bounds.X + bounds.Width;
                physicsStartZ = 0;
                physicsEndZ = 0;
                physicsHeight = System.Math.Max(0, bounds.Y + bounds.Height); // Z-sort disregards anything below-ground
                flatDirection = Oblique.Straight;
            }
            

            RegenerateDepthBounds();
        }

        public void SetPhysicsForCarpet()
        {
            if(Heightmap != null)
                return;

            // Default values for a static prop
            if(HasDefaultAnimation)
            {
                var bounds = DefaultAnimation.CalculateGraphicsBounds();
                physicsStartX = bounds.X;
                physicsEndX = bounds.X + bounds.Width;
                physicsStartZ = bounds.Y;
                physicsEndZ = bounds.Y + bounds.Height;
                physicsHeight = 0; // <- The magic.
                flatDirection = Oblique.Straight; // <- Arbitrary
            }

            RegenerateDepthBounds();
        }


        #endregion

		#region Z-Bounds

        /// <summary>Z-sort should do a static physics vs mover "above" check when sorting</summary>
        public bool doAboveCheck = false;

        public DepthBounds depthBounds;

        public void RegenerateDepthBounds()
        {
            if(Heightmap == null)
            {
                if(physicsHeight == 0)
                {
                    depthBounds = default(DepthBounds);
                }
                else
                {
                    physicsEndZ = physicsStartZ + DepthBounds.CalculateFlatPhysicsDepth(physicsEndX - physicsStartX, flatDirection);
                    depthBounds = DepthBounds.CreateForFlat(this);
                }
            }
            else
            {
                depthBounds = DepthBounds.CreateForHeightmap(Heightmap);
            }
        }

        #endregion

		#region Heightmap

        public Heightmap Heightmap { get; set; }

        public void RefreshDependentHeightmaps()
        {
            // Refresh ShadowReceiver heightmaps (as they may be taking data from this heightmap)
            foreach(var shadowReceiver in AllShadowReceivers())
            {
                shadowReceiver.heightmap.RefreshFromInstructions(this.Heightmap);
            }
        }
        
        #endregion

		#region Ceiling

        public Heightmap Ceiling { get; set; }

        #endregion
		
        #region Editor Helpers

        /// <summary>Get the set of all unused <see cref="Animation"/>s</summary>
        public IEnumerable<KeyValuePair<TagSet, Animation>> GetAllUnusedAnimations()
        {
            if (unusedAnimations == null) yield break;
            foreach (var animation in unusedAnimations)
            {
                yield return new KeyValuePair<TagSet, Animation>(null, animation);
            }
        }

        /// <summary>Return all animations and their TagSet that have assigned TagSets</summary>
        public IEnumerable<KeyValuePair<TagSet, Animation>> GetAllUsedAnimations()
        {
            {
                for (int i = 0; i < animations.Count; i++)
                    yield return new KeyValuePair<TagSet, Animation>(animations.Rules[i], animations.Values[i]);
            }
        }

        public Cel GetDefaultHeightmapBackground()
        {
            // TODO: Should this maybe use DefaultAnimation?
            var all = AllAnimations().ToArray();
            if (!all.Any()) return null;

            var animation = all.ToArray()[0];
            if (animation.FrameCount == 0) return null;
            
            return animation.Frames[0].layers[0];
        }

        #endregion

		#region Serialize

        public void RegisterImages(ImageWriter imageWriter)
        {
            // NOTE: This mutates the images as they are registered, to account for trimming that RegisterImage does.
            //       (Kinda ugly, but too late to come up with something better.)

            var cels = AllAnimations().SelectMany(a => a.Frames).SelectMany(f => f.layers);
            foreach(var cel in cels)
            {
                var sprite = cel.spriteRef.ResolveRequire();
                var newSprite = imageWriter.RegisterImage(sprite);

                if(sprite != newSprite)
                    cel.spriteRef = new SpriteRef(newSprite);
            }

            if(shadowLayers != null)
            {
                for(int i = 0; i < shadowLayers.Count; i++)
                {
                    var sl = shadowLayers[i];
                    var shadowSprite = sl.shadowSpriteRef.ResolveRequire();
                    var newShadowSprite = imageWriter.RegisterImage(shadowSprite);

                    if(shadowSprite != newShadowSprite)
                    {
                        sl.shadowSpriteRef = new SpriteRef(newShadowSprite);
                        shadowLayers[i] = sl;
                    }
                }
            }
        }


        [SerializationIgnoreDelegates]
        public void Serialize(AnimationSerializeContext context)
        {
            if(Asserts.enabled && !ValidateAlphaMasks())
                throw new InvalidOperationException("Attempting to save animation set with missing or invalid alpha masks");

            context.bw.WriteNullableString(friendlyName);
            context.bw.Write(importOrigin);
            context.bw.WriteNullableString(behaviour);

            if(context.bw.WriteBoolean(Heightmap != null))
                Heightmap.Serialize(context);


            // If you don't seem to have set any physics bounds, I will just generate them...
            if((physicsStartX == 0 && physicsEndX == 1 && physicsStartZ == 0 && physicsEndZ == 1 && physicsHeight == 0) || physicsEndX - physicsStartX <= 0)
            {
                AutoGeneratePhysicsAndDepthBounds();
            }


            // NOTE: only writing out values that cannot be auto-generated
            if(Heightmap != null)
            {
                Debug.Assert(!Asserts.enabled || Heightmap.IsObjectHeightmap || physicsHeight == 0);
                context.bw.Write(physicsHeight);
                if(physicsHeight > 0)
                    depthBounds.Serialize(context);
                context.bw.Write(flatDirection); // <- for the sake of editing, keep this value around
            }
            else
            {
                context.bw.Write(physicsStartX);
                context.bw.Write(physicsEndX);
                context.bw.Write(physicsStartZ);
                context.bw.Write(physicsHeight);
                context.bw.Write(flatDirection);

                if(physicsHeight == 0)
                    context.bw.Write(physicsEndZ);
            }

            if(context.Version >= 38)
                context.bw.Write(coplanarPriority);

            if(context.Version >= 36)
                context.bw.Write(doAboveCheck);

            if(context.bw.WriteBoolean(Ceiling != null))
                Ceiling.Serialize(context);


            animations.SerializeTagLookup(context, a => a.Serialize(context));


            // Unused Animations
            {
                if(unusedAnimations == null)
                    context.bw.Write(0); // unused animations is lazy-initialized
                else
                {
                    context.bw.Write(unusedAnimations.Count);
                    foreach(var animation in unusedAnimations)
                        animation.Serialize(context);
                }
            }

            context.bw.WriteNullableString(cue);


            // Shadow layers:
            {
                if(shadowLayers == null)
                    context.bw.Write((int)0);
                else
                {
                    context.bw.Write(shadowLayers.Count);
                    foreach(var sl in shadowLayers)
                    {
                        context.bw.Write(sl.startHeight);
                        sl.shadowSpriteRef.Serialize(context);
                    }

                    RecalculateCachedShadowBounds();
                    context.bw.Write(cachedShadowBounds);
                }
            }
		}
		
        /// <summary>Deserialize into new object instance</summary>
        [SerializationIgnoreDelegates]
        public AnimationSet(AnimationDeserializeContext context)
        {
            friendlyName = context.br.ReadNullableString();
            importOrigin = context.br.ReadPoint();
            behaviour = context.br.ReadNullableString();

            if(context.br.ReadBoolean())
                Heightmap = new Heightmap(context);

            if(Heightmap != null)
            {
                physicsStartX = Heightmap.StartX;
                physicsEndX = Heightmap.EndX;
                physicsStartZ = Heightmap.StartZ;
                physicsEndZ = Heightmap.EndZ;
                // Assume that reading is faster than walking the heightmap:
                physicsHeight = context.br.ReadInt32();
                if(physicsHeight > 0)
                    depthBounds = new DepthBounds(context);
                flatDirection = context.br.ReadOblique(); // <- for the sake of editing, keep this value around
            }
            else
            {
                physicsStartX = context.br.ReadInt32();
                physicsEndX = context.br.ReadInt32();
                physicsStartZ = context.br.ReadInt32();
                physicsHeight = context.br.ReadInt32();
                flatDirection = context.br.ReadOblique();

                if(physicsHeight == 0)
                    physicsEndZ = context.br.ReadInt32(); // physicsEndZ gets auto-set during regen, except for carpets

                RegenerateDepthBounds(); // <- Know this is reasonably fast to generate
            }

            if(context.Version >= 38)
                coplanarPriority = context.br.ReadInt32();

            if(context.Version >= 36)
                doAboveCheck = context.br.ReadBoolean();

            if(context.br.ReadBoolean())
                Ceiling = new Heightmap(context);

            animations = context.DeserializeTagLookup(() => new Animation(context));


            // Unused Animations
            {
                int count = context.br.ReadInt32();
                if(count > 0) // unusedAnimations is lazy-initialized
                {
                    unusedAnimations = new List<Animation>(count);
                    for(int i = 0; i < count; i++)
                        unusedAnimations.Add(new Animation(context));
                }
            }

            cue = context.br.ReadNullableString();

			// Shadow layers
            {
                int shadowLayerCount = context.br.ReadInt32();
                if(shadowLayerCount <= 0)
                    shadowLayers = null;
                else
                {
                    shadowLayers = new List<ShadowLayer>();
                    for(int i = 0; i < shadowLayerCount; i++)
                    {
                        shadowLayers.Add(new ShadowLayer(
                                context.br.ReadInt32(),
                                new SpriteRef(context)));
                    }

                    cachedShadowBounds = context.br.ReadBounds();
                }
            }
        }
		
        /// <summary>Check that an AnimationSet round-trips through serialization cleanly</summary>
        public void RoundTripCheck(GraphicsDevice graphicsDevice, bool useExternalImages)
        {
            // Serialize a first time
            MemoryStream firstMemoryStream = new MemoryStream();
            BinaryWriter firstBinaryWriter = new BinaryWriter(firstMemoryStream);
            ImageWriter firstImageWriter = null;
            if(useExternalImages)
            {
                firstImageWriter = new ImageWriter();
                this.RegisterImages(firstImageWriter);
                firstImageWriter.WriteOutAllImages(firstMemoryStream);
            }
            AnimationSerializeContext firstSerializeContext = new AnimationSerializeContext(firstBinaryWriter, firstImageWriter);
            Serialize(firstSerializeContext);
            byte[] originalData = firstMemoryStream.ToArray();

            // Then deserialize that data
            BinaryReader br = new BinaryReader(new MemoryStream(originalData));
            ImageBundle imageBundle = null;
            if(useExternalImages)
            {
                var helper = new SimpleTextureLoadHelper(graphicsDevice);
                imageBundle = new ImageBundle();
                br.BaseStream.Position = imageBundle.ReadAllImages(originalData, (int)br.BaseStream.Position, helper);
            }
            AnimationDeserializeContext deserializeContext = new AnimationDeserializeContext(br, imageBundle, graphicsDevice);
            AnimationSet deserialized = new AnimationSet(deserializeContext);

            // Then serialize that deserialized data and see if it matches
            // (Ideally we'd recursivly check the AnimationSet to figure out if it matches, but that's a bit too hard)
            MemoryCompareStream secondMemoryStream = new MemoryCompareStream(originalData);
            BinaryWriter secondBinaryWriter = new BinaryWriter(secondMemoryStream);
            ImageWriter secondImageWriter = null;
            if(useExternalImages)
            {
                secondImageWriter = new ImageWriter();
                deserialized.RegisterImages(secondImageWriter);
                secondImageWriter.WriteOutAllImages(secondMemoryStream);
            }
            AnimationSerializeContext secondSerializeContext = new AnimationSerializeContext(secondBinaryWriter, secondImageWriter);
            deserialized.Serialize(secondSerializeContext);

            // Clean-up:
            if(imageBundle != null)
                imageBundle.Dispose();
        }

        #endregion

		#region File Read/Write
        
        public void WriteToFile(string path)
        {
            ImageWriter imageWriter = new ImageWriter();
            this.RegisterImages(imageWriter);
            string texturePath = Path.ChangeExtension(path, ".tex");

#if false // OLD FORMAT
            using(var stream = File.Create(texturePath))
            {

                using(var zip = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    using(var bw = new BinaryWriter(zip))
                    {
                        imageWriter.WriteOutAllImagesOLD(bw);
                    }
                }
            }
#else
            MemoryStream ms = new MemoryStream();
            ms.WriteByte(0); // <- version
            imageWriter.WriteOutAllImages(ms);
            ms.Position = 0;
            File.WriteAllBytes(texturePath, ms.ToArray());
#endif

            using(var stream = File.Create(path))
            {
                using(var zip = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    using(var bw = new BinaryWriter(zip))
                    {
                        Serialize(new AnimationSerializeContext(bw, imageWriter));
                    }
                }
            }
        }

        public static AnimationSet ReadFromFile(string path, GraphicsDevice graphicsDevice)
        {
            string texturePath = Path.ChangeExtension(path, ".tex");
            ImageBundle imageBundle = null;
            if(File.Exists(texturePath))
            {
#if false // OLD FORMAT
                using(var stream = File.OpenRead(texturePath))
                {
                    using(var unzip = new GZipStream(stream, CompressionMode.Decompress, true))
                    {
                        using(var br = new BinaryReader(unzip))
                        {
                            imageBundle = new ImageBundle();
                            imageBundle.ReadAllImagesOLD(br, graphicsDevice);
                        }
                    }
                }
#else
#if !WINDOWS
                texturePath = texturePath.Replace('\\', '/');
#endif
                byte[] data = File.ReadAllBytes(texturePath);
                if(data[0] != 0)
                    throw new Exception("Bad version number");

                var helper = new SimpleTextureLoadHelper(graphicsDevice);
                imageBundle = new ImageBundle();
                imageBundle.ReadAllImages(data, 1, helper);
#endif
            }

            using(var stream = File.OpenRead(path))
            {
                using(var unzip = new GZipStream(stream, CompressionMode.Decompress, true))
                {
                    using(var br = new BinaryReader(unzip))
                    {
                        var deserializeContext = new AnimationDeserializeContext(br, imageBundle, graphicsDevice);
                        return new AnimationSet(deserializeContext);
                    }
                }
            }
        }

        #endregion
		
		/// <summary>Create a default animation that is just a single Cel</summary>
        public void AddStaticDefaultAnimation(Cel cel)
        {
            Animation animation = new Animation(true);
            animation.Frames.Add(new AnimationFrame(cel, 1));
            AddAnimation(TagSet.Empty, animation);
        }

	    public static AnimationSet CreateSingleSprite(Sprite sprite)
        {
            AnimationSet animationSet = new AnimationSet();
            animationSet.importOrigin = sprite.origin;
            animationSet.AddStaticDefaultAnimation(new Cel(sprite));
            animationSet.FinishGeneration();
            return animationSet;
        }
		
        [Browsable(false)]
        public Animation DefaultAnimation => animations.GetBaseFallback();

	    [Browsable(false)]
        public bool HasDefaultAnimation => animations.HasBaseFallback;

	    #region All Animations

        /// <summary>Return all animations and their TagSets, including animations without assigned TagSets (gives a key of null)</summary>
        public IEnumerable<KeyValuePair<TagSet, Animation>> GetAllAnimations()
        {
            foreach (var used in GetAllUsedAnimations()) yield return used;
            foreach (var unused in GetAllUnusedAnimations()) yield return unused;
        }

        public void AddAnimation(TagSet rule, Animation animation)
        {
            if(rule != null)
                animations.Add(rule, animation);
            else
                (unusedAnimations ?? (unusedAnimations = new List<Animation>())).Add(animation);
        }

        public bool RemoveAnimation(Animation animation)
        {
            if(unusedAnimations != null && unusedAnimations.Remove(animation))
                return true;

            for(int i = 0; i < animations.Count; i++)
            {
                if(ReferenceEquals(animations.Values[i], animation))
                {
                    animations.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        #endregion

		#region Formerly Shared Items

        /// <summary>Get the set of all <see cref="Mask"/>s (NOTE: does not include heightmap instructions)</summary>
        public HashSet<Mask> AllMasks()
        {
            HashSet<Mask> masks = new HashSet<Mask>(ReferenceEqualityComparer<Mask>.Instance);

            foreach(var animation in AllAnimations())
            {
                foreach(var frame in animation.Frames)
                {
                    foreach(var mask in frame.masks.Values)
                    {
                        Debug.Assert(mask != null); // <- Shouldn't have null masks?
                        masks.Add(mask);
                    }
                }
            }

            return masks;
        }

        /// <summary>Get the set of all <see cref="Cel"/>s</summary>
        public HashSet<Cel> AllCels()
        {
            HashSet<Cel> cels = new HashSet<Cel>(ReferenceEqualityComparer<Cel>.Instance);

            foreach(var animation in AllAnimations())
            {
                foreach(var frame in animation.Frames)
                {
                    foreach(var cel in frame.layers)
                    {
                        Debug.Assert(cel != null); // <- Shouldn't have null cels?
                        cels.Add(cel);
                    }
                }
            }

            return cels;
        }

        /// <summary>Get the set of all <see cref="ShadowReceiver"/>s.</summary>
        public HashSet<ShadowReceiver> AllShadowReceivers()
        {
            HashSet<ShadowReceiver> shadowReceivers = new HashSet<ShadowReceiver>(ReferenceEqualityComparer<ShadowReceiver>.Instance);

            foreach(var animation in AllAnimations())
            {
                foreach(var frame in animation.Frames)
                {
                    foreach(var cel in frame.layers)
                    {
                        Debug.Assert(cel != null); // <- Shouldn't have null cels?
                        if(cel.shadowReceiver != null)
                            shadowReceivers.Add(cel.shadowReceiver);
                    }
                }
            }

            return shadowReceivers;
        }

        #endregion

		/// <summary>Calculate the maximum world space bounds of the entire animation set (slow). EDITOR ONLY!</summary>
        public Rectangle CalculateGraphicsBounds()
        {
            Rectangle maxBounds = Rectangle.Empty;
            foreach(var animation in animations.Values)
            {
                maxBounds = RectangleExtensions.UnionIgnoreEmpty(maxBounds, animation.CalculateGraphicsBounds());
            }
            return maxBounds;
        }
		
        #region Alpha Masks

        public void RegenerateAlphaMasks()
        {
            foreach(var animation in AllAnimations())
            {
                foreach(var frame in animation.Frames)
                {
                    frame.RegenerateAlphaMask();
                }
            }
        }

        public bool ValidateAlphaMasks()
        {
            foreach(var animation in AllAnimations())
            {
                for (int f = 0; f < animation.Frames.Count; f++)
                {
                    var frame = animation.Frames[f];
                    bool foundAlphaMask = false;

					foreach (KeyValuePair<string, Mask> mask in frame.masks)
	                {
						if (mask.Value.isGeneratedAlphaMask)
		                {
			                foundAlphaMask = true;
						}
	                }

                    //for (int m = 0; m < frame.masks.Count; m++)
                    //{
                    //    if (frame.masks.Rules[m].Count == 0)
                    //    {
                    //        if (!frame.masks.Values[m].isGeneratedAlphaMask)
                    //        {
                    //            Debug.Assert(!Asserts.enabled || false);
                    //            return false;
                    //        }
                    //        if (foundAlphaMask)
                    //        {
                    //            Debug.Assert(!Asserts.enabled || false);
                    //            return false;
                    //        }
                    //        foundAlphaMask = true;
                    //    }
                    //    else
                    //    {
                    //        if (frame.masks.Values[m].isGeneratedAlphaMask)
                    //        {
                    //            Debug.Assert(!Asserts.enabled || false);
                    //            return false;
                    //        }
                    //    }
                    //}

                    if (!foundAlphaMask)
                    {
                        Debug.Assert(!Asserts.enabled || false);
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

		/// <summary>Recreate all generated items</summary>
        public void FinishGeneration()
        {
            Debug.Assert(CheckShadowLayerOrder());

            RegenerateAlphaMasks();
            RegeneratePhysicsAndDepthBounds();
        }
    }
}

