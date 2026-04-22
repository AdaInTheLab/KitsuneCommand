using System.Linq;
using System.Web.Http;
using KitsuneCommand.Features;
using KitsuneCommand.Web.Auth;
using KitsuneCommand.Web.Models;

namespace KitsuneCommand.Web.Controllers
{
    [Authorize]
    [RoutePrefix("api/trader")]
    public class TraderProtectionController : ApiController
    {
        private readonly FeatureManager _featureManager;

        public TraderProtectionController(FeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        private TraderProtectionFeature GetFeature()
        {
            return _featureManager.GetAllFeatures()
                .OfType<TraderProtectionFeature>()
                .FirstOrDefault();
        }

        [HttpGet]
        [Route("settings")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetSettings()
        {
            var feature = GetFeature();
            if (feature == null)
                return Ok(ApiResponse.Error(404, "Trader protection feature not available."));

            return Ok(ApiResponse.Ok(feature.Settings));
        }

        [HttpPut]
        [Route("settings")]
        [RoleAuthorize("admin")]
        public IHttpActionResult UpdateSettings([FromBody] TraderProtectionSettings model)
        {
            if (model == null)
                return BadRequest("Request body is required.");

            var feature = GetFeature();
            if (feature == null)
                return Ok(ApiResponse.Error(404, "Trader protection feature not available."));

            feature.UpdateSettings(model);

            var status = model.ProtectionEnabled ? "ON — trader zones are protected" : "OFF — trader zones can be edited";
            return Ok(ApiResponse.Ok($"Trader protection: {status}"));
        }

        [HttpPost]
        [Route("disable")]
        [RoleAuthorize("admin")]
        public IHttpActionResult DisableProtection()
        {
            var feature = GetFeature();
            if (feature == null)
                return Ok(ApiResponse.Error(404, "Trader protection feature not available."));

            var settings = feature.Settings;
            settings.ProtectionEnabled = false;
            feature.UpdateSettings(settings);

            return Ok(ApiResponse.Ok("Trader protection DISABLED — you can now edit blocks in trader zones."));
        }

        [HttpPost]
        [Route("enable")]
        [RoleAuthorize("admin")]
        public IHttpActionResult EnableProtection()
        {
            var feature = GetFeature();
            if (feature == null)
                return Ok(ApiResponse.Error(404, "Trader protection feature not available."));

            var settings = feature.Settings;
            settings.ProtectionEnabled = true;
            feature.UpdateSettings(settings);

            return Ok(ApiResponse.Ok("Trader protection ENABLED — trader zones are now protected."));
        }
    }
}
