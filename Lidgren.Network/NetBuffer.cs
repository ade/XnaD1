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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.IO;

namespace Lidgren.Network
{
	/// <summary>
	/// Wrapper around a byte array with methods for reading/writing at bit level
	/// </summary>
	public sealed partial class NetBuffer
	{
		// how many NetMessages are using this buffer?
		internal int m_refCount;

		internal int m_bitLength;
		internal int m_readPosition;

		[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
		public byte[] Data;

		/// <summary>
		/// User application data
		/// </summary>
		public object Tag;

		/// <summary>
		/// Creates a new NetBuffer
		/// </summary>
		public NetBuffer()
		{
			Data = new byte[8];
		}

		/// <summary>
		/// Creates a new NetBuffer initially capable of holding 'capacity' bytes
		/// </summary>
		public NetBuffer(int capacityBytes)
		{
			if (capacityBytes < 0)
				capacityBytes = 4;
			Data = new byte[capacityBytes];
		}

		/// <summary>
		/// Creates a new NetBuffer and copies the data supplied
		/// </summary>
		public NetBuffer(byte[] copyData)
		{
			if (copyData != null)
			{
				Data = new byte[copyData.Length];
				Buffer.BlockCopy(copyData, 0, Data, 0, copyData.Length);
				m_bitLength = copyData.Length * 8;
			}
			else
			{
				Data = new byte[4];
			}
		}

		public NetBuffer(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				Data = new byte[1];
				WriteVariableUInt32(0);
				return;
			}

			byte[] strData = Encoding.UTF8.GetBytes(str);
			Data = new byte[1 + strData.Length];
			WriteVariableUInt32((uint)strData.Length);
			Write(strData);
		}

		/// <summary>
		/// Gets a stream to the data held by this netbuffer
		/// </summary>e
		public MemoryStream GetStream()
		{
			return new MemoryStream(Data, 0, LengthBytes, false, true);
		}

		/// <summary>
		/// Data is NOT copied; but now owned by the NetBuffer
		/// </summary>
		public static NetBuffer FromData(byte[] data)
		{
			NetBuffer retval = new NetBuffer(false);
			retval.Data = data;
			retval.LengthBytes = data.Length;
			return retval;
		}

		internal NetBuffer(bool createDataStorage)
		{
			if (createDataStorage)
				Data = new byte[8];
		}

		/// <summary>
		/// Gets or sets the length of the buffer in bytes
		/// </summary>
		public int LengthBytes {
			get { return (m_bitLength >> 3) + ((m_bitLength & 7) > 0 ? 1 : 0); }
			set
			{
				m_bitLength = value * 8;
				InternalEnsureBufferSize(m_bitLength);
			}
		}

		/// <summary>
		/// Gets or sets the length of the buffer in bits
		/// </summary>
		public int LengthBits
		{
			get { return m_bitLength; }
			set
			{
				m_bitLength = value;
				InternalEnsureBufferSize(m_bitLength);
			}
		}

		/// <summary>
		/// Gets or sets the read position in the buffer, in bits (not bytes)
		/// </summary>
		public int Position
		{
			get { return m_readPosition; }
			set { m_readPosition = value; }
		}

		/// <summary>
		/// Resets read and write pointers
		/// </summary>
		public void Reset()
		{
			m_readPosition = 0;
			m_bitLength = 0;
			m_refCount = 0;
			Tag = null;
		}

		/// <summary>
		/// Resets read and write pointers
		/// </summary>
		internal void Reset(int readPos, int writePos)
		{
			m_readPosition = readPos;
			m_bitLength = writePos;
			m_refCount = 0;
			Tag = null;
		}
		
		/// <summary>
		/// Ensures this buffer can hold the specified number of bits prior to a write operation
		/// </summary>
		public void EnsureBufferSize(int numberOfBits)
		{
			int byteLen = (numberOfBits >> 3) + ((numberOfBits & 7) > 0 ? 1 : 0);
			if (Data == null)
			{
				Data = new byte[byteLen + 4]; // overallocate 4 bytes
				return;
			}
			if (Data.Length < byteLen)
				Array.Resize<byte>(ref Data, byteLen + 4); // overallocate 4 bytes
			return;
		}

		internal void InternalEnsureBufferSize(int numberOfBits)
		{
			int byteLen = (numberOfBits >> 3) + ((numberOfBits & 7) > 0 ? 1 : 0);
			if (Data == null)
			{
				Data = new byte[byteLen];
				return;
			}
			if (Data.Length < byteLen)
				Array.Resize<byte>(ref Data, byteLen);
			return;
		}

		/// <summary>
		/// Copies the content of the buffer to a new byte array
		/// </summary>
		public byte[] ToArray()
		{
			int len = LengthBytes;
			byte[] copy = new byte[len];
			Array.Copy(Data, copy, copy.Length);
			return copy;
		}

		public override string ToString()
		{
			return "[NetBuffer " + m_bitLength + " bits, " + m_readPosition + " read]";
		}
	}
}
