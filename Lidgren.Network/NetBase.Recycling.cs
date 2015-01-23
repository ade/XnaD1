using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	public partial class NetBase
	{
		private const int c_smallBufferSize = 24;
		private const int c_maxSmallItems = 32;
		private const int c_maxLargeItems = 16;

		private Stack<NetBuffer> m_smallBufferPool = new Stack<NetBuffer>(c_maxSmallItems);
		private Stack<NetBuffer> m_largeBufferPool = new Stack<NetBuffer>(c_maxLargeItems);
		private object m_smallBufferPoolLock = new object();
		private object m_largeBufferPoolLock = new object();

		internal void RecycleBuffer(NetBuffer item)
		{
			if (!m_config.m_useBufferRecycling)
				return;

			if (item.Data.Length <= c_smallBufferSize)
			{
				lock (m_smallBufferPoolLock)
				{
					if (m_smallBufferPool.Count >= c_maxSmallItems)
						return; // drop, we're full
					m_smallBufferPool.Push(item);
				}
				return;
			}
			lock (m_largeBufferPoolLock)
			{
				if (m_largeBufferPool.Count >= c_maxLargeItems)
					return; // drop, we're full
				m_largeBufferPool.Push(item);
			}
			return;
		}

		public NetBuffer CreateBuffer(int initialCapacity)
		{
			if (m_config.m_useBufferRecycling)
			{
				NetBuffer retval;
				if (initialCapacity <= c_smallBufferSize)
				{
					lock (m_smallBufferPoolLock)
					{
						if (m_smallBufferPool.Count == 0)
							return new NetBuffer(initialCapacity);
						retval = m_smallBufferPool.Pop();
					}
					retval.Reset();
					return retval;
				}

				lock (m_largeBufferPoolLock)
				{
					if (m_largeBufferPool.Count == 0)
						return new NetBuffer(initialCapacity);
					retval = m_largeBufferPool.Pop();
				}
				retval.Reset();
				return retval;
			}
			else
			{
				return new NetBuffer(initialCapacity);
			}
		}

		public NetBuffer CreateBuffer(string str)
		{
			// TODO: optimize
			NetBuffer retval = CreateBuffer(Encoding.UTF8.GetByteCount(str) + 1);
			retval.Write(str);
			return retval;
		}

		public NetBuffer CreateBuffer()
		{
			return CreateBuffer(m_config.m_defaultBufferCapacity);
		}

	}
}
