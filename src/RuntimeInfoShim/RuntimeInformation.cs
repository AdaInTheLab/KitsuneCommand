// Shim for System.Runtime.InteropServices.RuntimeInformation
// Provides the types SkiaSharp needs to detect platform and load native libraries.
// This is needed because Unity's Mono doesn't include this assembly.

using System;

namespace System.Runtime.InteropServices
{
    public static class RuntimeInformation
    {
        public static string FrameworkDescription => ".NET Framework 4.8 (Mono)";

        public static Architecture ProcessArchitecture => Architecture.X64;

        public static Architecture OSArchitecture => Architecture.X64;

        public static string OSDescription => "Microsoft Windows";

        public static bool IsOSPlatform(OSPlatform osPlatform)
        {
            return osPlatform == OSPlatform.Windows;
        }
    }

    public readonly struct OSPlatform : IEquatable<OSPlatform>
    {
        private readonly string _name;

        private OSPlatform(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static OSPlatform Linux { get; } = new OSPlatform("LINUX");
        public static OSPlatform OSX { get; } = new OSPlatform("OSX");
        public static OSPlatform Windows { get; } = new OSPlatform("WINDOWS");

        public static OSPlatform Create(string osPlatform)
        {
            return new OSPlatform(osPlatform);
        }

        public bool Equals(OSPlatform other)
        {
            return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is OSPlatform other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _name?.ToUpperInvariant().GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return _name ?? string.Empty;
        }

        public static bool operator ==(OSPlatform left, OSPlatform right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OSPlatform left, OSPlatform right)
        {
            return !left.Equals(right);
        }
    }

    public enum Architecture
    {
        X86 = 0,
        X64 = 1,
        Arm = 2,
        Arm64 = 3
    }
}
