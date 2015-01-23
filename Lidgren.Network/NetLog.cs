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
using System.IO;

namespace Lidgren.Network
{
	/*
	/// <summary>
	/// Simple debug log class, won't appear in Release builds
	/// </summary>
	public static class NetLog
	{
		private static bool s_useVerbose;
		private static object m_lock = new object();

		public static bool UseVerbose { get { return s_useVerbose; } set { s_useVerbose = value; } }

		/// <summary>
		/// Strictly debug messages; this method is never called in Release builds
		/// </summary>
		[Conditional("DEBUG")]
		public static void Write(string text)
		{
			uint ms = (uint)(NetTime.Now * 1000.0);
			lock(m_lock)
				File.WriteAllText("./netlog.txt", ms.ToString() + ": " + text + Environment.NewLine);
		}

		/// <summary>
		/// Strictly development debug messages; this method is never called in Release builds
		/// </summary>
		[Conditional("DEBUG")]
		public static void Verbose(string text)
		{
			if (s_useVerbose)
			{
				uint ms = (uint)(NetTime.Now * 1000.0);
				lock (m_lock)
					File.WriteAllText("./netlog.txt", ms.ToString() + ": " + text + Environment.NewLine);
			}
		}
	}
	*/
}
