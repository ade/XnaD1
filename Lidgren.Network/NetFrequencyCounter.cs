using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	public sealed class NetFrequencyCounter
	{
		private float m_windowSize;
		private double m_windowEnd;
		private int m_windowCount;
		private double m_lastCount;
		private double m_countLow, m_countHigh;

		private float m_frequency;
		private float m_low, m_high;

		public float AverageFrequency { get { return m_frequency; } }
		public float LowestFrequency { get { return m_low; } }
		public float HighestFrequency { get { return m_high; } }

		public NetFrequencyCounter(float windowSizeSeconds)
		{
			m_windowSize = windowSizeSeconds;
			m_windowEnd = NetTime.Now + m_windowSize;
			m_countLow = float.MinValue;
			m_countHigh = float.MaxValue;
			m_lastCount = 0;
		}

		public void Count()
		{
			Count(NetTime.Now);
		}

		public void Count(double now)
		{
			double thisLength = now - m_lastCount;
			if (thisLength > m_countLow)
				m_countLow = thisLength;
			if (thisLength < m_countHigh)
				m_countHigh = thisLength;

			if (now > m_windowEnd)
			{
				m_frequency = (float)((double)m_windowCount / (m_windowSize + (now - m_windowEnd)));
				m_low = (float)(1.0 / m_countLow);
				m_high = (float)(1.0 / m_countHigh);
				m_countLow = float.MinValue;
				m_countHigh = float.MaxValue;

				m_windowEnd += m_windowSize;
				m_windowCount = 0;
			}
			m_windowCount++;
			m_lastCount = now;
		}
	}
}
