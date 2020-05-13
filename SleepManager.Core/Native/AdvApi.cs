using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

internal static class Advapi32
{
	private struct WTS_SESSION_INFO
	{
		public int SessionID;

		[MarshalAs(UnmanagedType.LPStr)]
		public string pWinStationName;

		public WTS_CONNECTSTATE_CLASS State;
	}

	public enum WTS_CONNECTSTATE_CLASS
	{
		WTSActive,
		WTSConnected,
		WTSConnectQuery,
		WTSShadow,
		WTSDisconnected,
		WTSIdle,
		WTSListen,
		WTSReset,
		WTSDown,
		WTSInit
	}

	public enum TOKEN_TYPE
	{
		TokenPrimary = 1,
		TokenImpersonation
	}

	public struct SECURITY_ATTRIBUTES
	{
		public int Length;

		public IntPtr SecurityDescriptor;

		public int InheritHandle;
	}

	public enum SECURITY_IMPERSONATION_LEVEL
	{
		SecurityAnonymous,
		SecurityIdentification,
		SecurityImpersonation,
		SecurityDelegation
	}

	[Flags]
	public enum DesiredAccess : uint
	{
		STANDARD_RIGHTS_REQUIRED = 0xF0000,
		STANDARD_RIGHTS_READ = 0x20000,
		TOKEN_ASSIGN_PRIMARY = 0x1,
		TOKEN_DUPLICATE = 0x2,
		TOKEN_IMPERSONATE = 0x4,
		TOKEN_QUERY = 0x8,
		TOKEN_QUERY_SOURCE = 0x10,
		TOKEN_ADJUST_PRIVILEGES = 0x20,
		TOKEN_ADJUST_GROUPS = 0x40,
		TOKEN_ADJUST_DEFAULT = 0x80,
		TOKEN_ADJUST_SESSIONID = 0x100,
		TOKEN_READ = 0x20008,
		TOKEN_ALL_ACCESS = 0xF01FF
	}

	public struct PROCESS_INFORMATION
	{
		public IntPtr hProcess;

		public IntPtr hThread;

		public int dwProcessId;

		public int dwThreadId;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct STARTUPINFO
	{
		public int cb;

		public string lpReserved;

		public string lpDesktop;

		public string lpTitle;

		public int dwX;

		public int dwY;

		public int dwXSize;

		public int dwYSize;

		public int dwXCountChars;

		public int dwYCountChars;

		public int dwFillAttribute;

		public int dwFlags;

		public short wShowWindow;

		public short cbReserved2;

		public IntPtr lpReserved2;

		public IntPtr hStdInput;

		public IntPtr hStdOutput;

		public IntPtr hStdError;
	}

	public enum LogonFlags
	{
		WithProfile = 1,
		NetCredentialsOnly
	}

	[Flags]
	public enum CreationFlags : uint
	{
		DEBUG_PROCESS = 0x1,
		DEBUG_ONLY_THIS_PROCESS = 0x2,
		CREATE_SUSPENDED = 0x4,
		DETACHED_PROCESS = 0x8,
		CREATE_NEW_CONSOLE = 0x10,
		NORMAL_PRIORITY_CLASS = 0x20,
		IDLE_PRIORITY_CLASS = 0x40,
		HIGH_PRIORITY_CLASS = 0x80,
		REALTIME_PRIORITY_CLASS = 0x100,
		CREATE_NEW_PROCESS_GROUP = 0x200,
		CREATE_UNICODE_ENVIRONMENT = 0x400,
		CREATE_SEPARATE_WOW_VDM = 0x800,
		CREATE_SHARED_WOW_VDM = 0x1000,
		CREATE_FORCEDOS = 0x2000,
		BELOW_NORMAL_PRIORITY_CLASS = 0x4000,
		ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,
		INHERIT_PARENT_AFFINITY = 0x10000,
		INHERIT_CALLER_PRIORITY = 0x20000,
		CREATE_PROTECTED_PROCESS = 0x40000,
		EXTENDED_STARTUPINFO_PRESENT = 0x80000,
		PROCESS_MODE_BACKGROUND_BEGIN = 0x100000,
		PROCESS_MODE_BACKGROUND_END = 0x200000,
		CREATE_BREAKAWAY_FROM_JOB = 0x1000000,
		CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x2000000,
		CREATE_DEFAULT_ERROR_MODE = 0x4000000,
		CREATE_NO_WINDOW = 0x8000000,
		PROFILE_USER = 0x10000000,
		PROFILE_KERNEL = 0x20000000,
		PROFILE_SERVER = 0x40000000,
		CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000
	}

