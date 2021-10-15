using MatchmakingEngine.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MatchmakingEngine.Service
{
    public interface IMatchmakingEngineService
    {
        public void GetPlayerChunkFromQueue(out string[] players, out string[] qos);
        public void GetSessions(out Session[] pendingSessions, out Session[] startedSessions);
        public bool CreateSessions(List<Session> sessions, SessionState state);
    }
}
