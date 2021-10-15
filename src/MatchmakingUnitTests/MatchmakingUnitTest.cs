using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Matchmaking.Services;
using System;
using MatchmakingEngine.Service;
namespace MatchmakingIntegrationTests
{
    [TestClass]
    public class MatchmakingUnitTest
    {
        private IMatchmakingService _mmService;
        private IMatchmakingEngineService _mmEngineService;
        private Random _random;
        public MatchmakingUnitTest()
        {
            _mmService = new MatchmakingService(new LoggerFactory().CreateLogger<MatchmakingService>());
            _mmEngineService = new MatchmakingEngineService(new LoggerFactory().CreateLogger<MatchmakingEngineService>());
            _random = new Random();
        }

        [TestMethod]
        public void TestDbInsert()
        {
            AddRandomPlayers(100);

            string[] players;
            string[] qos;
            _mmEngineService.GetPlayerChunkFromQueue(out players, out qos);
            Assert.AreEqual(players.Length, qos.Length, "Inconsistency between profileids and latency");
        }

        [TestMethod]
        public void TestDbInsertRandom()
        {
            _mmService.AddToMatchmakingQueue(System.Guid.NewGuid(), 10);
            _mmService.AddToMatchmakingQueue(System.Guid.NewGuid(), 60);
            _mmService.AddToMatchmakingQueue(System.Guid.NewGuid(), 120);
            _mmService.AddToMatchmakingQueue(System.Guid.NewGuid(), 300);

        }

        [TestMethod]
        private void AddRandomPlayers(int n)
        {
            for (int i = 0; i < n; i++)
            {
                _mmService.AddToMatchmakingQueue(System.Guid.NewGuid(), _random.Next(0, 200));
            }
        }
    }
}
