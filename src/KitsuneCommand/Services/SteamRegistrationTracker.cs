using KitsuneCommand.Abstractions.Models;
using KitsuneCommand.Core;

namespace KitsuneCommand.Services
{
    /// <summary>
    /// Watches the game's log for Steam master-server registration signals and exposes
    /// a best-effort reachability state. If Steam's GameServer.LogOn succeeded, the server
    /// is reachable from the outside (that's how Steam browses list it). If it failed -
    /// or the server is set to ServerVisibility=0 so it never even tries - the server is
    /// probably not reachable via normal discovery.
    ///
    /// This is a proxy signal, not a true active port check. For a hard port check, hit
    /// an external service on demand.
    /// </summary>
    public class SteamRegistrationTracker
    {
        private readonly ModEventBus _eventBus;

        /// <summary>True iff Steam's GameServer.LogOn has succeeded since process start.</summary>
        public bool IsRegistered { get; private set; }

        /// <summary>Last observed Steam server ID, if registered.</summary>
        public string SteamServerId { get; private set; }

        /// <summary>True iff EOS (Epic Online Services) registered the session.</summary>
        public bool IsEosRegistered { get; private set; }

        public SteamRegistrationTracker(ModEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<LogCallbackEvent>(OnLog);
        }

        private void OnLog(LogCallbackEvent e)
        {
            if (string.IsNullOrEmpty(e?.Message)) return;
            var msg = e.Message;

            // Steamworks: "[Steamworks.NET] GameServer.LogOn successful, SteamID=<id>, public IP=<ip>"
            if (msg.Contains("[Steamworks.NET] GameServer.LogOn successful"))
            {
                IsRegistered = true;
                // Best-effort parse of the SteamID=... fragment if present.
                var idMarker = "SteamID=";
                var i = msg.IndexOf(idMarker);
                if (i >= 0)
                {
                    var after = msg.Substring(i + idMarker.Length);
                    var end = after.IndexOf(',');
                    SteamServerId = (end >= 0 ? after.Substring(0, end) : after).Trim();
                }
                return;
            }

            // Steamworks failed cases - clear the flag.
            if (msg.Contains("[Steamworks.NET] GameServer.LogOff") ||
                msg.Contains("[Steamworks.NET] GameServer.LogOn failed") ||
                msg.Contains("SteamServersDisconnected"))
            {
                IsRegistered = false;
                SteamServerId = null;
                return;
            }

            // EOS: "[EOS] Server registered, session: <id>, <n> attributes"
            if (msg.Contains("[EOS] Server registered"))
            {
                IsEosRegistered = true;
                return;
            }

            if (msg.Contains("[EOS] Server unregistered") || msg.Contains("[EOS] Session destroyed"))
            {
                IsEosRegistered = false;
            }
        }
    }
}
