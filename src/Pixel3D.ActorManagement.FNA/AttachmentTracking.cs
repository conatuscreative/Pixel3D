// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using Pixel3D.Animations;

namespace Pixel3D.ActorManagement
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
		public readonly Animation animation;
		public readonly Position incomingAttachment;
		public readonly bool inRange;
		public readonly OutgoingAttachmentView outgoingAttachmentView;

		public IncomingAttachmentAttempt(OutgoingAttachmentView outgoingAttachmentView, Animation animation,
			Position incomingAttachment, bool inRange)
		{
			this.outgoingAttachmentView = outgoingAttachmentView;
			this.animation = animation;
			this.incomingAttachment = incomingAttachment;
			this.inRange = inRange;
		}
	}
}