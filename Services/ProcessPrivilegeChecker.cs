using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WindowPlacementManager.Services;

public static class ProcessPrivilegeChecker
{
    const uint TOKEN_QUERY = 0x0008;
    const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    const int ERROR_ACCESS_DENIED = 5;

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1,
        TokenGroups,
        TokenPrivileges,
        TokenOwner,
        TokenPrimaryGroup,
        TokenDefaultDacl,
        TokenSource,
        TokenType,
        TokenImpersonationLevel,
        TokenStatistics,
        TokenRestrictedSids,
        TokenSessionId,
        TokenGroupsAndPrivileges,
        TokenSessionReference,
        TokenSandBoxInert,
        TokenAuditPolicy,
        TokenOrigin,
        TokenElevationType,
        TokenLinkedToken,
        TokenElevation,
        TokenHasRestrictions,
        TokenAccessInformation,
        TokenVirtualizationAllowed,
        TokenVirtualizationEnabled,
        TokenIntegrityLevel,
        TokenUIAccess,
        TokenMandatoryPolicy,
        TokenLogonSid,
        MaxTokenInfoClass
    }

    struct TOKEN_ELEVATION
    {
        public uint TokenIsElevated;
    }

    static bool? _isCurrentProcessElevatedCache = null;
    public static bool IsCurrentProcessElevated()
    {
        if(_isCurrentProcessElevatedCache == null)
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            _isCurrentProcessElevatedCache = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        return _isCurrentProcessElevatedCache.Value;
    }

    public static bool IsProcessElevated(int processId, out bool accessDeniedErrorOccurred)
    {
        accessDeniedErrorOccurred = false;

        if(Environment.OSVersion.Version.Major < 6)
        {
            return false;
        }

        IntPtr processHandle = IntPtr.Zero;
        IntPtr tokenHandle = IntPtr.Zero;

        try
        {
            processHandle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if(processHandle == IntPtr.Zero)
            {
                if(Marshal.GetLastWin32Error() == ERROR_ACCESS_DENIED)
                {
                    accessDeniedErrorOccurred = true;
                    return IsCurrentProcessElevated();
                }
                return false;
            }

            if(!OpenProcessToken(processHandle, TOKEN_QUERY, out tokenHandle))
            {
                if(Marshal.GetLastWin32Error() == ERROR_ACCESS_DENIED)
                {
                    accessDeniedErrorOccurred = true;
                    return IsCurrentProcessElevated();
                }
                return false;
            }

            TOKEN_ELEVATION elevation = new TOKEN_ELEVATION();
            uint elevationSize = (uint)Marshal.SizeOf(typeof(TOKEN_ELEVATION));
            IntPtr elevationPtr = Marshal.AllocHGlobal((int)elevationSize);

            try
            {
                if(!GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation, elevationPtr, elevationSize, out uint _))
                {
                    if(Marshal.GetLastWin32Error() == ERROR_ACCESS_DENIED)
                    {
                        accessDeniedErrorOccurred = true;
                        return IsCurrentProcessElevated();
                    }
                    return false;
                }
                elevation = (TOKEN_ELEVATION)Marshal.PtrToStructure(elevationPtr, typeof(TOKEN_ELEVATION));
                return elevation.TokenIsElevated != 0;
            }
            finally
            {
                Marshal.FreeHGlobal(elevationPtr);
            }
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Exception in IsProcessElevated for PID {processId}: {ex.Message}");
            return false;
        }
        finally
        {
            if(tokenHandle != IntPtr.Zero) CloseHandle(tokenHandle);
            if(processHandle != IntPtr.Zero) CloseHandle(processHandle);
        }
    }
}