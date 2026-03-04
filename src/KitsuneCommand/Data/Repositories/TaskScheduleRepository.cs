using Dapper;
using KitsuneCommand.Data.Entities;
using CommandDefinition = KitsuneCommand.Data.Entities.CommandDefinition;

namespace KitsuneCommand.Data.Repositories
{
    public interface ITaskScheduleRepository
    {
        // CRUD
        IEnumerable<TaskSchedule> GetAll(int pageIndex, int pageSize, string search = null);
        int GetTotalCount(string search = null);
        TaskSchedule GetById(int id);
        IEnumerable<TaskSchedule> GetAllEnabled();
        int Insert(TaskSchedule task);
        void Update(TaskSchedule task);
        void Delete(int id);

        // Junction tables
        IEnumerable<CommandDefinition> GetCommandsForTask(int taskId);
        void SetTaskCommands(int taskId, IEnumerable<int> commandDefinitionIds);

        // Execution tracking
        void UpdateLastRunAt(int id);
    }

    public class TaskScheduleRepository : ITaskScheduleRepository
    {
        private readonly DbConnectionFactory _db;

        public TaskScheduleRepository(DbConnectionFactory db)
        {
            _db = db;
        }

        // ─── CRUD ────────────────────────────────────────────────

        public IEnumerable<TaskSchedule> GetAll(int pageIndex, int pageSize, string search = null)
        {
            using var conn = _db.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE name LIKE @Search OR description LIKE @Search";
            return conn.Query<TaskSchedule>(
                $"SELECT * FROM task_schedules {where} ORDER BY id DESC LIMIT @Limit OFFSET @Offset",
                new { Limit = pageSize, Offset = pageIndex * pageSize, Search = $"%{search}%" });
        }

        public int GetTotalCount(string search = null)
        {
            using var conn = _db.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE name LIKE @Search OR description LIKE @Search";
            return conn.ExecuteScalar<int>(
                $"SELECT COUNT(*) FROM task_schedules {where}",
                new { Search = $"%{search}%" });
        }

        public TaskSchedule GetById(int id)
        {
            using var conn = _db.CreateConnection();
            return conn.QueryFirstOrDefault<TaskSchedule>(
                "SELECT * FROM task_schedules WHERE id = @Id", new { Id = id });
        }

        public IEnumerable<TaskSchedule> GetAllEnabled()
        {
            using var conn = _db.CreateConnection();
            return conn.Query<TaskSchedule>(
                "SELECT * FROM task_schedules WHERE is_enabled = 1 ORDER BY id ASC");
        }

        public int Insert(TaskSchedule task)
        {
            using var conn = _db.CreateConnection();
            return conn.ExecuteScalar<int>(@"
                INSERT INTO task_schedules (name, cron_expression, is_enabled, description, interval_minutes)
                VALUES (@Name, @CronExpression, @IsEnabled, @Description, @IntervalMinutes);
                SELECT last_insert_rowid();", task);
        }

        public void Update(TaskSchedule task)
        {
            using var conn = _db.CreateConnection();
            conn.Execute(@"
                UPDATE task_schedules
                SET name = @Name, cron_expression = @CronExpression,
                    is_enabled = @IsEnabled, description = @Description,
                    interval_minutes = @IntervalMinutes
                WHERE id = @Id", task);
        }

        public void Delete(int id)
        {
            using var conn = _db.CreateConnection();
            conn.Execute("DELETE FROM task_schedules WHERE id = @Id", new { Id = id });
        }

        // ─── Junction Tables ─────────────────────────────────────

        public IEnumerable<CommandDefinition> GetCommandsForTask(int taskId)
        {
            using var conn = _db.CreateConnection();
            return conn.Query<CommandDefinition>(@"
                SELECT d.* FROM command_definitions d
                INNER JOIN task_schedule_commands tsc ON tsc.command_id = d.id
                WHERE tsc.task_schedule_id = @TaskId
                ORDER BY d.command",
                new { TaskId = taskId });
        }

        public void SetTaskCommands(int taskId, IEnumerable<int> commandDefinitionIds)
        {
            using var conn = _db.CreateConnection();
            conn.Execute("DELETE FROM task_schedule_commands WHERE task_schedule_id = @TaskId",
                new { TaskId = taskId });

            foreach (var commandId in commandDefinitionIds)
            {
                conn.Execute(
                    "INSERT INTO task_schedule_commands (task_schedule_id, command_id) VALUES (@TaskId, @CommandId)",
                    new { TaskId = taskId, CommandId = commandId });
            }
        }

        // ─── Execution Tracking ──────────────────────────────────

        public void UpdateLastRunAt(int id)
        {
            using var conn = _db.CreateConnection();
            conn.Execute(
                "UPDATE task_schedules SET last_run_at = datetime('now') WHERE id = @Id",
                new { Id = id });
        }
    }
}
