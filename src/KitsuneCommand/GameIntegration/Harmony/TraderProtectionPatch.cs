using HarmonyLib;

namespace KitsuneCommand.GameIntegration.Harmony
{
    /// <summary>
    /// Harmony patches on World.IsWithinTraderPlacingProtection to allow toggling
    /// trader zone block protection on/off at runtime.
    ///
    /// When TraderProtectionConfig.ProtectionEnabled is false, these methods return false
    /// (position is NOT within protection), allowing block placement and destruction
    /// inside trader compounds.
    /// </summary>
    [HarmonyPatch(typeof(World))]
    public static class TraderProtectionPatch
    {
        /// <summary>
        /// Patch for IsWithinTraderPlacingProtection(Vector3i) — single block position check.
        /// </summary>
        [HarmonyPatch("IsWithinTraderPlacingProtection", typeof(Vector3i))]
        [HarmonyPrefix]
        public static bool PrefixSinglePos(ref bool __result, Vector3i _worldBlockPos)
        {
            if (!TraderProtectionConfig.FeatureEnabled) return true;
            if (TraderProtectionConfig.ProtectionEnabled) return true;

            if (TraderProtectionConfig.LogBypasses)
            {
                Log.Out($"[KitsuneCommand] Trader protection bypassed at {_worldBlockPos}");
            }

            __result = false;
            return false; // Skip original method
        }

        /// <summary>
        /// Patch for IsWithinTraderPlacingProtection(Bounds) — bounds-based check.
        /// </summary>
        [HarmonyPatch("IsWithinTraderPlacingProtection", typeof(UnityEngine.Bounds))]
        [HarmonyPrefix]
        public static bool PrefixBounds(ref bool __result, UnityEngine.Bounds _bounds)
        {
            if (!TraderProtectionConfig.FeatureEnabled) return true;
            if (TraderProtectionConfig.ProtectionEnabled) return true;

            if (TraderProtectionConfig.LogBypasses)
            {
                Log.Out($"[KitsuneCommand] Trader protection bypassed for bounds at {_bounds.center}");
            }

            __result = false;
            return false;
        }
    }
}
