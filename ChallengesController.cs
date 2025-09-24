using EmployeeWellnessAPI.Models;
using EmployeeWellnessAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeWellnessAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChallengesController : ControllerBase
    {
        private readonly IChallengeService _challengeService;
        private readonly RabbitMqService _rabbitMqService;

        public ChallengesController(IChallengeService challengeService, RabbitMqService rabbitMqService)
        {
            _challengeService = challengeService;
            _rabbitMqService = rabbitMqService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateChallenge([FromBody] Challenge challenge)
        {
            var created = await _challengeService.CreateChallengeAsync(challenge);
            return CreatedAtAction(nameof(GetChallenge), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChallenge(Guid id)
        {
            var challenge = await _challengeService.GetChallengeByIdAsync(id);
            if (challenge == null)
                return NotFound();
            return Ok(challenge);
        }

        [HttpGet("/api/users/{userId}/challenges/active")]
        public async Task<IActionResult> GetActiveChallengesForUser(Guid userId)
        {
            var challenges = await _challengeService.GetActiveChallengesForUserAsync(userId);
            return Ok(challenges);
        }

        [HttpPost("{challengeId}/participants")]
        public async Task<IActionResult> AddParticipant(Guid challengeId, [FromBody] AddParticipantDto dto)
        {
            var participant = await _challengeService.AddParticipantAsync(challengeId, dto.UserId);

            var result = new ParticipantDto
            {
                Id = participant.Id,
                UserId = participant.UserId,
                ChallengeId = participant.Challenge.Id
            };

            return CreatedAtAction(nameof(GetChallenge), new { id = challengeId }, result);
        }

        [HttpPost("{challengeId}/progress")]
        public IActionResult SubmitProgress(Guid challengeId, [FromBody] ProgressEntryDto dto)
        {
            var message = new ProgressMessage
            {
                ChallengeId = challengeId,
                UserId = dto.UserId,
                Value = dto.Value,
                Timestamp = DateTime.UtcNow
            };

            _rabbitMqService.Publish(message);

            return Accepted(dto); // 202 Accepted
        }

        // GET /api/challenges/{challengeId}/leaderboard
        [HttpGet("{challengeId}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(Guid challengeId)
        {
            var leaderboard = await _challengeService.GetLeaderboardAsync(challengeId);
            return Ok(leaderboard);
        }
    }
}
