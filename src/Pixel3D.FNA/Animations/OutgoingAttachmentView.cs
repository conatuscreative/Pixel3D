// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
namespace Pixel3D.Animations
{
    public struct OutgoingAttachmentView
    {
        public OutgoingAttachmentView(OutgoingAttachment sourceAttachment, Position sourcePosition, bool sourceFlipX)
        {
            attachment = sourceAttachment;
            facingLeft = sourceFlipX;
            position = sourcePosition;

            if(sourceAttachment != null)
            {
                position += sourceAttachment.position.MaybeFlipX(sourceFlipX);

                attachRange = sourceAttachment.attachRange;
                if(sourceFlipX)
                    attachRange.FlipXInPlace();
            }
            else
            {
                attachRange = default(AABB);
            }

            attachRange += position;
        }

        public bool IsValid { get { return attachment != null; } }

        public OutgoingAttachment attachment;
        public bool facingLeft;

        public Position position;
        public AABB attachRange;

    }
}
