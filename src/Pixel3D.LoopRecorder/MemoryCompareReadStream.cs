// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;
using System.IO;

namespace Pixel3D.LoopRecorder
{
	public class MemoryCompareReadStream : Stream
	{
		private long position;

		private readonly byte[] readBuffer1;
		private readonly byte[] readBuffer2;

		public MemoryCompareReadStream(byte[] buffer1, byte[] buffer2)
		{
			readBuffer1 = buffer1;
			readBuffer2 = buffer2;
			position = 0;
		}


		public override int Read(byte[] buffer, int offset, int count)
		{
			// TODO: Should allow reading fewer bytes than count (probably technically a bug that we don't)

			for (var i = 0; i < count; i++)
			{
				if (position >= readBuffer1.Length || position >= readBuffer2.Length)
				{
					Debug.Assert(false);
					throw new Exception("Ran out of data on a stream at " + position);
				}

				if (readBuffer1[position] != readBuffer2[position])
				{
					// Data for easier inspection:
					var buffer1Result = new byte[count];
					var buffer2Result = new byte[count];
					Array.Copy(readBuffer1, position, buffer1Result, 0, count);
					Array.Copy(readBuffer2, position, buffer2Result, 0, count);

					// Convenience:
					long integerResult1 = 0, integerResult2 = 0;
					if (count == 8)
					{
						integerResult1 = BitConverter.ToInt64(buffer1Result, 0);
						integerResult2 = BitConverter.ToInt64(buffer2Result, 0);
					}

					if (count == 4)
					{
						integerResult1 = BitConverter.ToInt32(buffer1Result, 0);
						integerResult2 = BitConverter.ToInt32(buffer2Result, 0);
					}

					if (count == 2)
					{
						integerResult1 = BitConverter.ToInt16(buffer1Result, 0);
						integerResult2 = BitConverter.ToInt16(buffer2Result, 0);
					}

					if (count == 1)
					{
						integerResult1 = buffer1Result[0];
						integerResult2 = buffer2Result[1];
					}

					// Here is where you look at the stack trace and try to guess what was being deserialized.
					// The stack trace will give you the containing type. You may be able to guess the field (eg: if it is a UInt32, and the object only has one UInt32).
					// Otherwise some sleuthing is required (eg: stepping through and looking at the stack traces leading up to the mismatch, while viewing the decompiled serialization code).
					// If the object hierarchy isn't too difficult, finding what is being deserialized in the visited object table will let you see what fields have yet to be initialized,
					// and you can work out the field currently being deserialized (note that field serialization is currently generated in alphabetical order - view the decompiled serializer to confirm).
					Debug.Assert(false);
					throw new Exception("Data mismatch at byte " + position);
				}

				buffer[offset + i] = readBuffer1[position];
				position++;
			}

			return count;
		}


		public override int ReadByte()
		{
			if (position >= readBuffer1.Length || position >= readBuffer2.Length ||
			    readBuffer1[position] != readBuffer2[position])
			{
				Debug.Assert(false);
				throw new Exception("Data mismatch at byte " + position);
			}

			var result = readBuffer1[position];
			position++;

			return result;
		}


		#region Boring Stream Stuff

	    public override bool CanRead
	    {
	        get { return true; }
	    }
		public override bool CanSeek
		{
		    get { return true; }
		}
		public override bool CanWrite
		{
		    get { return false; }
		}

		public override void Flush()
		{
		}

	    public override long Length
	    {
	        get { return Math.Min(readBuffer1.LongLength, readBuffer2.LongLength); }
	    }

	    public override long Position
		{
	        get
	        {
                return position;
	        }
	        set
	        {
	            position = value;
	        }
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}

		public override void WriteByte(byte value)
		{
			throw new InvalidOperationException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					position = offset;
					break;
				case SeekOrigin.Current:
					position += offset;
					break;
				case SeekOrigin.End:
					position = Length - offset;
					break;
			}

			return Position;
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		#endregion
	}
}