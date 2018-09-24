// Copyright © Conatus Creative, Inc. All rights reserved.
// Licensed under the Apache 2.0 License. See LICENSE.md in the project root for license terms.

using System;
using System.Diagnostics;

namespace Pixel3D.Network.Rollback
{
	internal class PacketTimeTracker
	{
		/// <summary>The number of packets to keep timing information for</summary>
		public const int Capacity = 10;


		private const double safeRangeMinimumFrames = 2;

		public double[] allExpectedCurrentFrames = new double[Capacity];


		public double currentNetworkTime;
		public FrameDataBuffer<double> packetSentTimes = new FrameDataBuffer<double>();

		public int Count => packetSentTimes.Count;

		// The range of frame times received, discarding outliers
		public double NominalFrameRangeBegin { get; private set; }
		public double NominalFrameRangeEnd { get; private set; }


		/// <summary>The frame we would like to be the current frame (at this moment)</summary>
		public double DesiredCurrentFrame => 0.5 * NominalFrameRangeBegin + 0.5 * NominalFrameRangeEnd;

		/// <summary>The beginning of the range of frames we consider "close enough" to the desired frame</summary>
		public double SafeFrameRangeBegin
		{
			get
			{
				var desiredFrame = DesiredCurrentFrame;
				var nominalSize = desiredFrame - NominalFrameRangeBegin;
				var mirrorSize = NominalFrameRangeEnd - desiredFrame;
				return desiredFrame - Math.Max(Math.Max(nominalSize, mirrorSize), safeRangeMinimumFrames);
			}
		}

		/// <summary>The end of the range of frames we consider "close enough" to the desired frame</summary>
		public double SafeFrameRangeEnd
		{
			get
			{
				var desiredFrame = DesiredCurrentFrame;
				var nominalSize = NominalFrameRangeEnd - desiredFrame;
				var mirrorSize = desiredFrame - NominalFrameRangeBegin;
				return desiredFrame + Math.Max(Math.Max(nominalSize, mirrorSize), safeRangeMinimumFrames);
			}
		}


		/// <summary>
		///     Indicate to the timing code that a new packet was received
		/// </summary>
		/// <param name="packetFrame">The frame associated with a received packet</param>
		/// <param name="packetSentTime">The estimated local network time that the packet was sent at (in seconds)</param>
		public void ReceivePacket(int packetFrame, double packetSentTime)
		{
			// Once we are at capacity, ignore any packets that are too old (they'd be removed immediately anyway)
			if (packetSentTimes.Count >= Capacity && packetFrame < packetSentTimes.Keys[0])
				return;

			// Ignore any duplicate packets
			if (packetSentTimes.ContainsKey(packetFrame))
				return;


			packetSentTimes.Add(packetFrame, packetSentTime);

			if (packetSentTimes.Count > Capacity)
				packetSentTimes.RemoveAt(0);
		}


		/// <summary>
		///     Set a new network time and recalculate timing state
		/// </summary>
		/// <param name="currentNetworkTime">Network time in seconds</param>
		public void Update(double currentNetworkTime)
		{
			this.currentNetworkTime = currentNetworkTime;

			if (packetSentTimes.Count == 0)
				return; // Don't attempt to do any timing maths if we've got no data

			// Calculate the expected current frame for all packets:
			Debug.Assert(packetSentTimes.Count <= Capacity);
			for (var i = 0; i < packetSentTimes.Count; i++)
				allExpectedCurrentFrames[i] =
					ExpectedCurrentFrameForPacket(packetSentTimes.Keys[i], packetSentTimes.Values[i]);

			// Sort the expected frame numbers (for doing maths on them)
			Array.Sort(allExpectedCurrentFrames, 0, packetSentTimes.Count);
			var rangeBegin = 0;
			var rangeEnd = packetSentTimes.Count;

			// Discard the extreme values
			if (rangeEnd - rangeBegin > 4)
				rangeEnd--;
			if (rangeEnd - rangeBegin > 4)
				rangeBegin++;
			if (rangeEnd - rangeBegin > 6)
				rangeEnd--;
			if (rangeEnd - rangeBegin > 6)
				rangeBegin++;

			// Use this as the nominal range for the frame number
			NominalFrameRangeBegin = allExpectedCurrentFrames[rangeBegin];
			NominalFrameRangeEnd = allExpectedCurrentFrames[rangeEnd - 1];
		}


		/// <summary>
		///     Calculate the expected current frame for a single packet.
		/// </summary>
		public double ExpectedCurrentFrameForPacket(int packetFrame, double packetSentTime)
		{
			var timeSincePacket = currentNetworkTime - packetSentTime;
			var framesSincePacket = timeSincePacket * RollbackDriver.FramesPerSecond;
			return packetFrame + framesSincePacket;
		}
	}
}