using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using StackExchange.Redis;

namespace Matchmaking.Services
{
    public enum LeaveOutcome
    {
        LeaveSuccess,
        CannotLeave,
        NotJoined,
        Error
    }
    public class MatchmakingService : IMatchmakingService
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<MatchmakingService> _logger;
        private readonly IConfiguration _config;
        public MatchmakingService(ILogger<MatchmakingService> logger, IConfiguration config)
        {
            _config = config;
            _redis = ConnectionMultiplexer.Connect(_config.GetValue<string>("Redis:Host"));
            _db = _redis.GetDatabase();
            _logger = logger;
        }
        public bool AddToMatchmakingQueue(Guid profileId, int qos)
        {
            bool committed;
            try
            {
                var transaction = _db.CreateTransaction();
                transaction.AddCondition(Condition.KeyNotExists(profileId.ToString()));
                transaction.StringSetAsync(profileId.ToString(), "");
                transaction.StringSetAsync($"{profileId}_qos", $"{profileId}:{qos}");
                var pushResult = transaction.ListLeftPushAsync("matchmaking_queue", profileId.ToString());
                var setQoSResult = transaction.ListLeftPushAsync("matchmaking_queue_qos", $"{profileId}:{qos}");
                committed = transaction.Execute();

                _logger?.LogDebug($"Profile id {profileId} pushed into slot {pushResult.Result}.");
            }
            catch (Exception e)
            {
                _logger?.LogError($"Something went wrong when trying to join matchmaking for profile {profileId} with qos {qos}: {e.Message}");
                return false;
            }
            return committed;
        }

        public LeaveOutcome RemoveFromMatchmakingQueue(Guid profileId)
        {
            try
            {
                string mmQosKey = _db.StringGet($"{profileId}_qos");
                if (mmQosKey == null)
                {
                    return LeaveOutcome.NotJoined;
                }
                var transaction = _db.CreateTransaction();
                var removeResult = transaction.ListRemoveAsync("matchmaking_queue", profileId.ToString());
                var deleteResult = transaction.ListRemoveAsync("matchmaking_queue_qos", mmQosKey);
                transaction.KeyDeleteAsync($"{profileId}_qos");
                transaction.KeyDeleteAsync(profileId.ToString());
                transaction.Execute();

                if (removeResult.Result >= 1 && deleteResult.Result >= 1)
                { 
                    return LeaveOutcome.LeaveSuccess;
                }
                else if (removeResult.Result == 0 && deleteResult.Result == 0)
                {
                    return LeaveOutcome.CannotLeave;
                }
                else
                {
                    _logger?.LogError($"[Matchmaking]RemoveFromMatchmakingQueue:Something hit the fan and the lists went out of synch");
                    return LeaveOutcome.Error;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError($"[Matchmaking]RemoveFromMatchmakingQueue:Something went wrong when trying to remove user from matchmaking: {e.Message}");
                return LeaveOutcome.Error;
            }
        }

        public string GetMatchmakingSession(Guid profileId)
        {
            string retVal = null;
            try 
            { 
                 var result =_db.StringGetAsync(profileId.ToString());
                _db.Wait(result);
                retVal = result.Result;
            }
            catch (Exception e)
            {
                _logger?.LogError($"[Matchmaking]GetMatchmakingSession:Something went wrong when trying to retrieve the user session for Profileid {profileId} : {e.Message}");
            }
            return retVal;
        }
    }
}
