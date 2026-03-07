namespace KitsuneCommand.Core
{
    /// <summary>
    /// Simple OS detection for cross-platform native library loading.
    /// Uses Environment.OSVersion.Platform which is available in both .NET Framework and Mono.
    /// </summary>
    public static class PlatformHelper
    {
        public static bool IsLinux =>
            Environment.OSVersion.Platform == PlatformID.Unix;

        public static bool IsWindows =>
            Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static bool Is64Bit => IntPtr.Size == 8;
    }
}
