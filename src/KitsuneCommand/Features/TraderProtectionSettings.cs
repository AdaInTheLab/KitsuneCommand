namespace KitsuneCommand.Features
{
    public class TraderProtectionSettings
    {
        /// <summary>Master toggle for the feature.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Whether trader zones are currently protected. Set to false to allow editing.</summary>
        public bool ProtectionEnabled { get; set; } = true;

        /// <summary>Log each time a protection check is bypassed.</summary>
        public bool LogBypasses { get; set; } = false;
    }
}
