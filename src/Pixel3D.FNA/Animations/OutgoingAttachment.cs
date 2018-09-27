using Pixel3D.Extensions;

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
        
        public OutgoingAttachment Clone()
        {
            var oa = new OutgoingAttachment();
            oa.position = position;
            oa.attachRange = attachRange;
            oa.facing = facing;
            oa.targetAnimationContext = targetAnimationContext;
            oa.targetAttachmentContext = targetAttachmentContext;
            return oa;
        }

		#region Serialization

	    #region OutgoingAttachment

	    public void Serialize(AnimationSerializeContext context)
	    {
		    context.bw.Write(position);
			targetAnimationContext.Serialize(context.bw);
		    targetAttachmentContext.Serialize(context.bw);
		    attachRange.Serialize(context.bw);
		    context.bw.Write((int)facing);
	    }

	    public OutgoingAttachment(AnimationDeserializeContext context)
	    {
		    position = context.br.ReadPosition();
		    targetAnimationContext = new TagSet(context.br);
		    targetAttachmentContext = new TagSet(context.br);
			attachRange = new AABB(context.br);
		    facing = (Facing)context.br.ReadInt32();
	    }

	    #endregion

		#endregion
	}
}
