using System;
using System.Collections.Generic;
using System.Web.Http;
using KitsuneCommand.Services;
using KitsuneCommand.Web.Auth;
using KitsuneCommand.Web.Models;

namespace KitsuneCommand.Web.Controllers
{
    /// <summary>
    /// API controller for reading and writing the serverconfig.xml file.
    /// Provides both structured property access and raw XML editing.
    /// </summary>
    [Authorize]
    [RoutePrefix("api/config")]
    public class ConfigController : ApiController
    {
        private readonly ServerConfigService _configService;

        public ConfigController(ServerConfigService configService)
        {
            _configService = configService;
        }

        /// <summary>
        /// Get current config properties, field definitions, and config file path.
        /// </summary>
        [HttpGet]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetConfig()
        {
            try
            {
                var properties = _configService.ReadConfig();
                var groups = _configService.GetFieldDefinitions();
                var configPath = _configService.GetConfigPath();

                return Ok(ApiResponse.Ok(new
                {
                    properties,
                    groups,
                    configPath
                }));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to read config: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get the raw XML content of the config file.
        /// </summary>
        [HttpGet]
        [Route("raw")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetRawXml()
        {
            try
            {
                var xml = _configService.ReadRawXml();
                return Ok(ApiResponse.Ok(new { xml }));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to read config: {ex.Message}"));
            }
        }

        /// <summary>
        /// Update config properties. Creates a backup before writing.
        /// </summary>
        [HttpPut]
        [Route("")]
        [RoleAuthorize("admin")]
        public IHttpActionResult SaveConfig([FromBody] Dictionary<string, string> properties)
        {
            if (properties == null || properties.Count == 0)
                return BadRequest("Properties are required.");

            try
            {
                _configService.SaveConfig(properties);
                return Ok(ApiResponse.Ok("Config saved. A backup was created. Changes require a server restart to take effect."));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to save config: {ex.Message}"));
            }
        }

        /// <summary>
        /// Save raw XML content to the config file. Creates a backup before writing.
        /// </summary>
        [HttpPut]
        [Route("raw")]
        [RoleAuthorize("admin")]
        public IHttpActionResult SaveRawXml([FromBody] RawXmlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Xml))
                return BadRequest("XML content is required.");

            try
            {
                _configService.SaveRawXml(request.Xml);
                return Ok(ApiResponse.Ok("Config saved from raw XML. A backup was created. Changes require a server restart."));
            }
            catch (System.Xml.XmlException ex)
            {
                return Ok(ApiResponse.Error(400, $"Invalid XML: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Error(500, $"Failed to save config: {ex.Message}"));
            }
        }

        /// <summary>
        /// List available world names.
        /// </summary>
        [HttpGet]
        [Route("worlds")]
        [RoleAuthorize("admin")]
        public IHttpActionResult GetWorlds()
        {
            try
            {
                var worlds = _configService.GetAvailableWorlds();
                return Ok(ApiResponse.Ok(worlds));
            }
            catch
            {
                return Ok(ApiResponse.Ok(new List<string> { "Navezgane" }));
            }
        }
    }

    public class RawXmlRequest
    {
        public string Xml { get; set; }
    }
}
