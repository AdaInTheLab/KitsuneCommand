using System.Web.Http;
using KitsuneCommand.Core;
using KitsuneCommand.Data.Entities;
using KitsuneCommand.Data.Repositories;
using KitsuneCommand.Web.Auth;
using KitsuneCommand.Web.Models;

namespace KitsuneCommand.Web.Controllers
{
    /// <summary>
    /// Task Schedule endpoints: admin CRUD for scheduled tasks,
    /// manual execution, and enable/disable toggling.
    /// </summary>
    [Authorize]
    [RoutePrefix("api/schedules")]
    public class TaskScheduleController : ApiController
    {
        private readonly ITaskScheduleRepository _taskRepo;

        public TaskScheduleController(ITaskScheduleRepository taskRepo)
        {
            _taskRepo = taskRepo;
        }

        // ─── CRUD ──────────────────────────────────────────────────

        /// <summary>
        /// Admin: List all task schedules (paginated).
        /// </summary>
        [HttpGet]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetSchedules(
            [FromUri] int pageIndex = 0,
            [FromUri] int pageSize = 50,
            [FromUri] string search = null)
        {
            pageSize = Math.Min(Math.Max(pageSize, 1), 200);
            pageIndex = Math.Max(pageIndex, 0);

            var items = _taskRepo.GetAll(pageIndex, pageSize, search);
            var total = _taskRepo.GetTotalCount(search);

            var enriched = items.Select(t => MapToDetail(t, false));

            return Ok(ApiResponse.Ok(new PaginatedResponse<TaskScheduleDetailResponse>
            {
                Items = enriched,
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            }));
        }

        /// <summary>
        /// Admin: Get task schedule detail with linked commands.
        /// </summary>
        [HttpGet]
        [Route("{id:int}")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetScheduleDetail(int id)
        {
            var task = _taskRepo.GetById(id);
            if (task == null) return NotFound();

            var detail = MapToDetail(task, true);
            return Ok(ApiResponse.Ok(detail));
        }

        /// <summary>
        /// Admin: Create a new task schedule.
        /// </summary>
        [HttpPost]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult CreateSchedule([FromBody] CreateTaskScheduleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Task name is required.");
            if (request.IntervalMinutes < 1)
                return BadRequest("IntervalMinutes must be at least 1.");

            var task = new TaskSchedule
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                IntervalMinutes = request.IntervalMinutes,
                CronExpression = $"*/{request.IntervalMinutes} * * * *", // auto-generate placeholder
                IsEnabled = 1
            };

            var id = _taskRepo.Insert(task);

            if (request.CommandIds?.Count > 0)
                _taskRepo.SetTaskCommands(id, request.CommandIds);

            return Ok(ApiResponse.Ok(new { id }));
        }

        /// <summary>
        /// Admin: Update a task schedule.
        /// </summary>
        [HttpPut]
        [Route("{id:int}")]
        [RoleAuthorize("admin")]
        public IHttpActionResult UpdateSchedule(int id, [FromBody] UpdateTaskScheduleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Task name is required.");
            if (request.IntervalMinutes < 1)
                return BadRequest("IntervalMinutes must be at least 1.");

            var existing = _taskRepo.GetById(id);
            if (existing == null) return NotFound();

            existing.Name = request.Name.Trim();
            existing.Description = request.Description;
            existing.IntervalMinutes = request.IntervalMinutes;
            existing.IsEnabled = request.IsEnabled;
            existing.CronExpression = $"*/{request.IntervalMinutes} * * * *";

            _taskRepo.Update(existing);

            if (request.CommandIds != null)
                _taskRepo.SetTaskCommands(id, request.CommandIds);

            return Ok(ApiResponse.Ok(new { id }));
        }