	private const uint STANDARD_RIGHTS_REQUIRED = 983040u;

	private const uint STANDARD_RIGHTS_READ = 131072u;

	private const uint TOKEN_ASSIGN_PRIMARY = 1u;

	private const uint TOKEN_DUPLICATE = 2u;

	private const uint TOKEN_IMPERSONATE = 4u;

	private const uint TOKEN_QUERY = 8u;

	private const uint TOKEN_QUERY_SOURCE = 16u;

	private const uint TOKEN_ADJUST_PRIVILEGES = 32u;

	private const uint TOKEN_ADJUST_GROUPS = 64u;

	private const uint TOKEN_ADJUST_DEFAULT = 128u;

	private const uint TOKEN_ADJUST_SESSIONID = 256u;

	private const uint TOKEN_READ = 131080u;

	public const uint TOKEN_ALL_ACCESS = 983551u;

	public const uint CREATE_NO_WINDOW = 134217728u;

	public const uint CREATE_UNICODE_ENVIRONMENT = 1024u;

	public const uint NORMAL_PRIORITY_CLASS = 32u;

	[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DeleteFile(string name);

	public static bool Unblock(string fileName)
	{
		return DeleteFile(fileName + ":Zone.Identifier");
	}

	[DllImport("wtsapi32.dll", SetLastError = true)]
	private static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

	private static int WTSEnumerateSessions(ref IntPtr ppSessionInfo, ref int pCount)
	{
		return WTSEnumerateSessions((IntPtr)0, 0, 1, ref ppSessionInfo, ref pCount);
	}

	[DllImport("wtsapi32.dll")]
	private static extern void WTSFreeMemory(IntPtr pMemory);

	[DllImport("wtsapi32.dll", SetLastError = true)]
	public static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

	[DllImport("wtsapi32.dll", SetLastError = true)]
	public static extern bool WTSQueryUserToken(ulong sessionId, out IntPtr Token);

	public static IEnumerable<Tuple<int, WTS_CONNECTSTATE_CLASS, string>> GetSessions()
	{
		var ppSessionInfo = IntPtr.Zero;
		var pCount = 0;
		var num = WTSEnumerateSessions(ref ppSessionInfo, ref pCount);
		var num2 = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
		var num3 = (long)ppSessionInfo;
		var list = new List<Tuple<int, WTS_CONNECTSTATE_CLASS, string>>();
		if (num != 0)
		{
			for (var i = 0; i < pCount; i++)
			{
				var wTS_SESSION_INFO = (WTS_SESSION_INFO)Marshal.PtrToStructure((IntPtr)num3, typeof(WTS_SESSION_INFO));
				num3 += num2;
				var state = wTS_SESSION_INFO.State;
				if ((uint)state <= 1u)
				{
					list.Add(Tuple.Create(wTS_SESSION_INFO.SessionID, wTS_SESSION_INFO.State, wTS_SESSION_INFO.pWinStationName));
				}
			}
		}
		WTSFreeMemory(ppSessionInfo);
		return list;
	}

	[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool DuplicateTokenEx(IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes, SECURITY_IMPERSONATION_LEVEL impersonationLevel, TOKEN_TYPE tokenType, out IntPtr newToken);

	public static IntPtr DuplicateTokenEx(IntPtr existingToken)
	{
		if (DuplicateTokenEx(existingToken, 983551u, (IntPtr)0, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, out IntPtr newToken))
		{
			return newToken;
		}
		return (IntPtr)0;
	}

	[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CreateProcessAsUser(IntPtr token, string applicationName, string commandLine, ref SECURITY_ATTRIBUTES processAttributes, ref SECURITY_ATTRIBUTES threadAttributes, bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

	[DllImport("userenv.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

	[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern bool CreateProcessWithTokenW(IntPtr hToken, LogonFlags dwLogonFlags, string lpApplicationName, string lpCommandLine, CreationFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

	[DllImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool OpenProcessToken(IntPtr processHandle, DesiredAccess desiredAccess, out IntPtr tokenHandle);
}
