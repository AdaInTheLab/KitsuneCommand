using System.IO;
using NUnit.Framework;
using KitsuneCommand.Data;
using KitsuneCommand.Data.Repositories;

namespace KitsuneCommand.Tests.Repositories
{
    [TestFixture]
    public class SettingsRepositoryTests
    {
        private string _dbPath;
        private DbConnectionFactory _db;
        private SettingsRepository _repo;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _dbPath = TestDbFixture.CreateTempDatabase();
            _db = TestDbFixture.CreateFactory(_dbPath);
            _repo = new SettingsRepository(_db);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestDbFixture.Cleanup(_dbPath);
        }

        [SetUp]
        public void SetUp()
        {
            using var conn = _db.CreateConnection();
            Dapper.SqlMapper.Execute(conn, "DELETE FROM settings");
        }

        [Test]
        public void Get_ReturnsNull_WhenKeyDoesNotExist()
        {
            var result = _repo.Get("nonexistent");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Set_And_Get_RoundTrips()
        {
            _repo.Set("test_key", "test_value");

            var result = _repo.Get("test_key");

            Assert.That(result, Is.EqualTo("test_value"));
        }

        [Test]
        public void Set_OverwritesExistingValue()
        {
            _repo.Set("key", "value1");
            _repo.Set("key", "value2");

            var result = _repo.Get("key");

            Assert.That(result, Is.EqualTo("value2"));
        }

        [Test]
        public void Set_CanStoreEmptyString()
        {
            _repo.Set("empty", "");

            var result = _repo.Get("empty");

            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void Set_CanStoreJsonString()
        {
            var json = "{\"enabled\":true,\"interval\":30}";
            _repo.Set("backup_settings", json);

            var result = _repo.Get("backup_settings");

            Assert.That(result, Is.EqualTo(json));
        }

        [Test]
        public void MultipleKeys_AreIndependent()
        {
            _repo.Set("key1", "value1");
            _repo.Set("key2", "value2");

            Assert.That(_repo.Get("key1"), Is.EqualTo("value1"));
            Assert.That(_repo.Get("key2"), Is.EqualTo("value2"));
        }

        [Test]
        public void Set_CanStoreNullValue()
        {
            _repo.Set("nullable", null);

            var result = _repo.Get("nullable");

            Assert.That(result, Is.Null);
        }
    }
}
