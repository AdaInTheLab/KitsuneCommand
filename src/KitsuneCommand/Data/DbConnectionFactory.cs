using System.Data;
using System.Data.SQLite;

namespace KitsuneCommand.Data
{
    /// <summary>
    /// Creates SQLite database connections.
    /// </summary>
    public class DbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(string databasePath)
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
        }

        public IDbConnection CreateConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public string ConnectionString => _connectionString;
    }
}
