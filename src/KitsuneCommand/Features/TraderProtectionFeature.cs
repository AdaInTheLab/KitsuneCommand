using KitsuneCommand.Configuration;
using KitsuneCommand.Core;
using KitsuneCommand.Data.Repositories;
using KitsuneCommand.GameIntegration;
using Newtonsoft.Json;

namespace KitsuneCommand.Features
{
    /// <summary>
    /// Trader zone protection toggle feature. Allows admins to temporarily disable
    /// trader area block protection for cleanup, then re-enable it.
    /// Settings are persisted to the database and can be changed live via API or console.
    /// </summary>
    public class TraderProtectionFeature : FeatureBase<TraderProtectionSettings>
    {
        private readonly ISettingsRepository _settingsRepo;
        private const string SettingsKey = "TraderProtection";

        public TraderProtectionFeature(
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
            ApplyToConfig();

            var status = Settings.ProtectionEnabled ? "ON (protected)" : "OFF (editable)";
            Log.Out($"[KitsuneCommand] Trader protection feature enabled. Protection={status}");
        }

        protected override void OnDisable()
        {
            TraderProtectionConfig.FeatureEnabled = false;
            TraderProtectionConfig.ProtectionEnabled = true;
        }

        /// <summary>
        /// Updates settings in memory, persists to database, and applies to the live Harmony patch.
        /// No server restart required.
        /// </summary>
        public void UpdateSettings(TraderProtectionSettings newSettings)
        {
            Settings = newSettings;
            ApplyToConfig();

            try
            {
                var json = JsonConvert.SerializeObject(newSettings);
                _settingsRepo.Set(SettingsKey, json);

                var status = newSettings.ProtectionEnabled ? "ON (protected)" : "OFF (editable)";
                Log.Out($"[KitsuneCommand] Trader protection updated. Protection={status}");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Failed to persist trader protection settings: {ex.Message}");
            }
        }

        private void ApplyToConfig()
        {
            TraderProtectionConfig.FeatureEnabled = Settings.Enabled;
            TraderProtectionConfig.ProtectionEnabled = Settings.ProtectionEnabled;
            TraderProtectionConfig.LogBypasses = Settings.LogBypasses;
        }

        private void LoadPersistedSettings()
        {
            try
            {
                var json = _settingsRepo.Get(SettingsKey);
                if (!string.IsNullOrEmpty(json))
                {
                    var loaded = JsonConvert.DeserializeObject<TraderProtectionSettings>(json);
                    if (loaded != null)
                    {
                        Settings = loaded;
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Failed to load trader protection settings, using defaults: {ex.Message}");
            }
        }
    }
}
