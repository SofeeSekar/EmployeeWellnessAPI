namespace EmployeeWellnessAPI.Models
{
    public class ProgressMessage
    {
        public Guid ChallengeId { get; set; }
        public Guid UserId { get; set; }
        public int Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
