using System;

namespace EmployeeWellnessAPI.Models
{
    public class Participant
    {
        public Guid Id { get; set; }
        public Guid ChallengeId { get; set; }
        public Guid UserId { get; set; }

        public Challenge Challenge { get; set; } = null!;
    }
}
