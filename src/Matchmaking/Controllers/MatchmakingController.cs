using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Matchmaking.Models;
using Matchmaking.Services;
using System;
using System.Net;

namespace Matchmaking.Controllers
{
    [Produces("application/json")]
    [Route("v1/matchmaking")]
    public class MatchmakingController : Controller
    {
        private IMatchmakingService _matchmakingService;
        private ILogger<MatchmakingController> _logger;
        public MatchmakingController(IMatchmakingService service, ILogger<MatchmakingController> logger)
        {
            _matchmakingService = service;
            _logger = logger;
        }

        [HttpPost("join")]
        public ActionResult StartMatchmaking([FromBody] StartMatchmakingBody body)
        {
            var result = _matchmakingService.AddToMatchmakingQueue(body.ProfileId, body.QoS);
            if (!result)
            {
                _logger?.LogError("Matchmaking queue join error");
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong when trying to join the matchmaking queue");
            }
            return Ok();
        }

        [HttpGet("{profileId}/session")]
        public ActionResult GetMatchmakingSession([FromRoute] Guid profileId)
        {
            var result = _matchmakingService.GetMatchmakingSession(profileId);
            
            if (result == null)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "User did not join matchmaking");
            }

            if (result == string.Empty)
            {
                _logger?.LogDebug("Session not ready");
                return Ok();
            }
            return Ok(new ReturnSession(result));
        }

        [HttpPost("leave")]
        public ActionResult LeaveMatchmaking([FromBody] StartMatchmakingBody body)
        {
            var result = _matchmakingService.RemoveFromMatchmakingQueue(body.ProfileId);
            if (!result)
            {
                _logger?.LogError("Matchmaking queue leave error");
                return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong when trying to leave the matchmaking queue");
            }
            return Ok();
        }
    }
}
