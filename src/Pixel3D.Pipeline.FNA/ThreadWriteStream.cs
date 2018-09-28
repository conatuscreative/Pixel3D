// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Pixel3D.Pipeline
{
	public class ThreadWriteStream : Stream
	{
		private const int internalBufferSize = 8192;
		private byte[] pendingBuffer;
		private int pendingBufferPosition;


		private long streamPosition;


		/// <summary>
		///     Transfers control of the outputStream to the internal writer thread. Don't touch it until Close or Dispose is
		///     called.
		/// </summary>
		public ThreadWriteStream(Stream outputStream)
		{
			this.outputStream = outputStream;
			workAvailableEvent = new AutoResetEvent(false);
			thread = new Thread(DoWork);
			thread.Start();
		}

        public override long Position
	    {
	        get { return streamPosition; }
	        set { throw new InvalidOperationException(); }
	    }

	    public override void Write(byte[] buffer, int offset, int count)
		{
			streamPosition += count;

			var didEnqueue = false;
			var written = 0;

			while (written != count)
			{
				Debug.Assert(written < count);

				if (pendingBuffer == null)
				{
					if (!bufferReturn.TryDequeue(out pendingBuffer))
						pendingBuffer = new byte[internalBufferSize];

					Debug.Assert(pendingBuffer.Length == internalBufferSize);
					pendingBufferPosition = 0;
				}

				var bytesToWrite = Math.Min(pendingBuffer.Length - pendingBufferPosition, count - written);

				Array.Copy(buffer, offset + written, pendingBuffer, pendingBufferPosition, bytesToWrite);

				written += bytesToWrite;
				pendingBufferPosition += bytesToWrite;
				Debug.Assert(pendingBufferPosition <= pendingBuffer.Length);

				if (pendingBufferPosition == pendingBuffer.Length)
				{
					workQueue.Enqueue(pendingBuffer);

					pendingBufferPosition = 0;
					pendingBuffer = null;

					didEnqueue = true;
				}
			}

			if (didEnqueue)
				workAvailableEvent.Set();
		}


		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (pendingBufferPosition != 0)
				{
					Array.Resize(ref pendingBuffer, pendingBufferPosition);
					workQueue.Enqueue(pendingBuffer);
					pendingBuffer = null;
					pendingBufferPosition = 0;
				}

				workQueue.Enqueue(null); // <- terminate
				workAvailableEvent.Set();
				thread.Join();
			}

			base.Dispose(disposing);
		}

		#region Thread

		private readonly Thread thread;
		private Stream outputStream;

		private readonly ConcurrentQueue<byte[]> workQueue = new ConcurrentQueue<byte[]>();
		private readonly AutoResetEvent workAvailableEvent;

		private readonly ConcurrentQueue<byte[]> bufferReturn = new ConcurrentQueue<byte[]>();

		private void DoWork()
		{
			while (true)
			{
				workAvailableEvent.WaitOne();

				byte[] work;
				while (workQueue.TryDequeue(out work))
					if (work != null)
					{
						outputStream.Write(work, 0, work.Length);
						bufferReturn.Enqueue(work);
					}
					else
					{
						outputStream.Close();
						outputStream = null;
						return;
					}
			}
		}

		#endregion

		#region Stream Guff

	    public override bool CanRead
	    {
	        get { return false; }
	    }

	    public override bool CanSeek
	    {
	        get { return false; }
	    }

		public override bool CanWrite
		{
		    get { return true; }
		}

		public override long Length
		{
		    get { return Position; }
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException();
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		public override void Flush()
		{
			// We could make it possible to signal the writer thread to flush (also wait for it?)
			throw new NotImplementedException();
		}

		#endregion
	}
}