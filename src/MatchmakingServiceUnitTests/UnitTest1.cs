using System;
using Xunit;
using Matchmaking.Services;
namespace MatchmakingServiceUnitTests
{
    public class MatchmakingServiceUnitTest
    {
        private IMatchmakingService _mmService;
        private const int K_USER_COUNT = 10;
        public MatchmakingServiceUnitTest (IMatchmakingService mmService)
        {
            _mmService = mmService;
        }

        [Fact]
        public void TestRedisMatchingData()
        {
            _mmService?.AddToMatchmakingQueue(new Guid(), 80);
        }
    }
}
