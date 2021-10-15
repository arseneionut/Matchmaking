using Microsoft.Extensions.Logging;
using System;
using StackExchange.Redis;

namespace Matchmaking.Services
{
    public class MatchmakingService : IMatchmakingService
    {
        private ConnectionMultiplexer _redis;
        private IDatabase _db;
        private ILogger<MatchmakingService> _logger;
        public MatchmakingService(ILogger<MatchmakingService> logger)
        {
            _redis = ConnectionMultiplexer.Connect("localhost");
            _db = _redis.GetDatabase();
            _logger = logger;
        }
        public bool AddToMatchmakingQueue(Guid profileId, int qos)
        {
            try
            {
                var transaction = _db.CreateTransaction();
                transaction.AddCondition(Condition.KeyNotExists(profileId.ToString()));
                transaction.StringSetAsync(profileId.ToString(), "");
                var removeResult = transaction.ListRemoveAsync("matchmaking_queue", profileId.ToString());
                var pushResult = transaction.ListLeftPushAsync("matchmaking_queue", profileId.ToString());
                var setQoSResult = transaction.ListLeftPushAsync("matchmaking_queue_qos", qos);
                bool committed = transaction.Execute();

                _logger?.LogDebug($"Profile id {profileId.ToString()} pushed into slot {pushResult}.");
            }
            catch (Exception e)
            {
                _logger?.LogError($"Something went wrong when trying to join matchmaking: {e.Message}");
                return false;
            }
            return true;
        }

        public bool RemoveFromMatchmakingQueue(Guid profileId)
        {
            try
            {
                var removeResult = _db.ListRemoveAsync("matchmaking_queue", profileId.ToString());
                var deleteResult = _db.HashDeleteAsync("matchmaking_queue_qos", profileId.ToString());
                _db.Wait(removeResult);
                _db.Wait(deleteResult);
            }
            catch (Exception e)
            {
                _logger?.LogError($"Something went wrong when trying to remove user from matchmaking: {e.Message}");
                return false;
            }
            return true;
        }

        public string GetMatchmakingSession(Guid profileId)
        {
            var result =_db.StringGetAsync(profileId.ToString());
            _db.Wait(result);
            return result.Result.ToString();
        }
    }
}
