using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	public sealed partial class NetConnection
	{
		/// <summary>
		/// Approves the connection and sends any (already set) local hail data
		/// </summary>
		public void Approve()
		{
			Approve(null);
		}

		/// <summary>
		/// Approves the connection and sents/sends local hail data provided
		/// </summary>
		public void Approve(byte[] localHailData)
		{
			if (m_approved == true)
				throw new NetException("Connection is already approved!");

			//
			// Continue connection phase
			//

			if (localHailData != null)
				m_localHailData = localHailData;

			// Add connection
			m_approved = true;

			NetServer server = m_owner as NetServer;
			server.AddConnection(NetTime.Now, this);
		}

		/// <summary>
		/// Disapprove the connection, rejecting it.
		/// </summary>
		public void Disapprove(string reason)
		{
			if (m_approved == true)
				throw new NetException("Connection is already approved!");

			// send connectionrejected
			NetBuffer buf = new NetBuffer(reason);
			m_owner.QueueSingleUnreliableSystemMessage(
				NetSystemType.ConnectionRejected,
				buf,
				m_remoteEndPoint,
				false
			);

			m_requestDisconnect = true;
			m_requestLinger = 0.0f;
			m_requestSendGoodbye = !string.IsNullOrEmpty(reason);
			m_futureDisconnectReason = reason;
		}
	}
}
