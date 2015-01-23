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
//#define USE_RELEASE_STATISTICS

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lidgren.Network
{
	public sealed partial class NetConnection
	{
		internal NetConnectionStatistics m_statistics;

		/// <summary>
		/// Gets the statistics object for this connection
		/// </summary>
		public NetConnectionStatistics Statistics { get { return m_statistics; } }
	}

	/// <summary>
	/// Statistics per connection
	/// </summary>
	public sealed class NetConnectionStatistics
	{
		private NetConnection m_connection;
		private double m_startTimestamp;
		private float m_totalTimeSpan;
		private float m_windowSize;

		//
		// Total
		//
		private long m_messagesSent;
		private long m_messagesReceived;
		private long m_packetsSent;
		private long m_packetsReceived;
		private long m_bytesSent;
		private long m_bytesReceived;

		private long m_messagesResent;
		private long m_duplicateMessagesRejected;
		private long m_acksSent;
		private long m_acksReceived;

		//
		// User
		//
		private long m_userMessagesSent;
		private long m_userMessagesReceived;
		private long m_userBytesSent;
		private long m_userBytesReceived;

		private long m_userMessagesResent;
		private long m_userDuplicateMessagesRejected;
		private long m_userSequencedMessagesRejected;

		private NetConnectionStatistics m_currentWindow;
		private NetConnectionStatistics m_previousWindow;

		//
		// Types
		//
		private long[] m_userTypeMessagesSent = new long[32];
		private long[] m_userTypeMessagesReceived = new long[32];

		/// <summary>
		/// Gets the number of packets received
		/// </summary>
		public long PacketsReceived { get { return m_packetsReceived; } }

		/// <summary>
		/// Gets the number of packets sent
		/// </summary>
		public long PacketsSent { get { return m_packetsSent; } }

		public long AcknowledgesSent { get { return m_acksSent; } }
		public long AcknowledgesReceived { get { return m_acksReceived; } }

		public long MessagesResent { get { return m_messagesResent; } }

		public long DuplicateMessagesRejected { get { return m_duplicateMessagesRejected; } }
		public long SequencedMessagesRejected { get { return m_userSequencedMessagesRejected; } }

		public long GetUserMessagesSent(NetChannel channel)
		{
			return m_userTypeMessagesSent[(int)channel];
		}

		public long GetUserMessagesReceived(NetChannel channel)
		{
			return m_userTypeMessagesReceived[(int)channel];
		}

		public long GetUserUnreliableSent()
		{
			return m_userTypeMessagesSent[(int)NetChannel.Unreliable];
		}

		public long GetUserReliableUnorderedSent()
		{
			return m_userTypeMessagesSent[(int)NetChannel.ReliableUnordered];
		}

		public long GetUserSequencedSent()
		{
			return m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder1] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder2] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder3] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder4] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder5] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder6] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder7] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder8] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder9] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder10] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder11] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder12] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder13] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder14] +
				m_userTypeMessagesSent[(int)NetChannel.UnreliableInOrder15];
		}

		public long GetUserOrderedSent()
		{
			return m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder1] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder2] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder3] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder4] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder5] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder6] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder7] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder8] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder9] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder10] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder11] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder12] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder13] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder14] +
				m_userTypeMessagesSent[(int)NetChannel.ReliableInOrder15];
		}



		public long GetUserUnreliableReceived()
		{
			return m_userTypeMessagesReceived[(int)NetChannel.Unreliable];
		}

		public long GetUserReliableUnorderedReceived()
		{
			return m_userTypeMessagesReceived[(int)NetChannel.ReliableUnordered];
		}

		public long GetUserSequencedReceived()
		{
			return m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder1] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder2] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder3] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder4] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder5] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder6] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder7] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder8] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder9] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder10] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder11] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder12] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder13] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder14] +
				m_userTypeMessagesReceived[(int)NetChannel.UnreliableInOrder15];
		}

		public long GetUserOrderedReceived()
		{
			return m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder1] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder2] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder3] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder4] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder5] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder6] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder7] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder8] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder9] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder10] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder11] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder12] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder13] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder14] +
				m_userTypeMessagesReceived[(int)NetChannel.ReliableInOrder15];
		}

		//
		// Average over time versions
		//

		/// <summary>
		/// Gets bytes sent per second
		/// </summary>
		public float GetBytesSentPerSecond(double now)
		{
			if (m_previousWindow != null)
				return m_previousWindow.GetBytesSentPerSecond(now);

			// (in previous window)
			return (float)m_bytesSent / m_totalTimeSpan;
		}

		/// <summary>
		/// Gets bytes received per second
		/// </summary>
		public float GetBytesReceivedPerSecond(double now)
		{
			if (m_previousWindow != null)
				return m_previousWindow.GetBytesReceivedPerSecond(now);

			// (in previous window)
			return (float)m_bytesReceived / m_totalTimeSpan;
		}

		/// <summary>
		/// Gets messages sent per second
		/// </summary>
		public float GetMessagesSentPerSecond(double now)
		{
			if (m_previousWindow != null)
				return m_previousWindow.GetMessagesSentPerSecond(now);

			// (in previous window)
			return (float)m_messagesSent / m_totalTimeSpan;
		}

		/// <summary>
		/// Gets messages received per second
		/// </summary>
		public float GetMessagesReceivedPerSecond(double now)
		{
			if (m_previousWindow != null)
				return m_previousWindow.GetMessagesReceivedPerSecond(now);

			// (in previous window)
			return (float)m_messagesReceived / m_totalTimeSpan;
		}

		public NetConnectionStatistics(NetConnection connection, float windowSize)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");
			m_connection = connection;
			m_windowSize = windowSize;
			if (windowSize > 0.0f)
			{
				m_currentWindow = new NetConnectionStatistics(connection, 0.0f);
				m_previousWindow = new NetConnectionStatistics(connection, 0.0f);
			}
			Reset();
		}

		private NetConnectionStatistics GetCurrentWindow(double now)
		{
			if (m_currentWindow == null)
				return null;

			if (now - m_currentWindow.m_startTimestamp >= m_windowSize)
			{
				// close this window and open new (actually, flip and reset)
				NetConnectionStatistics tmp = m_previousWindow;
				m_previousWindow = m_currentWindow;
				m_previousWindow.m_totalTimeSpan = (float)(now - m_previousWindow.m_startTimestamp);
				m_currentWindow = tmp;
				m_currentWindow.Reset(now);
			}
			return m_currentWindow;
		}

		/// <summary>
		/// Current number of stored messages for this connection
		/// </summary>
		public int CurrentlyStoredMessagesCount
		{
			get
			{
				int retval = 0;
				for (int i = 0; i < m_connection.m_storedMessages.Length; i++)
					if (m_connection.m_storedMessages[i] != null)
						retval += m_connection.m_storedMessages[i].Count;
				return retval;
			}
		}

		/// <summary>
		/// Current number of unsent messages (possibly due to throttling) for this connection
		/// </summary>
		public int CurrentlyUnsentMessagesCount
		{
			get
			{
				return m_connection.m_unsentMessages.Count;
			}
		}

		/// <summary>
		/// Current number of all withheld messages for this connection
		/// </summary>
		public int CurrentlyWithheldMessagesCount
		{
			get
			{
				int retval = 0;
				for (int i = 0; i < m_connection.m_withheldMessages.Length; i++)
				{
					List<IncomingNetMessage> list = m_connection.m_withheldMessages[i];
					if (list != null)
						retval += list.Count;
				}
				return retval;
			}
		}

		public void Reset()
		{
			Reset(NetTime.Now);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		private void Reset(double now)
		{
			m_startTimestamp = now;
			m_messagesSent = 0;
			m_messagesReceived = 0;
			m_packetsSent = 0;
			m_packetsReceived = 0;
			m_bytesSent = 0;
			m_bytesReceived = 0;
			m_userMessagesSent = 0;
			m_userMessagesReceived = 0;
			m_userBytesSent = 0;
			m_userBytesReceived = 0;
			m_acksSent = 0;

			if (m_currentWindow != null)
				m_currentWindow.Reset();
			if (m_previousWindow != null)
				m_previousWindow.Reset();
		}

		/// <summary>
		/// Gets the number of messages sent
		/// </summary>
		public long GetMessagesSent(bool includeLibraryMessages)
		{
			return (includeLibraryMessages ? m_messagesSent : m_userMessagesSent);
		}

		/// <summary>
		/// Gets the number of messages received
		/// </summary>
		public long GetMessagesReceived(bool includeLibraryMessages)
		{
			return (includeLibraryMessages ? m_messagesReceived : m_userMessagesReceived);
		}

		/// <summary>
		/// Gets the number of bytes received
		/// </summary>
		public long GetBytesReceived(bool includeLibraryMessages)
		{
			return (includeLibraryMessages ? m_bytesReceived : m_userBytesReceived);
		}

		/// <summary>
		/// Gets the number of bytes sent
		/// </summary>
		public long GetBytesSent(bool includeLibraryMessages)
		{
			return (includeLibraryMessages ? m_bytesSent : m_userBytesSent);
		}

		/// <summary>
		/// Gets the number of messages sent
		/// </summary>
		public double GetMessagesSentPerSecond(double now, bool includeLibraryMessages)
		{
			double age = now - m_startTimestamp;
			return (double)(includeLibraryMessages ? m_messagesSent : m_userMessagesSent) / age;
		}

		/// <summary>
		/// Gets the number of messages received
		/// </summary>
		public double GetMessagesReceivedPerSecond(double now, bool includeLibraryMessages)
		{
			double age = now - m_startTimestamp;
			return (double)(includeLibraryMessages ? m_messagesReceived : m_userMessagesReceived) / age;
		}

		/// <summary>
		/// Gets the number of messages sent
		/// </summary>
		public double GetBytesSentPerSecond(double now, bool includeLibraryMessages)
		{
			double age = now - m_startTimestamp;
			return (double)(includeLibraryMessages ? m_bytesSent : m_userBytesSent) / age;
		}

		/// <summary>
		/// Gets the number of messages received
		/// </summary>
		public double GetBytesReceivedPerSecond(double now, bool includeLibraryMessages)
		{
			double age = now - m_startTimestamp;
			return (double)(includeLibraryMessages ? m_bytesReceived : m_userBytesReceived) / age;
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountPacketReceived(int numBytes, double now)
		{
			m_packetsReceived++;
			m_bytesReceived += numBytes;

			NetConnectionStatistics window = GetCurrentWindow(now);
			if (window != null)
				window.CountPacketReceived(numBytes, now);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountMessageReceived(NetMessageLibraryType tp, NetChannel channel, int numBytes, double now)
		{
			m_messagesReceived++;
			if (tp == NetMessageLibraryType.User)
			{
				m_userMessagesReceived++;
				m_userBytesReceived += (4 + numBytes);
				m_userTypeMessagesReceived[(int)channel]++;
			}

			NetConnectionStatistics window = GetCurrentWindow(now);
			if (window != null)
				window.CountMessageReceived(tp, channel, numBytes, now);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountPacketSent(int numBytes)
		{
			m_packetsSent++;
			m_bytesSent += numBytes;

			NetConnectionStatistics window = GetCurrentWindow(NetTime.Now);
			if (window != null)
				window.CountPacketSent(numBytes);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountMessageSent(NetMessage msg, int numBytes)
		{
			m_messagesSent++;
			if (msg.m_type == NetMessageLibraryType.User)
			{
				m_userMessagesSent++;
				m_userBytesSent += numBytes;
				m_userTypeMessagesSent[(int)msg.m_sequenceChannel]++;
			}

			NetConnectionStatistics window = GetCurrentWindow(NetTime.Now);
			if (window != null)
				window.CountMessageSent(msg, numBytes);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountDuplicateMessage(NetMessage msg)
		{
			m_duplicateMessagesRejected++;
			if (msg.m_type == NetMessageLibraryType.User)
				m_userDuplicateMessagesRejected++;
			NetConnectionStatistics window = GetCurrentWindow(NetTime.Now);
			if (window != null)
				window.CountDuplicateMessage(msg);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountDroppedSequencedMessage()
		{
			m_userSequencedMessagesRejected++;

			NetConnectionStatistics window = GetCurrentWindow(NetTime.Now);
			if (window != null)
				window.CountDroppedSequencedMessage();
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountMessageResent(NetMessageLibraryType tp)
		{
			m_messagesResent++;
			if (tp == NetMessageLibraryType.User)
				m_userMessagesResent++;

			NetConnectionStatistics window = GetCurrentWindow(NetTime.Now);
			if (window != null)
				window.CountMessageResent(tp);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountAcknowledgesSent(int numberOfAcks)
		{
			m_acksSent += numberOfAcks;
			NetConnectionStatistics window = GetCurrentWindow(NetTime.Now);
			if (window != null)
				window.CountAcknowledgesSent(numberOfAcks);
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void CountAcknowledgesReceived(int numberOfAcks)
		{
			m_acksReceived += numberOfAcks;
			NetConnectionStatistics window = GetCurrentWindow(NetTime.Now);
			if (window != null)
				window.CountAcknowledgesReceived(numberOfAcks);
		}
	}
}
