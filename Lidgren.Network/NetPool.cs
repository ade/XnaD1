using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Lidgren.Network
{
	internal sealed class NetBufferPool
	{
		private Stack<NetBuffer> m_pool = new Stack<NetBuffer>();
		private object m_lock = new object();
		private int m_maxItems;

		internal NetBufferPool(int maxItems, int initialItems)
		{
			m_maxItems = maxItems;
			m_pool = new Stack<NetBuffer>(maxItems);
			for (int i = 0; i < initialItems; i++)
				m_pool.Push(new NetBuffer());
		}

		internal void Push(NetBuffer item)
		{
			lock (m_lock)
			{
				if (m_pool.Count >= m_maxItems)
					return;
				m_pool.Push(item);
			}
		}

		internal NetBuffer Pop()
		{
			lock (m_lock)
			{
				if (m_pool.Count == 0)
					return new NetBuffer();
				return m_pool.Pop();
			}
		}
 
	}

		/// <summary>
		/// A fixed size circular buffer
		/// </summary>
	internal sealed class NetPool<T> : IEnumerable<T> where T : class, new()
	{
		private T[] m_buffer;
		private int m_start, m_length;

		/// <summary>
		/// Gets the length of the buffer
		/// </summary>
		public int Length { get { return m_length; } }

		public NetPool(int capacity, int initialCount)
		{
			m_buffer = new T[capacity];

			for (int i = 0; i < initialCount; i++)
				m_buffer[i] = new T();
			m_length = initialCount;
		}

		public T Pop()
		{
			if (m_length == 0)
				return new T();

			T retval = m_buffer[m_start];
			m_length--;
			m_start++;
			if (m_start >= m_buffer.Length)
				m_start = 0;
			return retval;
		}

		public void Push(T t)
		{
			Add(t);
		}

		/// <summary>
		/// Clears the buffer
		/// </summary>
		public void Clear()
		{
			m_start = 0;
			m_length = 0;
		}

		/// <summary>
		/// Adds an item to the end of the buffer
		/// </summary>
		public void Add(T t)
		{
			int bufLen = m_buffer.Length;

			int pos = m_start + m_length;
			if (pos >= bufLen)
				pos -= bufLen;

			m_buffer[pos] = t;

			if (m_length < bufLen)
			{
				m_length++;
			}
			else
			{
				m_start++;
				if (m_start >= bufLen)
					m_start = 0;
			}
		}

		/// <summary>
		/// Inserts, if possible, the item at the index specified
		/// </summary>
		public void Insert(T t, int index)
		{
			if (index >= m_length)
				throw new ArgumentOutOfRangeException("index");

			int bufLen = m_buffer.Length;

			int pos = m_start + index;
			if (pos >= bufLen)
				pos -= bufLen;

			// insert and push
			int numPush = m_length - index;

			while (numPush-- > 0)
			{
				T tmp = m_buffer[pos];
				m_buffer[pos] = t;
				t = tmp;
			}
		}

		public bool Contains(T t)
		{
			int bufLen = m_buffer.Length;

			int ptr = m_start;
			int len = m_length;
			while (len-- > 0)
			{
				if (m_buffer[ptr] == t)
					return true;
				ptr++;
				if (ptr >= bufLen)
					ptr = 0;
			}
			return false;
		}

		public IEnumerator<T> GetEnumerator()
		{
			int bufLen = m_buffer.Length;

			int ptr = m_start;
			int len = m_length;
			while (len-- > 0)
			{
				yield return m_buffer[ptr];
				ptr++;
				if (ptr >= bufLen)
					ptr = 0;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			int bufLen = m_buffer.Length;

			int ptr = m_start;
			int len = m_length;
			while (len-- > 0)
			{
				yield return m_buffer[ptr];
				ptr++;
				if (ptr >= bufLen)
					ptr = 0;
			}
		}

		/// <summary>
		/// Gets the first item in the buffer
		/// </summary>
		public T GetHead()
		{
			if (m_length < 1)
				return default(T);
			return m_buffer[m_start];
		}

		/// <summary>
		/// Gets the first item in the buffer, and removes it
		/// </summary>
		public T DequeueHead()
		{
			if (m_length < 1)
				return default(T);

			T retval = m_buffer[m_start];
			m_length--;
			m_start++;
			if (m_start >= m_buffer.Length)
				m_start = 0;
			return retval;
		}

		/// <summary>
		/// Gets the last item in the buffer
		/// </summary>
		public T GetTail()
		{
			if (m_length < 1)
				return default(T);
			int idx = m_start + m_length;
			if (idx >= m_buffer.Length)
				idx -= m_buffer.Length;
			return m_buffer[idx];
		}

		/// <summary>
		/// Remove the first item in the buffer
		/// </summary>
		public void RemoveHead()
		{
			if (m_length > 0)
			{
				m_length--;
				m_start--;
				if (m_start < 0)
					m_start = m_buffer.Length - 1;
			}
		}

		/// <summary>
		/// Remove the last item in the buffer
		/// </summary>
		public void RemoveTail()
		{
			if (m_length > 0)
				m_length--;
		}
	}

	/*
	[DebuggerDisplay("Read = {m_readPtr} Write =  {m_writePtr}")]
	internal sealed class NetPool<T> where T : class, new()
	{
		private object m_lock;
		internal T[] m_pool;
		private int m_writePtr;
		private int m_readPtr;

		/// <summary>
		/// Amount of objects currently in the pool
		/// </summary>
		public int Count
		{
			get
			{
				lock (m_lock)
				{
					if (m_readPtr <= m_writePtr)
						return m_writePtr - m_readPtr;
					else
						return (m_writePtr + m_pool.Length - m_readPtr);
				}
			}
		}

		internal NetPool(int maxCount, int initialCount)
		{
			Debug.Assert(initialCount <= maxCount);

			m_pool = new T[maxCount];
			m_writePtr = 0;
			m_readPtr = 0;
			for (int i = 0; i < initialCount; i++)
			{
				T item = new T();
				m_pool[m_writePtr++] = item;
			}
			if (m_writePtr >= maxCount)
				m_writePtr -= maxCount;

			m_lock = new object();
		}

		internal T Pop()
		{
			lock (m_lock)
			{
				T retval = m_pool[m_readPtr++];
				if (m_readPtr == m_pool.Length)
					m_readPtr = 0;

				if (m_readPtr == m_writePtr)
				{
					// pool is empty; allocate so it contains at least one element
					T item = new T();
					//LogVerbose("Pool item created: " + typeof(T).Name);

					m_pool[m_readPtr] = item;
					m_writePtr++;
					if (m_writePtr == m_pool.Length)
						m_writePtr = 0;
				}

				//LogVerbose("Pool item popped: " + typeof(T).Name + " Pool count: " + this.Count);
				return retval;
			}
		}

		internal void Push(T item)
		{
			lock (m_lock)
			{
#if DEBUG
				if (item.GetType() == typeof(NetMessage))
					Debug.Assert((item as NetMessage).m_data == null);
#endif
				if (m_writePtr == m_readPtr)
					return; // pool is full
				m_pool[m_writePtr++] = item;
				if (m_writePtr == m_pool.Length)
					m_writePtr = 0;

				//LogVerbose("Item pushed onto pool: " + typeof(T).Name + " Pool count: " + this.Count);
			}
		}
	}
	*/
}