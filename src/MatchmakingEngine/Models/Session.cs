using System;
using System.Collections;
using System.Collections.Generic;

namespace MatchmakingEngine.Models
{
    public enum MatchmakingQoSIntervals //order should be high to low
    {
        GroupA,
        GroupB,
        GroupC,
        GroupD,
        Invalid = -1
    }

    public enum SessionState
    {
        Full,
        Started,
        Pending
    }
    public class SessionTimeComparer : IComparer
    {
        public int Compare(Object a, Object b)
        {
            if (((Session)a).CreatedTime < ((Session)b).CreatedTime)
                return 1;
            return 0;
        }
    }
    public class Session
    {
        public Session(MatchmakingQoSIntervals mmGroup)
        {
            MatchmakingGroup = mmGroup;
        }

        public Guid SessionId { get; set; } = Guid.NewGuid();
        public long CreatedTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public List<Guid> Users { get; set; } = new List<Guid>();
        public MatchmakingQoSIntervals MatchmakingGroup { get; set; }
    }
}
