// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.Extensions;
using Pixel3D.FrameworkExtensions;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D
{
	public static class CustomSerialization
	{
		#region Tag Lookup

		// Due to a limitation in the serializer generator, this needs to be in a different class to TagLookup<T>
		// because the generic parameters need to be on the Method and NOT on the Type. (Maybe we should fix this so they can be on either.)

		public class CustomSerializerForTagLookup
		{
			// At last count, TagLookup and its TagSet rules and their strings were taking up ~70% of definition objects.
			// These are definition-only objects that the game state should never even store references to.
			// Only thing the game state may care about is the contents of the lookup (values) - so we store that.

			[CustomFieldSerializer]
			public static void Serialize<T>(SerializeContext context, BinaryWriter bw, TagLookup<T> value)
			{
				for (var i = 0; i < value.Count; i++)
					Field.Serialize(context, bw, ref value.values[i]);
			}

			[CustomFieldSerializer]
			public static void Deserialize<T>(DeserializeContext context, BinaryReader br, ref TagLookup<T> value)
			{
				throw new InvalidOperationException();
			}
		}

		#endregion

		#region Tag Set

		// Definition-only at the field level (don't even bother storing it) - see TagLookup
		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, TagSet value)
		{
		}

		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref TagSet value)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region ImageBundle

		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ImageBundle value)
		{
			throw new InvalidOperationException();
		}

		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ImageBundle value)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region ImageBundleManager

		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ImageBundleManager value)
		{
			throw new InvalidOperationException();
		}

		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ImageBundleManager value)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region SpriteRef

		// Definition-only (TODO: Maybe we should care that the sprite references match up between players?)
		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref SpriteRef value)
		{
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref SpriteRef value)
		{
			throw new InvalidOperationException();
		}

		#region Animation Serialization

		public static void Serialize(this SpriteRef spriteRef, AnimationSerializeContext context)
		{
			// Bundling is handled by registering images, keyed on the sprite itself, so we just pass-through:
			spriteRef.ResolveRequire().Serialize(context);
		}

		public static SpriteRef DeserializeSpriteRef(this AnimationDeserializeContext context)
		{
			// IMPORTANT: This method is compatible with Sprite's deserializer
			var spriteRef = new SpriteRef();

			if (context.imageBundle != null)
			{
				spriteRef.bundle = context.imageBundle;
				// NOTE: AssetTool depends on us not actually resolving the sprite during load

				spriteRef.index = context.br.ReadInt32();
				if (spriteRef.index != -1)
					spriteRef.origin = context.br.ReadPoint();
				else
					spriteRef.origin = default(Point);
			}
			else // In place sprite
			{
				var sprite = context.DeserializeSprite(); // Pass through

				spriteRef.bundle = new ImageBundle(sprite);
				spriteRef.index = 0;
				spriteRef.origin = sprite.origin;
			}

			return spriteRef;
		}

		#endregion

		#endregion

		#region Sprite

		[CustomSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ref Sprite value)
		{
			// NOTE: Not visiting the texture object, because it could be deferred (so definitions can't know about it)
		}

		[CustomSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref Sprite value)
		{
			Debug.Assert(false); // Shouldn't happen! (Can't store Sprite in game state)
			throw new InvalidOperationException();
		}

		public static void Serialize(this Sprite sprite, AnimationSerializeContext context)
		{
			if (sprite.sourceRectangle.Width > 2048 || sprite.sourceRectangle.Height > 2048)
				throw new InvalidOperationException("Cannot handle textures larger than 2048"); // Due to Reach support

			if (context.imageWriter != null)
			{
				int index = context.imageWriter.GetImageIndex(sprite.texture, sprite.sourceRectangle);
				context.bw.Write(index);

				if (index >= 0)
					context.bw.Write(sprite.origin);
			}
			else // In-place sprite
			{
				if (sprite.texture == null || sprite.sourceRectangle.Width == 0 || sprite.sourceRectangle.Height == 0)
				{
					context.bw.Write(0); // Writing 0 width indicates blank texture (no need to write height)
				}
				else
				{
					var data = new byte[sprite.sourceRectangle.Width * sprite.sourceRectangle.Height * 4];
					sprite.texture.GetData<byte>(0, sprite.sourceRectangle, data, 0, data.Length);

					// Only write the size (we intentionally lose the source rectangle's position)
					context.bw.Write(sprite.sourceRectangle.Width);
					context.bw.Write(sprite.sourceRectangle.Height);

					context.bw.Write(data);

					context.bw.Write(sprite.origin);
				}
			}
		}

		public static Sprite DeserializeSprite(this AnimationDeserializeContext context)
		{
			// IMPORTANT: This method is compatible with SpriteRef's deserializer
			var sprite = new Sprite();

			if (context.imageBundle != null)
			{
				int index = context.br.ReadInt32();
				if (index == -1)
				{
					sprite.texture = null;
					sprite.sourceRectangle = default(Rectangle);
					sprite.origin = default(Point);
				}
				else
				{
					sprite = context.imageBundle.GetSprite(index, context.br.ReadPoint());
				}
			}
			else // In place sprite
			{
				int width = context.br.ReadInt32();
				if (width == 0) // A blank texture
				{
					sprite.sourceRectangle = Rectangle.Empty;
					sprite.texture = null;
					sprite.origin = default(Point);
				}
				else
				{
					int height = context.br.ReadInt32();
					sprite.sourceRectangle = new Rectangle(0, 0, width, height);
					byte[] data = context.br.ReadBytes(width * height * 4);

					if (context.GraphicsDevice != null) // <- Allow loading headless
					{
						sprite.texture = new Texture2D(context.GraphicsDevice, width, height);
						((Texture2D) sprite.texture).SetData(data);
					}
					else
					{
						sprite.texture = null;
					}

					sprite.origin = context.br.ReadPoint();
				}
			}

			return sprite;
		}

		#endregion

		#region TagSet

		// NOTE: Pass-through the animation serializer to a simple binary serializer (the format of `TagSet` is *really* stable, and some folks need to directly serialize us)

		public static void SerializeTagSet(this TagSet tagSet, AnimationSerializeContext context)
		{
			tagSet.Serialize(context.bw);
		}

		public static TagSet DeserializeTagSet(this AnimationDeserializeContext context)
		{
			return new TagSet(context.br);
		}

		#endregion

		#region TagLookup

		// NOTE: Pass-through the animation serializer to a simple binary serializer (the format of `TagLookup` is *really* stable, and some folks need to directly serialize us)

		public static void SerializeTagLookup<T>(this TagLookup<T> tagLookup, AnimationSerializeContext context,
			Action<T> serializeValue)
		{
			tagLookup.Serialize(context.bw, serializeValue);
		}

		/// <summary>Deserialize into new object instance</summary>
		public static TagLookup<T> DeserializeTagLookup<T>(this AnimationDeserializeContext context,
			Func<T> deserializeValue)
		{
			return new TagLookup<T>(context.br, deserializeValue);
		}

		#endregion

		#region OrderedDictionary

		public static void SerializeOrderedDictionary<T>(this OrderedDictionary<string, T> dictionary,
			AnimationSerializeContext context, Action<T> serializeValue)
		{
			context.bw.WriteSmallInt32(dictionary.Count);

			foreach (var item in dictionary)
			{
				var key = item.Key;
				var value = item.Value;
				context.bw.Write(key);
				serializeValue(value);
			}
		}

		public static OrderedDictionary<string, T> DeserializeOrderedDictionary<T>(
			this AnimationDeserializeContext context, Func<T> deserializeValue)
		{
			var dictionary = new OrderedDictionary<string, T>();

			int count = context.br.ReadSmallInt32();

			for (var i = 0; i < count; i++)
			{
				var key = context.br.ReadString();
				var value = deserializeValue();
				dictionary.Add(key, value);
			}

			return dictionary;
		}

		#endregion

		#region AnimationFrame

		[SerializationIgnoreDelegates]
		public static void Serialize(this AnimationFrame animationFrame, AnimationSerializeContext context)
		{
			context.bw.Write(animationFrame.delay);
			context.bw.Write(animationFrame.positionDelta);
			context.bw.Write(animationFrame.shadowOffset);

			context.bw.Write(animationFrame.SnapToGround);

			// NOTE: This walks the layer linked list twice, but is only O(n), so no biggie
			int layerCount = animationFrame.layers.Count;
			context.bw.Write(layerCount);
			foreach (var cel in animationFrame.layers)
				cel.Serialize(context);

			animationFrame.masks.SerializeOrderedDictionary(context, m => m.Serialize(context));

			animationFrame.outgoingAttachments.SerializeOrderedDictionary(context, oa => oa.Serialize(context));
			animationFrame.incomingAttachments.SerializeTagLookup(context, p => context.bw.Write(p));

			if (animationFrame.triggers == null)
			{
				context.bw.Write(0);
			}
			else
			{
				context.bw.Write(animationFrame.triggers.Count);
				for (var i = 0; i < animationFrame.triggers.Count; i++)
					context.bw.Write(animationFrame.triggers[i]);
			}

			context.bw.Write(animationFrame.attachAtLayer.Clamp(0, animationFrame.layers.Count));
			context.bw.Write(animationFrame.canDrawLayersAboveSortedAttachees);

			context.bw.WriteNullableString(animationFrame.cue);
		}

		/// <summary>Deserialize into new object instance</summary>
		[SerializationIgnoreDelegates]
		public static AnimationFrame DeserializeAnimationFrame(this AnimationDeserializeContext context)
		{
			var animationFrame = new AnimationFrame();

			animationFrame.delay = context.br.ReadInt32();
			animationFrame.positionDelta = context.br.ReadPosition();
			animationFrame.shadowOffset = context.br.ReadPosition();

			animationFrame.SnapToGround = context.br.ReadBoolean();

			int layersCount = context.br.ReadInt32();
			if (layersCount > 0)
			{
				Cel currentCel;
				animationFrame.firstLayer = currentCel = context.DeserializeCel();
				for (var i = 1; i < layersCount; i++)
				{
					currentCel.next = context.DeserializeCel();
					currentCel = currentCel.next;
				}
			}

			if (context.Version >= 39)
			{
                animationFrame.masks = context.DeserializeOrderedDictionary(() => context.DeserializeMask());
				animationFrame.outgoingAttachments =
					context.DeserializeOrderedDictionary(() => context.DeserializeOutgoingAttachment());
			}
			else
			{
				//
				// Masks:
				{
                    var legacy = context.DeserializeTagLookup(() => context.DeserializeMask());
					animationFrame.masks = new OrderedDictionary<string, Mask>();
					foreach (var mask in legacy)
					{
						Debug.Assert(mask.Key.Count < 2, "we don't support multi-tags yet");
						animationFrame.masks.Add(mask.Key.ToString(), mask.Value);
					}
				}

				//
				// Outgoing Attachments:
				{
                    var legacy = context.DeserializeTagLookup(() => context.DeserializeOutgoingAttachment());
					animationFrame.outgoingAttachments = new OrderedDictionary<string, OutgoingAttachment>();
					foreach (var outgoingAttachment in legacy)
					{
						Debug.Assert(outgoingAttachment.Key.Count < 2, "we don't support multi-tags yet");
						animationFrame.outgoingAttachments.Add(outgoingAttachment.Key.ToString(),
							outgoingAttachment.Value);
					}
				}
			}

			animationFrame.incomingAttachments = context.DeserializeTagLookup(() => context.br.ReadPosition());

			int triggerCount = context.br.ReadInt32();
			if (triggerCount > 0)
			{
				animationFrame.triggers = new List<string>(triggerCount);
				for (var i = 0; i < triggerCount; i++)
					animationFrame.triggers.Add(context.br.ReadString());
			}

			animationFrame.attachAtLayer = context.br.ReadInt32();
			animationFrame.canDrawLayersAboveSortedAttachees = context.br.ReadBoolean();

			animationFrame.cue = context.br.ReadNullableString();

			return animationFrame;
		}

		#endregion

		#region AnimationSet

		[SerializationIgnoreDelegates]
		public static void Serialize(this AnimationSet animationSet, AnimationSerializeContext context)
		{
			if (Asserts.enabled && !animationSet.ValidateAlphaMasks())
				throw new InvalidOperationException(
					"Attempting to save animation set with missing or invalid alpha masks");

			context.bw.WriteNullableString(animationSet.friendlyName);
			context.bw.Write(animationSet.importOrigin);
			context.bw.WriteNullableString(animationSet.behaviour);

			if (context.bw.WriteBoolean(animationSet.Heightmap != null))
				animationSet.Heightmap.Serialize(context);


			// If you don't seem to have set any physics bounds, I will just generate them...
			if (animationSet.physicsStartX == 0 && animationSet.physicsEndX == 1 && animationSet.physicsStartZ == 0 &&
			    animationSet.physicsEndZ == 1 && animationSet.physicsHeight == 0 ||
			    animationSet.physicsEndX - animationSet.physicsStartX <= 0)
				animationSet.AutoGeneratePhysicsAndDepthBounds();


			// NOTE: only writing out values that cannot be auto-generated
			if (animationSet.Heightmap != null)
			{
				Debug.Assert(!Asserts.enabled || animationSet.Heightmap.IsObjectHeightmap ||
				             animationSet.physicsHeight == 0);
				context.bw.Write(animationSet.physicsHeight);
				if (animationSet.physicsHeight > 0)
					animationSet.depthBounds.Serialize(context);
				context.bw.Write(animationSet.flatDirection); // <- for the sake of editing, keep this value around
			}
			else
			{
				context.bw.Write(animationSet.physicsStartX);
				context.bw.Write(animationSet.physicsEndX);
				context.bw.Write(animationSet.physicsStartZ);
				context.bw.Write(animationSet.physicsHeight);
				context.bw.Write(animationSet.flatDirection);

				if (animationSet.physicsHeight == 0)
					context.bw.Write(animationSet.physicsEndZ);
			}

			if (context.Version >= 38)
				context.bw.Write(animationSet.coplanarPriority);

			if (context.Version >= 36)
				context.bw.Write(animationSet.doAboveCheck);

			if (context.bw.WriteBoolean(animationSet.Ceiling != null))
				animationSet.Ceiling.Serialize(context);

			animationSet.animations.SerializeTagLookup(context, a => a.Serialize(context));

			// Unused Animations
			{
				if (animationSet.unusedAnimations == null)
				{
					context.bw.Write(0); // unused animations is lazy-initialized
				}
				else
				{
					context.bw.Write(animationSet.unusedAnimations.Count);
					foreach (var animation in animationSet.unusedAnimations)
						animation.Serialize(context);
				}
			}

			context.bw.WriteNullableString(animationSet.cue);


			// Shadow layers:
			{
				if (animationSet.shadowLayers == null)
				{
					context.bw.Write(0);
				}
				else
				{
					context.bw.Write(animationSet.shadowLayers.Count);
					foreach (var sl in animationSet.shadowLayers)
					{
						context.bw.Write(sl.startHeight);
						sl.shadowSpriteRef.Serialize(context);
					}

					animationSet.RecalculateCachedShadowBounds();
					context.bw.Write(animationSet.cachedShadowBounds);
				}
			}
		}

		/// <summary>Deserialize into new object instance</summary>
		[SerializationIgnoreDelegates]
		public static AnimationSet DeserializeAnimationSet(this AnimationDeserializeContext context)
		{
			var animationSet = new AnimationSet();

			animationSet.friendlyName = context.br.ReadNullableString();
			animationSet.importOrigin = context.br.ReadPoint();
			animationSet.behaviour = context.br.ReadNullableString();

			if (context.br.ReadBoolean())
				animationSet.Heightmap = context.DeserializeHeightmap();

			if (animationSet.Heightmap != null)
			{
				animationSet.physicsStartX = animationSet.Heightmap.StartX;
				animationSet.physicsEndX = animationSet.Heightmap.EndX;
				animationSet.physicsStartZ = animationSet.Heightmap.StartZ;
				animationSet.physicsEndZ = animationSet.Heightmap.EndZ;

				// Assume that reading is faster than walking the heightmap:
				animationSet.physicsHeight = context.br.ReadInt32();
				if (animationSet.physicsHeight > 0)
					animationSet.depthBounds = context.DeserializeDepthBounds();
				animationSet.flatDirection =
					context.br.ReadOblique(); // <- for the sake of editing, keep this value around
			}
			else
			{
				animationSet.physicsStartX = context.br.ReadInt32();
				animationSet.physicsEndX = context.br.ReadInt32();
				animationSet.physicsStartZ = context.br.ReadInt32();
				animationSet.physicsHeight = context.br.ReadInt32();
				animationSet.flatDirection = context.br.ReadOblique();

				if (animationSet.physicsHeight == 0)
					animationSet.physicsEndZ =
						context.br.ReadInt32(); // physicsEndZ gets auto-set during regen, except for carpets

				animationSet.RegenerateDepthBounds(); // <- Know this is reasonably fast to generate
			}

			if (context.Version >= 38)
				animationSet.coplanarPriority = context.br.ReadInt32();

			if (context.Version >= 36)
				animationSet.doAboveCheck = context.br.ReadBoolean();

			if (context.br.ReadBoolean())
				animationSet.Ceiling = context.DeserializeHeightmap();

            animationSet.animations = context.DeserializeTagLookup(() => context.DeserializeAnimation());

			// Unused Animations
			{
				int count = context.br.ReadInt32();
				if (count > 0) // unusedAnimations is lazy-initialized
				{
					animationSet.unusedAnimations = new List<Animation>(count);
					for (var i = 0; i < count; i++)
						animationSet.unusedAnimations.Add(context.DeserializeAnimation());
				}
			}

			animationSet.cue = context.br.ReadNullableString();

			// Shadow layers
			{
				int shadowLayerCount = context.br.ReadInt32();
				if (shadowLayerCount <= 0)
				{
					animationSet.shadowLayers = null;
				}
				else
				{
					animationSet.shadowLayers = new List<ShadowLayer>();
					for (var i = 0; i < shadowLayerCount; i++)
						animationSet.shadowLayers.Add(new ShadowLayer(
							context.br.ReadInt32(),
							context.DeserializeSpriteRef()));

					animationSet.cachedShadowBounds = context.br.ReadBounds();
				}
			}

			return animationSet;
		}

		/// <summary>Check that an AnimationSet round-trips through serialization cleanly</summary>
		public static void RoundTripCheck(this AnimationSet animationSet, GraphicsDevice graphicsDevice,
			bool useExternalImages)
		{
			// Serialize a first time
			var firstMemoryStream = new MemoryStream();
			var firstBinaryWriter = new BinaryWriter(firstMemoryStream);
			ImageWriter firstImageWriter = null;
			if (useExternalImages)
			{
				firstImageWriter = new ImageWriter();
				animationSet.RegisterImages(firstImageWriter);
				firstImageWriter.WriteOutAllImages(firstMemoryStream);
			}

			var firstSerializeContext = new AnimationSerializeContext(firstBinaryWriter, firstImageWriter);
			animationSet.Serialize(firstSerializeContext);
			var originalData = firstMemoryStream.ToArray();

			// Then deserialize that data
			var br = new BinaryReader(new MemoryStream(originalData));
			ImageBundle imageBundle = null;
			if (useExternalImages)
			{
				var helper = new SimpleTextureLoadHelper(graphicsDevice);
				imageBundle = new ImageBundle();
				br.BaseStream.Position = imageBundle.ReadAllImages(originalData, (int) br.BaseStream.Position, helper);
			}

			var deserializeContext = new AnimationDeserializeContext(br, imageBundle, graphicsDevice);
			var deserialized = deserializeContext.DeserializeAnimationSet();

			// Then serialize that deserialized data and see if it matches
			// (Ideally we'd recursivly check the AnimationSet to figure out if it matches, but that's a bit too hard)
			var secondMemoryStream = new MemoryCompareStream(originalData);
			var secondBinaryWriter = new BinaryWriter(secondMemoryStream);
			ImageWriter secondImageWriter = null;
			if (useExternalImages)
			{
				secondImageWriter = new ImageWriter();
				deserialized.RegisterImages(secondImageWriter);
				secondImageWriter.WriteOutAllImages(secondMemoryStream);
			}

			var secondSerializeContext = new AnimationSerializeContext(secondBinaryWriter, secondImageWriter);
			deserialized.Serialize(secondSerializeContext);

			// Clean-up:
            if(imageBundle != null)
			    imageBundle.Dispose();
		}

		#endregion

		#region Animation

		public static void Serialize(this Animation animation, AnimationSerializeContext context)
		{
			context.bw.Write(animation.isLooped);
			context.bw.WriteNullableString(animation.friendlyName);

			context.bw.Write(animation.Frames.Count);
			for (var i = 0; i < animation.Frames.Count; i++) animation.Frames[i].Serialize(context);


			if (!context.monitor)
				animation.cachedBounds = new Bounds(animation.CalculateGraphicsBounds());
			if (context.Version >= 35)
				context.bw.Write(animation.cachedBounds);


			context.bw.Write(animation.isShared);
			context.bw.WriteNullableString(animation.cue);
			context.bw.WriteBoolean(animation.preventDropMotion);
		}

		/// <summary>Deserialize into new object instance</summary>
		public static Animation DeserializeAnimation(this AnimationDeserializeContext context)
		{
			var animation = new Animation();

			animation.isLooped = context.br.ReadBoolean();
			animation.friendlyName = context.br.ReadNullableString();

			int frameCount = context.br.ReadInt32();
			animation.Frames = new List<AnimationFrame>(frameCount);
			for (var i = 0; i < frameCount; i++) animation.Frames.Add(context.DeserializeAnimationFrame());

			if (context.Version >= 35)
				animation.cachedBounds = context.br.ReadBounds();

			// NOTE: Had to remove call to CalculateGraphicsBounds for old sprites (because we can't get that data at load time in the engine). Time to do a full rewrite.

			animation.isShared = context.br.ReadBoolean();
			animation.cue = context.br.ReadNullableString();
			animation.preventDropMotion = context.br.ReadBoolean();

			return animation;
		}

		#endregion

		#region OutgoingAttachment

		public static void Serialize(this OutgoingAttachment oa, AnimationSerializeContext context)
		{
			context.bw.Write(oa.position);
			oa.targetAnimationContext.SerializeTagSet(context);
			oa.targetAttachmentContext.SerializeTagSet(context);
			context.bw.Write(oa.attachRange);
			context.bw.Write((int) oa.facing);
		}

		/// <summary>Deserialize into new object instance</summary>
		public static OutgoingAttachment DeserializeOutgoingAttachment(this AnimationDeserializeContext context)
		{
			var oa = new OutgoingAttachment();
			oa.position = context.br.ReadPosition();
			oa.targetAnimationContext = context.DeserializeTagSet();
			oa.targetAttachmentContext = context.DeserializeTagSet();
			oa.attachRange = context.br.ReadAABB();
			oa.facing = (OutgoingAttachment.Facing) context.br.ReadInt32();
			return oa;
		}

		#endregion

		#region Mask

		public static void Serialize(this Mask mask, AnimationSerializeContext context)
		{
			if (context.Version < 37)
				context.bw.WriteNullableString(string.Empty); // was friendly name

			context.bw.Write(mask.isGeneratedAlphaMask);

			Debug.Assert(!Asserts.enabled || mask.data.Valid);
			mask.data.Serialize(context.bw);
		}

		/// <summary>Deserialize into new object instance</summary>
		public static Mask DeserializeMask(this AnimationDeserializeContext context)
		{
			var mask = new Mask();

			if (context.Version < 37)
				context.br.ReadNullableString(); // was friendly name

			mask.isGeneratedAlphaMask = context.br.ReadBoolean();

			if (context.customMaskDataReader != null)
			{
				// NOTE: Matches MaskData deserializing constructor:
				var rect = context.br.ReadRectangle();
				mask.data = new MaskData(
					context.customMaskDataReader.Read(MaskData.WidthToDataWidth(rect.Width) * rect.Height), rect);
			}
			else
			{
				mask.data = context.br.DeserializeMaskData(context.fastReadHack);
			}

			return mask;
		}

		#endregion

		#region MaskData

		// IMPORTANT: Because both levels and animation sets can serialize these, we don't have a version number!
		//            (Could pass a custom one as a parameter, or use multiple methods, if the need arises...)

		public static void Serialize(this MaskData maskData, BinaryWriter bw)
		{
			bw.Write(maskData.Bounds);

			if (maskData.packedData != null)
				for (var i = 0; i < maskData.packedData.Length; i++)
					bw.Write(maskData.packedData[i]);
		}

		public static MaskData DeserializeMaskData(this BinaryReader br, bool fastReadHack)
		{
			var maskData = new MaskData(br.ReadRectangle());

			if (maskData.packedData != null)
			{
				if (!fastReadHack)
				{
					for (var i = 0; i < maskData.packedData.Length; i++) maskData.packedData[i] = br.ReadUInt32();
				}
				else // FAST READ!
				{
					var bytesToRead = maskData.packedData.Length * 4;
					br.ReadBytes(bytesToRead);
				}
			}

			Debug.Assert(maskData.Valid);

			return maskData;
		}

		#endregion

		#region HeightmapInstruction

		// NOTE: Because heightmap instructions will eventually only be stored in the unoptimised data
		//       we can be a little bit lazy and just serialize all arguments, even if they are not actually
		//       used by the given op-code.

		// NOTE: Masks are not shared for HeightmapInstruction (otherwise we get a nasty circular dependency through ShadowReceiver)

		public static void Serialize(this HeightmapInstruction instruction, AnimationSerializeContext context)
		{
			context.bw.Write((int) instruction.Operation);

			context.bw.Write(instruction.Height);
			context.bw.Write(instruction.ObliqueDirection);
			context.bw.Write(instruction.FrontEdgeDepth);
			context.bw.Write(instruction.Depth);
			context.bw.Write(instruction.Slope);
			context.bw.Write(instruction.Offset);

			context.bw.Write(instruction.Mask != null);

            if(instruction.Mask != null)
			    instruction.Mask.Serialize(context);
		}

		/// <summary>Deserialize into a new object instance</summary>
		public static HeightmapInstruction DeserializeHeightmapInstruction(this AnimationDeserializeContext context)
		{
			var instruction = new HeightmapInstruction
			{
				Operation = (HeightmapOp) context.br.ReadInt32(),
				Height = context.br.ReadByte(),
				ObliqueDirection = context.br.ReadOblique(),
				FrontEdgeDepth = context.br.ReadInt32(),
				Depth = context.br.ReadInt32(),
				Slope = context.br.ReadInt32(),
				Offset = context.br.ReadInt32()
			};

			instruction.Mask = context.br.ReadBoolean() ? context.DeserializeMask() : null;
			return instruction;
		}

		#endregion

		#region List<HeightmapInstruction>

		public static void Serialize(this List<HeightmapInstruction> instructions, AnimationSerializeContext context)
		{
			context.bw.Write(instructions != null);
			if (instructions != null)
			{
				context.bw.Write(instructions.Count);
				for (var i = 0; i < instructions.Count; i++) instructions[i].Serialize(context);
			}
		}

		public static List<HeightmapInstruction> DeserializeHeightmapInstructions(
			this AnimationDeserializeContext context)
		{
			if (context.br.ReadBoolean())
			{
				int count = context.br.ReadInt32();
				var instructions = new List<HeightmapInstruction>(count);
				for (var i = 0; i < count; i++) instructions.Add(context.DeserializeHeightmapInstruction());
				return instructions;
			}

			return null;
		}

		#endregion

		#region Heightmap

		public static void Serialize(this Heightmap heightmap, AnimationSerializeContext context)
		{
			context.bw.Write(heightmap.DefaultHeight);

			context.bw.Write(heightmap.OneWay);
			context.bw.Write(heightmap.OneWayThickness);

			if (context.bw.WriteBoolean(heightmap.HasData))
			{
				context.bw.Write(heightmap.heightmapData.Bounds);
				context.bw.Write(heightmap.heightmapData.Data, 0,
					heightmap.heightmapData.Width * heightmap.heightmapData.Height);
			}

			heightmap.instructions.Serialize(context);
		}

		public static Heightmap DeserializeHeightmap(this AnimationDeserializeContext context)
		{
			var heightmap = new Heightmap();

			heightmap.DefaultHeight = context.br.ReadByte();

			heightmap.OneWay = context.br.ReadBoolean();
			heightmap.OneWayThickness = context.br.ReadByte();

			if (context.br.ReadBoolean())
			{
				Rectangle bounds = context.br.ReadRectangle();
				byte[] data = context.br.ReadBytes(bounds.Width * bounds.Height);
				heightmap.heightmapData = new Data2D<byte>(data, bounds);
			}
			else
			{
				heightmap.heightmapData = default(Data2D<byte>);
			}

			heightmap.instructions = context.DeserializeHeightmapInstructions();
			return heightmap;
		}

		#endregion

		#region ShadowReceiver

		public static void Serialize(this ShadowReceiver receiver, AnimationSerializeContext context)
		{
			receiver.heightmap.Serialize(context);
			context.bw.Write(receiver.heightmapExtendDirection);
		}

		/// <summary>Deserialize into new object instance</summary>
		public static ShadowReceiver DeserializeShadowReceiver(this AnimationDeserializeContext context)
		{
			var heightmap = context.DeserializeHeightmap();
			var heightmapExtendDirection = context.br.ReadOblique();

			var receiver = new ShadowReceiver(heightmap, heightmapExtendDirection);
			return receiver;
		}

		#endregion

		#region Cel

		public static void Serialize(this Cel cel, AnimationSerializeContext context)
		{
			context.bw.WriteNullableString(cel.friendlyName);
			cel.spriteRef.Serialize(context);

		    if (context.bw.WriteBoolean(cel.shadowReceiver != null))
		    {
		        cel.shadowReceiver.Serialize(context);
		    }
		}

		/// <summary>Deserialize into new object instance</summary>
		public static Cel DeserializeCel(this AnimationDeserializeContext context)
		{
			var cel = new Cel();
			cel.friendlyName = context.br.ReadNullableString();
			cel.spriteRef = context.DeserializeSpriteRef();
			if (context.br.ReadBoolean())
				cel.shadowReceiver = context.DeserializeShadowReceiver();
			return cel;
		}

		#endregion
		
		#region DepthBounds

		public static void Serialize(this DepthBounds bounds, AnimationSerializeContext context)
		{
			Debug.Assert(bounds.slices != null); // <- should never serialize an empty depth bound (check in caller)

			if (bounds.heights == null)
			{
				context.bw.Write((int)0);
			}
			else
			{
				context.bw.Write(bounds.heights.Length);
				context.bw.Write(bounds.heights);
			}

			// NOTE: slices.Length is implicit
			for (int i = 0; i < bounds.slices.Length; i++)
			{
				context.bw.Write(bounds.slices[i].xOffset);
				context.bw.Write(bounds.slices[i].zOffset);

				context.bw.Write(bounds.slices[i].depths.Length);
				for (int j = 0; j < bounds.slices[i].depths.Length; j++)
				{
					context.bw.Write(bounds.slices[i].depths[j].front);
					context.bw.Write(bounds.slices[i].depths[j].back);
				}
			}
		}

		/// <summary>Deserialize.</summary>
		public static DepthBounds DeserializeDepthBounds(this AnimationDeserializeContext context)
		{
			var bounds = new DepthBounds();

			int heightCount = context.br.ReadInt32();
			bounds.heights = (heightCount == 0) ? null : context.br.ReadBytes(heightCount);

			bounds.slices = new DepthSlice[heightCount + 1];
			for (int i = 0; i < bounds.slices.Length; i++)
			{
				bounds.slices[i] = new DepthSlice()
				{
					xOffset = context.br.ReadInt32(),
					zOffset = context.br.ReadInt32(),
					depths = new FrontBack[context.br.ReadInt32()],
				};

				for (int j = 0; j < bounds.slices[i].depths.Length; j++)
				{
					bounds.slices[i].depths[j].front = context.br.ReadByte();
					bounds.slices[i].depths[j].back = context.br.ReadByte();
				}
			}

			return bounds;
		}

		#endregion
	}
}