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
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;

namespace Lidgren.Network
{
	/// <summary>
	/// Base class for NetClient, NetServer and NetPeer
	/// </summary>
	public abstract partial class NetBase : IDisposable
	{
		private Socket m_socket;
		private EndPoint m_senderRemote;
		internal bool m_isBound;
		internal byte[] m_randomIdentifier;
		internal NetDiscovery m_discovery;

		private object m_bindLock;
		protected bool m_shutdownRequested;
		protected string m_shutdownReason;
		protected bool m_shutdownComplete;
		protected NetFrequencyCounter m_heartbeatCounter;

		// ready for reading by the application
		internal NetQueue<IncomingNetMessage> m_receivedMessages;
		private NetQueue<NetBuffer> m_unsentOutOfBandMessages;
		private NetQueue<IPEndPoint> m_unsentOutOfBandRecipients;
		private Queue<SUSystemMessage> m_susmQueue;
		internal List<IPEndPoint> m_holePunches;
		private double m_lastHolePunch;
		protected AutoResetEvent m_dataReceivedEvent;

		internal NetConfiguration m_config;
		internal NetBuffer m_receiveBuffer;
		internal NetBuffer m_sendBuffer;
		internal NetBuffer m_scratchBuffer;

		internal Thread m_heartbeatThread;
		private int m_runSleep = 1;

		internal NetMessageType m_enabledMessageTypes;

		/// <summary>
		/// Signalling event which can be waited on to determine when a message is queued for reading.
        /// Note that there is no guarantee that after the event is signaled the blocked thread will 
        /// find the message in the queue. Other user created threads could be preempted and dequeue 
        /// the message before the waiting thread wakes up.
		/// </summary>
		public AutoResetEvent DataReceivedEvent { get { return m_dataReceivedEvent; } }

		/// <summary>
		/// Gets or sets what types of messages are delievered to the client
		/// </summary>
		public NetMessageType EnabledMessageTypes { get { return m_enabledMessageTypes; } set { m_enabledMessageTypes = value; } }

		/// <summary>
		/// Gets or sets how many milliseconds to sleep between heartbeat ticks; a higher value means less cpu is used
		/// </summary>
		public int RunSleep { get { return m_runSleep; } set { m_runSleep = value; } }

		/// <summary>
		/// Average number of heartbeats performed per second; over a 3 seconds window
		/// </summary>
		public float HeartbeatAverageFrequency { get { return m_heartbeatCounter.AverageFrequency; } }
		
		/// <summary>
		/// Enables or disables a particular type of message
		/// </summary>
		public void SetMessageTypeEnabled(NetMessageType type, bool enabled)
		{
			if (enabled)
			{
#if DEBUG
#else
				if ((type | NetMessageType.DebugMessage) == NetMessageType.DebugMessage)
					throw new NetException("Not possible to enable Debug messages in a Release build!");
				if ((type | NetMessageType.VerboseDebugMessage) == NetMessageType.VerboseDebugMessage)
					throw new NetException("Not possible to enable VerboseDebug messages in a Release build!");
#endif
				m_enabledMessageTypes |= type;
			}
			else
			{
				m_enabledMessageTypes &= (~type);
			}
		}

		/// <summary>
		/// Gets the configuration for this NetBase instance
		/// </summary>
		public NetConfiguration Configuration { get { return m_config; } }

		/// <summary>
		/// Gets which port this netbase instance listens on, or -1 if it's not listening.
		/// </summary>
		public int ListenPort
		{
			get
			{
				if (m_isBound)
					return (m_socket.LocalEndPoint as IPEndPoint).Port;
				return -1;
			}
		}

		/// <summary>
		/// Is the instance listening on the socket?
		/// </summary>
		public bool IsListening { get { return m_isBound; } }

