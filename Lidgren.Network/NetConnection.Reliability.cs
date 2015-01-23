using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	public sealed partial class NetConnection
	{
		internal double[] m_earliestResend;
		internal Queue<int> m_acknowledgesToSend;
		//internal ushort[] m_nextExpectedSequence;
		internal List<OutgoingNetMessage>[] m_storedMessages;
		//internal List<NetMessage> m_withheldMessages;
		internal List<NetMessage> m_removeList;
		//internal uint[][] m_receivedSequences;
		private int[] m_nextSequenceToSend;
		//private uint[] m_currentSequenceRound;


		// next expected UnreliableOrdered
		private int[] m_nextExpectedSequenced = new int[16];
		private bool[][] m_reliableReceived = new bool[NetConstants.NumSequenceChannels][];
		internal List<IncomingNetMessage>[] m_withheldMessages = new List<IncomingNetMessage>[16]; // number of reliable channels
		private int[] m_allReliableReceivedUpTo = new int[16];

		internal void InitializeReliability()
		{
			m_storedMessages = new List<OutgoingNetMessage>[NetConstants.NumReliableChannels];
			//m_withheldMessages = new List<NetMessage>(2);
			//m_nextExpectedSequence = new ushort[NetConstants.NumSequenceChannels];
			m_nextSequenceToSend = new int[NetConstants.NumSequenceChannels];

			//m_currentSequenceRound = new uint[NetConstants.NumSequenceChannels];
			//for (int i = 0; i < m_currentSequenceRound.Length; i++)
			//	m_currentSequenceRound[i] = NetConstants.NumKeptSequenceNumbers;

			//m_receivedSequences = new uint[NetConstants.NumSequenceChannels][];
			//for (int i = 0; i < m_receivedSequences.Length; i++)
			//	m_receivedSequences[i] = new uint[NetConstants.NumKeptSequenceNumbers];

			m_earliestResend = new double[NetConstants.NumReliableChannels];
			for (int i = 0; i < m_earliestResend.Length; i++)
				m_earliestResend[i] = double.MaxValue;

			m_acknowledgesToSend = new Queue<int>(4);
			m_removeList = new List<NetMessage>(4);
		}

		internal void ResetReliability()
		{
			for (int i = 0; i < m_storedMessages.Length; i++)
				m_storedMessages[i] = null;

			for (int i = 0; i < m_allReliableReceivedUpTo.Length; i++)
				m_allReliableReceivedUpTo[i] = 0;

			for (int i = 0; i < m_withheldMessages.Length; i++)
			{
				if (m_withheldMessages[i] != null)
					m_withheldMessages[i].Clear();
			}

			for(int i=0;i<m_nextExpectedSequenced.Length;i++)
				m_nextExpectedSequenced[i] = 0;
		
			for(int i=0;i<NetConstants.NumSequenceChannels;i++)
			{
				if (m_reliableReceived[i] != null)
				{
					for (int o = 0; o < m_reliableReceived[i].Length; o++)
						m_reliableReceived[i][o] = false;
				}
			}

			m_acknowledgesToSend.Clear();
			m_removeList.Clear();
		}

		/// <summary>
		/// Returns positive numbers for early, 0 for as expected, negative numbers for late message
		/// </summary>
		private int Relate(int receivedSequenceNumber, int expected)
		{
			int diff = expected - receivedSequenceNumber;
			if (diff < -NetConstants.EarlyArrivalWindowSize)
				diff += NetConstants.NumSequenceNumbers;
			else if (diff > NetConstants.EarlyArrivalWindowSize)
				diff -= NetConstants.NumSequenceNumbers;
			return -diff;
		}

		/// <summary>
		/// Process a user message
		/// </summary>
		internal void HandleUserMessage(IncomingNetMessage msg)
		{
#if DEBUG
			if (System.Threading.Thread.CurrentThread != m_owner.m_heartbeatThread)
				throw new Exception("Threading error; should be heartbeat thread. Please check callstack!");
#endif

			//
			// Unreliable
			//
			if (msg.m_sequenceChannel == NetChannel.Unreliable)
			{
				AcceptMessage(msg);
				return;
			}

			//
			// Sequenced
			//
			if (msg.m_sequenceChannel >= NetChannel.UnreliableInOrder1 && msg.m_sequenceChannel <= NetChannel.UnreliableInOrder15)
			{
				// relate to expected
				int seqChanNr = (int)msg.m_sequenceChannel - (int)NetChannel.UnreliableInOrder1;
				int sdiff = Relate(msg.m_sequenceNumber, m_nextExpectedSequenced[seqChanNr]);

				if (sdiff < 0)
				{
					// Reject late sequenced message
					m_owner.LogVerbose("Rejecting late sequenced " + msg);
					m_statistics.CountDroppedSequencedMessage();
					return;
				}
				AcceptMessage(msg);
				int nextExpected = msg.m_sequenceNumber + 1;
				if (nextExpected > NetConstants.NumSequenceNumbers)
					nextExpected = 0;
				m_nextExpectedSequenced[seqChanNr] = nextExpected;
				return;
			}

			//
			// Reliable and ReliableOrdered
			//

			// Send ack, regardless of anything
			m_acknowledgesToSend.Enqueue(((int)msg.m_sequenceChannel << 16) | msg.m_sequenceNumber);

			// relate to all received up to
			int relChanNr = (int)msg.m_sequenceChannel - (int)NetChannel.ReliableUnordered;
			int arut = m_allReliableReceivedUpTo[relChanNr];
			int diff = Relate(msg.m_sequenceNumber, arut);

			if (diff < 0)
			{
				// Reject duplicate
				m_statistics.CountDuplicateMessage(msg);
				m_owner.LogVerbose("Rejecting(1) duplicate reliable " + msg, this);
				return;
			}

			bool isOrdered = (msg.m_sequenceChannel >= NetChannel.ReliableInOrder1);

			if (arut == msg.m_sequenceNumber)
			{
				// Right on time
				AcceptMessage(msg);
				PostAcceptReliableMessage(msg, arut);
				return;
			}

			// get bools list we must check
			bool[] recList = m_reliableReceived[relChanNr];
			if (recList == null)
			{
				recList = new bool[NetConstants.NumSequenceNumbers];
				m_reliableReceived[relChanNr] = recList;
			}

			if (recList[msg.m_sequenceNumber])
			{
				// Reject duplicate
				m_statistics.CountDuplicateMessage(msg);
				m_owner.LogVerbose("Rejecting(2) duplicate reliable " + msg, this);
				return;
			}

			// It's an early reliable message
			if (m_reliableReceived[relChanNr] == null)
				m_reliableReceived[relChanNr] = new bool[NetConstants.NumSequenceNumbers];
			m_reliableReceived[relChanNr][msg.m_sequenceNumber] = true;

			if (!isOrdered)
			{
				AcceptMessage(msg);
				return;
			}

			// Early ordered message; withhold
			List<IncomingNetMessage> wmlist = m_withheldMessages[relChanNr];
			if (wmlist == null)
			{
				wmlist = new List<IncomingNetMessage>();
				m_withheldMessages[relChanNr] = wmlist;
			}

			m_owner.LogVerbose("Withholding " + msg + " (waiting for " + arut + ")", this);
			wmlist.Add(msg);
			return;
		}

		/// <summary>
		/// Run this when current ARUT arrives
		/// </summary>
		private void PostAcceptReliableMessage(NetMessage msg, int arut)
		{
			int seqChan = (int)msg.m_sequenceChannel;
			int relChanNr = seqChan - (int)NetChannel.ReliableUnordered;

			// step forward until next AllReliableReceivedUpTo (arut)
			bool nextArutAlreadyReceived = false;
			do
			{
				if (m_reliableReceived[relChanNr] == null)
					m_reliableReceived[relChanNr] = new bool[NetConstants.NumSequenceNumbers];
				m_reliableReceived[relChanNr][arut] = false;
				arut++;
				if (arut >= NetConstants.NumSequenceNumbers)
					arut = 0;
				nextArutAlreadyReceived = m_reliableReceived[relChanNr][arut];
				if (nextArutAlreadyReceived)
				{
					// ordered?
					if (seqChan >= (int)NetChannel.ReliableInOrder1)
					{
						// this should be a withheld message
						int wmlidx = (int)seqChan - (int)NetChannel.ReliableUnordered;
						bool foundWithheld = false;
						foreach (IncomingNetMessage wm in m_withheldMessages[wmlidx])
						{
							if ((int)wm.m_sequenceChannel == seqChan && wm.m_sequenceNumber == arut)
							{
								// Found withheld message due for delivery
								m_owner.LogVerbose("Releasing withheld message " + wm, this);
								AcceptMessage(wm);
								foundWithheld = true;
								m_withheldMessages[wmlidx].Remove(wm);
								break;
							}
						}
						if (!foundWithheld)
							throw new NetException("Failed to find withheld message!");
					}
				}
			} while (nextArutAlreadyReceived);

			m_allReliableReceivedUpTo[relChanNr] = arut;
		}

		private void AssignSequenceNumber(OutgoingNetMessage msg)
		{
			int idx = (int)msg.m_sequenceChannel;
			int nr = m_nextSequenceToSend[idx];
			msg.m_sequenceNumber = nr;
			nr++;
			if (nr >= NetConstants.NumSequenceNumbers)
				nr = 0;
			m_nextSequenceToSend[idx] = nr;
		}

		internal void StoreMessage(double now, OutgoingNetMessage msg)
		{
			int chanBufIdx = (int)msg.m_sequenceChannel - (int)NetChannel.ReliableUnordered;

			List<OutgoingNetMessage> list = m_storedMessages[chanBufIdx];
			if (list == null)
			{
				list = new List<OutgoingNetMessage>();
				m_storedMessages[chanBufIdx] = list;
			}
			list.Add(msg);

			// schedule resend
			float multiplier = (1 + (msg.m_numSent * msg.m_numSent)) * m_owner.m_config.m_resendTimeMultiplier;
			double nextResend = now + (0.025f + (float)m_currentAvgRoundtrip * 1.1f * multiplier);
			msg.m_nextResend = nextResend;

			m_owner.LogVerbose("Stored " + msg + " @ " + NetTime.ToMillis(now) + " next resend in " + NetTime.ToMillis(msg.m_nextResend - now) + " ms", this);

			// earliest?
			if (nextResend < m_earliestResend[chanBufIdx])
				m_earliestResend[chanBufIdx] = nextResend;
		}

		internal void ResendMessages(double now)
		{
			for (int i = 0; i < m_storedMessages.Length; i++)
			{
				List<OutgoingNetMessage> list = m_storedMessages[i];
				if (list == null || list.Count < 1)
					continue;

				if (now > m_earliestResend[i])
				{
					double newEarliest = double.MaxValue;
					foreach (OutgoingNetMessage msg in list)
					{
						double resend = msg.m_nextResend;
						if (now > resend)
						{
							// Re-enqueue message in unsent list
							m_owner.LogVerbose("Resending " + msg +
								" now: " + NetTime.ToMillis(now) +
								" nextResend: " + NetTime.ToMillis(msg.m_nextResend), this);
							m_statistics.CountMessageResent(msg.m_type);
							m_removeList.Add(msg);
							m_unsentMessages.Enqueue(msg);
						}
						if (resend < newEarliest)
							newEarliest = resend;
					}

					m_earliestResend[i] = newEarliest;
					foreach (OutgoingNetMessage msg in m_removeList)
						list.Remove(msg);
					m_removeList.Clear();
				}
			}
		}

		/// <summary>
		/// Create ack message(s) for sending
		/// </summary>
		private void CreateAckMessages()
		{
			int mtuBits = ((m_owner.m_config.m_maximumTransmissionUnit - 10) / 3) * 8;

			OutgoingNetMessage ackMsg = null;
			int numAcks = m_acknowledgesToSend.Count;
			for (int i = 0; i < numAcks; i++)
			{
				if (ackMsg == null)
				{
					ackMsg = m_owner.CreateOutgoingMessage();
					ackMsg.m_sequenceChannel = NetChannel.Unreliable;
					ackMsg.m_type = NetMessageLibraryType.Acknowledge;
				}

				int ack = m_acknowledgesToSend.Dequeue();

				ackMsg.m_data.Write((byte)((ack >> 16) & 255));
				ackMsg.m_data.Write((byte)(ack & 255));
				ackMsg.m_data.Write((byte)((ack >> 8) & 255));

				//NetChannel ac = (NetChannel)(ack >> 16);
				//int asn = ack & ushort.MaxValue;
				//LogVerbose("Sending ack " + ac + "|" + asn);

				if (ackMsg.m_data.LengthBits >= mtuBits && m_acknowledgesToSend.Count > 0)
				{
					// send and begin again
					m_unsentMessages.Enqueue(ackMsg);
					ackMsg = null;
				}
			}

			if (ackMsg != null)
				m_unsentMessages.EnqueueFirst(ackMsg); // push acks to front of queue

			m_statistics.CountAcknowledgesSent(numAcks);
		}

		internal void HandleAckMessage(IncomingNetMessage ackMessage)
		{
			int len = ackMessage.m_data.LengthBytes;
			if ((len % 3) != 0)
			{
				if ((m_owner.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
					m_owner.NotifyApplication(NetMessageType.BadMessageReceived, "Malformed ack message; length must be multiple of 3; it's " + len, this, ackMessage.m_senderEndPoint);
				return;
			}

			for (int i = 0; i < len; i += 3)
			{

				NetChannel chan = (NetChannel)ackMessage.m_data.ReadByte();
				int seqNr = ackMessage.m_data.ReadUInt16();

				// LogWrite("Acknowledgement received: " + chan + "|" + seqNr);
				m_statistics.CountAcknowledgesReceived(1);

				// remove saved message
				int chanIdx = (int)chan - (int)NetChannel.ReliableUnordered;
				if (chanIdx < 0)
				{
					if ((m_owner.m_enabledMessageTypes & NetMessageType.BadMessageReceived) == NetMessageType.BadMessageReceived)
						m_owner.NotifyApplication(NetMessageType.BadMessageReceived, "Malformed ack message; indicated netchannel " + chan, this, ackMessage.m_senderEndPoint);
					continue;
				}

				List<OutgoingNetMessage> list = m_storedMessages[chanIdx];
				if (list != null)
				{
					int cnt = list.Count;
					if (cnt > 0)
					{
						for (int o = 0; o < cnt; o++)
						{
							OutgoingNetMessage msg = list[o];
							if (msg.m_sequenceNumber == seqNr)
							{

								//LogWrite("Removed stored message: " + msg);
								list.RemoveAt(o);

								// reduce estimated amount of packets on wire
								//CongestionCountAck(msg.m_packetNumber);

								// fire receipt
								if (msg.m_receiptData != null)
								{
									m_owner.LogVerbose("Got ack, removed from storage: " + msg + " firing receipt; " + msg.m_receiptData, this);
									m_owner.FireReceipt(this, msg.m_receiptData);
								}
								else
								{
									m_owner.LogVerbose("Got ack, removed from storage: " + msg, this);
								}

								// recycle
								msg.m_data.m_refCount--;
								if (msg.m_data.m_refCount <= 0)
									m_owner.RecycleBuffer(msg.m_data); // time to recycle buffer
	
								msg.m_data = null;
								//m_owner.m_messagePool.Push(msg);

								break;
							}
						}
					}
				}
			}

			// recycle
			NetBuffer rb = ackMessage.m_data;
			rb.m_refCount = 0; // ack messages can't be used by more than one message
			ackMessage.m_data = null;

			m_owner.RecycleBuffer(rb);
			//m_owner.m_messagePool.Push(ackMessage);
		}
	}
}
