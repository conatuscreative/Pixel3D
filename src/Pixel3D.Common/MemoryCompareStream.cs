// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.
using System;
using System.Diagnostics;
using System.IO;

namespace Pixel3D
{
	// Use this class for debugging - to find where a memory stream mismatches:
	public class MemoryCompareStream : Stream
	{
		private readonly byte[] compareTo;
		private long position;

		public MemoryCompareStream(byte[] compareTo)
		{
			this.compareTo = compareTo;
			position = 0;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			for (var i = 0; i < count; i++)
				if (buffer[offset + i] != compareTo[position + i])
				{
					Debug.Assert(false);
					throw new Exception("Data mismatch");
				}

			position += count;
		}

		public override void WriteByte(byte value)
		{
			if (compareTo[position] != value)
			{
				Debug.Assert(false);
				throw new Exception("Data mismatch");
			}

			position++;
		}


		#region Boring Stream Stuff

		public override bool CanRead
		{
			get { return false; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override void Flush()
		{
		}

		public override long Length
		{
			get { return compareTo.Length; }
		}

		public override long Position
		{
			get { return position; }
			set { position = value; }
		}

		public override int Read(byte[] buffer, int offset, int count)
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
					position = compareTo.Length - offset;
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