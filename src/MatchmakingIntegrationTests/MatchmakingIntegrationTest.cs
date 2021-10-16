using System.Text;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Matchmaking;
using Matchmaking.Models;
using MatchmakingEngine;
using MatchmakingEngine.Service;
using StackExchange.Redis;
using Xunit;

namespace MatchmakingIntegrationTests
{
    public class StartupFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IMatchmakingEngineService, MatchmakingEngineService>();
                services.AddHostedService<Worker>();
            }).UseEnvironment("");
            base.ConfigureWebHost(builder);
        }
    }

    [CollectionDefinition("Non-Parallel Collection", DisableParallelization = true)]
    public class NonParallelCollectionDefinitionClass
    {
    }

    [Collection("Non-Parallel Collection")]
    public class MatchmakingIntegrationTest : IClassFixture<StartupFactory>
    {
        private ConnectionMultiplexer _redis;
        private IDatabase _db;
        private StartupFactory _factory;
        public MatchmakingIntegrationTest(StartupFactory factory)
        {
            _redis = ConnectionMultiplexer.Connect("127.0.0.1");
            _db = _redis.GetDatabase();

            _factory = factory;
        }
        public enum Verb
        {
            GET,
            POST,
            PUT,
            DELETE
        }
        private async Task<HttpResponseMessage> HttpCall(Verb verb, string url, string body = "")
        {
            HttpClient client = _factory.CreateClient();
            HttpResponseMessage response = new HttpResponseMessage();
            switch (verb)
            {
                case Verb.GET:
                    {
                        response = await client.GetAsync(url);
                        break;
                    };
                case Verb.POST:
                    {
                        HttpContent content = new StringContent(body, Encoding.UTF8, "application/json");
                        response = await client.PostAsync(url, content);
                        break;
                    };
            }

            return response;
        }

        private void FlushRedis()
        {
            var result = _db.ExecuteAsync("FLUSHALL");
            _db.Wait(result);
        }

        private Task<HttpResponseMessage> JoinMatchmaking(string guid, int qos = 10)
        {
            var Url = $"v1/matchmaking/join";
            string body = JsonConvert.SerializeObject(new { ProfileId = guid, QoS = qos });
            return HttpCall(Verb.POST, Url, body);
        }

        private Task<HttpResponseMessage> GetSession(string guid)
        {
            var Url = $"v1/matchmaking/{guid}/session";
            return HttpCall(Verb.GET, Url);
        }

        private Task<HttpResponseMessage> LeaveMatchmaking(string guid)
        {
            var Url = $"v1/matchmaking/leave";
            string body = JsonConvert.SerializeObject(new { ProfileId = guid});
            return HttpCall(Verb.POST, Url, body);
        }

        [Fact]
        public async Task TestJoinMatchmaking()
        {
            FlushRedis();
            var response = await JoinMatchmaking(Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TestJoinMatchmakingWrongGuid()
        {
            FlushRedis();
            var response = await JoinMatchmaking("WRONG_GUID");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task TestLeaveMatchmaking()
        {
            FlushRedis();
            Guid player = Guid.NewGuid();
            var response = await JoinMatchmaking(player.ToString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseLeave = await LeaveMatchmaking(player.ToString());
            Assert.Equal(HttpStatusCode.OK, responseLeave.StatusCode);
        }

        [Fact]
        public async Task TestLeaveMatchmakingWrongGuid()
        {
            FlushRedis();
            var responseLeave = await LeaveMatchmaking("WRONG_GUID");
            Assert.Equal(HttpStatusCode.InternalServerError, responseLeave.StatusCode);
        }

        [Fact]
        public async Task TestGetSession()
        {
            var response = await GetSession(Guid.NewGuid().ToString());
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task TestGetSessionWrongGuid()
        {
            var response = await GetSession("WRONG_GUID");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task TestJoinAndGetSession()
        {
            FlushRedis();
            Guid profile = Guid.NewGuid();

            var joinResp = await JoinMatchmaking(profile.ToString());
            Assert.Equal(HttpStatusCode.OK, joinResp.StatusCode);

            var getResp = await GetSession(profile.ToString());
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        }

        [Fact]
        public async Task TestMatchmakeIntoSessionNotFull()
        {
            FlushRedis();
            Guid pp1 = Guid.NewGuid();
            Guid pp2 = Guid.NewGuid();

            await JoinMatchmaking(pp1.ToString());
            await JoinMatchmaking(pp2.ToString());

            ReturnSession session = new ReturnSession();
            while (session.SessionId == "")
            {
                var body = await GetSession(pp1.ToString()).Result.Content.ReadAsStringAsync();
                session = JsonConvert.DeserializeObject<ReturnSession>(body);
                if (session == null)
                {
                    session = new ReturnSession();
                }
                Task.Delay(500).Wait();
            }
            Assert.NotEqual(new Guid(session.SessionId), Guid.Empty);
        }

        [Fact]
        public async Task TestMatchmakeIntoSessionFull()
        {
            FlushRedis();
            Guid pp1 = Guid.NewGuid();
            await JoinMatchmaking(pp1.ToString());
            for (int i= 0; i < 9; i++)
                await JoinMatchmaking(Guid.NewGuid().ToString());

            ReturnSession session = new ReturnSession();
            HttpResponseMessage result = new HttpResponseMessage();
            while (session.SessionId == "")
            {
                result = await GetSession(pp1.ToString());
                var body = await result.Content.ReadAsStringAsync();
                session = JsonConvert.DeserializeObject<ReturnSession>(body);
                if (session == null)
                {
                    session = new ReturnSession();
                }

                Task.Delay(500).Wait();
            }
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotEqual(new Guid(session.SessionId), Guid.Empty);
        }

        [Fact]
        public async Task TestMatchmakeIntoSessionFullTryLeave()
        {
            FlushRedis();
            Guid pp1 = Guid.NewGuid();
            await JoinMatchmaking(pp1.ToString());
            for (int i = 0; i < 9; i++)
                await JoinMatchmaking(Guid.NewGuid().ToString());

            ReturnSession session = new ReturnSession();
            HttpResponseMessage result = new HttpResponseMessage();
            while (session.SessionId == "")
            {
                result = await GetSession(pp1.ToString());
                var body = await result.Content.ReadAsStringAsync();
                session = JsonConvert.DeserializeObject<ReturnSession>(body);
                if (session == null)
                {
                    session = new ReturnSession();
                }

                Task.Delay(500).Wait();
            }
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotEqual(new Guid(session.SessionId), Guid.Empty);
            var leaveResponse = await LeaveMatchmaking(pp1.ToString());
            Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);
            string str = await leaveResponse.Content.ReadAsStringAsync();
            Assert.Equal("\"Cannot Leave\"", str);

        }
    }
}
