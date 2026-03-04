using System;
using System.IO;
using System.Web.Http;
using KitsuneCommand.Services;
using KitsuneCommand.Web.Auth;
using KitsuneCommand.Web.Models;

namespace KitsuneCommand.Web.Controllers
{
    /// <summary>
    /// API controller for managing world save backups.
    /// </summary>
    [Authorize]
    [RoutePrefix("api/backups")]
    public class BackupsController : ApiController
    {
        private readonly BackupService _backupService;

        public BackupsController(BackupService backupService)
        {
            _backupService = backupService;
        }

        /// <summary>
        /// List all backup records.
        /// </summary>
        [HttpGet]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetBackups()
        {
            try
            {
                var backups = _backupService.GetAll();
                return Ok(ApiResponse.Ok(backups));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to list backups: {ex.Message}"));
            }
        }

        /// <summary>
        /// Create a manual backup.
        /// </summary>
        [HttpPost]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult CreateBackup([FromBody] CreateBackupRequest request)
        {
            try
            {
                var record = _backupService.CreateBackup("manual", request?.Notes);
                return Ok(ApiResponse.Ok(record));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to create backup: {ex.Message}"));
            }
        }

        /// <summary>
        /// Restore from a backup. Creates a safety backup first.
        /// </summary>
        [HttpPost]
        [Route("{id}/restore")]
        [RoleAuthorize("admin")]
        public IHttpActionResult RestoreBackup(int id)
        {
            try
            {
                _backupService.RestoreBackup(id);
                return Ok(ApiResponse.Ok("Backup restored. A safety backup of the previous state was created. Server restart recommended."));
            }
            catch (FileNotFoundException ex)
            {
                return Ok(ApiResponse.Error(404, ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to restore backup: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete a backup file and its record.
        /// </summary>
        [HttpDelete]
        [Route("{id}")]
        [RoleAuthorize("admin")]
        public IHttpActionResult DeleteBackup(int id)
        {
            try
            {
                _backupService.DeleteBackup(id);
                return Ok(ApiResponse.Ok("Backup deleted."));
            }
            catch (FileNotFoundException ex)
            {
                return Ok(ApiResponse.Error(404, ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to delete backup: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get backup schedule settings.
        /// </summary>
        [HttpGet]
        [Route("settings")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetSettings()
        {
            return Ok(ApiResponse.Ok(_backupService.Settings));
        }

        /// <summary>
        /// Update backup schedule settings.
        /// </summary>
        [HttpPut]
        [Route("settings")]
        [RoleAuthorize("admin")]
        public IHttpActionResult UpdateSettings([FromBody] BackupSettings settings)
        {
            if (settings == null)
                return BadRequest("Settings body is required.");

            if (settings.IntervalMinutes < 5) settings.IntervalMinutes = 5;
            if (settings.MaxBackups < 1) settings.MaxBackups = 1;
            if (string.IsNullOrWhiteSpace(settings.BackupPath)) settings.BackupPath = "KitsuneBackups";

            try
            {
                _backupService.UpdateSettings(settings);
                return Ok(ApiResponse.Ok("Backup settings updated."));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to update settings: {ex.Message}"));
            }
        }
    }

    public class CreateBackupRequest
    {
        public string Notes { get; set; }
    }
}
