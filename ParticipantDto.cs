namespace EmployeeWellnessAPI.Models
{
    public class ParticipantDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ChallengeId { get; set; }
    }
}
