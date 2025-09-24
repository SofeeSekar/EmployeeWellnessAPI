using System;
using System.Collections.Generic;

namespace EmployeeWellnessAPI.Models
{
    public class Challenge
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Goal { get; set; } = string.Empty;
        public ICollection<ProgressEntry> ProgressEntries { get; set; } = new List<ProgressEntry>();
        public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    }
}
