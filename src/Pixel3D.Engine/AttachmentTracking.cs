using Pixel3D.Animations;

namespace Pixel3D.Engine
{
    public class OutgoingAttachmentAttempt
    {
        public readonly Animation animation;
        public readonly OutgoingAttachmentView outgoingAttachmentView;

        public OutgoingAttachmentAttempt(Animation animation, OutgoingAttachmentView outgoingAttachmentView)
        {
            this.animation = animation;
            this.outgoingAttachmentView = outgoingAttachmentView;
        }
    }


    public class IncomingAttachmentAttempt
    {
        public readonly OutgoingAttachmentView outgoingAttachmentView;
        public readonly Animation animation;
        public readonly Position incomingAttachment;
        public readonly bool inRange;

        public IncomingAttachmentAttempt(OutgoingAttachmentView outgoingAttachmentView, Animation animation, Position incomingAttachment, bool inRange)
        {
            this.outgoingAttachmentView = outgoingAttachmentView;
            this.animation = animation;
            this.incomingAttachment = incomingAttachment;
            this.inRange = inRange;
        }

    }
}
