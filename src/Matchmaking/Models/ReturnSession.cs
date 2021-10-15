namespace Matchmaking.Models
{
    public class ReturnSession
    {
        public ReturnSession() { }
        public ReturnSession(string session)
        {
            SessionId = session;
        }
        public string SessionId { get; set; } = string.Empty;
    }
}
