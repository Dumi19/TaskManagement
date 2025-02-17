using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Models
{
    public class ClockEntry
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } // Navigation property

        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; } // Nullable for unclocked out entries
    }
}
