using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Lidgren.Network
{
	/// <summary>
	/// A client which can initiate and accept multiple connections
	/// </summary>
	public class NetPeer : NetServer
	{
		public NetPeer(NetConfiguration config)
			: base(config)
		{
			m_allowOutgoingConnections = true;
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing hailData to the server
		/// </summary>
		public NetConnection Connect(string host, int port)
		{
			IPAddress ip = NetUtility.Resolve(host);
			if (ip == null)
				throw new NetException("Unable to resolve host");
			return Connect(new IPEndPoint(ip, port), null);
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing hailData to the server
		/// </summary>
		public NetConnection Connect(string host, int port, byte[] hailData)
		{
			IPAddress ip = NetUtility.Resolve(host);
			if (ip == null)
				throw new NetException("Unable to resolve host");
			return Connect(new IPEndPoint(ip, port), hailData);
		}

		/// <summary>
		/// Connects to the specified endpoint
		/// </summary>
		public NetConnection Connect(IPEndPoint remoteEndpoint)
		{
			return Connect(remoteEndpoint, null);
		}

		/// <summary>
		/// Connects to the specified endpoint; passing (outgoing) hailData to the server
		/// </summary>
		public NetConnection Connect(IPEndPoint remoteEndpoint, byte[] localHailData)
		{
			// ensure we're bound to socket
			if (!m_isBound)
				Start();

			// find empty slot
			if (m_connections.Count >= m_config.MaxConnections)
				throw new NetException("No available slots!");

			NetConnection connection;
			if (m_connectionLookup.TryGetValue(remoteEndpoint, out connection))
			{
				// Already connected to this remote endpoint
			}
			else
			{
				// create new connection
				connection = new NetConnection(this, remoteEndpoint, localHailData, null);
				lock (m_connections)
					m_connections.Add(connection);
				m_connectionLookup.Add(remoteEndpoint, connection);
			}

			// connect
			connection.Connect();

			return connection;
		}

		/// <summary>
		/// Emit a discovery signal to your subnet
		/// </summary>
		public void DiscoverLocalPeers(int port)
		{
			m_discovery.SendDiscoveryRequest(new IPEndPoint(IPAddress.Broadcast, port), true);
		}

		/// <summary>
		/// Emit a discovery signal to a certain host
		/// </summary>
		public void DiscoverKnownPeer(string host, int serverPort)
		{
			IPAddress address = NetUtility.Resolve(host);
			IPEndPoint endPoint = new IPEndPoint(address, serverPort);
			m_discovery.SendDiscoveryRequest(endPoint, false);
		}

		/// <summary>
		/// Emit a discovery signal to a host or subnet
		/// </summary>
		public void DiscoverKnownPeer(IPEndPoint endPoint, bool useBroadcast)
		{
			m_discovery.SendDiscoveryRequest(endPoint, useBroadcast);
		}
	}
}
