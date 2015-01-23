using System;
using System.Collections.Generic;

using System.Net;

namespace Lidgren.Network
{
	internal sealed class NetDiscoveryRequest
	{
		private IPEndPoint m_endPoint;
		private ushort m_number;
		private List<IPEndPoint> m_discovered;
		private double m_timeOut;
		private double m_lastSend;
		private float m_interval;
		private bool m_useBroadcast;

		public ushort Number { get { return m_number; } }
		public double TimeOut { get { return m_timeOut; } }

		internal NetDiscoveryRequest(IPEndPoint endPoint, ushort number, bool useBroadcast)
		{
			m_endPoint = endPoint;
			m_number = number;
			m_useBroadcast = useBroadcast;
			Setup(10.0f, 30.0f);
		}

		internal NetDiscoveryRequest(IPEndPoint endPoint, ushort number, bool useBroadcast, float interval, float timeout)
		{
			m_endPoint = endPoint;
			m_number = number;
			m_useBroadcast = useBroadcast;
			Setup(interval, timeout);
		}

		private void Setup(float interval, float timeout)
		{
			m_interval = interval;
			m_lastSend = NetTime.Now;
			m_timeOut = m_lastSend + timeout;
		}

		internal void Heartbeat(NetDiscovery discovery, double now)
		{
			if (now > m_lastSend + m_interval)
			{
				// time for resend
				discovery.DoSendDiscoveryRequest(m_endPoint, m_useBroadcast, this, m_interval, (float)(now - m_timeOut));
				m_lastSend = now;
			}
		}

		internal bool HasDiscovered(IPEndPoint endPoint)
		{
			if (m_discovered != null && m_discovered.Contains(endPoint))
				return true;
			if (m_discovered == null)
				m_discovered = new List<IPEndPoint>();
			m_discovered.Add(endPoint);
			return false;
		}
	}

	internal sealed class NetDiscovery
	{
		private NetBase m_netBase;
		private List<NetDiscoveryRequest> m_requests;
		private ushort m_nextRequestNumber;

		internal NetDiscovery(NetBase netBase)
		{
			m_nextRequestNumber = 1;
			m_netBase = netBase;
			m_requests = null;
		}

		internal void Heartbeat(double now)
		{
			if (m_requests == null)
				return;

			foreach (NetDiscoveryRequest request in m_requests)
			{
				request.Heartbeat(this, now);
				if (now > request.TimeOut)
				{
					m_netBase.LogVerbose("Removing discovery request " + request.Number);
					m_requests.Remove(request);
					if (m_requests.Count < 1)
						m_requests = null;
					break;
				}
			}
		}

		/// <summary>
		/// Emit a discovery signal to a host or subnet
		/// </summary>
		internal void SendDiscoveryRequest(IPEndPoint endPoint, bool useBroadcast)
		{
			DoSendDiscoveryRequest(endPoint, useBroadcast, null, 10.0f, 30.0f);
		}

		internal void SendDiscoveryRequest(IPEndPoint endPoint, bool useBroadcast, float interval, float timeout)
		{
			DoSendDiscoveryRequest(endPoint, useBroadcast, null, interval, timeout);
		}

		internal void DoSendDiscoveryRequest(
			IPEndPoint endPoint,
			bool useBroadcast,
			NetDiscoveryRequest request,
			float interval,
			float timeout)
		{
			if (!m_netBase.m_isBound)
				m_netBase.Start();

			m_netBase.LogWrite("Discovery request to " + endPoint);

			if (request == null)
			{
				m_netBase.LogVerbose("Creating discovery request " + m_nextRequestNumber);
				request = new NetDiscoveryRequest(endPoint, m_nextRequestNumber++, useBroadcast, interval, timeout);
				if (m_requests == null)
					m_requests = new List<NetDiscoveryRequest>();
				m_requests.Add(request);
			}

			string appIdent = m_netBase.m_config.ApplicationIdentifier;
			NetBuffer data = new NetBuffer(appIdent.Length + 8);
			
			// write app identifier
			data.Write(appIdent);

			// write netbase identifier to avoid self-discovery
			data.Write(m_netBase.m_randomIdentifier);

			// write request number
			data.Write((ushort)request.Number);

			m_netBase.LogWrite("Discovering " + endPoint.ToString() + "...");
			m_netBase.QueueSingleUnreliableSystemMessage(NetSystemType.Discovery, data, endPoint, useBroadcast);
		}

