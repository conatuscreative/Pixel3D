using System.Diagnostics;

namespace Pixel3D.Animations
{
    public struct OutgoingAttachmentView
    {
        public OutgoingAttachmentView(OutgoingAttachment sourceAttachment, Position sourcePosition, bool sourceFlipX)
        {
            this.attachment = sourceAttachment;
            this.facingLeft = sourceFlipX;
            this.position = sourcePosition;

            if(sourceAttachment != null)
            {
                this.position += sourceAttachment.position.MaybeFlipX(sourceFlipX);

                this.attachRange = sourceAttachment.attachRange;
                if(sourceFlipX)
                    this.attachRange.FlipXInPlace();
            }
            else
            {
                this.attachRange = default(AABB);
            }

            this.attachRange += this.position;
        }

        public bool IsValid { get { return attachment != null; } }

        public OutgoingAttachment attachment;
        public bool facingLeft;

        public Position position;
        public AABB attachRange;

    }
}
