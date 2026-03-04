namespace KitsuneCommand.Data.Entities
{
    /// <summary>
    /// Maps to the task_schedules SQLite table. Scheduled tasks that run commands at intervals.
    /// </summary>
    public class TaskSchedule
    {
        public int Id { get; set; }
        public string CreatedAt { get; set; }
        public string Name { get; set; }
        public string CronExpression { get; set; }
        public int IsEnabled { get; set; }
        public string LastRunAt { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Interval in minutes between executions. Used by the scheduler timer.
        /// </summary>
        public int IntervalMinutes { get; set; }
    }
}
