using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Models
{
    public class User
    {

        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string userName { get; set; }

     //   public ICollection<ClockEntry> ClockEntries { get; set; } = new List<ClockEntry>();
        public ICollection<TaskTime> TaskTimes { get; set; } = new List<TaskTime>();
        public string? CurrentTask { get; set; }

        public User() { }

        public User(string name) : this()
        {
            userName = name;
        }

        public void AddTaskTime(string taskName, TaskTime taskTime)
        {
            TaskTimes.Add(taskTime); // Add the TaskTime object to the collection
        }
    }
}

