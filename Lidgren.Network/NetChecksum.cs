using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	/// <summary>
	/// CCITT-16 and Adler16
	/// </summary>
	public static class NetChecksum
	{
		private static ushort[] m_table;

		static NetChecksum()
		{
			m_table = new ushort[256];

			// generate lookup table for ccitt16
			for (int i = 0; i < 256; i++)
			{
				ushort crc = (ushort)i;
				crc <<= 8;
				for (int j = 0; j < 8; j++)
				{
					ushort bit = (ushort)(crc & 32768);
					crc <<= 1;
					if (bit != 0)
						crc ^= 0x1021;
				}
				m_table[i] = crc;
			}
		}

		[CLSCompliant(false)]
		public static ushort CalculateCCITT16(byte[] data, int offset, int len)
		{
			ulong crc = 0x1D0F;
			for (int i = 0; i < len; i++)
				crc = (crc << 8) ^ m_table[((crc >> 8) & 0xff) ^ data[offset + i]];
			return (ushort)crc;
		}

		// Adler16; superior to adler32 and fletcher16 for small size data
		// see http://www.zlib.net/maxino06_fletcher-adler.pdf
		[CLSCompliant(false)]
		public static ushort Adler16(byte[] data, int offset, int len)
		{
			int a = 1;
			int b = 0;

			int ptr = offset;
			int end = offset + len;
			while (ptr < end)
			{
				int tlen = (end - ptr > 5550 ? 5550 : end - ptr);
				for (int i = 0; i < tlen; i++)
				{
					a += data[ptr++];
					b += a;
				}
				a %= 251;
				b %= 251;
			}
			return (ushort)(b << 8 | a);
		}
	}
}
