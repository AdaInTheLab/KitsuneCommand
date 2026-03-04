using System.Web.Http;
using KitsuneCommand.Data;
using KitsuneCommand.Data.Repositories;
using KitsuneCommand.Web.Auth;
using KitsuneCommand.Web.Models;
using Dapper;

namespace KitsuneCommand.Web.Controllers
{
    /// <summary>
    /// Dashboard aggregate statistics endpoint.
    /// Provides overview data for the web panel dashboard.
    /// </summary>
    [Authorize]
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        private readonly DbConnectionFactory _db;

        public DashboardController(DbConnectionFactory db)
        {
            _db = db;
        }

        /// <summary>
        /// Get aggregated dashboard statistics across all features.
        /// </summary>
        [HttpGet]
        [Route("stats")]
        public IHttpActionResult GetStats()
        {
            try
            {
                using var conn = _db.CreateConnection();

                // Points economy
                var totalPlayers = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM points_info");
                var totalPointsInCirculation = conn.ExecuteScalar<long>("SELECT COALESCE(SUM(points), 0) FROM points_info");

                // Store
                var totalPurchases = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM purchase_history");
                var totalPointsSpent = conn.ExecuteScalar<long>("SELECT COALESCE(SUM(price), 0) FROM purchase_history");
                var totalStoreItems = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM goods");

                // Teleport
                var totalTeleports = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM tele_records");
                var totalCities = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM city_locations");

                // CD Keys
                var totalCdKeys = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM cd_keys");
                var totalRedemptions = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM cd_key_redeem_records");

                // VIP Gifts
                var totalVipGifts = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM vip_gifts");

                // Task Schedules
                var totalSchedules = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM task_schedules");
                var activeSchedules = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM task_schedules WHERE is_enabled = 1");

                // Chat records
                var totalChatMessages = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM chat_records");

                return Ok(ApiResponse.Ok(new
                {
                    totalPlayers,
                    totalPointsInCirculation,
                    totalPurchases,
                    totalPointsSpent,
                    totalStoreItems,
                    totalTeleports,
                    totalCities,
                    totalCdKeys,
                    totalRedemptions,
                    totalVipGifts,
                    totalSchedules,
                    activeSchedules,
                    totalChatMessages
                }));
            }
            catch (Exception ex)
            {
                Log.Warning($"[KitsuneCommand] Dashboard stats error: {ex.Message}");
                return InternalServerError();
            }
        }
    }
}
