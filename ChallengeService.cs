using EmployeeWellnessAPI.Data;
using EmployeeWellnessAPI.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace EmployeeWellnessAPI.Services
{
    public class ChallengeService : IChallengeService
    {
        private readonly AppDbContext _context;
        private readonly IConnectionMultiplexer _redis;

        public ChallengeService(AppDbContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis;
        }

        public async Task<Challenge> CreateChallengeAsync(Challenge challenge)
        {
            _context.Challenges.Add(challenge);
            await _context.SaveChangesAsync();
            return challenge;
        }

        public async Task<Challenge> GetChallengeByIdAsync(Guid challengeId)
        {
            return await _context.Challenges
                .Include(c => c.Participants)
                .Include(c => c.ProgressEntries)
                .FirstOrDefaultAsync(c => c.Id == challengeId);
        }

        public async Task<List<Challenge>> GetActiveChallengesForUserAsync(Guid userId)
        {
            var today = DateTime.UtcNow;
            return await _context.Participants
                .Where(p => p.UserId == userId && p.Challenge.StartDate <= today && p.Challenge.EndDate >= today)
                .Select(p => p.Challenge)
                .Distinct()  // ✅ Ensure each challenge appears only once
                .ToListAsync();
        }

        public async Task<Participant> AddParticipantAsync(Guid challengeId, Guid userId)
        {
            var challenge = await _context.Challenges.FindAsync(challengeId);
            if (challenge == null)
                throw new Exception("Challenge not found");

            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Challenge = challenge
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();

            return participant;
        }

        public async Task<ProgressEntry> SubmitProgressAsync(Guid challengeId, Guid userId, int value)
        {
            var participant = await _context.Participants
                .Include(p => p.Challenge)
                .FirstOrDefaultAsync(p => p.Challenge.Id == challengeId && p.UserId == userId);

            if (participant == null)
                throw new Exception("Participant not found");

            var progress = new ProgressEntry
            {
                Id = Guid.NewGuid(),
                Challenge = participant.Challenge,
                UserId = userId,
                Value = value,
                Timestamp = DateTime.UtcNow
            };

            _context.ProgressEntries.Add(progress);
            await _context.SaveChangesAsync();

            return progress;
        }

        public async Task<List<LeaderboardDto>> GetLeaderboardAsync(Guid challengeId)
        {
            var db = _redis.GetDatabase();

            // 1️⃣ Try cache first
            var cached = await db.StringGetAsync($"leaderboard:{challengeId}");
            if (cached.HasValue)
            {
                return JsonSerializer.Deserialize<List<LeaderboardDto>>(cached)!;
            }

            // 2️⃣ If not cached, compute from DB
            var leaderboard = await _context.ProgressEntries
                .Where(pe => pe.Challenge.Id == challengeId)
                .GroupBy(pe => pe.UserId)
                .Select(g => new LeaderboardDto
                {
                    UserId = g.Key,
                    TotalValue = g.Sum(pe => pe.Value)
                })
                .OrderByDescending(x => x.TotalValue)
                .Take(10)
                .ToListAsync();

            // 3️⃣ Store in Redis (cache for 10 seconds)
            await db.StringSetAsync(
                $"leaderboard:{challengeId}",
                JsonSerializer.Serialize(leaderboard),
                TimeSpan.FromSeconds(10));

            return leaderboard;
        }
    }
}
