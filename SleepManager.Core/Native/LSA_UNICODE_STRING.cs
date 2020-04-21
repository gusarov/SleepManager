using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SleepManager.Native
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	struct LSA_UNICODE_STRING
	{
		public ushort Length;
		public ushort MaximumLength;
		[MarshalAs(UnmanagedType.LPWStr)] internal string Buffer;

		public LSA_UNICODE_STRING(string str)
		{
			Buffer = str;
			// Buffer = Marshal.StringToHGlobalUni(str);
			Length = (ushort)(str.Length * UnicodeEncoding.CharSize);
			MaximumLength = Length;
		}

		public void Set(string str)
		{
			this = new LSA_UNICODE_STRING(str);
		}

		public override string ToString()
		{
			//string str = Marshal.PtrToStringUni(Buffer, Length/UnicodeEncoding.CharSize);
			//Console.WriteLine("ToString: {2} ({3}) Length: {0} Max: {1}", Length, MaximumLength, str, str.Length);
			return Buffer;
		}
	}
}