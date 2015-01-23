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
using System.Text;
using System.Threading;

namespace Lidgren.Network
{
	/// <summary>
	/// A server which can accept connections from multiple NetClients
	/// </summary>
	public class NetServer : NetBase
	{
		protected List<NetConnection> m_connections;
		protected Dictionary<IPEndPoint, NetConnection> m_connectionLookup;
		protected bool m_allowOutgoingConnections; // used by NetPeer
		
		/// <summary>
		/// Gets a copy of the list of connections
		/// </summary>
		public List<NetConnection> Connections
		{
			get
			{
				lock (m_connections)
				{
					return new List<NetConnection>(m_connections);
				}
			}
		}

		/// <summary>
		/// Creates a new NetServer
		/// </summary>
		public NetServer(NetConfiguration config)
			: base(config)
		{
			m_connections = new List<NetConnection>();
			m_connectionLookup = new Dictionary<IPEndPoint, NetConnection>();
		}
		
		/// <summary>
		/// Reads and sends messages from the network
		/// </summary>
		protected override void Heartbeat()
		{
			double now = NetTime.Now;
			m_heartbeatCounter.Count(now);

			if (m_shutdownRequested)
			{
				PerformShutdown(m_shutdownReason);
				return;
			}
						
			// read messages from network
			BaseHeartbeat(now);

			lock (m_connections)
			{
				List<NetConnection> deadConnections = null;
				foreach (NetConnection conn in m_connections)
				{
					if (conn.m_status == NetConnectionStatus.Disconnected)
					{
						if (deadConnections == null)
							deadConnections = new List<NetConnection>();
						deadConnections.Add(conn);
						continue;
					}

					conn.Heartbeat(now);
				}

				if (deadConnections != null)
				{
					foreach (NetConnection conn in deadConnections)
					{
						m_connections.Remove(conn);
						m_connectionLookup.Remove(conn.RemoteEndpoint);
					}
				}
			}
		}

		public override NetConnection GetConnection(IPEndPoint remoteEndpoint)
		{
			NetConnection retval;
			if (m_connectionLookup.TryGetValue(remoteEndpoint, out retval))
				return retval;
			return null;
		}

