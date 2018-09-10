// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;

namespace Pixel3D.Pipeline
{
	/// <summary>Pass-through stream that captures data that is read through it</summary>
	public class CaptureStream : Stream
	{
		public CaptureStream(Stream streamToCapture, Stream captureTarget)
		{
			if (!streamToCapture.CanRead)
				throw new ArgumentException("Can only capture from readable streams");

			StreamToCapture = streamToCapture;
			CaptureTarget = captureTarget;
		}

		public Stream StreamToCapture { get; }
		public Stream CaptureTarget { get; }


		public override bool CanRead => StreamToCapture.CanRead;
		public override bool CanSeek => false;
		public override bool CanWrite => false;

		public override long Length => StreamToCapture.Length;

		public override long Position
		{
			get => StreamToCapture.Position;
			set => StreamToCapture.Position = value;
		}


		protected override void Dispose(bool disposing)
		{
			if (disposing) StreamToCapture?.Dispose();
			base.Dispose(disposing);
		}

		public override void Flush()
		{
			StreamToCapture.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			// Read into the given buffer:
			var bytesRead = StreamToCapture.Read(buffer, offset, count);
			Debug.Assert(bytesRead <= count);

			// Copy that data to the capture buffer:
			CaptureTarget?.Write(buffer, offset, bytesRead);

			return bytesRead;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException();
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}
	}
}