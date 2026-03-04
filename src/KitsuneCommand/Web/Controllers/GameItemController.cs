using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using KitsuneCommand.Services;
using KitsuneCommand.Web.Models;

namespace KitsuneCommand.Web.Controllers
{
    /// <summary>
    /// Game item catalog endpoints: browse, search, autocomplete, and icon serving.
    /// </summary>
    [Authorize]
    [RoutePrefix("api/game-items")]
    public class GameItemController : ApiController
    {
        private readonly GameItemCatalog _catalog;
        private readonly ItemIconService _iconService;

        public GameItemController(GameItemCatalog catalog, ItemIconService iconService)
        {
            _catalog = catalog;
            _iconService = iconService;
        }

        /// <summary>
        /// Paginated search of all game items with optional query and group filter.
        /// </summary>
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetItems(string search = null, string group = null,
            int pageIndex = 0, int pageSize = 50)
        {
            if (pageSize > 200) pageSize = 200;
            if (pageIndex < 0) pageIndex = 0;

            var result = _catalog.Search(search, group, pageIndex, pageSize);
            return Ok(ApiResponse.Ok(result));
        }

        /// <summary>
        /// Get all item groups/categories.
        /// </summary>
        [HttpGet]
        [Route("groups")]
        public IHttpActionResult GetGroups()
        {
            var groups = _catalog.GetGroups();
            return Ok(ApiResponse.Ok(groups));
        }

        /// <summary>
        /// Lightweight autocomplete search — returns top N matches.
        /// </summary>
        [HttpGet]
        [Route("search")]
        public IHttpActionResult SearchItems(string query = "", int limit = 20)
        {
            if (limit > 100) limit = 100;

            var items = _catalog.SearchNames(query, limit);
            return Ok(ApiResponse.Ok(items));
        }

        /// <summary>
        /// Get a single item by exact name.
        /// </summary>
        [HttpGet]
        [Route("by-name/{name}")]
        public IHttpActionResult GetByName(string name)
        {
            var item = _catalog.GetByName(name);
            if (item == null)
                return NotFound();

            return Ok(ApiResponse.Ok(item));
        }

        /// <summary>
        /// Returns the PNG icon for the given item icon name.
        /// Anonymous access — icons are static game assets, not sensitive data.
        /// </summary>
        [HttpGet]
        [Route("icon/{iconName}")]
        [AllowAnonymous]
        public HttpResponseMessage GetIcon(string iconName, int size = 0)
        {
            if (!_iconService.IsAvailable)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("Icon service not available")
                };
            }

            var iconData = _iconService.GetIcon(iconName, size);
            if (iconData == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("Icon not found")
                };
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(iconData)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            response.Headers.CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(7)
            };

            return response;
        }
    }
}
