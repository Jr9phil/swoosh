using Microsoft.EntityFrameworkCore;
using Swoosh.Api.Domain;

namespace Swoosh.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasIndex(t => new { t.UserId, t.IsCompleted });
    }
}