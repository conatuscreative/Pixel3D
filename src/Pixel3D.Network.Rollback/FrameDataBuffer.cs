// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System.Collections.Generic;
using System.Diagnostics;

namespace Pixel3D.Network.Rollback
{
	// TODO: Needs a new name? Gets indexed by things other than frame number (such as JLE#)

	internal class FrameDataBuffer<T> : SortedList<int, T>
	{
		/// <summary>
		///     Remove items with a frame less than <paramref name="frame" /> such that the result given
		///     by <see cref="TryGetLastBeforeOrAtFrame" /> for that frame does not change.
		/// </summary>
		public void CleanUpBefore(int frame)
		{
			while (Count > 1 && Keys[0] < frame && Keys[1] <= frame)
				RemoveAt(0);
		}


		/// <summary>
		///     Remove all entries before the given frame (exclusive). Can change the result given by
		///     <see cref="TryGetLastBeforeOrAtFrame" />.
		/// </summary>
		public void RemoveAllBefore(int frame)
		{
			while (Count > 0 && Keys[0] < frame)
				RemoveAt(0);
		}

		/// <summary>Delete all entries at and after the given frame (inclusive)</summary>
		public void RemoveAllFrom(int frame)
		{
			while (Count > 0 && Keys[Count - 1] >= frame)
				RemoveAt(Count - 1);
		}

		#region Construct and Clone

		public FrameDataBuffer()
		{
		}

		public FrameDataBuffer(IDictionary<int, T> d) : base(d)
		{
		}

		public FrameDataBuffer<T> GetCopy()
		{
			return new FrameDataBuffer<T>(this);
		}

		#endregion


		#region Info

		public int OldestStoredFrame => Count > 0 ? Keys[0] : 0;
		public int NewestStoredFrame => Count > 0 ? Keys[Count - 1] : 0;

		#endregion


		#region Searching

		/// <summary>Find the last stored item from a frame less than or equal to `frame`.</summary>
		/// <param name="i">The index where the value was found, or -1 if not found.</param>
		/// <returns>The frame where the value was found, or -1 if not found.</returns>
		public int TryGetLastBeforeOrAtFrame(int frame, out T result, out int i)
		{
			for (i = Count - 1; i >= 0; i--) // search backwards
				if (Keys[i] <= frame) // first encountered valid item
				{
					result = Values[i];
					return Keys[i];
				}

			result = default(T);
			return -1;
		}

		public int TryGetLastBeforeOrAtFrame(int frame, out T result)
		{
			int i;
			return TryGetLastBeforeOrAtFrame(frame, out result, out i);
		}


		public T GetLastBeforeOrAtFrameOrDefault(int frame)
		{
			T v;
			TryGetLastBeforeOrAtFrame(frame, out v);
			return v;
		}


		public T GetLastBeforeOrAtFrameUnchecked(int frame)
		{
			T v;
			var debugCheck = TryGetLastBeforeOrAtFrame(frame, out v);
			Debug.Assert(debugCheck >= 0);
			return v;
		}


		/// <summary>Find the first stored item from a frame greater than `frame`.</summary>
		/// <returns>The frame where the value was found, or -1 if not found.</returns>
		public int TryGetFirstAfterFrame(int frame, out T result)
		{
			for (var i = 0; i < Count; i++) // search forwards
				if (Keys[i] > frame) // first valid encountered item
				{
					result = Values[i];
					return Keys[i];
				}

			result = default(T);
			return -1;
		}


		/// <summary>
		///     Returns the first frame number of a missing value in the frame buffer,
		///     from the starting frame onwards.
		/// </summary>
		public int FirstUnknownFrameFrom(int startingFrame)
		{
			var lookingForFrame = startingFrame;

			for (var i = 0; i < Count; i++)
				if (Keys[i] < lookingForFrame)
					continue; // seeking forward
				else if (Keys[i] == lookingForFrame)
					lookingForFrame++; // found it, look for the next one
				else
					break;

			return lookingForFrame;
		}

		#endregion
	}
}