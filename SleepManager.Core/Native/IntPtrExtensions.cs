using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace SleepManager.Native
{
	static class IntPtrExtensions
	{
		public static IntPtr Increment(this IntPtr ptr, int cbSize)
		{
			return new IntPtr(ptr.ToInt64() + cbSize);
		}

		public static IntPtr Increment<T>(this IntPtr ptr)
		{
			return ptr.Increment(Marshal.SizeOf(typeof (T)));
		}

		public static T ElementAt<T>(this IntPtr ptr, int index, int? size = null)
		{
			var offset = size ?? Marshal.SizeOf(typeof (T))*index;
			var offsetPtr = ptr.Increment(offset);
			return (T) Marshal.PtrToStructure(offsetPtr, typeof (T));
		}
	}
}