		internal override void HandleReceivedMessage(IncomingNetMessage message, IPEndPoint senderEndpoint)
		{
			double now = NetTime.Now;

			int payLen = message.m_data.LengthBytes;

			// NAT introduction?
			if (HandleNATIntroduction(message))
				return;

			// Out of band?
			if (message.m_type == NetMessageLibraryType.OutOfBand)
			{
				if ((m_enabledMessageTypes & NetMessageType.OutOfBandData) != NetMessageType.OutOfBandData)
					return; // drop

				// just deliever
				message.m_msgType = NetMessageType.OutOfBandData;
				message.m_senderEndPoint = senderEndpoint;

				EnqueueReceivedMessage(message);
				return;
			}

			if (message.m_sender == null)
			{
				//
				// Handle unconnected message
				//

				// not a connected sender; only allow System messages
				if (message.m_type != NetMessageLibraryType.System)
				{
					if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						NotifyApplication(NetMessageType.BadMessageReceived, "Rejecting non-system message from unconnected source: " + message, null, message.m_senderEndPoint);
					return;
				}

				// read type of system message
				NetSystemType sysType = (NetSystemType)message.m_data.ReadByte();
				switch (sysType)
				{
					case NetSystemType.Connect:

						LogVerbose("Connection request received from " + senderEndpoint);

						// check app ident
						if (payLen < 4)
						{
							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Malformed Connect message received from " + senderEndpoint, null, senderEndpoint);
							return;
						}
						string appIdent = message.m_data.ReadString();
						if (appIdent != m_config.ApplicationIdentifier)
						{
							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Connect for different application identification received: " + appIdent, null, senderEndpoint);
							return;
						}

						// read random identifer
						byte[] rnd = message.m_data.ReadBytes(8);
						if (NetUtility.CompareElements(rnd, m_randomIdentifier))
						{
							// don't allow self-connect
							if ((m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								NotifyApplication(NetMessageType.ConnectionRejected, "Connection to self not allowed", null, senderEndpoint);
							return;
						}

						int bytesReadSoFar = (message.m_data.Position / 8);
						int hailLen = message.m_data.LengthBytes - bytesReadSoFar;
						byte[] hailData = null;
						if (hailLen > 0)
						{
							hailData = new byte[hailLen];
							Buffer.BlockCopy(message.m_data.Data, bytesReadSoFar, hailData, 0, hailLen);
						}

						if (m_connections.Count >= m_config.m_maxConnections)
						{
							if ((m_enabledMessageTypes & NetMessageType.ConnectionRejected) == NetMessageType.ConnectionRejected)
								NotifyApplication(NetMessageType.ConnectionRejected, "Server full; rejecting connect from " + senderEndpoint, null, senderEndpoint);
							return;
						}

						// Create connection
						LogWrite("New connection: " + senderEndpoint);
						NetConnection conn = new NetConnection(this, senderEndpoint, null, hailData);

						// Connection approval?
						if ((m_enabledMessageTypes & NetMessageType.ConnectionApproval) == NetMessageType.ConnectionApproval)
						{
							// Ask application if this connection is allowed to proceed
							IncomingNetMessage app = CreateIncomingMessage();
							app.m_msgType = NetMessageType.ConnectionApproval;
							if (hailData != null)
								app.m_data.Write(hailData);
							app.m_sender = conn;
							conn.m_approved = false;
							EnqueueReceivedMessage(app);
							// Don't add connection; it's done as part of the approval procedure
							return;
						}

						// it's ok
						AddConnection(now, conn);
						break;
					case NetSystemType.ConnectionEstablished:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Connection established received from non-connection! " + senderEndpoint, null, senderEndpoint);
						return;
					case NetSystemType.Discovery:
						if (m_config.AnswerDiscoveryRequests)
							m_discovery.HandleRequest(message, senderEndpoint);
						break;
					case NetSystemType.DiscoveryResponse:
						if (m_allowOutgoingConnections)
						{
							// NetPeer
							IncomingNetMessage resMsg = m_discovery.HandleResponse(message, senderEndpoint);
							if (resMsg != null)
							{
								resMsg.m_senderEndPoint = senderEndpoint;
								EnqueueReceivedMessage(resMsg);
							}
						}
						break;
					default:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Undefined behaviour for " + this + " receiving system type " + sysType + ": " + message + " from unconnected source", null, senderEndpoint);
						break;
				}
				// done
				return;
			}

			// ok, we have a sender
			if (message.m_type == NetMessageLibraryType.Acknowledge)
			{
				message.m_sender.HandleAckMessage(message);
				return;
			}

			if (message.m_type == NetMessageLibraryType.System)
			{
				//
				// Handle system messages from connected source
				//

				if (payLen < 1)
				{
					if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						NotifyApplication(NetMessageType.BadMessageReceived, "Received malformed system message; payload length less than 1 byte", null, senderEndpoint);
					return;
				}
				NetSystemType sysType = (NetSystemType)message.m_data.ReadByte();
				switch (sysType)
				{
					case NetSystemType.Connect:
					case NetSystemType.ConnectionEstablished:
					case NetSystemType.Ping:
					case NetSystemType.Pong:
					case NetSystemType.Disconnect:
					case NetSystemType.ConnectionRejected:
					case NetSystemType.StringTableAck:
						message.m_sender.HandleSystemMessage(message, now);
						break;
					case NetSystemType.ConnectResponse:
						if (m_allowOutgoingConnections)
						{
							message.m_sender.HandleSystemMessage(message, now);
						}
						else
						{
							if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
								NotifyApplication(NetMessageType.BadMessageReceived, "Undefined behaviour for server and system type " + sysType, null, senderEndpoint);
						}
						break;
					case NetSystemType.Discovery:
						// Allow discovery even if connected
						if (m_config.AnswerDiscoveryRequests)
							m_discovery.HandleRequest(message, senderEndpoint);
						break;
					default:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Undefined behaviour for server and system type " + sysType, null, senderEndpoint);
						break;
				}
				return;
			}

			message.m_sender.HandleUserMessage(message);
		}

		internal void AddConnection(double now, NetConnection conn)
		{
			conn.SetStatus(NetConnectionStatus.Connecting, "Connecting");

			LogWrite("Adding connection " + conn);

			// send response; even if connected
			OutgoingNetMessage response = CreateSystemMessage(NetSystemType.ConnectResponse);
			if (conn.LocalHailData != null)
				response.m_data.Write(conn.LocalHailData);
			conn.m_unsentMessages.Enqueue(response);

			conn.m_handshakeInitiated = now;
			
			conn.m_approved = true;
			lock (m_connections)
				m_connections.Add(conn);
			m_connectionLookup.Add(conn.m_remoteEndPoint, conn);
		}

		/*
		/// <summary>
		/// Read any received message in any connection queue
		/// </summary>
		public NetBuffer ReadMessage(out NetConnection sender)
		{
			if (m_receivedMessages.Count < 1)
			{
				sender = null;
				return null;
			}

			NetMessage msg = m_receivedMessages.Dequeue();
			sender = msg.m_sender;

			NetBuffer retval = msg.m_data;
			msg.m_data = null;
			m_messagePool.Push(msg);

			Debug.Assert(retval.Position == 0);

			return retval;
		}
		*/

		/// <summary>
		/// Read any received message in any connection queue
		/// </summary>
		public bool ReadMessage(
			NetBuffer intoBuffer,
			IList<NetConnection> onlyFor,
			bool includeNullConnectionMessages,
			out NetMessageType type,
			out NetConnection sender)
		{
			if (m_receivedMessages.Count < 1)
			{
				sender = null;
				type = NetMessageType.None;
				m_dataReceivedEvent.Reset();
				return false;
			}

			IncomingNetMessage msg = null;
			lock (m_receivedMessages)
			{
				int sz = m_receivedMessages.Count;
				for (int i = 0; i < sz; i++)
				{
					msg = m_receivedMessages.Peek(i);
					if (msg != null)
					{
						if ((msg.m_sender == null && includeNullConnectionMessages) ||
							onlyFor.Contains(msg.m_sender))
						{
							m_receivedMessages.Dequeue(i);
							break;
						}
						msg = null;
					}
				}
			}

			if (msg == null)
			{
				sender = null;
				type = NetMessageType.None;
				return false;
			}

			sender = msg.m_sender;

			// recycle NetMessage object
			NetBuffer content = msg.m_data;
			msg.m_data = null;
			type = msg.m_msgType;

			// m_messagePool.Push(msg);

			Debug.Assert(content.Position == 0);

			// swap content of buffers
			byte[] tmp = intoBuffer.Data;
			intoBuffer.Data = content.Data;
			content.Data = tmp;

			// set correct values for returning value (ignore the other, it's being recycled anyway)
			intoBuffer.m_bitLength = content.m_bitLength;
			intoBuffer.m_readPosition = 0;

			// recycle NetBuffer object (incl. old intoBuffer byte array)
			RecycleBuffer(content);

			return true;
		}

		/// <summary>
		/// Read any received message in any connection queue
		/// </summary>
		public bool ReadMessage(NetBuffer intoBuffer, out NetMessageType type, out NetConnection sender)
		{
			IPEndPoint senderEndPoint;
			return ReadMessage(intoBuffer, out type, out sender, out senderEndPoint);
		}
				
		/// <summary>
		/// Read any received message in any connection queue
		/// </summary>
		public bool ReadMessage(
			NetBuffer intoBuffer,
			out NetMessageType type,
			out NetConnection sender,
			out IPEndPoint senderEndPoint)
		{
			if (intoBuffer == null)
				throw new ArgumentNullException("intoBuffer");

			if (m_receivedMessages.Count < 1)
			{
				sender = null;
				senderEndPoint = null;
				type = NetMessageType.None;
				m_dataReceivedEvent.Reset();
				return false;
			}

			IncomingNetMessage msg;
			lock(m_receivedMessages)
				msg = m_receivedMessages.Dequeue();

			if (msg == null)
			{
				sender = null;
				type = NetMessageType.None;
				senderEndPoint = null;
				return false;
			}

#if DEBUG
			if (msg.m_data == null)
				throw new NetException("Ouch, no data!");
			if (msg.m_data.Position != 0)
				throw new NetException("Ouch, stale data!");
#endif

			senderEndPoint = msg.m_senderEndPoint;
			sender = msg.m_sender;

			intoBuffer.Tag = msg.m_data.Tag;

			//
			// recycle NetMessage object and NetBuffer
			//
			
			NetBuffer content = msg.m_data;

			msg.m_data = null;
			type = msg.m_msgType;

			// swap content of buffers
			byte[] tmp = intoBuffer.Data;
			intoBuffer.Data = content.Data;
			if (tmp == null)
				tmp = new byte[8]; // application must have lost it somehow
			content.Data = tmp;

			// set correct values for returning value (ignore the other, it's being recycled anyway)
			intoBuffer.m_bitLength = content.m_bitLength;
			intoBuffer.m_readPosition = 0;

			// recycle message
			// m_messagePool.Push(msg);

			// recycle buffer
			RecycleBuffer(content);

			return true;
		}

		/// <summary>
		/// Sends a message to a specific connection
		/// </summary>
		public void SendMessage(NetBuffer data, NetConnection recipient, NetChannel channel)
		{
			if (recipient == null)
				throw new ArgumentNullException("recipient");
			recipient.SendMessage(data, channel);
		}

		/// <summary>
		/// Sends a message to the specified connections; takes ownership of the NetBuffer, don't reuse it after this call
		/// </summary>
		public void SendMessage(NetBuffer data, IEnumerable<NetConnection> recipients, NetChannel channel)
		{
			if (recipients == null)
				throw new ArgumentNullException("recipients");

			foreach (NetConnection recipient in recipients)
				recipient.SendMessage(data, channel);
		}

		/// <summary>
		/// Sends a message to all connections to this server
		/// </summary>
		public void SendToAll(NetBuffer data, NetChannel channel)
		{
			lock (m_connections)
			{
				foreach (NetConnection conn in m_connections)
				{
					if (conn.Status == NetConnectionStatus.Connected)
						conn.SendMessage(data, channel);
				}
			}
		}

		/// <summary>
		/// Sends a message to all connections to this server, except 'exclude'
		/// </summary>
		public void SendToAll(NetBuffer data, NetChannel channel, NetConnection exclude)
		{
			lock (m_connections)
			{
				foreach (NetConnection conn in m_connections)
				{
					if (conn.Status == NetConnectionStatus.Connected && conn != exclude)
						conn.SendMessage(data, channel);
				}
			}
		}

		internal override void HandleConnectionForciblyClosed(NetConnection connection, SocketException sex)
		{
			if (connection != null)
				connection.Disconnect("Connection forcibly closed", 0, false, false);
			return;
		}

		protected override void PerformShutdown(string reason)
		{
			double now = NetTime.Now;
			foreach (NetConnection conn in m_connections)
			{
				if (conn.m_status != NetConnectionStatus.Disconnected)
				{
					conn.Disconnect(reason, 0, true, true);
					conn.SendUnsentMessages(now); // give disconnect message a chance to get out
				}
			}
			base.PerformShutdown(reason);
		}
	}
}
