using EmployeeWellnessAPI.Models;

namespace EmployeeWellnessAPI.Services
{
    public interface IChallengeService
    {
        Task<Challenge> CreateChallengeAsync(Challenge challenge);
        Task<Challenge> GetChallengeByIdAsync(Guid challengeId);       // new
        Task<List<Challenge>> GetActiveChallengesForUserAsync(Guid userId);
        Task<Participant> AddParticipantAsync(Guid challengeId, Guid userId);

        // New methods
        Task<ProgressEntry> SubmitProgressAsync(Guid challengeId, Guid userId, int value);
        Task<List<LeaderboardDto>> GetLeaderboardAsync(Guid challengeId);
    }
}
