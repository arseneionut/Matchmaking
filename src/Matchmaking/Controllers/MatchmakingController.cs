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
        private readonly IMatchmakingService _matchmakingService;
        private readonly ILogger<MatchmakingController> _logger;
        public MatchmakingController(IMatchmakingService service, ILogger<MatchmakingController> logger)
        {
            _matchmakingService = service;
            _logger = logger;
        }

        [HttpPost("join")]
        public ActionResult StartMatchmaking([FromBody] StartMatchmakingBody body)
        {
            if (!ModelState.IsValid)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Invalid request body {Request.Body}");
            }

            var result = _matchmakingService.AddToMatchmakingQueue(body.ProfileId, body.QoS);
            if (!result)
            {
                return Ok("User already joined");
            }
            return Ok();
        }

        [HttpGet("{profileId}/session")]
        public ActionResult GetMatchmakingSession([FromRoute] Guid profileId)
        {
            if (!ModelState.IsValid)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Invalid profile id : {profileId}");
            }

            var result = _matchmakingService.GetMatchmakingSession(profileId);
            
            if (result == null)
            {
                _logger?.LogDebug("[Matchmaking]GetMatchmakingSession:User didn't join matchmaking");
                return Ok("User did not join matchmaking");
            }

            if (result == string.Empty)
            {
                _logger?.LogDebug("[Matchmaking]GetMatchmakingSession:Session not ready");
                return Ok();
            }
            return Ok(new ReturnSession(result));
        }

        [HttpPost("leave")]
        public ActionResult LeaveMatchmaking([FromBody] BaseRequestBody body)
        {
            if (!ModelState.IsValid)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Invalid request body {Request.Body}");
            }

            var result = _matchmakingService.RemoveFromMatchmakingQueue(body.ProfileId);
            switch(result)
            {
                case LeaveOutcome.LeaveSuccess: return Ok();
                case LeaveOutcome.Error: return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong when trying to leave the matchmaking queue");
                case LeaveOutcome.CannotLeave: return Ok("Cannot Leave");
                case LeaveOutcome.NotJoined: return Ok("User not in matchmaking");
            }
            return Ok();
        }
    }
}
