using System;
using System.Diagnostics;
using System.IO;

namespace Pixel3D.Pipeline
{
    /// <summary>Pass-through stream that captures data that is read through it</summary>
    public class CaptureStream : Stream
    {
        public Stream StreamToCapture { get; }
        public Stream CaptureTarget { get; }


        public CaptureStream(Stream streamToCapture, Stream captureTarget)
        {
            if(!streamToCapture.CanRead)
                throw new ArgumentException("Can only capture from readable streams");

            StreamToCapture = streamToCapture;
            CaptureTarget = captureTarget;
        }


        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
	            StreamToCapture?.Dispose();
            }
            base.Dispose(disposing);
        }


        public override bool CanRead => StreamToCapture.CanRead;
	    public override bool CanSeek => false;
	    public override bool CanWrite => false;

	    public override void Flush()
        {
            StreamToCapture.Flush();
        }

        public override long Length => StreamToCapture.Length;

	    public override long Position
        {
            get => StreamToCapture.Position;
	        set => StreamToCapture.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Read into the given buffer:
            int bytesRead = StreamToCapture.Read(buffer, offset, count);
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
