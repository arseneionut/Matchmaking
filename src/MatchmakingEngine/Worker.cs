using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MatchmakingEngine.Service;
using MatchmakingEngine.Models;
using System.Linq;

namespace MatchmakingEngine
{
    public class Worker : BackgroundService
    {
        private Dictionary<MatchmakingQoSIntervals, int> MatchmakingIntervalMappings = new Dictionary<MatchmakingQoSIntervals, int>
        {
            {MatchmakingQoSIntervals.GroupA, 50},
            {MatchmakingQoSIntervals.GroupB, 100},
            {MatchmakingQoSIntervals.GroupC, 150},
            {MatchmakingQoSIntervals.GroupD, 9999}
        };

        private readonly int _maxTimeInLobby = 30 * 1000;
        private readonly int _maxUsersPerSession = 10;
        private readonly int _minUsersToStart = 2;

        private readonly ILogger<Worker> _logger;
        private IMatchmakingEngineService _mmEngineService;
        private Dictionary<MatchmakingQoSIntervals, Session> _cachedSessions;
        private List<Session> _sessionsToStart;
        private List<Guid> _failedMatchmaking;
        private IConfiguration _config;

        public Worker(ILogger<Worker> logger, IMatchmakingEngineService mmEngineService, IConfiguration config)
        {
            _config = config;
            _logger = logger;
            _mmEngineService = mmEngineService;
            _cachedSessions = new Dictionary<MatchmakingQoSIntervals, Session>();
            _sessionsToStart = new List<Session>();
            _failedMatchmaking = new List<Guid>();

            _maxTimeInLobby = _config.GetValue<int>("Matchmaking:MaxLobbyTime");
            _maxUsersPerSession = _config.GetValue<int>("Matchmaking:MaxUserCount");
            _minUsersToStart = _config.GetValue<int>("Matchmaking:MinUsersToStart");

            foreach (MatchmakingQoSIntervals value in Enum.GetValues(typeof(MatchmakingQoSIntervals)))
            {
                _cachedSessions[value] = new Session(value);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                string[] players;
                string[] qos;
                _mmEngineService.GetPlayerChunkFromQueue(out players, out qos);

                Session[] pendingSessions;
                Session[] startedSessions;
                _mmEngineService.GetSessions(out pendingSessions, out startedSessions);

                // TODO Join on the fly
                ProcessOldPlayers(ref pendingSessions, ref startedSessions);
                if (players.Length > 0)
                    ProcessNewPlayers(players, qos, ref pendingSessions, ref startedSessions);

                StartSessions();

                await Task.Delay(5000, stoppingToken);
            }
        }

        private void ProcessNewPlayers(string[] players, string[] playerQoS, ref Session[] pendingSessions, ref Session[] startedSessions)
        {
            _logger?.LogDebug($"Found {players.Length} new players in queue");
            
            if (players.Length != playerQoS.Length)
            {
                _logger?.LogError("[MatchmakingEngine]ProcessNewPlayers: Lists went out of synch");
            }

            MatchmakingQoSIntervals playerPing = MatchmakingQoSIntervals.Invalid;
            for (int i = 0; i < players.Length; i++)
            {
                int playerQos;
                if (i < playerQoS.Length) //if for some reason in the world the qos list goes out of sych, just drop the qos prioritization
                {
                    bool success = int.TryParse(playerQoS[i].Split(":")[1], out playerQos);
                    if (!success)
                    {
                        _logger?.LogError($"[MatchmakingEngine]ProcessNewPlayers: Could not parse the qos for a player, defaulting to 100 ping for player {players[i]}");
                        playerQos = 100;
                    }
                }
                else 
                {
                    _logger?.LogError("[MatchmakingEngine]ProcessNewPlayers: QoS out of synch with the player list, dropping qos prioritization");
                    playerQos = 100;
                }
                foreach (MatchmakingQoSIntervals value in Enum.GetValues(typeof(MatchmakingQoSIntervals)))
                {
                    if (value != MatchmakingQoSIntervals.Invalid && MatchmakingIntervalMappings[value] >= playerQos)
                    {
                        playerPing = value;
                        break;
                    }
                }

                _cachedSessions[playerPing].Users.Add(new Guid(players[i]));

                if (_cachedSessions[playerPing].Users.Count >= _maxUsersPerSession)
                {
                    _sessionsToStart.Add(_cachedSessions[playerPing]);
                    _cachedSessions[playerPing] = new Session(playerPing);
                    _logger?.LogDebug("A session has been filled up.");
                }
            }
        }

        private void StartSessions()
        {
            if(_sessionsToStart.Count > 0)
            {
                _logger?.LogDebug($"Starting up {_sessionsToStart.Count} new sessions.");
                _mmEngineService.CreateSessions(_sessionsToStart, SessionState.Full);
                _sessionsToStart.Clear();
            }
        }

        private void ProcessOldPlayers(ref Session[] pendingSessions, ref Session[] startedSessions)
        {
            long crtUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int playersWaitingAlone = 0;
            _cachedSessions.Select(i => i.Value).
                Where(d => d.MatchmakingGroup != MatchmakingQoSIntervals.Invalid && (crtUnix - d.CreatedTime) >= _maxTimeInLobby && d.Users.Count > 0).ToList().
                ForEach(s => 
                {
                    if (s.Users.Count < _minUsersToStart)
                    {
                        playersWaitingAlone += s.Users.Count;
                    }else
                    {
                        _sessionsToStart.Add(s); 
                        _cachedSessions[s.MatchmakingGroup] = new Session(s.MatchmakingGroup);
                    }
                });

            if (playersWaitingAlone > 0)
            {
                _logger?.LogDebug($"{playersWaitingAlone} players are waiting alone, cascading players in order to create a session");
                foreach (MatchmakingQoSIntervals first in Enum.GetValues(typeof(MatchmakingQoSIntervals)))
                {
                    foreach (MatchmakingQoSIntervals next in Enum.GetValues(typeof(MatchmakingQoSIntervals)))
                    {
                        if (next == 0)
                            continue;
                        _cachedSessions[first].Users.AddRange(_cachedSessions[next].Users);
                        playersWaitingAlone -= _cachedSessions[next].Users.Count;
                        _cachedSessions[next].Users.Clear();
                        if (_cachedSessions[first].Users.Count >= _minUsersToStart)
                        {
                            _sessionsToStart.Add(_cachedSessions[first]);
                            _cachedSessions[first] = new Session(first);
                        }
                    }
                }
            }
        }
    }
}
