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
using System.Net;
using System.Threading;

namespace Lidgren.Network
{
	internal sealed class DelayedPacket
	{
		public byte[] Data;
		public IPEndPoint Recipient;
		public double DelayedUntil;
	}

	/// <summary>
	/// Methods for simulating outgoing latency and loss
	/// </summary>
	public abstract partial class NetBase
	{
		private float m_simulatedLoss;
		private float m_simulatedMinimumLatency;
		private float m_simulatedLatencyVariance;
		private float m_simulatedDuplicateChance;

		/// <summary>
		/// Simulates chance for a packet to become lost in transit; 0.0f means no packets; 1.0f means all packets are lost
		/// </summary>
		public float SimulatedLoss { get { return m_simulatedLoss; } set { m_simulatedLoss = value; } }

		/// <summary>
		/// Simulates chance for a packet to become duplicated in transit; 0.0f means no packets; 1.0f means all packets are duplicated
		/// </summary>
		public float SimulatedDuplicates { get { return m_simulatedDuplicateChance; } set { m_simulatedDuplicateChance = value; } }

		/// <summary>
		/// Simulates minimum two-way latency, ie. roundtrip (in seconds) of outgoing packets
		/// </summary>
		public float SimulatedMinimumLatency { get { return m_simulatedMinimumLatency; } set { m_simulatedMinimumLatency = value; } }

		/// <summary>
		/// Simulates maximum amount of random variance (in seconds) in roundtrip latency added to the MinimumLatency
		/// </summary>
		public float SimulatedLatencyVariance { get { return m_simulatedLatencyVariance; } set { m_simulatedLatencyVariance = value; } }

		private List<DelayedPacket> m_delayedPackets = new List<DelayedPacket>();
		private bool m_suppressSimulatedLag;
		private List<DelayedPacket> m_removeDelayedPackets = new List<DelayedPacket>();

		/// <summary>
		/// Simulates bad outgoing networking conditions - similar settings should be used on both server and client
		/// </summary>
		/// <param name="lossChance">0.0 means no packets dropped; 1.0 means all packets dropped</param>
		/// <param name="duplicateChance">0.0 means no packets duplicated; 1.0f means all packets duplicated</param>
		/// <param name="minimumLatency">the minimum roundtrip time in seconds</param>
		/// <param name="latencyVariance">the maximum variance in roundtrip time (randomly added on top of minimum latency)</param>
		public void Simulate(
			float lossChance,
			float duplicateChance,
			float minimumLatency,
			float latencyVariance)
		{
			m_simulatedLoss = lossChance;
			m_simulatedDuplicateChance = duplicateChance;
			m_simulatedMinimumLatency = minimumLatency;
			m_simulatedLatencyVariance = latencyVariance;
		}

		/// <summary>
		/// returns true if packet should be sent by calling code
		/// </summary>
		private bool SimulatedSendPacket(byte[] data, int length, IPEndPoint remoteEP)
		{
			if (m_simulatedLoss > 0.0f)
			{
				if (NetRandom.Instance.NextFloat() < m_simulatedLoss)
				{
					// packet was lost!
					// LogWrite("Faking lost packet...");
					m_statistics.CountSimulatedDroppedPacket();
					return false;
				}
			}

			if (m_simulatedDuplicateChance > 0.0f)
			{
				if (NetRandom.Instance.NextFloat() < m_simulatedDuplicateChance)
				{
					// duplicate; send one now, one after max variance
					float first = m_simulatedMinimumLatency;
					float second = (m_simulatedLatencyVariance > 0.0f ? m_simulatedLatencyVariance : 0.01f); // min 10 ms

					DelayPacket(data, length, remoteEP, second);

					if (first <= 0.0f)
						return true; // send first right away using regular code

					DelayPacket(data, length, remoteEP, first);
					return false; // delay both copies
				}
			}

			if (m_simulatedMinimumLatency > 0.0f || m_simulatedLatencyVariance > 0.0f)
			{
				DelayPacket(
					data, length, remoteEP,
					(m_simulatedMinimumLatency * 0.5f) +
					(m_simulatedLatencyVariance * NetRandom.Instance.NextFloat() * 0.5f)
				);
				return false;
			}

			// just send
			return true;
		}

		private void DelayPacket(byte[] data, int length, IPEndPoint remoteEP, float delay)
		{
#if DEBUG
			if (Thread.CurrentThread != m_heartbeatThread)
				throw new Exception("Threading error; should be heartbeat thread. Please check callstack!");
#endif
			DelayedPacket pk = new DelayedPacket();
			pk.Data = new byte[length];
			Buffer.BlockCopy(data, 0, pk.Data, 0, length);
			pk.Recipient = remoteEP;

			double now = NetTime.Now;
			pk.DelayedUntil = now + delay;
			m_delayedPackets.Add(pk);
		}

		private void SendDelayedPackets(double now)
		{
			if (m_delayedPackets.Count < 1)
				return;

			m_removeDelayedPackets.Clear();
			foreach (DelayedPacket pk in m_delayedPackets)
			{
				if (now >= pk.DelayedUntil)
				{
					m_suppressSimulatedLag = true;
					SendPacket(pk.Data, pk.Data.Length, pk.Recipient);
					m_suppressSimulatedLag = false;
					m_removeDelayedPackets.Add(pk);
				}
			}
			foreach (DelayedPacket pk in m_removeDelayedPackets)
				m_delayedPackets.Remove(pk);
		}
	}
}
