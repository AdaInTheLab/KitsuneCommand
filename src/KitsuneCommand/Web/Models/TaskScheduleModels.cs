using KitsuneCommand.Data.Entities;

namespace KitsuneCommand.Web.Models
{
    public class TaskScheduleDetailResponse
    {
        public int Id { get; set; }
        public string CreatedAt { get; set; }
        public string Name { get; set; }
        public string CronExpression { get; set; }
        public int IsEnabled { get; set; }
        public string LastRunAt { get; set; }
        public string Description { get; set; }
        public int IntervalMinutes { get; set; }
        public string NextRunAt { get; set; }
        public List<CommandDefinition> Commands { get; set; } = new List<CommandDefinition>();
    }

    public class CreateTaskScheduleRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int IntervalMinutes { get; set; }
        public List<int> CommandIds { get; set; } = new List<int>();
    }

    public class UpdateTaskScheduleRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int IntervalMinutes { get; set; }
        public int IsEnabled { get; set; }
        public List<int> CommandIds { get; set; }
    }
}
