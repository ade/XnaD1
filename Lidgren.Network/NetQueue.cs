using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

namespace Lidgren.Network
{
	/// <summary>
	/// Simplified System.Collection.Generics.Queue with a few modifications:
	/// Doesn't cast exceptions when failing to Peek() or Dequeue()
	/// EnqueueFirst() to push an item on the beginning of the queue
	/// </summary>
	public sealed class NetQueue<T> : IEnumerable<T> where T : class
	{
		private const int m_defaultCapacity = 4;

		private int m_head;
		private int m_size;
		private int m_tail;
		private T[] m_items;

		/// <summary>
		/// Gets the number of items in the queue
		/// </summary>
		public int Count { get { return m_size; } }
		
		/// <summary>
		/// Initializes a new instance of the NetQueue class that is empty and has the default capacity
		/// </summary>
		public NetQueue()
		{
			m_items = new T[m_defaultCapacity];
		}

		/// <summary>
		/// Initializes a new instance of the NetQueue class that is empty and has the specified capacity
		/// </summary>
		public NetQueue(int capacity)
		{
			if (capacity < 0)
				throw new ArgumentException("capacity must be positive", "capacity");
			m_items = new T[capacity];
			m_head = 0;
			m_tail = 0;
			m_size = 0;
		}

		/// <summary>
		/// Removes all objects from the queue
		/// </summary>
		public void Clear()
		{
			Array.Clear(m_items, 0, m_items.Length);
			m_head = 0;
			m_tail = 0;
			m_size = 0;
		}

		/// <summary>
		/// Determines whether an element is in the queue
		/// </summary>
		public bool Contains(T item)
		{
			int index = m_head;
			int num2 = m_size;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			while (num2-- > 0)
			{
				if (item == null)
				{
					if (m_items[index] == null)
					{
						return true;
					}
				}
				else if ((m_items[index] != null) && comparer.Equals(m_items[index], item))
				{
					return true;
				}
				index = (index + 1) % m_items.Length;
			}
			return false;
		}

		/// <summary>
		/// Removes and returns an object from the beginning of the queue
		/// </summary>
		public T Dequeue()
		{
			if (m_size == 0)
				return null;

			T local = m_items[m_head];
			m_items[m_head] = default(T);
			m_head = (m_head + 1) % m_items.Length;
			m_size--;

			return local;
		}

		/// <summary>
		/// Removes and returns an object from the beginning of the queue
		/// </summary>
		public T Dequeue(int stepsForward)
		{
			if (stepsForward == 0)
				return Dequeue();

			if (stepsForward > m_size - 1)
				return null; // outside valid range

			int ptr = (m_head + stepsForward) % m_items.Length;
			T local = m_items[ptr];

			while (ptr != m_head)
			{
				m_items[ptr] = m_items[ptr - 1];
				ptr--;
				if (ptr < 0)
					ptr = m_items.Length - 1;
			}
			m_items[ptr] = default(T);
			m_head = (m_head + 1) % m_items.Length;
			m_size--;

			return local;
		}

		/// <summary>
		/// Adds an object to the end of the queue
		/// </summary>
		public void Enqueue(T item)
		{
			if (m_size == m_items.Length)
			{
				int capacity = (int)((m_items.Length * 200L) / 100L);
				if (capacity < (m_items.Length + 4))
				{
					capacity = m_items.Length + 4;
				}
				SetCapacity(capacity);
			}
			m_items[m_tail] = item;
			m_tail = (m_tail + 1) % m_items.Length;
			m_size++;
		}

		/// <summary>
		/// Adds an object to the beginning of the queue
		/// </summary>
		public void EnqueueFirst(T item)
		{
			if (m_size == m_items.Length)
			{
				int capacity = (int)((m_items.Length * 200L) / 100L);
				if (capacity < (m_items.Length + 4))
				{
					capacity = m_items.Length + 4;
				}
				SetCapacity(capacity);
			}

			m_head--;
			if (m_head < 0)
				m_head = m_items.Length - 1;
			m_items[m_head] = item;
			m_size++;
		}

		public T Peek(int stepsForward)
		{
			return m_items[(m_head + stepsForward) % m_items.Length];
		}

		/// <summary>
		/// Returns the object at the beginning of the queue without removing it
		/// </summary>
		public T Peek()
		{
			if (m_size == 0)
				return null;
			return m_items[m_head];
		}

		private void SetCapacity(int capacity)
		{
			T[] destinationArray = new T[capacity];
			if (m_size > 0)
			{
				if (m_head < m_tail)
				{
					Array.Copy(m_items, m_head, destinationArray, 0, m_size);
				}
				else
				{
					Array.Copy(m_items, m_head, destinationArray, 0, m_items.Length - m_head);
					Array.Copy(m_items, 0, destinationArray, m_items.Length - m_head, m_tail);
				}
			}
			m_items = destinationArray;
			m_head = 0;
			m_tail = (m_size == capacity) ? 0 : m_size;
		}

		public IEnumerator<T> GetEnumerator()
		{
			int bufLen = m_items.Length;

			int ptr = m_head;
			int len = m_size;
			while (len-- > 0)
			{
				yield return m_items[ptr];
				ptr++;
				if (ptr >= bufLen)
					ptr = 0;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			int bufLen = m_items.Length;

			int ptr = m_head;
			int len = m_size;
			while (len-- > 0)
			{
				yield return m_items[ptr];
				ptr++;
				if (ptr >= bufLen)
					ptr = 0;
			}
		}
	}
}