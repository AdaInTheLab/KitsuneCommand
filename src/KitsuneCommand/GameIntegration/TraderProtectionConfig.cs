namespace KitsuneCommand.GameIntegration
{
    /// <summary>
    /// Static configuration for trader zone protection toggling.
    /// Read by TraderProtectionPatch (Harmony), written by TraderProtectionFeature.
    /// </summary>
    public static class TraderProtectionConfig
    {
        /// <summary>When true, the feature is active and will respect the ProtectionEnabled flag.</summary>
        public static volatile bool FeatureEnabled = true;

        /// <summary>When false, trader area protection is bypassed — blocks can be placed/destroyed in trader zones.</summary>
        public static volatile bool ProtectionEnabled = true;

        /// <summary>Log when protection checks are bypassed.</summary>
        public static volatile bool LogBypasses = false;
    }
}
