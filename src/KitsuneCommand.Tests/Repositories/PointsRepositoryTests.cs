using System.IO;
using System.Linq;
using NUnit.Framework;
using KitsuneCommand.Data;
using KitsuneCommand.Data.Repositories;

namespace KitsuneCommand.Tests.Repositories
{
    [TestFixture]
    public class PointsRepositoryTests
    {
        private string _dbPath;
        private DbConnectionFactory _db;
        private PointsRepository _repo;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _dbPath = TestDbFixture.CreateTempDatabase();
            _db = TestDbFixture.CreateFactory(_dbPath);
            _repo = new PointsRepository(_db);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestDbFixture.Cleanup(_dbPath);
        }

        [SetUp]
        public void SetUp()
        {
            // Clear points_info table between tests
            using var conn = _db.CreateConnection();
            Dapper.SqlMapper.Execute(conn, "DELETE FROM points_info");
        }

        [Test]
        public void GetByPlayerId_ReturnsNull_WhenPlayerDoesNotExist()
        {
            var result = _repo.GetByPlayerId("nonexistent");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void UpsertPlayer_CreatesNewPlayerWithZeroPoints()
        {
            _repo.UpsertPlayer("player1", "TestPlayer");

            var result = _repo.GetByPlayerId("player1");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo("player1"));
            Assert.That(result.PlayerName, Is.EqualTo("TestPlayer"));
            Assert.That(result.Points, Is.EqualTo(0));
        }

        [Test]
        public void UpsertPlayer_UpdatesName_WithoutOverwritingPoints()
        {
            _repo.UpsertPlayer("player1", "OldName");
            _repo.AdjustPoints("player1", 100);

            // Upsert again with a new name
            _repo.UpsertPlayer("player1", "NewName");

            var result = _repo.GetByPlayerId("player1");
            Assert.That(result.PlayerName, Is.EqualTo("NewName"));
            Assert.That(result.Points, Is.EqualTo(100), "Points should not be overwritten by upsert");
        }

        [Test]
        public void AdjustPoints_AddsPositiveAmount()
        {
            _repo.UpsertPlayer("player1", "Test");

            var newTotal = _repo.AdjustPoints("player1", 50);

            Assert.That(newTotal, Is.EqualTo(50));
        }

        [Test]
        public void AdjustPoints_SubtractsNegativeAmount()
        {
            _repo.UpsertPlayer("player1", "Test");
            _repo.AdjustPoints("player1", 100);

            var newTotal = _repo.AdjustPoints("player1", -30);

            Assert.That(newTotal, Is.EqualTo(70));
        }

        [Test]
        public void AdjustPoints_AllowsNegativeBalance()
        {
            _repo.UpsertPlayer("player1", "Test");

            var newTotal = _repo.AdjustPoints("player1", -50);

            Assert.That(newTotal, Is.EqualTo(-50));
        }

        [Test]
        public void AdjustPoints_MultipleCalls_Accumulate()
        {
            _repo.UpsertPlayer("player1", "Test");

            _repo.AdjustPoints("player1", 10);
            _repo.AdjustPoints("player1", 20);
            _repo.AdjustPoints("player1", 30);

            var result = _repo.GetByPlayerId("player1");
            Assert.That(result.Points, Is.EqualTo(60));
        }

        [Test]
        public void TrySignIn_AwardsBonusOnFirstSignIn()
        {
            _repo.UpsertPlayer("player1", "Test");

            var awarded = _repo.TrySignIn("player1", 10);

            Assert.That(awarded, Is.True);
            var result = _repo.GetByPlayerId("player1");
            Assert.That(result.Points, Is.EqualTo(10));
        }

        [Test]
        public void TrySignIn_ReturnsFalseOnDuplicateSameDay()
        {
            _repo.UpsertPlayer("player1", "Test");

            var first = _repo.TrySignIn("player1", 10);
            var second = _repo.TrySignIn("player1", 10);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);

            var result = _repo.GetByPlayerId("player1");
            Assert.That(result.Points, Is.EqualTo(10), "Points should only be awarded once");
        }

        [Test]
        public void GetAll_ReturnsPaginatedResults()
        {
            // Insert 5 players with varying points
            for (int i = 1; i <= 5; i++)
            {
                _repo.UpsertPlayer($"p{i}", $"Player{i}");
                _repo.AdjustPoints($"p{i}", i * 10); // 10, 20, 30, 40, 50
            }

            // Page 0, size 3 — should get top 3 by points DESC
            var page0 = _repo.GetAll(0, 3).ToList();
            Assert.That(page0, Has.Count.EqualTo(3));
            Assert.That(page0[0].Points, Is.EqualTo(50)); // Highest first

            // Page 1, size 3 — should get remaining 2
            var page1 = _repo.GetAll(1, 3).ToList();
            Assert.That(page1, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetAll_SearchFilter_MatchesByName()
        {
            _repo.UpsertPlayer("p1", "Alpha");
            _repo.UpsertPlayer("p2", "Beta");
            _repo.UpsertPlayer("p3", "AlphaTwo");

            var results = _repo.GetAll(0, 10, "Alpha").ToList();

            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results.All(r => r.PlayerName.Contains("Alpha")), Is.True);
        }

        [Test]
        public void GetTotalCount_ReturnsCorrectCount()
        {
            _repo.UpsertPlayer("p1", "A");
            _repo.UpsertPlayer("p2", "B");
            _repo.UpsertPlayer("p3", "C");

            Assert.That(_repo.GetTotalCount(), Is.EqualTo(3));
        }

        [Test]
        public void GetTotalCount_WithSearch_FiltersCorrectly()
        {
            _repo.UpsertPlayer("p1", "Alpha");
            _repo.UpsertPlayer("p2", "Beta");
            _repo.UpsertPlayer("p3", "Gamma");

            Assert.That(_repo.GetTotalCount("Beta"), Is.EqualTo(1));
        }
    }
}
