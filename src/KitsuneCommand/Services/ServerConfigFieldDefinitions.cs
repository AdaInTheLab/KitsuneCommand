using System.Collections.Generic;

namespace KitsuneCommand.Services
{
    /// <summary>
    /// Defines all known serverconfig.xml fields with metadata for the config editor UI.
    /// Covers all vanilla 7D2D V2 server settings.
    /// </summary>
    public static class ServerConfigFieldDefinitions
    {
        public static List<ConfigFieldGroup> GetGroups()
        {
            return new List<ConfigFieldGroup>
            {
                new ConfigFieldGroup
                {
                    Key = "core",
                    Fields = new List<ConfigFieldDef>
                    {
                        TextField("ServerName", "My 7D2D Server", "Display name shown in the server browser"),
                        PasswordField("ServerPassword", "", "Password required to join the server (blank = no password)"),
                        TextField("ServerDescription", "A 7 Days to Die Server", "Description shown in the server browser listing"),
                        TextField("ServerLoginConfirmationText", "", "Message players must accept before joining"),
                        TextField("ServerWebsiteURL", "", "URL shown in the server browser as a clickable link"),
                        SelectField("Region", "NorthAmericaEast", new[] { "NorthAmericaEast", "NorthAmericaWest", "CentralAmerica", "SouthAmerica", "Europe", "Russia", "Asia", "MiddleEast", "Africa", "Oceania" }, description: "Server region for browser filtering"),
                        TextField("Language", "English", "Primary language for this server"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "world",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("GameWorld", "Navezgane", new[] { "Navezgane", "RWG" }, description: "Navezgane = fixed map, RWG = random generated"),
                        TextField("WorldGenSeed", "SomeSeed", "Seed string used for random world generation"),
                        SelectField("WorldGenSize", "6144", new[] { "6144", "8192", "10240" }, description: "RWG world size in blocks"),
                        TextField("GameName", "My Game", "Save game name — changing this starts a new save"),
                        SelectField("GameMode", "GameModeSurvival", new[] { "GameModeSurvival" }, description: "Game mode for the server"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "blockDamage",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("BlockDamagePlayer", "100", DamagePercentOptions(), description: "Player block damage multiplier (%)"),
                        SelectField("BlockDamageAI", "100", DamagePercentOptions(), description: "Zombie block damage multiplier (%)"),
                        SelectField("BlockDamageAIBM", "100", DamagePercentOptions(), description: "Blood moon zombie block damage multiplier (%)"),
                        SelectField("XPMultiplier", "100", DamagePercentOptions(), description: "XP gain multiplier (%)"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "gameplay",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("GameDifficulty", "2", new[] { "0", "1", "2", "3", "4", "5" }, new[] { "0 - Scavenger", "1 - Adventurer", "2 - Nomad", "3 - Warrior", "4 - Survivalist", "5 - Insane" }, "Overall difficulty"),
                        NumberField("DayNightLength", "60", 10, 240, "Real-time minutes per in-game 24h cycle"),
                        NumberField("DayLightLength", "18", 1, 23, "In-game hours of daylight per day"),
                        SelectField("PlayerKillingMode", "3", new[] { "0", "1", "2", "3" }, new[] { "0 - No Killing", "1 - Kill Allies Only", "2 - Kill Strangers Only", "3 - Kill Everyone" }, "PvP rules"),
                        SelectField("DeathPenalty", "1", new[] { "0", "1", "2", "3" }, new[] { "0 - Nothing", "1 - Classic XP Penalty", "2 - Injured", "3 - Permanent Death" }, "Penalty after dying"),
                        SelectField("DropOnDeath", "1", new[] { "0", "1", "2", "3", "4" }, new[] { "0 - Nothing", "1 - Everything", "2 - Toolbelt Only", "3 - Backpack Only", "4 - Delete All" }, "What drops when killed"),
                        SelectField("DropOnQuit", "1", new[] { "0", "1", "2", "3" }, new[] { "0 - Nothing", "1 - Everything", "2 - Toolbelt Only", "3 - Backpack Only" }, "What drops when disconnecting"),
                        NumberField("QuestProgressionDailyLimit", "3", 0, 100, "Max quest-tier progressions per day"),
                        SelectField("JarRefund", "0", new[] { "0", "5", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" }, description: "Empty jar refund percentage after consuming"),
                        BoolField("BiomeProgression", "true", "Zombies get harder in further biomes"),
                        NumberField("StormFreq", "100", 0, 500, "Weather storm frequency (%)"),
                        BoolField("BuildCreate", "false", "Cheat/creative mode"),
                        NumberField("PlayerSafeZoneLevel", "5", 0, 100, "Players at or below this level create a safe zone on spawn"),
                        NumberField("PlayerSafeZoneHours", "5", 0, 100, "Hours the safe zone exists"),
                        SelectField("AllowSpawnNearFriend", "2", new[] { "0", "1", "2" }, new[] { "0 - Disabled", "1 - Always", "2 - Forest Biome Only" }, "New players can spawn near friends"),
                        SelectField("CameraRestrictionMode", "0", new[] { "0", "1", "2" }, new[] { "0 - Free", "1 - First Person Only", "2 - Third Person Only" }, "Camera mode restriction"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "zombies",
                    Fields = new List<ConfigFieldDef>
                    {
                        BoolField("EnemySpawnMode", "true", "Enable/disable enemy spawning"),
                        SelectField("EnemyDifficulty", "0", new[] { "0", "1" }, new[] { "0 - Normal", "1 - Feral" }, "Feral adds more challenging zombie types"),
                        SelectField("ZombieFeralSense", "0", new[] { "0", "1", "2", "3" }, new[] { "0 - Off", "1 - Day", "2 - Night", "3 - All" }, "When ferals can sense players"),
                        SelectField("ZombieMove", "0", ZombieMoveOptions(), ZombieMoveLabelOptions(), "Daytime zombie speed"),
                        SelectField("ZombieMoveNight", "3", ZombieMoveOptions(), ZombieMoveLabelOptions(), "Nighttime zombie speed"),
                        SelectField("ZombieFeralMove", "3", ZombieMoveOptions(), ZombieMoveLabelOptions(), "Feral zombie speed"),
                        SelectField("ZombieBMMove", "3", ZombieMoveOptions(), ZombieMoveLabelOptions(), "Blood moon zombie speed"),
                        SelectField("AISmellMode", "3", new[] { "0", "1", "2", "3", "4", "5" }, new[] { "0 - Off", "1 - Walk", "2 - Jog", "3 - Run", "4 - Sprint", "5 - Nightmare" }, "Zombie smell-tracking speed"),
                        NumberField("MaxSpawnedZombies", "64", 1, 500, "Max alive zombies at once"),
                        NumberField("MaxSpawnedAnimals", "50", 1, 500, "Max alive animals at once"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "bloodMoon",
                    Fields = new List<ConfigFieldDef>
                    {
                        NumberField("BloodMoonFrequency", "7", 0, 100, "Blood moon every N days (0 = disabled)"),
                        NumberField("BloodMoonRange", "0", 0, 7, "Random +/- days deviation (0 = exact)"),
                        NumberField("BloodMoonWarning", "8", -1, 22, "Hour the red day number appears (-1 = never)"),
                        NumberField("BloodMoonEnemyCount", "8", 1, 64, "Max zombies per player during blood moon"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "lootAndDrops",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("LootAbundance", "100", DamagePercentOptions(), description: "Loot quantity multiplier (%)"),
                        NumberField("LootRespawnDays", "7", 1, 100, "Days before looted containers respawn"),
                        NumberField("AirDropFrequency", "72", 0, 999, "Air drop interval in in-game hours (0 = disabled)"),
                        BoolField("AirDropMarker", "true", "Show marker on map/compass for air drops"),
                        NumberField("PartySharedKillRange", "100", 0, 10000, "Distance for shared party XP and quest credit"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "landClaims",
                    Fields = new List<ConfigFieldDef>
                    {
                        NumberField("LandClaimCount", "3", 1, 50, "Maximum land claims per player"),
                        NumberField("LandClaimSize", "41", 1, 200, "Protected area size in blocks"),
                        NumberField("LandClaimDeadZone", "30", 0, 200, "Minimum distance between land claims"),
                        NumberField("LandClaimExpiryTime", "7", 1, 365, "Days before an unvisited claim expires"),
                        SelectField("LandClaimDecayMode", "0", new[] { "0", "1", "2" }, new[] { "0 - Slow (Linear)", "1 - Fast (Exponential)", "2 - None (Full Protection)" }, "How claim protection decays"),
                        NumberField("LandClaimOnlineDurabilityModifier", "4", 0, 100, "Block hardness multiplier when owner is online"),
                        NumberField("LandClaimOfflineDurabilityModifier", "4", 0, 100, "Block hardness multiplier when owner is offline"),
                        NumberField("LandClaimOfflineDelay", "0", 0, 1440, "Minutes after logout before transitioning to offline protection"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "networkAndSlots",
                    Fields = new List<ConfigFieldDef>
                    {
                        NumberField("ServerPort", "26900", 1024, 65535, "Main game port (also uses +1 and +2)"),
                        SelectField("ServerVisibility", "2", new[] { "0", "1", "2" }, new[] { "0 - Not Listed", "1 - Friends Only", "2 - Public" }, "Server browser visibility"),
                        NumberField("ServerMaxPlayerCount", "8", 1, 64, "Maximum concurrent players"),
                        NumberField("ServerReservedSlots", "0", 0, 10, "Reserved slots for permissioned players"),
                        NumberField("ServerReservedSlotsPermission", "100", 0, 1000, "Permission level required for reserved slots"),
                        NumberField("ServerAdminSlots", "0", 0, 10, "Extra admin-only slots above max"),
                        NumberField("ServerAdminSlotsPermission", "0", 0, 1000, "Permission level required for admin slots"),
                        NumberField("ServerMaxWorldTransferSpeedKiBs", "512", 64, 10240, "Max world transfer speed in KiB/s"),
                        TextField("ServerDisabledNetworkProtocols", "", "Protocols to disable (e.g. SteamNetworking)"),
                        NumberField("ServerMaxAllowedViewDistance", "12", 6, 12, "Max view distance clients can request"),
                        NumberField("MaxQueuedMeshLayers", "1000", 100, 10000, "Max chunk mesh layers in queue"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "admin",
                    Fields = new List<ConfigFieldDef>
                    {
                        BoolField("TelnetEnabled", "true", "Enable the telnet remote console"),
                        NumberField("TelnetPort", "8081", 1024, 65535, "Telnet port"),
                        PasswordField("TelnetPassword", "", "Telnet access password"),
                        NumberField("TelnetFailedLoginLimit", "10", 0, 100, "Failed logins before temporary ban"),
                        NumberField("TelnetFailedLoginsBlocktime", "10", 0, 3600, "Block duration after failed logins (seconds)"),
                        BoolField("EACEnabled", "true", "Easy Anti-Cheat (disable for modded clients)"),
                        BoolField("ServerAllowCrossplay", "false", "Enable crossplay support"),
                        BoolField("IgnoreEOSSanctions", "false", "Ignore EOS sanctions when allowing players"),
                        SelectField("HideCommandExecutionLog", "0", new[] { "0", "1", "2", "3" }, new[] { "0 - Show All", "1 - Hide from Telnet", "2 - Hide from Clients", "3 - Hide Everything" }, "Admin command log visibility"),
                        BoolField("PersistentPlayerProfiles", "false", "Lock players to their last-used profile"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "advanced",
                    Fields = new List<ConfigFieldDef>
                    {
                        BoolField("TerminalWindowEnabled", "true", "Show the terminal window (Windows only)"),
                        BoolField("WebDashboardEnabled", "false", "Enable the built-in 7D2D web dashboard"),
                        NumberField("WebDashboardPort", "8080", 1024, 65535, "Built-in dashboard port"),
                        TextField("WebDashboardUrl", "", "External URL for reverse proxy setups"),
                        BoolField("EnableMapRendering", "false", "Render map tiles while exploring (used by web dashboard)"),
                        NumberField("MaxChunkAge", "-1", -1, 9999, "Days before unvisited chunks reset (-1 = never)"),
                        NumberField("SaveDataLimit", "-1", -1, 100000, "Max save game disk space in MB (-1 = unlimited)"),
                        NumberField("BedrollExpiryTime", "45", 1, 365, "Real-world days before a bedroll expires"),
                        NumberField("BedrollDeadZoneSize", "15", 0, 100, "Bedroll safe zone radius in blocks"),
                        NumberField("MaxUncoveredMapChunksPerPlayer", "131072", 0, 1000000, "Max map chunks revealed per player"),
                        BoolField("DynamicMeshEnabled", "true", "Enable the dynamic mesh system"),
                        BoolField("DynamicMeshLandClaimOnly", "true", "Dynamic mesh only in land claim areas"),
                        NumberField("DynamicMeshLandClaimBuffer", "3", 0, 10, "Dynamic mesh LCB chunk radius"),
                        NumberField("DynamicMeshMaxItemCache", "3", 1, 20, "Concurrent mesh items to process"),
                        NumberField("TwitchServerPermission", "90", 0, 1000, "Permission level for Twitch integration"),
                        BoolField("TwitchBloodMoonAllowed", "false", "Allow Twitch actions during blood moon"),
                    }
                },
            };
        }

        private static ConfigFieldDef TextField(string key, string defaultValue, string description = null)
            => new ConfigFieldDef { Key = key, Type = "text", DefaultValue = defaultValue, Description = description };

        private static ConfigFieldDef PasswordField(string key, string defaultValue, string description = null)
            => new ConfigFieldDef { Key = key, Type = "password", DefaultValue = defaultValue, Description = description };

        private static ConfigFieldDef NumberField(string key, string defaultValue, int min, int max, string description = null)
            => new ConfigFieldDef { Key = key, Type = "number", DefaultValue = defaultValue, Min = min, Max = max, Description = description };

        private static ConfigFieldDef BoolField(string key, string defaultValue, string description = null)
            => new ConfigFieldDef { Key = key, Type = "bool", DefaultValue = defaultValue, Description = description };

        private static ConfigFieldDef SelectField(string key, string defaultValue, string[] options, string[] labels = null, string description = null)
            => new ConfigFieldDef { Key = key, Type = "select", DefaultValue = defaultValue, Options = options, Labels = labels, Description = description };

        private static string[] DamagePercentOptions()
            => new[] { "25", "50", "75", "100", "125", "150", "175", "200", "300" };

        private static string[] ZombieMoveOptions()
            => new[] { "0", "1", "2", "3", "4" };

        private static string[] ZombieMoveLabelOptions()
            => new[] { "0 - Walk", "1 - Jog", "2 - Run", "3 - Sprint", "4 - Nightmare" };
    }

    public class ConfigFieldGroup
    {
        public string Key { get; set; }
        public List<ConfigFieldDef> Fields { get; set; } = new List<ConfigFieldDef>();
    }

    public class ConfigFieldDef
    {
        public string Key { get; set; }
        public string Type { get; set; } // text, password, number, bool, select
        public string DefaultValue { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public string[] Options { get; set; }
        public string[] Labels { get; set; }
        public string Description { get; set; }
    }
}
