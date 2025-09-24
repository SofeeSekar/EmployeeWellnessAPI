using Microsoft.EntityFrameworkCore;
using EmployeeWellnessAPI.Models; // (we'll create these next)

namespace EmployeeWellnessAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<ProgressEntry> ProgressEntries { get; set; }
        public DbSet<Participant> Participants { get; set; }
    }
}
