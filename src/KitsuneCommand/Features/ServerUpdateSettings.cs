namespace KitsuneCommand.Features
{
    /// <summary>
    /// Settings for the Linux server auto-update + sticky-config feature.
    ///
    /// Mirrors the Windows launch.bat pattern:
    ///   1. steamcmd update (on every startup, gated by AutoUpdate)
    ///   2. restore serverconfig.xml from serverconfig.xml.bak (the sticky copy)
    ///   3. rotate logs to keep the most recent N
    ///
    /// KC writes these settings to /home/ada/7d2d-server/kitsune-update.conf as shell
    /// key=value pairs. The ExecStartPre script sources that file on every boot.
    /// </summary>
    public class ServerUpdateSettings
    {
        /// <summary>Master toggle. When false, the pre-start script skips steamcmd entirely.</summary>
        public bool AutoUpdate { get; set; } = false;

        /// <summary>Steam beta branch to follow. Empty or "public" = default stable branch.</summary>
        public string Branch { get; set; } = "public";

        /// <summary>Password for gated branches. Empty for public/unlocked branches.</summary>
        public string BranchPassword { get; set; } = "";

        /// <summary>How many output_log__*.txt files to keep. Older ones are deleted.</summary>
        public int LogRetention { get; set; } = 20;

        /// <summary>Steam app ID. 251570 = 7 Days to Die (main game, used for Linux dedicated).</summary>
        public int SteamAppId { get; set; } = 251570;
    }
}
