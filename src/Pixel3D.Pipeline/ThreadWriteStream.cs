using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Pixel3D.Pipeline
{
    public class ThreadWriteStream : Stream
    {

        #region Thread

        Thread thread;
        Stream outputStream;

        ConcurrentQueue<byte[]> workQueue = new ConcurrentQueue<byte[]>();
        AutoResetEvent workAvailableEvent;

        ConcurrentQueue<byte[]> bufferReturn = new ConcurrentQueue<byte[]>();

        void DoWork()
        {
            while(true)
            {
                workAvailableEvent.WaitOne();

                byte[] work;
                while(workQueue.TryDequeue(out work))
                {
                    if(work != null)
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
        }

        #endregion


        /// <summary>Transfers control of the outputStream to the internal writer thread. Don't touch it until Close or Dispose is called.</summary>
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


        long streamPosition = 0;

        const int internalBufferSize = 8192;
        byte[] pendingBuffer;
        int pendingBufferPosition;

        public override void Write(byte[] buffer, int offset, int count)
        {
            streamPosition += count;

            bool didEnqueue = false;
            int written = 0;

            while(written != count)
            {
                Debug.Assert(written < count);

                if(pendingBuffer == null)
                {
                    if(!bufferReturn.TryDequeue(out pendingBuffer))
                        pendingBuffer = new byte[internalBufferSize];

                    Debug.Assert(pendingBuffer.Length == internalBufferSize);
                    pendingBufferPosition = 0;
                }

                int bytesToWrite = Math.Min(pendingBuffer.Length - pendingBufferPosition, count - written);

                Array.Copy(buffer, offset + written, pendingBuffer, pendingBufferPosition, bytesToWrite);

                written += bytesToWrite;
                pendingBufferPosition += bytesToWrite;
                Debug.Assert(pendingBufferPosition <= pendingBuffer.Length);

                if(pendingBufferPosition == pendingBuffer.Length)
                {
                    workQueue.Enqueue(pendingBuffer);

                    pendingBufferPosition = 0;
                    pendingBuffer = null;

                    didEnqueue = true;
                }
            }

            if(didEnqueue)
                workAvailableEvent.Set();
        }

	    #region Stream Guff

	    public override bool CanRead => false;
	    public override bool CanSeek => false;
	    public override bool CanWrite => true;

	    public override long Length => Position;

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


		protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(pendingBufferPosition != 0)
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
    }
}
