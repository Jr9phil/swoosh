using Microsoft.EntityFrameworkCore;
using Swoosh.Api.Domain;

namespace Swoosh.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<RecurringTask> RecurringTasks => Set<RecurringTask>();
    public DbSet<NoteCard> NoteCards => Set<NoteCard>();
    public DbSet<Reminder> Reminders => Set<Reminder>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasIndex(t => new { t.UserId });

        modelBuilder.Entity<RecurringTask>()
            .HasIndex(r => r.UserId);

        modelBuilder.Entity<NoteCard>()
            .HasIndex(n => n.UserId);

        modelBuilder.Entity<Reminder>()
            .HasIndex(r => r.UserId);
    }
}