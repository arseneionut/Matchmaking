using MatchmakingEngine.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace MatchmakingEngine.Service
{
    public class MatchmakingEngineService : IMatchmakingEngineService
    {
        private readonly int _matchmakingChunk = 50;

        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<MatchmakingEngineService> _logger;
        private readonly IConfiguration _config;

        public MatchmakingEngineService(ILogger<MatchmakingEngineService> logger, IConfiguration config)
        {
            _config = config;
            _redis = ConnectionMultiplexer.Connect(_config.GetValue<string>("Redis:Host"));
            _db = _redis.GetDatabase();
            _logger = logger;
            _matchmakingChunk = config.GetValue<int>("Matchmaking:MatchmakingChunk");
        }

        public void GetPlayerChunkFromQueue(out string[] players, out string[] qos)
        {
            var transaction = _db.CreateTransaction();
            transaction.AddCondition(Condition.ListLengthGreaterThan("matchmaking_queue",0));
            
            var result = transaction.ListRangeAsync("matchmaking_queue", _matchmakingChunk * -1, -1);
            transaction.ListTrimAsync("matchmaking_queue", 0, (_matchmakingChunk + 1) * -1);
            var resultQoS = transaction.ListRangeAsync("matchmaking_queue_qos", _matchmakingChunk * -1, -1);
            transaction.ListTrimAsync("matchmaking_queue_qos", 0, (_matchmakingChunk + 1) * -1);
            bool committed = transaction.Execute();
            if (committed)
            {
                players = result.Result.ToStringArray();
                qos = resultQoS.Result.ToStringArray();
                return;
            }

            players = new string[0];
            qos = new string[0];
        }

        public void GetSessions(out Session[] pendingSessions, out Session[] startedSessions)
        {
            var resultPending = _db.HashGetAllAsync("matchmaking_sessions_pending");
            var resultStarted = _db.HashGetAllAsync("matchmaking_sessions_started");
            _db.Wait(resultPending);
            _db.Wait(resultStarted);

            pendingSessions = new Session[resultPending.Result.Length];
            for (int i = 0; i< resultPending.Result.Length; i++)
            {
                pendingSessions[i] = JsonConvert.DeserializeObject<Session>(resultPending.Result[i].Value);
            }

            startedSessions = new Session[resultStarted.Result.Length];
            for (int i = 0; i < resultStarted.Result.Length; i++)
            {
                startedSessions[i] = JsonConvert.DeserializeObject<Session>(resultStarted.Result[i].Value);
            }
        }

        public bool CreateSession(Session session, SessionState state)
        {
            string sessionString = JsonConvert.SerializeObject(session);
            var result = _db.HashSetAsync($"matchmaking_sessions_{state.ToString()}", session.SessionId.ToString(), sessionString);
            _db.Wait(result);                

            return result.Result;
        }

        public bool CreateSessions(List<Session> sessions, SessionState state)
        {
            HashEntry[] sessionEntrys = new HashEntry[sessions.Count];
            int count = 0;
            var transaction = _db.CreateTransaction();
            foreach (Session s in sessions)
            {
                foreach (Guid user in s.Users)
                {
                    transaction.StringSetAsync(user.ToString(), s.SessionId.ToString());
                }
                sessionEntrys[count] = new HashEntry(s.SessionId.ToString(), JsonConvert.SerializeObject(s));
                count++;
            }
            transaction.HashSetAsync($"matchmaking_sessions_{state.ToString().ToLower()}", sessionEntrys);
            var commited = transaction.Execute();

            return commited;
        }
    }
}
