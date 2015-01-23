using System;
using System.Collections.Generic;

namespace Lidgren.Network
{
	/// <summary>
	/// Adds support for a dynamic string table, which will automatically send strings as a single number
	/// if the mapping is known to exist in the remote connection, else it will send it.
	/// </summary>
	public partial class NetConnection
	{
		internal const int c_maxTabledStrings = (1 << 15);

		/// <summary>
		/// Lower two bytes: Id
		/// Higher two bytes: 1 if known to exist at remote host
		/// </summary>
		internal Dictionary<string, int> m_stringTable;

		/// <summary>
		/// Lookup mirror of above, with higher byte zeroed
		/// </summary>
		internal Dictionary<int, string> m_stringTableLookUp;

		private void InitializeStringTable()
		{
			m_stringTable = new Dictionary<string,int>();
			m_stringTableLookUp = new Dictionary<int,string>();
			m_stringTable.Add("kThisIsAnErrorStringxyz", 0);
		}

		public void AddToStringTable(string str)
		{
			int val = m_stringTable.Count;
			if (val >= c_maxTabledStrings)
				return;
			m_stringTableLookUp[val] = str;
			m_stringTable[str] = val;
		}

		internal void StringTableAcknowledgeReceived(ushort val)
		{
			string plain;
			if (!m_stringTableLookUp.TryGetValue(val, out plain))
				return; // Ack for unknown value?
			m_stringTable[plain] = val; // without 1 << 16
		}

		internal void WriteStringTable(NetBuffer buffer, string str)
		{
			// Bits:
			// 0: Does the actual string follows? (if so, send confirmation)
			// 1: Is the value contained in the following 6 bits ONLY? (false = use one more byte for 14 bits total)
			// 2-7: value
			// 8-15: (possibly) value

			int val;
			if (!m_stringTable.TryGetValue(str, out val))
			{
				// new value
				val = m_stringTable.Count;
				if (val >= c_maxTabledStrings)
				{
					val = 0;
				}
				else
				{
					m_stringTableLookUp[val] = str;

					val |= (1 << 16); // 1 means not acknowledged yet
					m_stringTable[str] = val;
				}
			}

			// is it known at destination?
			bool known = (val < (1 << 16));
			ushort actualVal = (ushort)val;

			buffer.Write(known);
			if (actualVal < 64)
			{
				buffer.Write(false);
				buffer.Write(actualVal, 6);
			} else {
				buffer.Write(true);
				buffer.Write(actualVal, 6 + 8);
			}
			if (!known)
				buffer.Write(str);
		}

		internal string ReadStringTable(NetBuffer buffer)
		{
			byte b = buffer.ReadByte();

			bool stringFollows = ((b & 1) == 0);
			bool shortVal = ((b & 2) == 0);

			int val = 0;
			if (shortVal)
				val = b >> 2;
			else
				val = ((b & (255 << 2)) << 6) | buffer.ReadByte();

			string retval = string.Empty;
			if (stringFollows)
			{
				retval = buffer.ReadString();

				m_stringTable[retval] = val;
				m_stringTableLookUp[val] = retval;

				//
				// Send confirmation
				//
				NetBuffer buf = new NetBuffer(2);
				buf.Write((ushort)val);
				m_owner.QueueSingleUnreliableSystemMessage(
					NetSystemType.StringTableAck,
					buf,
					m_remoteEndPoint,
					false
				);

				return retval;
			}

			if (!m_stringTableLookUp.TryGetValue(val, out retval))
			{
				// Ack! Failed to find string table value!
				throw new Exception("ACK!");
			}
			return retval;
		}
	}
}