		/// <summary>
		/// Handle a discovery request
		/// </summary>
		internal void HandleRequest(IncomingNetMessage message, IPEndPoint senderEndpoint)
		{
			ushort number;

			if (!VerifyIdentifiers(message, senderEndpoint, out number))
				return; // bad app ident or self discovery

			NetBuffer buf = m_netBase.CreateBuffer(2);
			buf.Write(number);

			m_netBase.LogWrite("Sending discovery response to " + senderEndpoint + " request " + number);
			
			// send discovery response
			m_netBase.SendSingleUnreliableSystemMessage(
				NetSystemType.DiscoveryResponse,
				buf,
				senderEndpoint,
				false
			);

			m_netBase.RecycleBuffer(buf);
		}

		internal bool VerifyIdentifiers(
			IncomingNetMessage message,
			IPEndPoint endPoint,
			out ushort number
		)
		{
			number = 0;
			int payLen = message.m_data.LengthBytes;
			if (payLen < 13)
			{
				if ((m_netBase.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
					m_netBase.NotifyApplication(NetMessageType.BadMessageReceived, "Malformed Discovery message received from " + endPoint, null, message.m_senderEndPoint);
				return false;
			}

			// check app identifier
			string appIdent2 = message.m_data.ReadString();
			if (appIdent2 != m_netBase.m_config.ApplicationIdentifier)
			{
				if ((m_netBase.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
					m_netBase.NotifyApplication(NetMessageType.BadMessageReceived, "Discovery for different application identification received: " + appIdent2, null, message.m_senderEndPoint);
				return false;
			}

			// check netbase identifier
			byte[] nbid = message.m_data.ReadBytes(m_netBase.m_randomIdentifier.Length);
			if (NetUtility.CompareElements(nbid, m_netBase.m_randomIdentifier))
				return false; // don't respond to your own discovery request

			// retrieve number
			number = message.m_data.ReadUInt16();

			// it's ok 
			return true;
		}

		/// <summary>
		/// Returns a NetMessage to return to application; or null if nothing
		/// </summary>
		internal IncomingNetMessage HandleResponse(
			IncomingNetMessage message,
			IPEndPoint endPoint
		)
		{
			if ((m_netBase.m_enabledMessageTypes & NetMessageType.ServerDiscovered) != NetMessageType.ServerDiscovered)
				return null; // disabled

			if (message.m_data == null || m_requests == null)
				return null;

			ushort number = message.m_data.ReadUInt16();

			// find corresponding request
			NetDiscoveryRequest found = null;
			foreach (NetDiscoveryRequest request in m_requests)
			{
				if (request.Number == number)
				{
					found = request;
					break;
				}
			}

			if (found == null)
			{
				m_netBase.LogVerbose("Received discovery response to request " + number + " - unknown/old request!");
				return null; // Timed out request or not found
			}

			if (found.HasDiscovered(endPoint))
			{
				m_netBase.LogVerbose("Received discovery response to request " + number + " - previously known response!");
				return null; // Already discovered in this request, else stored it
			}

			m_netBase.LogVerbose("Received discovery response to request " + number + " - passing on to app...");

			IncomingNetMessage resMsg = m_netBase.CreateIncomingMessage();
			resMsg.m_msgType = NetMessageType.ServerDiscovered;

			// write sender, assume ipv4
			resMsg.m_data.Write(endPoint);

			return resMsg;
		}
	}
}
