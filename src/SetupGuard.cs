// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

[assembly: System.Reflection.AssemblyTitle("MD Show Setup Guard")]
[assembly: System.Reflection.AssemblyCompany("MONOLIT Agency LLC / ООО «Агентство «МОНОЛИТ»")]
[assembly: System.Reflection.AssemblyCopyright("Copyright (c) 2026 ООО «Агентство «МОНОЛИТ»")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace MdShow
{
    // Small separate helper: no WebView dependency, no process shutdown, no arbitrary deletion path.
    internal static class SetupGuard
    {
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmStartSession(out uint handle, int flags, StringBuilder key);
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmRegisterResources(uint handle, uint count, string[] files, uint applications, IntPtr processes, uint services, IntPtr names);
        [DllImport("rstrtmgr.dll")]
        private static extern int RmGetList(uint handle, out uint needed, ref uint count, IntPtr processes, ref uint reasons);
        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint handle);
        [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WTSQuerySessionInformation(IntPtr server, int session, int information, out IntPtr buffer, out uint bytes);
        [DllImport("wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr buffer);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr token, int information, out int value, int size, out int needed);
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int access, bool inherit, uint process);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr process, int access, out IntPtr token);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr existing, int access, IntPtr attributes, int level, int type, out IntPtr duplicate);
        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inherit);
        [DllImport("userenv.dll")]
        private static extern bool DestroyEnvironmentBlock(IntPtr environment);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            internal int Size;
            internal string Reserved, Desktop, Title;
            internal int X, Y, XSize, YSize, XChars, YChars, Fill, Flags;
            internal short Show, ReservedSize;
            internal IntPtr ReservedBytes, Input, Output, Error;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInfo { internal IntPtr Process, Thread; internal int ProcessId, ThreadId; }
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateProcessWithTokenW(IntPtr token, int logonFlags, string application, StringBuilder command,
            int creationFlags, IntPtr environment, string directory, ref StartupInfo startup, out ProcessInfo process);
        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        [DllImport("kernel32.dll")]
        private static extern bool GetExitCodeProcess(IntPtr process, out uint code);

        // 0 = clear; 1 = in use; 2 = unable to establish safety. Never treat API failure as clear.
        internal static int CheckApplication(string executable)
        {
            if (!Path.IsPathRooted(executable) || !String.Equals(Path.GetFileName(executable), "MDShow.exe", StringComparison.OrdinalIgnoreCase)) return 2;
            uint handle;
            int started = RmStartSession(out handle, 0, new StringBuilder(33));
            if (started != 0) { Console.Error.WriteLine("Restart Manager start error: " + started); return 2; }
            try
            {
                int registered = RmRegisterResources(handle, 1, new[] { Path.GetFullPath(executable) }, 0, IntPtr.Zero, 0, IntPtr.Zero);
                if (registered != 0) { Console.Error.WriteLine("Restart Manager register error: " + registered); return 2; }
                uint needed, count = 0, reasons = 0;
                int status = RmGetList(handle, out needed, ref count, IntPtr.Zero, ref reasons);
                if (status != 0 && status != 234) Console.Error.WriteLine("Restart Manager list error: " + status);
                return Classify(status, needed, reasons);
            }
            finally { RmEndSession(handle); }
        }

        internal static int Classify(int status, uint needed, uint reasons)
        {
            if (status != 0 && status != 234) return 2; // ERROR_MORE_DATA means at least one resource user.
            return status == 234 || needed != 0 || reasons != 0 ? 1 : 0;
        }

        private static string SessionText(int information)
        {
            IntPtr buffer;
            uint bytes;
            if (!WTSQuerySessionInformation(IntPtr.Zero, Process.GetCurrentProcess().SessionId, information, out buffer, out bytes))
                throw new IOException("Cannot identify the interactive account.");
            try { return Marshal.PtrToStringUni(buffer) ?? ""; }
            finally { WTSFreeMemory(buffer); }
        }

        internal static bool SameAccount(SecurityIdentifier tokenSid, SecurityIdentifier sessionSid)
        { return tokenSid != null && sessionSid != null && tokenSid.Equals(sessionSid); }

        private static int CleanInteractiveProfile(bool userProcess)
        {
            string user = SessionText(5), domain = SessionText(7);
            if (String.IsNullOrEmpty(user)) throw new IOException("No interactive account.");
            var sessionSid = (SecurityIdentifier)new NTAccount(domain, user).Translate(typeof(SecurityIdentifier));
            using (var identity = WindowsIdentity.GetCurrent())
            {
                if (!SameAccount(identity.User, sessionSid)) return 3; // Different UAC identity: retain every profile.
                string directory = Profile.DirectoryPath;
                if (!Directory.Exists(directory)) return 0;
                if (userProcess)
                {
                    if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return 2;
                    Profile.DeleteInstalledData(directory);
                    return 0;
                }
                // Elevated deletion of a user-writable tree could follow a raced junction.
                // Use the desktop's primary token: TokenLinkedToken may only provide
                // an identification token. Never impersonate inside the CLR.
                uint shellId;
                if (GetWindowThreadProcessId(GetShellWindow(), out shellId) == 0) return 2;
                IntPtr shell = OpenProcess(0x1000, false, shellId); // PROCESS_QUERY_LIMITED_INFORMATION
                if (shell == IntPtr.Zero) return 2;
                IntPtr shellToken;
                try
                {
                    // TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY
                    if (!OpenProcessToken(shell, 0x000B, out shellToken)) return 2;
                }
                finally { CloseHandle(shell); }
                try
                {
                    using (var restricted = new WindowsIdentity(shellToken))
                    {
                        int tokenSession, needed;
                        if (!SameAccount(identity.User, restricted.User) || new WindowsPrincipal(restricted).IsInRole(WindowsBuiltInRole.Administrator))
                            throw new IOException("Cannot safely drop administrator privileges.");
                        if (!GetTokenInformation(shellToken, 12, out tokenSession, sizeof(int), out needed) ||
                            tokenSession != Process.GetCurrentProcess().SessionId) return 2;
                        string executable = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        var startup = new StartupInfo { Size = Marshal.SizeOf(typeof(StartupInfo)), Flags = 1 };
                        ProcessInfo process;
                        IntPtr primary;
                        // MAXIMUM_ALLOWED opens the duplicate with the rights needed by
                        // process creation; it does not enable privileges in the token.
                        if (!DuplicateTokenEx(shellToken, 0x02000000, IntPtr.Zero, 2, 1, out primary)) return 2;
                        try
                        {
                            IntPtr environment;
                            if (!CreateEnvironmentBlock(out environment, primary, false)) return 2;
                            try
                            {
                                // CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT; no inherited elevated environment.
                                if (!CreateProcessWithTokenW(primary, 0, executable, new StringBuilder("\"" + executable + "\" --clean-profile-user"),
                                    0x08000400, environment, Environment.SystemDirectory, ref startup, out process)) return 2;
                            }
                            finally { DestroyEnvironmentBlock(environment); }
                        }
                        finally { CloseHandle(primary); }
                        try
                        {
                            // Timeout aborts common-file removal; never terminate a child.
                            if (WaitForSingleObject(process.Process, 60000) != 0) return 2;
                            uint code;
                            return GetExitCodeProcess(process.Process, out code) ? (int)code : 2;
                        }
                        finally { CloseHandle(process.Thread); CloseHandle(process.Process); }
                    }
                }
                finally { CloseHandle(shellToken); }
            }
        }

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 2 && args[0] == "--check-app") return CheckApplication(args[1]);
                if (args.Length == 1 && args[0] == "--clean-profile") return CleanInteractiveProfile(false);
                if (args.Length == 1 && args[0] == "--clean-profile-user") return CleanInteractiveProfile(true);
                return 2;
            }
            catch (Exception) { return 2; } // Caller displays a localized fail-safe message; no private paths logged.
        }
    }
}
