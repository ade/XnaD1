/* Copyright (c) 2008 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Lidgren.Network
{
	/// <summary>
	/// Time service
	/// </summary>
	public static class NetTime
	{
		private static long s_timeInitialized = Stopwatch.GetTimestamp();
		private static double s_dInvFreq = 1.0 / (double)Stopwatch.Frequency;

		/// <summary>
		/// Get number of seconds since the application started
		/// </summary>
		public static double Now { get { return (double)(Stopwatch.GetTimestamp() - s_timeInitialized) * s_dInvFreq; } }

		/// <summary>
		/// Get current time encoded into a cyclic ushort
		/// </summary>
		[CLSCompliant(false)]
		public static ushort NowEncoded
		{
			get
			{
				return (ushort)(Now * 1000 % ushort.MaxValue);
			}
		}

		public static string ToMillis(double time)
		{
			return ((int)(time * 1000.0)).ToString();
		}

		/// <summary>
		/// Get encoded cyclic ushort from a certain timestamp
		/// </summary>
		[CLSCompliant(false)]
		public static ushort Encoded(double now)
		{
			return (ushort)(now * 1000 % ushort.MaxValue);
		}

		/// <summary>
		/// Returns absolute timestamp
		/// Note; will only accept encoded timestamps IN THE PAST
		/// </summary>
		[CLSCompliant(false)]
		public static double FromEncoded(
			double now,
			int remoteMillisOffset,
			ushort encodedRemoteTimestamp,
			out int adjustRemoteMillis)
		{
			// my encoded time
			ushort localNow = (ushort)(now * 1000 % ushort.MaxValue);
			ushort localStamp = NetTime.NormalizeEncoded(encodedRemoteTimestamp + remoteMillisOffset);
			int elapsedMillis = NetTime.GetElapsedMillis(localStamp, localNow);

			adjustRemoteMillis = 0;
			return now - ((float)elapsedMillis / 1000.0f);
		}

		public static int GetElapsedMillis(int encodedEarlier, int encodedLater)
		{
			if (encodedLater < encodedEarlier)
				encodedLater += ushort.MaxValue;
			return encodedLater - encodedEarlier;
		}

		internal static ushort NormalizeEncoded(int val)
		{
			val = val % ushort.MaxValue;
			if (val < 0)
				val += ushort.MaxValue;
			return (ushort)val;
		}

		//internal static int CalculateOffset(double now, ushort val, double roundtrip)
		//{
		//	int oneWayMillis = (int)(roundtrip * 500.0);
		//	int remoteNow = NormalizeEncoded(val + oneWayMillis);
		//	int localNow = NetTime.Encoded(now);
		//
		//	return remoteNow - localNow;
		//}

		internal static int MergeOffsets(int currentOffset, int two)
		{
			int diff = Math.Abs(currentOffset - two);

			int altTwo = (two < 0 ? two + ushort.MaxValue : two - ushort.MaxValue);
			if (Math.Abs(currentOffset - altTwo) < diff)
				two = altTwo;

			return (currentOffset + two) / 2;
		}
	}
}