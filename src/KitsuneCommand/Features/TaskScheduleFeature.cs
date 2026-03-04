using KitsuneCommand.Configuration;
using KitsuneCommand.Core;
using KitsuneCommand.Data.Repositories;

namespace KitsuneCommand.Features
{
    /// <summary>
    /// Automated task scheduling feature: executes linked server commands
    /// at regular intervals based on interval_minutes.
    /// Uses a 60-second timer tick to check all enabled tasks.
    /// </summary>
    public class TaskScheduleFeature : FeatureBase<TaskScheduleSettings>
    {
        private readonly ITaskScheduleRepository _taskRepo;
        private Timer _schedulerTimer;

        public TaskScheduleFeature(
            ModEventBus eventBus,
            ConfigManager config,
            ITaskScheduleRepository taskRepo)
            : base(eventBus, config)
        {
            _taskRepo = taskRepo;
        }

        protected override void OnEnable()
        {
            // Tick every 60 seconds to check for due tasks
            _schedulerTimer = new Timer(OnSchedulerTick, null, 60_000, 60_000);
            Log.Out("[KitsuneCommand] TaskSchedule feature enabled. Checking tasks every 60 seconds.");
        }

        protected override void OnDisable()
        {
            _schedulerTimer?.Dispose();
            _schedulerTimer = null;
            Log.Out("[KitsuneCommand] TaskSchedule feature disabled.");
        }

        /// <summary>
        /// Timer callback: runs on ThreadPool every 60 seconds.
        /// Loads all enabled tasks and executes any that are due.
        /// </summary>
        private void OnSchedulerTick(object _)
        {
            try
            {
                var tasks = _taskRepo.GetAllEnabled();
                if (tasks == null) return;

                var now = DateTime.UtcNow;

                foreach (var task in tasks)
                {
                    try
                    {
                        if (!IsTaskDue(task, now)) continue;

                        ExecuteTaskCommands(task);
                        _taskRepo.UpdateLastRunAt(task.Id);

                        Log.Out($"[KitsuneCommand] TaskSchedule: Executed task '{task.Name}' (id={task.Id})");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[KitsuneCommand] TaskSchedule: Error executing task '{task.Name}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[KitsuneCommand] TaskSchedule: Scheduler tick error: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if enough time has elapsed since the task's last run.
        /// </summary>
        private static bool IsTaskDue(Data.Entities.TaskSchedule task, DateTime now)
        {
            if (task.IntervalMinutes <= 0) return false;

            // Never run before — due immediately
            if (string.IsNullOrEmpty(task.LastRunAt)) return true;

            if (!DateTime.TryParse(task.LastRunAt, out var lastRun))
                return true; // Parse error, allow execution

            var elapsed = now - lastRun;
            return elapsed.TotalMinutes >= task.IntervalMinutes;
        }

        /// <summary>
        /// Loads linked commands for a task and executes each on the game thread.
        /// </summary>
        private void ExecuteTaskCommands(Data.Entities.TaskSchedule task)
        {
            var commands = _taskRepo.GetCommandsForTask(task.Id);
            if (commands == null) return;

            foreach (var cmdDef in commands)
            {
                try
                {
                    var output = ExecuteConsoleCommand(cmdDef.Command);
                    Log.Out($"[KitsuneCommand] TaskSchedule: [{task.Name}] Ran '{cmdDef.Command}' → {output}");
                }
                catch (Exception ex)
                {
                    Log.Warning($"[KitsuneCommand] TaskSchedule: [{task.Name}] Command '{cmdDef.Command}' failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Executes a console command on the main thread and returns the output.
        /// Must be called from a non-game thread (timer callback).
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