		protected NetBase(NetConfiguration config)
		{
			Debug.Assert(config != null, "Config must not be null");
			if (string.IsNullOrEmpty(config.ApplicationIdentifier))
				throw new ArgumentException("Must set ApplicationIdentifier in NetConfiguration!");
			m_config = config;
			m_receiveBuffer = new NetBuffer(config.ReceiveBufferSize);
			m_sendBuffer = new NetBuffer(config.SendBufferSize);
			m_senderRemote = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
			m_statistics = new NetBaseStatistics();
			m_receivedMessages = new NetQueue<IncomingNetMessage>(4);
			m_scratchBuffer = new NetBuffer(32);
			m_bindLock = new object();
			m_discovery = new NetDiscovery(this);
			m_heartbeatCounter = new NetFrequencyCounter(3.0f);

			m_randomIdentifier = new byte[8];
			NetRandom.Instance.NextBytes(m_randomIdentifier);

			m_unsentOutOfBandMessages = new NetQueue<NetBuffer>();
			m_unsentOutOfBandRecipients = new NetQueue<IPEndPoint>();
			m_susmQueue = new Queue<SUSystemMessage>();
			m_dataReceivedEvent = new AutoResetEvent(false);

			// default enabled message types
			m_enabledMessageTypes =
				NetMessageType.Data | NetMessageType.StatusChanged | NetMessageType.ServerDiscovered |
				NetMessageType.DebugMessage | NetMessageType.Receipt;
		}

		internal void EnqueueReceivedMessage(IncomingNetMessage msg)
		{
			lock (m_receivedMessages)
			{
				m_receivedMessages.Enqueue(msg);				
			}
            m_dataReceivedEvent.Set();
		}

		/// <summary>
		/// Creates an outgoing net message
		/// </summary>
		internal OutgoingNetMessage CreateOutgoingMessage()
		{
			// no recycling for messages
			OutgoingNetMessage msg = new OutgoingNetMessage();
			msg.m_sequenceNumber = -1;
			msg.m_numSent = 0;
			msg.m_nextResend = double.MaxValue;
			msg.m_msgType = NetMessageType.Data;
			msg.m_data = CreateBuffer();
			return msg;
		}

		/// <summary>
		/// Creates an incoming net message
		/// </summary>
		internal IncomingNetMessage CreateIncomingMessage()
		{
			// no recycling for messages
			IncomingNetMessage msg = new IncomingNetMessage();
			msg.m_msgType = NetMessageType.Data;
			msg.m_data = CreateBuffer();
			return msg;
		}

		/// <summary>
		/// Called to bind to socket and start heartbeat thread.
        /// The socket will be bound to listen on any network interface unless the <see cref="NetConfiguration.Address"/> explicitly specifies an interface. 
		/// </summary>
		public void Start()
		{
            if(m_config.Address!=null)
                Start(m_config.Address);
            else
			    Start(IPAddress.Any);
		}

		/// <summary>
		/// Called to bind to socket and start heartbeat thread
		/// </summary>
		public void Start(IPAddress localAddress)
		{
			if (m_isBound)
				return;
			lock (m_bindLock)
			{
				if (m_isBound)
					return;

				// Bind to config.Port
				try
				{
					IPEndPoint iep = new IPEndPoint(localAddress, m_config.Port);
					EndPoint ep = (EndPoint)iep;

					m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					m_socket.Blocking = false;
					m_socket.Bind(ep);

					LogWrite("Listening on " + m_socket.LocalEndPoint);
				}
				catch (SocketException sex)
				{
					if (sex.SocketErrorCode == SocketError.AddressAlreadyInUse)
						throw new NetException("Failed to bind to port " + m_config.Port + " - Address already in use!", sex);
					throw;
				}
				catch (Exception ex)
				{
					throw new NetException("Failed to bind to port " + m_config.Port, ex);
				}

				m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, m_config.ReceiveBufferSize);
				m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, m_config.SendBufferSize);

				// display simulated networking conditions in debug log
				if (m_simulatedLoss > 0.0f)
					LogWrite("Simulating " + (m_simulatedLoss * 100.0f) + "% loss");
				if (m_simulatedMinimumLatency > 0.0f || m_simulatedLatencyVariance > 0.0f)
					LogWrite("Simulating " + ((int)(m_simulatedMinimumLatency * 1000.0f)) + " - " + ((int)((m_simulatedMinimumLatency + m_simulatedLatencyVariance) * 1000.0f)) + " ms roundtrip latency");
				if (m_simulatedDuplicateChance > 0.0f)
					LogWrite("Simulating " + (m_simulatedDuplicateChance * 100.0f) + "% chance of packet duplication");

				if (m_config.m_throttleBytesPerSecond > 0)
					LogWrite("Throtting to " + m_config.m_throttleBytesPerSecond + " bytes per second");

