using System.IO;
using System.Text;
using KitsuneCommand.Configuration;
using KitsuneCommand.Core;
using KitsuneCommand.Data.Repositories;
using Newtonsoft.Json;

namespace KitsuneCommand.Features
{
    /// <summary>
    /// Linux server update + sticky-config feature. Writes settings to a shell-readable
    /// conf file that kitsune-pre-start.sh (installed via scripts/install-linux-updater.sh)
    /// sources on every systemd ExecStartPre.
    ///
    /// KC itself never runs steamcmd — that has to happen between systemd stop and start,
    /// not inside the game process.
    /// </summary>
    public class ServerUpdateFeature : FeatureBase<ServerUpdateSettings>
    {
        private readonly ISettingsRepository _settingsRepo;
        private const string SettingsKey = "ServerUpdate";

        // Conf file lives in the server install dir so the pre-start script can find it
        // with a relative path. Overridden in tests via SetConfPath.
        private static string _confPath = "kitsune-update.conf";
        // Sticky config backup — mirrors the Windows launch.bat's serverconfig.xml.bak pattern.
        private static string _serverConfigBakPath = "serverconfig.xml.bak";

        public ServerUpdateFeature(
            ModEventBus eventBus,
            ConfigManager config,
            ISettingsRepository settingsRepo)
            : base(eventBus, config)
        {
            _settingsRepo = settingsRepo;
        }

        protected override void OnEnable()
        {
            LoadPersistedSettings();
            WriteConfFile();
            Log.Out($"[KitsuneCommand] ServerUpdate feature enabled. AutoUpdate={Settings.AutoUpdate}, Branch={Settings.Branch}, LogRetention={Settings.LogRetention}.");
        }

        protected override void OnDisable()
        {
            // Leave the conf file on disk — feature disabled just means KC isn't managing
            // it anymore, not that we should nuke pre-start behavior.
        }

        public void UpdateSettings(ServerUpdateSettings newSettings)
        {
            Settings = newSettings;
            WriteConfFile();

            try
            {
                var json = JsonConvert.SerializeObject(newSettings);
                _settingsRepo.Set(SettingsKey, json);
                Log.Out($"[KitsuneCommand] ServerUpdate settings saved. AutoUpdate={newSettings.AutoUpdate}, Branch={newSettings.Branch}.");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Failed to persist ServerUpdate settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Read the sticky-config file (serverconfig.xml.bak) contents for editing.
        /// Returns null if the file doesn't exist yet.
        /// </summary>
        public string GetServerConfigBak()
        {
            try
            {
                if (!File.Exists(_serverConfigBakPath)) return null;
                return File.ReadAllText(_serverConfigBakPath, Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Failed to read {_serverConfigBakPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Write the sticky-config file. This is what gets restored over serverconfig.xml
        /// on every server start (after steamcmd's validate clobbers the live copy).
        /// </summary>
        public bool SetServerConfigBak(string xml)
        {
            try
            {
                File.WriteAllText(_serverConfigBakPath, xml, new UTF8Encoding(false));
                Log.Out($"[KitsuneCommand] Wrote {_serverConfigBakPath} ({xml.Length} chars). Will be restored on next server start.");
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[KitsuneCommand] Failed to write {_serverConfigBakPath}: {ex.Message}");
                return false;
            }
        }

        private void WriteConfFile()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Managed by KitsuneCommand ServerUpdateFeature - do not edit directly.");
                sb.AppendLine("# Edit via the web UI; changes here are overwritten on next settings save.");
                sb.AppendLine($"AutoUpdate={(Settings.AutoUpdate ? "true" : "false")}");
                sb.AppendLine($"Branch={Settings.Branch ?? ""}");
                sb.AppendLine($"BranchPassword={Settings.BranchPassword ?? ""}");
                sb.AppendLine($"LogRetention={Settings.LogRetention}");
                sb.AppendLine($"SteamAppId={Settings.SteamAppId}");
                File.WriteAllText(_confPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Failed to write {_confPath}: {ex.Message}");
            }
        }

        private void LoadPersistedSettings()
        {
            try
            {
                var json = _settingsRepo.Get(SettingsKey);
                if (!string.IsNullOrEmpty(json))
                {
                    var loaded = JsonConvert.DeserializeObject<ServerUpdateSettings>(json);
                    if (loaded != null)
                    {
                        Settings = loaded;
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Failed to load ServerUpdate settings, using defaults: {ex.Message}");
            }
        }
    }
}
