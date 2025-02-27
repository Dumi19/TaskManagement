using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Models
{
    public class TaskTime
    {
        [Key]
        public int Id { get; set; }
        public string TaskName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int UserId { get; set; } // Foreign key to User
        public User User { get; set; } // Navigation property to User
    }
}
