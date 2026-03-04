using System.Collections.Generic;

namespace KitsuneCommand.Services
{
    /// <summary>
    /// Defines all known serverconfig.xml fields with metadata for the config editor UI.
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
                        TextField("ServerName", "My 7D2D Server"),
                        TextField("ServerPassword", ""),
                        TextField("ServerDescription", "A 7 Days to Die Server"),
                        TextField("ServerLoginConfirmationText", ""),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "world",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("GameWorld", "Navezgane", new[] { "Navezgane", "RWG" }),
                        TextField("WorldGenSeed", "SomeSeed"),
                        SelectField("WorldGenSize", "6144", new[] { "2048", "3072", "4096", "5120", "6144", "7168", "8192", "10240" }),
                        TextField("GameName", "My Game"),
                        SelectField("GameMode", "GameModeSurvival", new[] { "GameModeSurvival" }),
                        NumberField("BedrollDeadZoneSize", "15", 0, 100),
                        NumberField("MaxUncoveredMapChunksPerPlayer", "131072", 0, 1000000),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "blockDamage",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("BlockDamagePlayer", "100", DamagePercentOptions()),
                        SelectField("BlockDamageAI", "100", DamagePercentOptions()),
                        SelectField("BlockDamageAIBM", "100", DamagePercentOptions()),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "gameplay",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("GameDifficulty", "2", new[] { "0", "1", "2", "3", "4", "5" }),
                        NumberField("DayNightLength", "60", 10, 240),
                        SelectField("PlayerKillingMode", "3", new[] { "0", "1", "2", "3" }),
                        NumberField("QuestProgressionDailyLimit", "3", 0, 100),
                        SelectField("JarRefund", "0", new[] { "0", "25", "50", "75", "100" }),
                        BoolField("BiomeProgression", "true"),
                        NumberField("StormFreq", "100", 0, 500),
                        NumberField("BloodMoonFrequency", "7", 1, 100),
                        NumberField("BloodMoonRange", "0", 0, 7),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "zombies",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("ZombieMove", "0", ZombieMoveOptions()),
                        SelectField("ZombieMoveNight", "3", ZombieMoveOptions()),
                        SelectField("ZombieFeralMove", "3", ZombieMoveOptions()),
                        SelectField("ZombieBMMove", "3", ZombieMoveOptions()),
                        SelectField("EnemyDifficulty", "0", new[] { "0", "1" }),
                        NumberField("EnemySpawnMode", "1", 0, 2),
                        NumberField("MaxSpawnedZombies", "64", 1, 500),
                        NumberField("MaxSpawnedAnimals", "50", 1, 500),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "lootAndDrops",
                    Fields = new List<ConfigFieldDef>
                    {
                        SelectField("LootAbundance", "100", DamagePercentOptions()),
                        NumberField("LootRespawnDays", "7", 1, 100),
                        BoolField("AirDropFrequency", "72"),
                        SelectField("DropOnDeath", "1", new[] { "0", "1", "2", "3", "4" }),
                        SelectField("DropOnQuit", "1", new[] { "0", "1", "2", "3" }),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "landClaims",
                    Fields = new List<ConfigFieldDef>
                    {
                        NumberField("LandClaimSize", "41", 1, 200),
                        NumberField("LandClaimDeadZone", "30", 0, 200),
                        NumberField("LandClaimExpiryTime", "7", 1, 365),
                        SelectField("LandClaimDecayMode", "0", new[] { "0", "1", "2" }),
                        NumberField("LandClaimOnlineDurabilityModifier", "4", 0, 100),
                        NumberField("LandClaimOfflineDurabilityModifier", "4", 0, 100),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "networkAndSlots",
                    Fields = new List<ConfigFieldDef>
                    {
                        NumberField("ServerPort", "26900", 1024, 65535),
                        SelectField("ServerVisibility", "2", new[] { "0", "1", "2" }),
                        NumberField("ServerMaxPlayerCount", "8", 1, 64),
                        NumberField("ServerReservedSlots", "0", 0, 10),
                        NumberField("ServerMaxWorldTransferSpeedKiBs", "512", 64, 10240),
                        TextField("ServerDisabledNetworkProtocols", ""),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "admin",
                    Fields = new List<ConfigFieldDef>
                    {
                        BoolField("TelnetEnabled", "true"),
                        NumberField("TelnetPort", "8081", 1024, 65535),
                        TextField("TelnetPassword", ""),
                        NumberField("TelnetFailedLoginLimit", "10", 0, 100),
                        NumberField("TelnetFailedLoginsBlocktime", "10", 0, 3600),
                        BoolField("EACEnabled", "true"),
                        NumberField("ServerAdminSlots", "0", 0, 10),
                        BoolField("HideCommandExecutionLog", "0"),
                    }
                },
                new ConfigFieldGroup
                {
                    Key = "advanced",
                    Fields = new List<ConfigFieldDef>
                    {
                        TextField("ServerWebsiteURL", ""),
                        BoolField("TerminalWindowEnabled", "true"),
                        BoolField("WebDashboardEnabled", "false"),
                        NumberField("WebDashboardPort", "8080", 1024, 65535),
                        TextField("WebDashboardUrl", ""),
                    }
                },
            };
        }

        private static ConfigFieldDef TextField(string key, string defaultValue)
            => new ConfigFieldDef { Key = key, Type = "text", DefaultValue = defaultValue };

        private static ConfigFieldDef NumberField(string key, string defaultValue, int min, int max)
            => new ConfigFieldDef { Key = key, Type = "number", DefaultValue = defaultValue, Min = min, Max = max };

        private static ConfigFieldDef BoolField(string key, string defaultValue)
            => new ConfigFieldDef { Key = key, Type = "bool", DefaultValue = defaultValue };

        private static ConfigFieldDef SelectField(string key, string defaultValue, string[] options)
            => new ConfigFieldDef { Key = key, Type = "select", DefaultValue = defaultValue, Options = options };

        private static string[] DamagePercentOptions()
            => new[] { "25", "50", "75", "100", "125", "150", "175", "200", "300" };

        private static string[] ZombieMoveOptions()
            => new[] { "0", "1", "2", "3", "4" };
    }

    public class ConfigFieldGroup
    {
        public string Key { get; set; }
        public List<ConfigFieldDef> Fields { get; set; } = new List<ConfigFieldDef>();
    }

    public class ConfigFieldDef
    {
        public string Key { get; set; }
        public string Type { get; set; } // text, number, bool, select
        public string DefaultValue { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public string[] Options { get; set; }
    }
}
