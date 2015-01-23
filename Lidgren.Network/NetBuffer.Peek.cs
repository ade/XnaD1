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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Lidgren.Network
{
	public sealed partial class NetBuffer
	{
		//
		// 1 bit
		//
		public bool PeekBoolean()
		{
			byte retval = NetBitWriter.ReadByte(Data, 1, m_readPosition);
			return (retval > 0 ? true : false);
		}

		//
		// 8 bit 
		//
		public byte PeekByte()
		{
			byte retval = NetBitWriter.ReadByte(Data, 8, m_readPosition);
			return retval;
		}

		public byte PeekByte(int numberOfBits)
		{
			byte retval = NetBitWriter.ReadByte(Data, numberOfBits, m_readPosition);
			return retval;
		}

		public byte[] PeekBytes(int numberOfBytes)
		{
			byte[] retval = new byte[numberOfBytes];
			NetBitWriter.ReadBytes(Data, numberOfBytes, m_readPosition, retval, 0);
			return retval;
		}

		//
		// 16 bit
		//
		public Int16 PeekInt16()
		{
			Debug.Assert(m_bitLength - m_readPosition >= 16, "tried to read past buffer size");
			uint retval = NetBitWriter.ReadUInt32(Data, 16, m_readPosition);
			return (short)retval;
		}

		[CLSCompliant(false)]
		public UInt16 PeekUInt16()
		{
			Debug.Assert(m_bitLength - m_readPosition >= 16, "tried to read past buffer size");
			uint retval = NetBitWriter.ReadUInt32(Data, 16, m_readPosition);
			return (ushort)retval;
		}

		//
		// 32 bit
		//
		public Int32 PeekInt32()
		{
			Debug.Assert(m_bitLength - m_readPosition >= 32, "tried to read past buffer size");
			uint retval = NetBitWriter.ReadUInt32(Data, 32, m_readPosition);
			return (Int32)retval;
		}

		public Int32 PeekInt32(int numberOfBits)
		{
			Debug.Assert((numberOfBits > 0 && numberOfBits <= 32), "PeekInt32() can only read between 1 and 32 bits");
			Debug.Assert(m_bitLength - m_readPosition >= numberOfBits, "tried to read past buffer size");

			uint retval = NetBitWriter.ReadUInt32(Data, numberOfBits, m_readPosition);

			if (numberOfBits == 32)
				return (int)retval;

			int signBit = 1 << (numberOfBits - 1);
			if ((retval & signBit) == 0)
				return (int)retval; // positive

			// negative
			unchecked
			{
				uint mask = ((uint)-1) >> (33 - numberOfBits);
				uint tmp = (retval & mask) + 1;
				return -((int)tmp);
			}
		}

		[CLSCompliant(false)]
		public UInt32 PeekUInt32()
		{
			uint retval = NetBitWriter.ReadUInt32(Data, 32, m_readPosition);
			return retval;
		}

		[CLSCompliant(false)]
		public UInt32 PeekUInt32(int numberOfBits)
		{
			Debug.Assert((numberOfBits > 0 && numberOfBits <= 32), "ReadUInt() can only read between 1 and 32 bits");
			//Debug.Assert(m_bitLength - m_readBitPtr >= numberOfBits, "tried to read past buffer size");

			UInt32 retval = NetBitWriter.ReadUInt32(Data, numberOfBits, m_readPosition);
			return retval;
		}

		//
		// 64 bit
		//
		[CLSCompliant(false)]
		public UInt64 PeekUInt64()
		{
			Debug.Assert(m_bitLength - m_readPosition >= 64, "tried to read past buffer size");

			ulong low = NetBitWriter.ReadUInt32(Data, 32, m_readPosition);
			ulong high = NetBitWriter.ReadUInt32(Data, 32, m_readPosition + 32);

			ulong retval = low + (high << 32);

			return retval;
		}

		public Int64 PeekInt64()
		{
			Debug.Assert(m_bitLength - m_readPosition >= 64, "tried to read past buffer size");
			unchecked
			{
				ulong retval = PeekUInt64();
				long longRetval = (long)retval;
				return longRetval;
			}
		}

		[CLSCompliant(false)]
		public UInt64 PeekUInt64(int numberOfBits)
		{
			Debug.Assert((numberOfBits > 0 && numberOfBits <= 64), "ReadUInt() can only read between 1 and 64 bits");
			Debug.Assert(m_bitLength - m_readPosition >= numberOfBits, "tried to read past buffer size");

			ulong retval;
			if (numberOfBits <= 32)
			{
				retval = (ulong)NetBitWriter.ReadUInt32(Data, numberOfBits, m_readPosition);
			}
			else
			{
				retval = NetBitWriter.ReadUInt32(Data, 32, m_readPosition);
				retval |= NetBitWriter.ReadUInt32(Data, numberOfBits - 32, m_readPosition + 32) << 32;
			}
			return retval;
		}

		public Int64 PeekInt64(int numberOfBits)
		{
			Debug.Assert(((numberOfBits > 0) && (numberOfBits < 65)), "ReadInt64(bits) can only read between 1 and 64 bits");
			return (long)PeekUInt64(numberOfBits);
		}

		//
		// Floating point
		//
		public float PeekFloat()
		{
			return PeekSingle();
		}

		public float PeekSingle()
		{
			Debug.Assert(m_bitLength - m_readPosition >= (4 * 8), "tried to read past buffer size");

			if ((m_readPosition & 7) == 0) // read directly
			{
				// endianness is handled inside BitConverter.ToSingle
				float retval = BitConverter.ToSingle(Data, m_readPosition >> 3);
				return retval;
			}

			byte[] bytes = PeekBytes(4);
			return BitConverter.ToSingle(bytes, 0); // endianness is handled inside BitConverter.ToSingle
		}

		public double PeekDouble()
		{
			Debug.Assert(m_bitLength - m_readPosition >= (8 * 8), "tried to read past buffer size");

			if ((m_readPosition & 7) == 0) // read directly
			{
				// read directly
				double retval = BitConverter.ToDouble(Data, m_readPosition >> 3);
				return retval;
			}

			byte[] bytes = PeekBytes(8);
			return BitConverter.ToDouble(bytes, 0); // endianness is handled inside BitConverter.ToSingle
		}

		public string PeekString()
		{
			int pos = m_readPosition;
			string retval = ReadString();
			m_readPosition = pos;
			return retval;
		}
	}
}