        /// <summary>
        /// Admin: Delete a task schedule.
        /// </summary>
        [HttpDelete]
        [Route("{id:int}")]
        [RoleAuthorize("admin")]
        public IHttpActionResult DeleteSchedule(int id)
        {
            var existing = _taskRepo.GetById(id);
            if (existing == null) return NotFound();

            _taskRepo.Delete(id);
            return Ok(ApiResponse.Ok("Deleted."));
        }

        // ─── Special Actions ───────────────────────────────────────

        /// <summary>
        /// Admin: Manually trigger a task now (execute all linked commands).
        /// </summary>
        [HttpPost]
        [Route("{id:int}/run")]
        [RoleAuthorize("admin")]
        public IHttpActionResult RunNow(int id)
        {
            var task = _taskRepo.GetById(id);
            if (task == null) return NotFound();

            var commands = _taskRepo.GetCommandsForTask(id).ToList();
            if (!commands.Any())
                return BadRequest("This task has no linked commands.");

            var results = new List<string>();
            foreach (var cmdDef in commands)
            {
                var output = ExecuteConsoleCommand(cmdDef.Command);
                results.Add($"[{cmdDef.Command}]: {output}");
            }

            _taskRepo.UpdateLastRunAt(id);

            return Ok(ApiResponse.Ok(new
            {
                message = $"Task '{task.Name}' executed with {commands.Count} command(s).",
                results
            }));
        }

        /// <summary>
        /// Admin: Toggle a task's enabled/disabled status.
        /// </summary>
        [HttpPost]
        [Route("{id:int}/toggle")]
        [RoleAuthorize("admin")]
        public IHttpActionResult ToggleSchedule(int id)
        {
            var task = _taskRepo.GetById(id);
            if (task == null) return NotFound();

            task.IsEnabled = task.IsEnabled == 1 ? 0 : 1;
            _taskRepo.Update(task);

            var status = task.IsEnabled == 1 ? "enabled" : "disabled";
            return Ok(ApiResponse.Ok(new
            {
                id,
                isEnabled = task.IsEnabled,
                message = $"Task '{task.Name}' {status}."
            }));
        }

        // ─── Helpers ───────────────────────────────────────────────

        private TaskScheduleDetailResponse MapToDetail(TaskSchedule task, bool includeCommands)
        {
            var detail = new TaskScheduleDetailResponse
            {
                Id = task.Id,
                CreatedAt = task.CreatedAt,
                Name = task.Name,
                CronExpression = task.CronExpression,
                IsEnabled = task.IsEnabled,
                LastRunAt = task.LastRunAt,
                Description = task.Description,
                IntervalMinutes = task.IntervalMinutes,
                NextRunAt = ComputeNextRunAt(task)
            };

            if (includeCommands)
                detail.Commands = _taskRepo.GetCommandsForTask(task.Id).ToList();

            return detail;
        }

        /// <summary>
        /// Computes the approximate next run time based on last_run_at + interval.
        /// </summary>
        private static string ComputeNextRunAt(TaskSchedule task)
        {
            if (task.IsEnabled == 0) return null;
            if (task.IntervalMinutes <= 0) return null;

            if (string.IsNullOrEmpty(task.LastRunAt))
                return "Pending (first run)";

            if (!DateTime.TryParse(task.LastRunAt, out var lastRun))
                return "Pending";

            var nextRun = lastRun.AddMinutes(task.IntervalMinutes);
            return nextRun.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Executes a console command on the main thread and returns the output.
        /// </summary>
        private static string ExecuteConsoleCommand(string command)
        {
            string result = null;
            var waitHandle = new ManualResetEventSlim(false);

            ModEntry.MainThreadContext.Post(_ =>
            {
                try
                {
                    var output = SdtdConsole.Instance.ExecuteSync(command, null);
                    result = output != null ? string.Join("\n", output) : "";
                }
                catch (Exception ex)
                {
                    result = $"Error: {ex.Message}";
                }
                finally
                {
                    waitHandle.Set();
                }
            }, null);

            waitHandle.Wait(TimeSpan.FromSeconds(10));
            return result ?? "Command timed out.";
        }
    }
}
