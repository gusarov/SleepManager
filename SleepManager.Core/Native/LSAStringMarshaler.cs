using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace SleepManager.Native
{
	public class LSAStringMarshaler : ICustomMarshaler
	{
		private readonly Hashtable _myAllocated = new Hashtable();

		private static readonly LSAStringMarshaler _marshaler = new LSAStringMarshaler();

		public static ICustomMarshaler GetInstance(string cookie)
		{
			return _marshaler;
		}

		public object MarshalNativeToManaged(System.IntPtr pNativeData)
		{
			if (pNativeData != IntPtr.Zero)
			{
				LSA_UNICODE_STRING lus =
					(LSA_UNICODE_STRING) Marshal.PtrToStructure(pNativeData, typeof (LSA_UNICODE_STRING));
				return lus.ToString();
			}
			return null;
		}

		private static readonly int nativeSize = IntPtr.Size + sizeof (UInt16) + sizeof (UInt16);

		public System.IntPtr MarshalManagedToNative(object ManagedObj)
		{
			IntPtr memory = Marshal.AllocHGlobal(nativeSize);
			_myAllocated[memory] = memory;
			//Console.WriteLine("MarshalManagedToNative");
			var lus = new LSA_UNICODE_STRING();
			lus.Set(((string) ManagedObj));
			Marshal.StructureToPtr(lus, memory, true);
			return memory;
		}

		public void CleanUpManagedData(object ManagedObj)
		{
			//Console.WriteLine("CCC Cleanup Managed Data");            
		}

		public int GetNativeDataSize()
		{
			return nativeSize;
		}

		public void CleanUpNativeData(IntPtr pNativeData)
		{
			//Console.WriteLine("CCC Cleanup Native Data");            

			if (_myAllocated.ContainsKey(pNativeData))
			{
				_myAllocated.Remove(pNativeData);
				var lus = (LSA_UNICODE_STRING) Marshal.PtrToStructure(pNativeData, typeof (LSA_UNICODE_STRING));
				Marshal.FreeHGlobal(pNativeData);
			}
		}
	}
}