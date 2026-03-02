using System.Runtime.InteropServices;
using KitsuneCommand.Configuration;

namespace KitsuneCommand.Core
{
    /// <summary>
    /// Main entry point for the KitsuneCommand mod.
    /// Implements the 7 Days to Die IModApi interface.
    /// </summary>
    public class ModEntry : IModApi
    {
        public static Mod ModInstance { get; private set; }
        public static string ModPath { get; private set; }
        public static SynchronizationContext MainThreadContext { get; private set; }
        public static bool IsGameStartDone { get; internal set; }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private ModLifecycle _lifecycle;

        public void InitMod(Mod _modInstance)
        {
            ModInstance = _modInstance;
            ModPath = _modInstance.Path;
            MainThreadContext = SynchronizationContext.Current;

            // Add mod's x64/ folder to DLL search path so Mono can find SQLite.Interop.dll
            var nativePath = Path.Combine(_modInstance.Path, "x64");
            if (Directory.Exists(nativePath))
            {
                SetDllDirectory(nativePath);
                Log.Out("[KitsuneCommand] Added native library path: " + nativePath);
            }

            Log.Out("[KitsuneCommand] Initializing KitsuneCommand v2.0.0...");

            _lifecycle = new ModLifecycle();
            _lifecycle.Initialize();

            // Restore default DLL search directory
            SetDllDirectory(null);

            Log.Out("[KitsuneCommand] Initialization complete. Web panel will be available after game start.");
        }
    }
}
