using System;
using System.Collections.Generic;
using Pixel3D.Animations.Serialization;

namespace Pixel3D.Animations
{
    public class OutgoingAttachment
    {
        // Zero-argument constructor, because we have a deserialize constructor
        public OutgoingAttachment()
        {
            targetAnimationContext = TagSet.Empty;
            targetAttachmentContext = TagSet.Empty;
        }

        public Position position;
        public TagSet targetAnimationContext;
        public TagSet targetAttachmentContext;
        public AABB attachRange;

        /// <summary>Which way the victim of the attachment should face</summary>
        public Facing facing;

        public enum Facing
        {
            Any = 0,
            Same = 1,
            Opposite = 2,
        }
        
        #region Serialization

        public void Serialize(AnimationSerializeContext context)
        {
            context.bw.Write(position);
            targetAnimationContext.SerializeTagSet(context);
            targetAttachmentContext.SerializeTagSet(context);
            context.bw.Write(attachRange);
            context.bw.Write((int)facing);
        }

        /// <summary>Deserialize into new object instance</summary>
        public OutgoingAttachment(AnimationDeserializeContext context)
        {
            position = context.br.ReadPosition();
	        targetAnimationContext = context.DeserializeTagSet();
            targetAttachmentContext = context.DeserializeTagSet();
			attachRange = context.br.ReadAABB();
            facing = (Facing)context.br.ReadInt32();
        }

        #endregion

        public OutgoingAttachment Clone()
        {
            var oa = new OutgoingAttachment();
            oa.position = this.position;
            oa.attachRange = this.attachRange;
            oa.facing = this.facing;
            oa.targetAnimationContext = this.targetAnimationContext;
            oa.targetAttachmentContext = this.targetAttachmentContext;
            return oa;
        }
    }
}
