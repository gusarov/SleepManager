using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SleepManager.Native
{
	class LSAPolicy : IDisposable
	{
		private const string _seInteractiveLogonRight = "SeInteractiveLogonRight";
		private IntPtr _policy;

		[Flags]
		public enum LSA_AccessPolicy : long
		{
			POLICY_VIEW_LOCAL_INFORMATION = 0x00000001L,
			POLICY_VIEW_AUDIT_INFORMATION = 0x00000002L,
			POLICY_GET_PRIVATE_INFORMATION = 0x00000004L,
			POLICY_TRUST_ADMIN = 0x00000008L,
			POLICY_CREATE_ACCOUNT = 0x00000010L,
			POLICY_CREATE_SECRET = 0x00000020L,
			POLICY_CREATE_PRIVILEGE = 0x00000040L,
			POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x00000080L,
			POLICY_SET_AUDIT_REQUIREMENTS = 0x00000100L,
			POLICY_AUDIT_LOG_ADMIN = 0x00000200L,
			POLICY_SERVER_ADMIN = 0x00000400L,
			POLICY_LOOKUP_NAMES = 0x00000800L,
			POLICY_NOTIFICATION = 0x00001000L
		}


		public LSAPolicy()
			: this(
				LSA_AccessPolicy.POLICY_AUDIT_LOG_ADMIN |
					LSA_AccessPolicy.POLICY_CREATE_ACCOUNT |
					LSA_AccessPolicy.POLICY_CREATE_PRIVILEGE |
					LSA_AccessPolicy.POLICY_CREATE_SECRET |
					LSA_AccessPolicy.POLICY_GET_PRIVATE_INFORMATION |
					LSA_AccessPolicy.POLICY_LOOKUP_NAMES |
					LSA_AccessPolicy.POLICY_NOTIFICATION |
					LSA_AccessPolicy.POLICY_SERVER_ADMIN |
					LSA_AccessPolicy.POLICY_SET_AUDIT_REQUIREMENTS |
					LSA_AccessPolicy.POLICY_SET_DEFAULT_QUOTA_LIMITS |
					LSA_AccessPolicy.POLICY_TRUST_ADMIN |
					LSA_AccessPolicy.POLICY_VIEW_AUDIT_INFORMATION |
					LSA_AccessPolicy.POLICY_VIEW_LOCAL_INFORMATION
				)
		{
		}

		public LSAPolicy(LSA_AccessPolicy access)
		{
			//initialize an empty unicode-string
			string systemName = null;

			//these attributes are not used, but LsaOpenPolicy wants them to exist
			// (MSDN: "the structure members are not used, initalize them to NULL or zero")
			var objectAttributes = new LSA_OBJECT_ATTRIBUTES();
			//				objectAttributes.Length = 0;
			//				objectAttributes.RootDirectory = IntPtr.Zero;
			//				objectAttributes.Attributes = 0;
			//				objectAttributes.SecurityDescriptor = IntPtr.Zero;
			//				objectAttributes.SecurityQualityOfService = IntPtr.Zero;
			//
			//get a policy handle
			var resultPolicy = LsaOpenPolicy(ref systemName, ref objectAttributes, (int) access, out _policy);
			var winErrorCode = LsaNtStatusToWinError(resultPolicy);

			if (winErrorCode != 0)
			{
				throw new Win32Exception(winErrorCode, "OpenPolicy failed: " + winErrorCode);
			}

		}

		~LSAPolicy()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Dispose(bool disposing)
		{
			if (_policy != IntPtr.Zero)
			{
				LsaClose(_policy);
				_policy = IntPtr.Zero;
			}
		}

		public string RetrievePrivateData(string key)
		{
			string result = null;
			var ntstatus = LsaRetrievePrivateData(_policy, key, ref result);
			var winErrorCode = LsaNtStatusToWinError(ntstatus);
			if (winErrorCode != 0)
			{
				throw new Win32Exception(winErrorCode, "RetreivePrivateData failed: " + winErrorCode);
			}
			return result;
		}

		public void StorePrivateData(string key, string value)
		{
			var ntstatus = LsaStorePrivateData(_policy, key, value);
			var winErrorCode = LsaNtStatusToWinError(ntstatus);
			if (winErrorCode != 0)
			{
				throw new Win32Exception(winErrorCode, "RetreivePrivateData failed: " + winErrorCode);
			}
		}

		public void RemoveSids(IEnumerable<string> sids)
		{
			foreach (var sid in sids)
			{
				var sidBytes = ConvertStringSidToSid(sid);
				LsaRemoveAccountRights(_policy, sidBytes, false, new[] { new LSA_UNICODE_STRING(_seInteractiveLogonRight) }, 1);
			}
		}

		public void AddSids(IEnumerable<string> sids)
		{

		}

		public IEnumerable<string> EnumerateAllowLogonLocalSids()
		{

			// You should already have the HPolicy and SID ready 
			IntPtr rightsPtr;
			int countOfRights;

			var right = new LSA_UNICODE_STRING();
			right.Set(_seInteractiveLogonRight);

			LsaEnumerateAccountsWithUserRight(_policy, new[] {right}, out rightsPtr, out countOfRights);
			try
			{
				for (Int32 i = 0; i < countOfRights; i++)
				{

					var structure = rightsPtr.ElementAt<LSA_ENUMERATION_INFORMATION>(i);
					
					/*
					var name = new StringBuilder();
					uint cchName = 0;
					uint cchName2 = 0;
					var domainName = new StringBuilder();
					SID_NAME_USE use;

					LookupAccountSid(null, structure.Sid, name, ref cchName, domainName, ref cchName2, out use);
					*/
					var str = ConvertSidToStringSid(structure.Sid);
					// var bytes = ConvertStringSidToSid(str);
					yield return str;

				}
			}
			finally
			{
				LsaFreeMemory(rightsPtr);
			}
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool LookupAccountName(
			string lpSystemName,
			string lpAccountName,
			[MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
			ref uint cbSid,
			StringBuilder ReferencedDomainName,
			ref uint cchReferencedDomainName,
			out SID_NAME_USE peUse);

		private enum SID_NAME_USE
		{
			SidTypeUser = 1,
			SidTypeGroup,
			SidTypeDomain,
			SidTypeAlias,
			SidTypeWellKnownGroup,
			SidTypeDeletedAccount,
			SidTypeInvalid,
			SidTypeUnknown,
			SidTypeComputer
		}

		[DllImport("advapi32", SetLastError = true)]
		private static extern bool LookupAccountSid(
			string lpSystemName,
			// [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
			IntPtr Sid,
			StringBuilder lpName,
			ref uint cchName,
			StringBuilder ReferencedDomainName,
			ref uint cchReferencedDomainName,
			out SID_NAME_USE peUse);

		[DllImport("advapi32", CharSet = CharSet.Auto)]
		static extern bool ConvertSidToStringSid(IntPtr pSid, out string strSid);

		[DllImport("advapi32")]
		static extern bool ConvertStringSidToSid(string StringSid, out IntPtr ptrSid);

		static string ConvertSidToStringSid(IntPtr pSid)
		{
			string str;
			ConvertSidToStringSid(pSid, out str);
			return str;
		}

		static IntPtr ConvertStringSidToSid(string sid)
		{
			IntPtr ptr;
			ConvertStringSidToSid(sid, out ptr);
			return ptr;
		}

		[DllImport("advapi32", SetLastError = true)]
		private static extern bool ConvertSidToStringSid(
			[MarshalAs(UnmanagedType.LPArray)] byte[] pSID,
			out IntPtr ptrSid);

		[StructLayout(LayoutKind.Sequential)]
		private struct LSA_ENUMERATION_INFORMATION
		{
			// [MarshalAs(UnmanagedType.LPArray)] public byte[] Sid;
			public IntPtr Sid;
		}

		[DllImport("advapi32")]
		private static extern int LsaRetrievePrivateData(
			IntPtr policyHandle,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (LSAStringMarshaler))] string KeyName,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (LSAStringMarshaler))] ref string
				PrivateData
			);

		[DllImport("advapi32")]
		private static extern int LsaStorePrivateData(
			IntPtr policyHandle,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (LSAStringMarshaler))] string KeyName,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (LSAStringMarshaler))] string PrivateData
			);

		[DllImport("advapi32")]
		private static extern int LsaClose(IntPtr ObjectHandle);

		[DllImport("advapi32")]
		private static extern int LsaFreeMemory(IntPtr pBuffer);

		[StructLayout(LayoutKind.Sequential)]
		private struct LSA_OBJECT_ATTRIBUTES
		{
			public int Length;
			public IntPtr RootDirectory;
			public LSA_UNICODE_STRING ObjectName;
			public UInt32 Attributes;
			public IntPtr SecurityDescriptor;
			public IntPtr SecurityQualityOfService;
		}

		[DllImport("advapi32", CharSet = CharSet.Unicode)]
		private static extern int LsaOpenPolicy(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (LSAStringMarshaler))] ref string
				SystemName,
			ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
			Int32 DesiredAccess,
			out IntPtr PolicyHandle
			);

		[DllImport("advapi32")]
		private static extern int LsaNtStatusToWinError(int status);

		[DllImport("advapi32", SetLastError = true)]
		[SuppressUnmanagedCodeSecurity]
		private static extern uint LsaEnumerateAccountRights(
			IntPtr PolicyHandle,
			[MarshalAs(UnmanagedType.LPArray)] byte[] AccountSid,
			out IntPtr UserRights,
			out int CountOfRights
			);

		[DllImport("advapi32", CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
		private static extern uint LsaEnumerateAccountsWithUserRight(
			IntPtr PolicyHandle,
			LSA_UNICODE_STRING[] UserRights,
			out IntPtr EnumerationBuffer,
			out int CountReturned);

		[DllImport("advapi32")]
		private static extern uint LsaRemoveAccountRights(
			IntPtr PolicyHandle,
			IntPtr AccountSid,
			[MarshalAs(UnmanagedType.U1)] bool AllRights,
			LSA_UNICODE_STRING[] UserRights,
			uint CountOfRights);

		[DllImport("advapi32")]
		static extern uint LsaAddAccountRights(
			IntPtr PolicyHandle,
			IntPtr AccountSid,
			LSA_UNICODE_STRING[] UserRights,
			uint CountOfRights);

		public void LsaAddAccountRights(string accountSid)
		{
			var ptr = ConvertStringSidToSid(accountSid);
			LsaAddAccountRights(ptr);
		}

		void LsaAddAccountRights(IntPtr AccountSid)
		{
			LsaAddAccountRights(AccountSid, new LSA_UNICODE_STRING(_seInteractiveLogonRight));
		}

		void LsaAddAccountRights(IntPtr AccountSid, LSA_UNICODE_STRING UserRight)
		{
			LsaAddAccountRights(_policy, AccountSid, new[] {UserRight}, 1);
		}
		
		public void LsaRemoveAccountRights(string accountSid)
		{
			var ptr = ConvertStringSidToSid(accountSid);
			LsaRemoveAccountRights(ptr);
		}

		void LsaRemoveAccountRights(IntPtr AccountSid)
		{
			LsaRemoveAccountRights(AccountSid, new LSA_UNICODE_STRING(_seInteractiveLogonRight));
		}

		void LsaRemoveAccountRights(IntPtr AccountSid, LSA_UNICODE_STRING UserRight)
		{
			LsaRemoveAccountRights(_policy, AccountSid, false, new[] { UserRight }, 1);
		}
	}
}