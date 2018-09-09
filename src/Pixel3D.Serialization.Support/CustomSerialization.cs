using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixel3D.Animations;
using Pixel3D.Animations.Serialization;
using Pixel3D.Serialization;
using Pixel3D.Serialization.Context;

namespace Pixel3D
{
	public static class CustomSerialization
	{
		#region Tag Set

		// Definition-only at the field level (don't even bother storing it) - see TagLookup
		[CustomFieldSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, TagSet value) { }
		[CustomFieldSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, ref TagSet value) { throw new InvalidOperationException(); }

		#endregion

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
				for (int i = 0; i < value.Count; i++)
					Field.Serialize(context, bw, ref value.values[i]);
			}

			[CustomFieldSerializer]
			public static void Deserialize<T>(DeserializeContext context, BinaryReader br, ref TagLookup<T> value)
			{
				throw new InvalidOperationException();
			}
		}

		#endregion

		#region ImageBundle

		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ImageBundle value) { throw new InvalidOperationException(); }
		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ImageBundle value) { throw new InvalidOperationException(); }

		#endregion

		#region ImageBundleManager

		[CustomFieldSerializer]
		public static void Serialize(SerializeContext context, BinaryWriter bw, ImageBundleManager value) { throw new InvalidOperationException(); }

		[CustomFieldSerializer]
		public static void Deserialize(DeserializeContext context, BinaryReader br, ref ImageBundleManager value) { throw new InvalidOperationException(); }

		#endregion

		#region SpriteRef (definition only)

		// Definition-only (TODO: Maybe we should care that the sprite references match up between players?)
		[CustomSerializer] public static void Serialize(SerializeContext context, BinaryWriter bw, ref SpriteRef value) { }
		[CustomSerializer] public static void Deserialize(DeserializeContext context, BinaryReader br, ref SpriteRef value) { throw new InvalidOperationException(); }

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
				context.bw.Write((int)0);
			else
			{
				context.bw.Write(animationFrame.triggers.Count);
				for (int i = 0; i < animationFrame.triggers.Count; i++)
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
				Cel currentCel = null;
				animationFrame.firstLayer = currentCel = new Cel(context);
				for (int i = 1; i < layersCount; i++)
				{
					currentCel.next = new Cel(context);
					currentCel = currentCel.next;
				}
			}

			if (context.Version >= 39)
			{
				animationFrame.masks = context.DeserializeOrderedDictionary(() => new Mask(context));
				animationFrame.outgoingAttachments = context.DeserializeOrderedDictionary(() => new OutgoingAttachment(context));
			}
			else
			{
				//
				// Masks:
				{
					var legacy = context.DeserializeTagLookup(() => new Mask(context));
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
					var legacy = context.DeserializeTagLookup(() => new OutgoingAttachment(context));
					animationFrame.outgoingAttachments = new OrderedDictionary<string, OutgoingAttachment>();
					foreach (var outgoingAttachment in legacy)
					{
						Debug.Assert(outgoingAttachment.Key.Count < 2, "we don't support multi-tags yet");
						animationFrame.outgoingAttachments.Add(outgoingAttachment.Key.ToString(), outgoingAttachment.Value);
					}
				}
			}

			animationFrame.incomingAttachments = context.DeserializeTagLookup(() => context.br.ReadPosition());

			int triggerCount = context.br.ReadInt32();
			if (triggerCount > 0)
			{
				animationFrame.triggers = new List<string>(triggerCount);
				for (int i = 0; i < triggerCount; i++)
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
				throw new InvalidOperationException("Attempting to save animation set with missing or invalid alpha masks");

			context.bw.WriteNullableString(animationSet.friendlyName);
			context.bw.Write(animationSet.importOrigin);
			context.bw.WriteNullableString(animationSet.behaviour);

			if (context.bw.WriteBoolean(animationSet.Heightmap != null))
				animationSet.Heightmap.Serialize(context);


			// If you don't seem to have set any physics bounds, I will just generate them...
			if ((animationSet.physicsStartX == 0 && animationSet.physicsEndX == 1 && animationSet.physicsStartZ == 0 && animationSet.physicsEndZ == 1 && animationSet.physicsHeight == 0) || animationSet.physicsEndX - animationSet.physicsStartX <= 0)
			{
				animationSet.AutoGeneratePhysicsAndDepthBounds();
			}


			// NOTE: only writing out values that cannot be auto-generated
			if (animationSet.Heightmap != null)
			{
				Debug.Assert(!Asserts.enabled || animationSet.Heightmap.IsObjectHeightmap || animationSet.physicsHeight == 0);
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
					context.bw.Write(0); // unused animations is lazy-initialized
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
					context.bw.Write((int)0);
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
				animationSet.Heightmap = new Heightmap(context);

			if (animationSet.Heightmap != null)
			{
				animationSet.physicsStartX = animationSet.Heightmap.StartX;
				animationSet.physicsEndX = animationSet.Heightmap.EndX;
				animationSet.physicsStartZ = animationSet.Heightmap.StartZ;
				animationSet.physicsEndZ = animationSet.Heightmap.EndZ;

				// Assume that reading is faster than walking the heightmap:
				animationSet.physicsHeight = context.br.ReadInt32();
				if (animationSet.physicsHeight > 0)
					animationSet.depthBounds = new DepthBounds(context);
				animationSet.flatDirection = context.br.ReadOblique(); // <- for the sake of editing, keep this value around
			}
			else
			{
				animationSet.physicsStartX = context.br.ReadInt32();
				animationSet.physicsEndX = context.br.ReadInt32();
				animationSet.physicsStartZ = context.br.ReadInt32();
				animationSet.physicsHeight = context.br.ReadInt32();
				animationSet.flatDirection = context.br.ReadOblique();

				if (animationSet.physicsHeight == 0)
					animationSet.physicsEndZ = context.br.ReadInt32(); // physicsEndZ gets auto-set during regen, except for carpets

				animationSet.RegenerateDepthBounds(); // <- Know this is reasonably fast to generate
			}

			if (context.Version >= 38)
				animationSet.coplanarPriority = context.br.ReadInt32();

			if (context.Version >= 36)
				animationSet.doAboveCheck = context.br.ReadBoolean();

			if (context.br.ReadBoolean())
				animationSet.Ceiling = new Heightmap(context);

			animationSet.animations = context.DeserializeTagLookup(context.DeserializeAnimation);
			
			// Unused Animations
			{
				int count = context.br.ReadInt32();
				if (count > 0) // unusedAnimations is lazy-initialized
				{
					animationSet.unusedAnimations = new List<Animation>(count);
					for (int i = 0; i < count; i++)
						animationSet.unusedAnimations.Add(context.DeserializeAnimation());
				}
			}

			animationSet.cue = context.br.ReadNullableString();

			// Shadow layers
			{
				int shadowLayerCount = context.br.ReadInt32();
				if (shadowLayerCount <= 0)
					animationSet.shadowLayers = null;
				else
				{
					animationSet.shadowLayers = new List<ShadowLayer>();
					for (int i = 0; i < shadowLayerCount; i++)
					{
						animationSet.shadowLayers.Add(new ShadowLayer(
								context.br.ReadInt32(),
								new SpriteRef(context)));
					}

					animationSet.cachedShadowBounds = context.br.ReadBounds();
				}
			}

			return animationSet;
		}

		/// <summary>Check that an AnimationSet round-trips through serialization cleanly</summary>
		public static void RoundTripCheck(this AnimationSet animationSet, GraphicsDevice graphicsDevice, bool useExternalImages)
		{
			// Serialize a first time
			MemoryStream firstMemoryStream = new MemoryStream();
			BinaryWriter firstBinaryWriter = new BinaryWriter(firstMemoryStream);
			ImageWriter firstImageWriter = null;
			if (useExternalImages)
			{
				firstImageWriter = new ImageWriter();
				animationSet.RegisterImages(firstImageWriter);
				firstImageWriter.WriteOutAllImages(firstMemoryStream);
			}
			AnimationSerializeContext firstSerializeContext = new AnimationSerializeContext(firstBinaryWriter, firstImageWriter);
			animationSet.Serialize(firstSerializeContext);
			byte[] originalData = firstMemoryStream.ToArray();

			// Then deserialize that data
			BinaryReader br = new BinaryReader(new MemoryStream(originalData));
			ImageBundle imageBundle = null;
			if (useExternalImages)
			{
				var helper = new SimpleTextureLoadHelper(graphicsDevice);
				imageBundle = new ImageBundle();
				br.BaseStream.Position = imageBundle.ReadAllImages(originalData, (int)br.BaseStream.Position, helper);
			}
			AnimationDeserializeContext deserializeContext = new AnimationDeserializeContext(br, imageBundle, graphicsDevice);
			AnimationSet deserialized = deserializeContext.DeserializeAnimationSet();

			// Then serialize that deserialized data and see if it matches
			// (Ideally we'd recursivly check the AnimationSet to figure out if it matches, but that's a bit too hard)
			MemoryCompareStream secondMemoryStream = new MemoryCompareStream(originalData);
			BinaryWriter secondBinaryWriter = new BinaryWriter(secondMemoryStream);
			ImageWriter secondImageWriter = null;
			if (useExternalImages)
			{
				secondImageWriter = new ImageWriter();
				deserialized.RegisterImages(secondImageWriter);
				secondImageWriter.WriteOutAllImages(secondMemoryStream);
			}
			AnimationSerializeContext secondSerializeContext = new AnimationSerializeContext(secondBinaryWriter, secondImageWriter);
			deserialized.Serialize(secondSerializeContext);

			// Clean-up:
			imageBundle?.Dispose();
		}

		#endregion

		#region Animation

		public static void Serialize(this Animation animation, AnimationSerializeContext context)
		{
			context.bw.Write(animation.isLooped);
			context.bw.WriteNullableString(animation.friendlyName);

			context.bw.Write(animation.Frames.Count);
			for (int i = 0; i < animation.Frames.Count; i++)
			{
				animation.Frames[i].Serialize(context);
			}


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
			for (int i = 0; i < frameCount; i++)
			{
				animation.Frames.Add(context.DeserializeAnimationFrame());
			}

			if (context.Version >= 35)
				animation.cachedBounds = context.br.ReadBounds();

			// NOTE: Had to remove call to CalculateGraphicsBounds for old sprites (because we can't get that data at load time in the engine). Time to do a full rewrite.

			animation.isShared = context.br.ReadBoolean();
			animation.cue = context.br.ReadNullableString();
			animation.preventDropMotion = context.br.ReadBoolean();

			return animation;
		}

		#endregion
	}
}