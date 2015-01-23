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

namespace Lidgren.Network
{
	/// <summary>
	/// A client which can connect to a single NetServer
	/// </summary>
	public class NetClient : NetBase
	{
		private NetConnection m_serverConnection;

		private bool m_connectRequested;
		private byte[] m_localHailData; // temporary container until NetConnection has been created
		private IPEndPoint m_connectEndpoint;
		private object m_startLock;

		/// <summary>
		/// Gets the connection to the server
		/// </summary>
		public NetConnection ServerConnection { get { return m_serverConnection; } }

		/// <summary>
		/// Gets the status of the connection to the server
		/// </summary>
		public NetConnectionStatus Status
		{
			get
			{
				if (m_serverConnection == null)
					return NetConnectionStatus.Disconnected;
				return m_serverConnection.Status;
			}
		}

		/// <summary>
		/// Creates a new NetClient
		/// </summary>
		public NetClient(NetConfiguration config)
			: base(config)
		{
			m_startLock = new object();
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing no hail data
		/// </summary>
		public void Connect(string host, int port)
		{
			Connect(host, port, null);
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing hailData to the server
		/// </summary>
		public void Connect(string host, int port, byte[] hailData)
		{
			IPAddress ip = NetUtility.Resolve(host);
			if (ip == null)
				throw new NetException("Unable to resolve host");
			Connect(new IPEndPoint(ip, port), hailData);
		}

		/// <summary>
		/// Connects to the specified remove endpoint
		/// </summary>
		public void Connect(IPEndPoint remoteEndpoint)
		{
			Connect(remoteEndpoint, null);
		}

		/// <summary>
		/// Connects to the specified remote endpoint; passing hailData to the server
		/// </summary>
		public void Connect(IPEndPoint remoteEndpoint, byte[] hailData)
		{
			m_connectRequested = true;
			m_connectEndpoint = remoteEndpoint;
			m_localHailData = hailData;

			Start(); // start heartbeat thread etc
		}

		internal void PerformConnect()
		{
			// ensure we're bound to socket
			Start();

			m_connectRequested = false;

			if (m_serverConnection != null)
			{
				m_serverConnection.Disconnect("New connect", 0, m_serverConnection.Status == NetConnectionStatus.Connected, true);
				if (m_serverConnection.RemoteEndpoint.Equals(m_connectEndpoint))
					m_serverConnection = new NetConnection(this, m_connectEndpoint, m_localHailData, null);
			}
			else
			{
				m_serverConnection = new NetConnection(this, m_connectEndpoint, m_localHailData, null);
			}

			// connect
			m_serverConnection.Connect();

			m_connectEndpoint = null;
			m_localHailData = null;
		}

		/// <summary>
		/// Initiate explicit disconnect
		/// </summary>
		public void Disconnect(string message)
		{
			if (m_serverConnection == null || m_serverConnection.Status == NetConnectionStatus.Disconnected)
			{
				LogWrite("Disconnect - Not connected!");
				return;
			}
			m_serverConnection.Disconnect(message, 1.0f, true, false);
		}

		/// <summary>
		/// Sends unsent messages and reads new messages from the wire
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

			if (m_connectRequested)
			{
				PerformConnect();
			}

			// read messages from network
			BaseHeartbeat(now);

			if (m_serverConnection != null)
				m_serverConnection.Heartbeat(now); // will send unsend messages etc.
		}

		/// <summary>
		/// Returns ServerConnection if passed the correct endpoint
		/// </summary>
		public override NetConnection GetConnection(IPEndPoint remoteEndpoint)
		{
			if (m_serverConnection != null && m_serverConnection.RemoteEndpoint.Equals(remoteEndpoint))
				return m_serverConnection;
			return null;
		}

		internal override void HandleReceivedMessage(IncomingNetMessage message, IPEndPoint senderEndpoint)
		{
			//LogWrite("NetClient received message " + message);
			double now = NetTime.Now;

			int payLen = message.m_data.LengthBytes;

			// Discovery response?
			if (message.m_type == NetMessageLibraryType.System && payLen > 0)
			{
				NetSystemType sysType = (NetSystemType)message.m_data.PeekByte();

				// NAT introduction?
				if (HandleNATIntroduction(message))
					return;

				

				if (sysType == NetSystemType.DiscoveryResponse)
				{
					message.m_data.ReadByte(); // step past system type byte
					IncomingNetMessage resMsg = m_discovery.HandleResponse(message, senderEndpoint);
					if (resMsg != null)
					{
						resMsg.m_senderEndPoint = senderEndpoint;
						EnqueueReceivedMessage(resMsg);
					}
					return;
				}
			}

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

			if (message.m_sender != m_serverConnection && m_serverConnection != null)
				return; // don't talk to strange senders after this

			if (message.m_type == NetMessageLibraryType.Acknowledge)
			{
				m_serverConnection.HandleAckMessage(message);
				return;
			}

			// Handle system types
			if (message.m_type == NetMessageLibraryType.System)
			{
				if (payLen < 1)
				{
					if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						NotifyApplication(NetMessageType.BadMessageReceived, "Received malformed system message: " + message, m_serverConnection, senderEndpoint);
					return;
				}
				NetSystemType sysType = (NetSystemType)message.m_data.Data[0];
				switch (sysType)
				{
					case NetSystemType.ConnectResponse:
					case NetSystemType.Ping:
					case NetSystemType.Pong:
					case NetSystemType.Disconnect:
					case NetSystemType.ConnectionRejected:
					case NetSystemType.StringTableAck:
						if (m_serverConnection != null)
							m_serverConnection.HandleSystemMessage(message, now);
						return;
					case NetSystemType.Connect:
					case NetSystemType.ConnectionEstablished:
					case NetSystemType.Discovery:
					case NetSystemType.Error:
					default:
						if ((m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
							NotifyApplication(NetMessageType.BadMessageReceived, "Undefined behaviour for client and " + sysType, m_serverConnection, senderEndpoint);
						return;
				}
			}

			Debug.Assert(
				message.m_type == NetMessageLibraryType.User ||
				message.m_type == NetMessageLibraryType.UserFragmented
			);

			if (m_serverConnection.Status == NetConnectionStatus.Connecting)
			{
				// lost connectresponse packet?
				// Emulate it; 
				LogVerbose("Received user message before ConnectResponse; emulating ConnectResponse...", m_serverConnection);
				IncomingNetMessage emuMsg = CreateIncomingMessage();
				emuMsg.m_type = NetMessageLibraryType.System;
				emuMsg.m_data.Reset();
				emuMsg.m_data.Write((byte)NetSystemType.ConnectResponse);
				m_serverConnection.HandleSystemMessage(emuMsg, now);

				// ... and proceed to pick up user message
			}

			// add to pick-up queue
			m_serverConnection.HandleUserMessage(message);
		}

		/// <summary>
		/// Reads the content of an available message into 'intoBuffer' and returns true. If no message is available it returns false.
		/// </summary>
		/// <param name="intoBuffer">A NetBuffer whose content will be overwritten with the read message</param>
		/// <returns>true if a message was read</returns>
		public bool ReadMessage(NetBuffer intoBuffer, out NetMessageType type)
		{
			IPEndPoint senderEndPoint; // unused
			return ReadMessage(intoBuffer, out type, out senderEndPoint);
		}

		/// <summary>
		/// Reads the content of an available message into 'intoBuffer' and returns true. If no message is available it returns false.
		/// </summary>
		/// <param name="intoBuffer">A NetBuffer whose content will be overwritten with the read message</param>
		/// <returns>true if a message was read</returns>
		public bool ReadMessage(NetBuffer intoBuffer, out NetMessageType type, out IPEndPoint senderEndpoint)
		{
			if (m_receivedMessages.Count < 1)
			{
				type = NetMessageType.None;
				senderEndpoint = null;
				m_dataReceivedEvent.Reset();
				return false;
			}

			IncomingNetMessage msg;
			lock (m_receivedMessages)
				msg = m_receivedMessages.Dequeue();

			if (msg == null)
			{
				type = NetMessageType.None;
				senderEndpoint = null;
				return false;
			}

			senderEndpoint = msg.m_senderEndPoint;

			intoBuffer.Tag = msg.m_data.Tag;

			// recycle NetMessage object
			NetBuffer content = msg.m_data;
			msg.m_data = null;
			type = msg.m_msgType;


			// swap content of buffers
			byte[] tmp = intoBuffer.Data;
			intoBuffer.Data = content.Data;
			content.Data = tmp;

			// set correct values for returning value (ignore the other, it's being recycled anyway)
			intoBuffer.m_bitLength = content.m_bitLength;
			intoBuffer.m_readPosition = 0;

			// recycle
			RecycleBuffer(content);

			return true;
		}

		/// <summary>
		/// Sends a message using the specified channel; takes ownership of the NetBuffer, don't reuse it after this call
		/// </summary>
		public void SendMessage(NetBuffer data, NetChannel channel)
		{
			if (m_serverConnection == null || m_serverConnection.Status != NetConnectionStatus.Connected)
				throw new NetException("You must be connected first!");
			m_serverConnection.SendMessage(data, channel);
		}

		/// <summary>
		/// Sends a message using the specified channel, with the specified data as receipt; takes ownership of the NetBuffer, don't reuse it after this call
		/// </summary>
		public void SendMessage(NetBuffer data, NetChannel channel, NetBuffer receipt)
		{
			if (m_serverConnection == null || m_serverConnection.Status != NetConnectionStatus.Connected)
				throw new NetException("You must be connected first!");
			if ((m_enabledMessageTypes & NetMessageType.Receipt) != NetMessageType.Receipt)
				LogVerbose("Warning; Receipt messagetype is not enabled!");
			m_serverConnection.SendMessage(data, channel, receipt);
		}

		/// <summary>
		/// Emit a discovery signal to your subnet
		/// </summary>
		public void DiscoverLocalServers(int serverPort)
		{
			m_discovery.SendDiscoveryRequest(new IPEndPoint(IPAddress.Broadcast, serverPort), true);
		}

		/// <summary>
		/// Emit a discovery signal to your subnet; polling every 'interval' second until 'timeout' seconds is reached
		/// </summary>
		public void DiscoverLocalServers(int serverPort, float interval, float timeout)
		{
			m_discovery.SendDiscoveryRequest(new IPEndPoint(IPAddress.Broadcast, serverPort), true, interval, timeout);
		}
		
		/// <summary>
		/// Emit a discovery signal to a single host
		/// </summary>
		public void DiscoverKnownServer(string host, int serverPort)
		{
			IPAddress address = NetUtility.Resolve(host);
			IPEndPoint ep = new IPEndPoint(address, serverPort);

			m_discovery.SendDiscoveryRequest(ep, false);
		}

		/// <summary>
		/// Emit a discovery signal to a host or subnet
		/// </summary>
		public void DiscoverKnownServer(IPEndPoint address, bool useBroadcast)
		{
			m_discovery.SendDiscoveryRequest(address, useBroadcast);
		}

		internal override void HandleConnectionForciblyClosed(NetConnection connection, SocketException sex)
		{
			if (m_serverConnection == null)
				return;

			if (m_serverConnection.Status == NetConnectionStatus.Connecting)
			{
				// failed to connect; server is not listening
				m_serverConnection.Disconnect("Failed to connect; server is not listening", 0, false, true);
				return;
			}

			m_connectRequested = false;
			m_serverConnection.Disconnect("Connection forcibly closed by server", 0, false, true);
			return;
		}

		/// <summary>
		/// Disconnects from server and closes socket
		/// </summary>
		protected override void PerformShutdown(string reason)
		{
			if (m_serverConnection != null)
			{
				m_serverConnection.Disconnect(reason, 0, true, true);
				m_serverConnection.SendUnsentMessages(NetTime.Now); // give disconnect message a chance to get away
			}
			m_connectRequested = false;
			base.PerformShutdown(reason);
		}
	}
}
