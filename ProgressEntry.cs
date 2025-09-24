using System;

namespace EmployeeWellnessAPI.Models
{
    public class ProgressEntry
    {
        public Guid Id { get; set; }
        public Guid ChallengeId { get; set; }
        public Guid UserId { get; set; }
        public int Value { get; set; }
        public DateTime Timestamp { get; set; }

        public Challenge Challenge { get; set; } = null!;
    }
}
