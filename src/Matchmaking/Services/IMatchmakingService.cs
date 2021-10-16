using System;

namespace Matchmaking.Services
{
    public interface IMatchmakingService
    {
        abstract public bool AddToMatchmakingQueue(Guid profileId, int qos);
        abstract public LeaveOutcome RemoveFromMatchmakingQueue(Guid profileId);
        abstract public string GetMatchmakingSession(Guid profileId);
    }
}