				m_isBound = true;
				m_shutdownComplete = false;
				m_shutdownRequested = false;
				m_statistics.Reset();

				//
				// Start heartbeat thread
				//

				// remove old if any
				if (m_heartbeatThread != null)
				{
					if (m_heartbeatThread.IsAlive)
						return; // already got one
					m_heartbeatThread = null;
				}

				m_heartbeatThread = new Thread(new ThreadStart(Run));
				m_heartbeatThread.Name = "Lidgren network thread";
				m_heartbeatThread.IsBackground = true;
				m_heartbeatThread.Start();

				return;
			}
		}

		private void Run()
		{
			while (!m_shutdownComplete)
			{
				try
				{
					Heartbeat();
				}
				catch (Exception ex)
				{
					LogWrite("Heartbeat() failed on network thread: " + ex.Message);
				}

				// wait here to give cpu to other threads/processes
				Thread.Sleep(m_runSleep);
			}
		}

		/// <summary>
		/// Stop any udp hole punching in progress towards ep
		/// </summary>
		public void CeaseHolePunching(IPEndPoint ep)
		{
			if (ep == null)
				return;
			
			if (m_holePunches != null)
			{
				for (int i = 0; i < m_holePunches.Count; )
				{
					if (m_holePunches[i] != null && m_holePunches[i].Equals(ep))
					{
						LogVerbose("Ceasing hole punching to " + m_holePunches[i]);
						m_holePunches.RemoveAt(i);
					}
					else
						i++;
				}
				if (m_holePunches.Count < 1)
					m_holePunches = null;
			}
		}

		/// <summary>
		/// Reads all packets and create messages
		/// </summary>
		protected void BaseHeartbeat(double now)
		{
			if (!m_isBound)
				return;

			// discovery
			m_discovery.Heartbeat(now);

			// hole punching
			if (m_holePunches != null)
			{
				if (now > m_lastHolePunch + NetConstants.HolePunchingFrequency)
				{
					if (m_holePunches.Count <= 0)
					{
						m_holePunches = null;
					}
					else
					{
						IPEndPoint dest = m_holePunches[0];
						m_holePunches.RemoveAt(0);
						NotifyApplication(NetMessageType.DebugMessage, "Sending hole punch to " + dest, null);
						NetConnection.SendPing(this, dest, now);
						if (m_holePunches.Count < 1)
							m_holePunches = null;
						m_lastHolePunch = now;
					}
				}
			}

			// Send queued system messages
			if (m_susmQueue.Count > 0)
			{
				lock (m_susmQueue)
				{
					while (m_susmQueue.Count > 0)
					{
						SUSystemMessage su = m_susmQueue.Dequeue();
						SendSingleUnreliableSystemMessage(su.Type, su.Data, su.Destination, su.UseBroadcast);
					}
				}
			}

			// Send out-of-band messages
			if (m_unsentOutOfBandMessages.Count > 0)
			{
				lock (m_unsentOutOfBandMessages)
				{
					while (m_unsentOutOfBandMessages.Count > 0)
					{
						NetBuffer buf = m_unsentOutOfBandMessages.Dequeue();
						IPEndPoint ep = m_unsentOutOfBandRecipients.Dequeue();
						DoSendOutOfBandMessage(buf, ep);
					}
				}
			}

			try
			{
#if DEBUG
				SendDelayedPackets(now);
#endif

				while (true)
				{
					if (m_socket == null || m_socket.Available < 1)
						return;
					m_receiveBuffer.Reset();

					int bytesReceived = 0;
					try
					{
						bytesReceived = m_socket.ReceiveFrom(m_receiveBuffer.Data, 0, m_receiveBuffer.Data.Length, SocketFlags.None, ref m_senderRemote);
					}
					catch (SocketException)
					{
						// no good response to this yet
						return;
					}
					if (bytesReceived < 1)
						return;
					if (bytesReceived > 0)
						m_statistics.CountPacketReceived(bytesReceived);
					m_receiveBuffer.LengthBits = bytesReceived * 8;

					//LogVerbose("Read packet: " + bytesReceived + " bytes");

					IPEndPoint ipsender = (IPEndPoint)m_senderRemote;

					NetConnection sender = GetConnection(ipsender);
					if (sender != null)
						sender.m_statistics.CountPacketReceived(bytesReceived, now);

					// create messages from packet
					while (m_receiveBuffer.Position < m_receiveBuffer.LengthBits)
					{
						int beginPosition = m_receiveBuffer.Position;

						// read message header
						IncomingNetMessage msg = CreateIncomingMessage();
						msg.m_sender = sender;
						msg.ReadFrom(m_receiveBuffer, ipsender);

						// statistics
						if (sender != null)
							sender.m_statistics.CountMessageReceived(msg.m_type, msg.m_sequenceChannel, (m_receiveBuffer.Position - beginPosition) / 8, now);

						// handle message
						HandleReceivedMessage(msg, ipsender);
					}
				}
			}
			catch (SocketException sex)
			{
				if (sex.ErrorCode == 10054)
				{
					// forcibly closed; but m_senderRemote is unreliable, we can't trust it!
					//NetConnection conn = GetConnection((IPEndPoint)m_senderRemote);
					//HandleConnectionForciblyClosed(conn, sex);
					return;
				}
			}
			catch (Exception ex)
			{
				throw new NetException("ReadPacket() exception", ex);
			}
		}

		protected abstract void Heartbeat();

		public abstract NetConnection GetConnection(IPEndPoint remoteEndpoint);

		internal abstract void HandleReceivedMessage(IncomingNetMessage message, IPEndPoint senderEndpoint);

		internal abstract void HandleConnectionForciblyClosed(NetConnection connection, SocketException sex);

		/// <summary>
		/// Returns true if message should be dropped
		/// </summary>
		internal bool HandleNATIntroduction(IncomingNetMessage message)
		{
			if (message.m_type == NetMessageLibraryType.System)
			{
				if (message.m_data.LengthBytes > 4 && message.m_data.PeekByte() == (byte)NetSystemType.NatIntroduction)
				{
					if ((m_enabledMessageTypes & NetMessageType.NATIntroduction) != NetMessageType.NATIntroduction)
						return true; // drop
					try
					{
						message.m_data.ReadByte(); // step past system type byte
						IPEndPoint presented = message.m_data.ReadIPEndPoint();

						LogVerbose("Received NATIntroduction to " + presented + "; sending punching ping...");

						double now = NetTime.Now;
						NetConnection.SendPing(this, presented, now);

						if (m_holePunches == null)
							m_holePunches = new List<IPEndPoint>();

						for (int i = 0; i < 5; i++)
							m_holePunches.Add(new IPEndPoint(presented.Address, presented.Port));

						NetBuffer info = CreateBuffer();
						info.Write(presented);
						NotifyApplication(NetMessageType.NATIntroduction, info, message.m_sender, message.m_senderEndPoint);
						return true;
					}
					catch (Exception ex)
					{
						NotifyApplication(NetMessageType.BadMessageReceived, "Bad NAT introduction message received: " + ex.Message, message.m_sender, message.m_senderEndPoint);
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Notify application that a connection changed status
		/// </summary>
		internal void NotifyStatusChange(NetConnection connection, string reason)
		{
			if ((m_enabledMessageTypes & NetMessageType.StatusChanged) != NetMessageType.StatusChanged)
				return; // disabled
			
			//NotifyApplication(NetMessageType.StatusChanged, reason, connection);
			NetBuffer buffer = CreateBuffer(reason.Length + 2);
			buffer.Write(reason);
			buffer.Write((byte)connection.Status);

			IncomingNetMessage msg = new IncomingNetMessage();
			msg.m_data = buffer;
			msg.m_msgType = NetMessageType.StatusChanged;
			msg.m_sender = connection;
			msg.m_senderEndPoint = null;

			EnqueueReceivedMessage(msg);
		}
		
		internal OutgoingNetMessage CreateSystemMessage(NetSystemType systemType)
		{
			OutgoingNetMessage msg = CreateOutgoingMessage();
			msg.m_type = NetMessageLibraryType.System;
			msg.m_sequenceChannel = NetChannel.Unreliable;
			msg.m_sequenceNumber = 0;
			msg.m_data.Write((byte)systemType);
			return msg;
		}

		/// <summary>
		/// Send a single, out-of-band unreliable message
		/// </summary>
		public void SendOutOfBandMessage(NetBuffer data, IPEndPoint recipient)
		{
			lock (m_unsentOutOfBandMessages)
			{
				m_unsentOutOfBandMessages.Enqueue(data);
				m_unsentOutOfBandRecipients.Enqueue(recipient);
			}
		}

		/// <summary>
		/// Send a NAT introduction messages to one and two, allowing them to connect
		/// </summary>
		public void SendNATIntroduction(
			IPEndPoint one,
			IPEndPoint two
		)
		{
			NetBuffer toOne = CreateBuffer();
			toOne.Write(two);
			QueueSingleUnreliableSystemMessage(NetSystemType.NatIntroduction, toOne, one, false);

			NetBuffer toTwo = CreateBuffer();
			toTwo.Write(one);
			QueueSingleUnreliableSystemMessage(NetSystemType.NatIntroduction, toTwo, two, false);
		}

		/// <summary>
		/// Send a NAT introduction messages to ONE about contacting TWO
		/// </summary>
		public void SendSingleNATIntroduction(
			IPEndPoint one,
			IPEndPoint two
		)
		{
			NetBuffer toOne = CreateBuffer();
			toOne.Write(two);
			QueueSingleUnreliableSystemMessage(NetSystemType.NatIntroduction, toOne, one, false);
		}

		/// <summary>
		/// Send a single, out-of-band unreliable message
		/// </summary>
		internal void DoSendOutOfBandMessage(NetBuffer data, IPEndPoint recipient)
		{
			m_sendBuffer.Reset();

			// message type and channel
			m_sendBuffer.Write((byte)((int)NetMessageLibraryType.OutOfBand | ((int)NetChannel.Unreliable << 3)));
			m_sendBuffer.Write((ushort)0);

			// payload length; variable byte encoded
			if (data == null)
			{
				m_sendBuffer.WriteVariableUInt32((uint)0);
			}
			else
			{
				int dataLen = data.LengthBytes;
				m_sendBuffer.WriteVariableUInt32((uint)(dataLen));
				m_sendBuffer.Write(data.Data, 0, dataLen);
			}

			SendPacket(recipient);

			// unreliable; we can recycle this immediately
			RecycleBuffer(data);
		}

		/// <summary>
		/// Thread-safe SendSingleUnreliableSystemMessage()
		/// </summary>
		internal void QueueSingleUnreliableSystemMessage(
			NetSystemType tp,
			NetBuffer data,
			IPEndPoint remoteEP,
			bool useBroadcast)
		{
			SUSystemMessage susm = new SUSystemMessage();
			susm.Type = tp;
			susm.Data = data;
			susm.Destination = remoteEP;
			susm.UseBroadcast = useBroadcast;
			lock (m_susmQueue)
				m_susmQueue.Enqueue(susm);
		}

		/// <summary>
		/// Pushes a single system message onto the wire directly
		/// </summary>
		internal void SendSingleUnreliableSystemMessage(
			NetSystemType tp,
			NetBuffer data,
			IPEndPoint remoteEP,
			bool useBroadcast)
		{
			// packet number
			m_sendBuffer.Reset();

			// message type and channel
			m_sendBuffer.Write((byte)((int)NetMessageLibraryType.System | ((int)NetChannel.Unreliable << 3)));
			m_sendBuffer.Write((ushort)0);

			// payload length; variable byte encoded
			if (data == null)
			{
				m_sendBuffer.WriteVariableUInt32((uint)1);
				m_sendBuffer.Write((byte)tp);
			}
			else
			{
				int dataLen = data.LengthBytes;
				m_sendBuffer.WriteVariableUInt32((uint)(dataLen + 1));
				m_sendBuffer.Write((byte)tp);
				m_sendBuffer.Write(data.Data, 0, dataLen);
			}

			if (useBroadcast)
			{
				bool wasSSL = m_suppressSimulatedLag;
				try
				{
					m_suppressSimulatedLag = true;
					m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
					SendPacket(remoteEP);
				}
				finally
				{
					m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
					m_suppressSimulatedLag = wasSSL;
				}
			}
			else
			{
				SendPacket(remoteEP);
			}
		}

		/*
		internal void BroadcastUnconnectedMessage(NetBuffer data, int port)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			if (!m_isBound)
				Start();

			m_sendBuffer.Reset();

			// message type, netchannel and sequence number
			m_sendBuffer.Write((byte)((int)NetMessageLibraryType.System | ((int)NetChannel.Unreliable << 3)));
			m_sendBuffer.Write((ushort)0);

			// payload length
			int len = data.LengthBytes;
			m_sendBuffer.WriteVariableUInt32((uint)len);

			// copy payload
			m_sendBuffer.Write(data.Data, 0, len);

			IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, port);

			try
			{

				m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
				int bytesSent = m_socket.SendTo(m_sendBuffer.Data, 0, m_sendBuffer.LengthBytes, SocketFlags.None, broadcastEndpoint);
				if (bytesSent > 0)
					m_statistics.CountPacketSent(bytesSent);
				LogVerbose("Bytes broadcasted: " + bytesSent);
				return;
			}
			catch (SocketException sex)
			{
				if (sex.SocketErrorCode == SocketError.WouldBlock)
				{
#if DEBUG
					// send buffer overflow?
					LogWrite("SocketException.WouldBlock thrown during sending; send buffer overflow? Increase buffer using NetAppConfiguration.SendBufferSize");
					throw new NetException("SocketException.WouldBlock thrown during sending; send buffer overflow? Increase buffer using NetConfiguration.SendBufferSize", sex);
#else
					return;
#endif
				}

				if (sex.SocketErrorCode == SocketError.ConnectionReset ||
					sex.SocketErrorCode == SocketError.ConnectionRefused ||
					sex.SocketErrorCode == SocketError.ConnectionAborted)
				{
					LogWrite("Remote socket forcefully closed: " + sex.SocketErrorCode);
					// TODO: notify connection somehow
					return;
				}

				throw;
			}
			finally
			{
				m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, false);
			}
		}
		*/

		/// <summary>
		/// Pushes a single packet onto the wire from m_sendBuffer
		/// </summary>
		internal void SendPacket(IPEndPoint remoteEP)
		{
			SendPacket(m_sendBuffer.Data, m_sendBuffer.LengthBytes, remoteEP);
		}

		/// <summary>
		/// Pushes a single packet onto the wire
		/// </summary>
		internal void SendPacket(byte[] data, int length, IPEndPoint remoteEP)
		{
			if (length <= 0 || length > m_config.SendBufferSize)
			{
				string str = "Invalid packet size; Must be between 1 and NetConfiguration.SendBufferSize - Invalid value: " + length;
				LogWrite(str);
				throw new NetException(str);
			}

			if (!m_isBound)
				Start();

#if DEBUG
			if (!m_suppressSimulatedLag)
			{
				bool send = SimulatedSendPacket(data, length, remoteEP);
				if (!send)
				{
					m_statistics.CountPacketSent(length);
					return;
				}
			}
#endif

			try
			{
				//m_socket.SendTo(data, 0, length, SocketFlags.None, remoteEP);
				int bytesSent = m_socket.SendTo(data, 0, length, SocketFlags.None, remoteEP);
				//LogVerbose("Sent " + bytesSent + " bytes");
#if DEBUG || USE_RELEASE_STATISTICS
				if (!m_suppressSimulatedLag)
					m_statistics.CountPacketSent(bytesSent);
#endif
				return;
			}
			catch (SocketException sex)
			{
				if (sex.SocketErrorCode == SocketError.WouldBlock)
				{
#if DEBUG
					// send buffer overflow?
					LogWrite("SocketException.WouldBlock thrown during sending; send buffer overflow? Increase buffer using NetAppConfiguration.SendBufferSize");
					throw new NetException("SocketException.WouldBlock thrown during sending; send buffer overflow? Increase buffer using NetConfiguration.SendBufferSize", sex);
#else
					// gulp
					return;
#endif
				}

				if (sex.SocketErrorCode == SocketError.ConnectionReset ||
					sex.SocketErrorCode == SocketError.ConnectionRefused ||
					sex.SocketErrorCode == SocketError.ConnectionAborted)
				{
					LogWrite("Remote socket forcefully closed: " + sex.SocketErrorCode);
					// TODO: notify connection somehow?
					return;
				}

				throw;
			}
		}

		/// <summary>
		/// Emit receipt event
		/// </summary>
		internal void FireReceipt(NetConnection connection, NetBuffer receiptData)
		{
			if ((m_enabledMessageTypes & NetMessageType.Receipt) != NetMessageType.Receipt)
				return; // disabled

			IncomingNetMessage msg = CreateIncomingMessage();
			msg.m_sender = connection;
			msg.m_msgType = NetMessageType.Receipt;
			msg.m_data = receiptData;

			EnqueueReceivedMessage(msg);
		}

		[Conditional("DEBUG")]
		internal void LogWrite(string message)
		{
			if ((m_enabledMessageTypes & NetMessageType.DebugMessage) != NetMessageType.DebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.DebugMessage, message, null); //sender);
		}

		[Conditional("DEBUG")]
		internal void LogVerbose(string message)
		{
			if ((m_enabledMessageTypes & NetMessageType.VerboseDebugMessage) != NetMessageType.VerboseDebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.VerboseDebugMessage, message, null); //sender);
		}

		[Conditional("DEBUG")]
		internal void LogWrite(string message, NetConnection connection)
		{
			if ((m_enabledMessageTypes & NetMessageType.DebugMessage) != NetMessageType.DebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.DebugMessage, message, connection);
		}

		[Conditional("DEBUG")]
		internal void LogVerbose(string message, NetConnection connection)
		{
			if ((m_enabledMessageTypes & NetMessageType.VerboseDebugMessage) != NetMessageType.VerboseDebugMessage)
				return; // disabled

			NotifyApplication(NetMessageType.VerboseDebugMessage, message, connection);
		}

		internal void NotifyApplication(NetMessageType tp, string message, NetConnection conn)
		{
			NetBuffer buf = CreateBuffer(message);
			NotifyApplication(tp, buf, conn);
		}
		
		internal void NotifyApplication(NetMessageType tp, string message, NetConnection conn, IPEndPoint ep)
		{
			NetBuffer buf = CreateBuffer(message);
			NotifyApplication(tp, buf, conn, ep);
		}

		internal void NotifyApplication(NetMessageType tp, NetBuffer buffer, NetConnection conn)
		{
			NotifyApplication(tp, buffer, conn, null);
		}

		internal void NotifyApplication(NetMessageType tp, NetBuffer buffer, NetConnection conn, IPEndPoint ep)
		{
			IncomingNetMessage msg = new IncomingNetMessage();
			msg.m_data = buffer;
			msg.m_msgType = tp;
			msg.m_sender = conn;
			msg.m_senderEndPoint = ep;

			EnqueueReceivedMessage(msg);
		}

		/// <summary>
		/// Override this to process a received NetBuffer on the networking thread (note! This can be problematic, only use this if you know what you are doing)
		/// </summary>
		public virtual void ProcessReceived(NetBuffer buffer)
		{
		}

		/// <summary>
		/// Initiates a shutdown
		/// </summary>
		public void Shutdown(string reason)
		{
			LogWrite("Shutdown initiated (" + reason + ")");
			m_shutdownRequested = true;
			m_shutdownReason = reason;
			Thread.Sleep(50); // give network thread some time to send disconnect messages
		}

		protected virtual void PerformShutdown(string reason)
		{
			LogWrite("Performing shutdown (" + reason + ")");
#if DEBUG
			// just send all delayed packets; since we won't have the possibility to do it after socket is closed
			SendDelayedPackets(NetTime.Now + this.SimulatedMinimumLatency + this.SimulatedLatencyVariance + 1000.0);
#endif
			lock (m_bindLock)
			{
				try
				{
					if (m_socket != null)
					{
						m_socket.Shutdown(SocketShutdown.Receive);
						m_socket.Close(2);
					}
				}
				finally
				{
					m_socket = null;
					m_isBound = false;
				}
				m_shutdownComplete = true;

				LogWrite("Socket closed");
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~NetBase()
		{
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Unless we're already shut down, this is the equivalent of killing the process
			m_shutdownComplete = true;
			m_isBound = false;
			if (disposing)
			{
				if (m_socket != null)
				{
					m_socket.Close();
					m_socket = null;
				}
			}
		}
	}

	internal sealed class SUSystemMessage
	{
		public NetSystemType Type;
		public NetBuffer Data;
		public IPEndPoint Destination;
		public bool UseBroadcast;
	}
}
