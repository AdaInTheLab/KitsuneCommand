using System.Web.Http;
using KitsuneCommand.Features;
using KitsuneCommand.Web.Auth;
using KitsuneCommand.Web.Models;

namespace KitsuneCommand.Web.Controllers
{
    /// <summary>
    /// API controller for managing Points economy settings.
    /// Admin-only access.
    /// </summary>
    [RoutePrefix("api/settings/points")]
    public class PointsSettingsController : ApiController
    {
        private readonly PointsFeature _feature;

        public PointsSettingsController(PointsFeature feature)
        {
            _feature = feature;
        }

        /// <summary>
        /// Get current points settings.
        /// </summary>
        [HttpGet]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetSettings()
        {
            return Ok(ApiResponse.Ok(_feature.Settings));
        }

        /// <summary>
        /// Update points settings.
        /// </summary>
        [HttpPut]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult UpdateSettings([FromBody] PointsSettings settings)
        {
            if (settings == null)
                return BadRequest("Settings body is required.");

            // Validate ranges
            if (settings.ZombieKillPoints < 0) settings.ZombieKillPoints = 0;
            if (settings.PlayerKillPoints < 0) settings.PlayerKillPoints = 0;
            if (settings.SignInBonus < 0) settings.SignInBonus = 0;
            if (settings.PlaytimePointsPerHour < 0) settings.PlaytimePointsPerHour = 0;
            if (settings.PlaytimeIntervalMinutes < 1) settings.PlaytimeIntervalMinutes = 1;
            if (settings.PlaytimeIntervalMinutes > 1440) settings.PlaytimeIntervalMinutes = 1440;

            _feature.UpdateSettings(settings);
            return Ok(ApiResponse.Ok("Points settings updated."));
        }
    }
}
