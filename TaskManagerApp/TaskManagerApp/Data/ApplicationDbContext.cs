using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Models;

namespace TaskManagerApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TaskTime> TaskTimes { get; set; } // Add this
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Specify the schema for the tables
            modelBuilder.Entity<User>().ToTable("Users", "timemanager");
            modelBuilder.Entity<TaskTime>().ToTable("TaskTimes", "timemanager");
        }
    }
}

