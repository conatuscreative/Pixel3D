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
    }
}
