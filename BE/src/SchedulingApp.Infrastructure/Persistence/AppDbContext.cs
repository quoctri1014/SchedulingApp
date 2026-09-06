using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchedulingApp.Domain.Entities;

namespace SchedulingApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<EmployeeAssignment> EmployeeAssignments { get; set; } = null!;
    public DbSet<EmployeeLeave> EmployeeLeaves { get; set; } = null!;
    public DbSet<EmployeePreference> EmployeePreferences { get; set; } = null!;
    public DbSet<Shift> Shifts { get; set; } = null!;
    public DbSet<AlgorithmRun> AlgorithmRuns { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Suppress pending model changes warning for migration purposes
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters for Soft-Delete
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Shift>().HasQueryFilter(e => !e.IsDeleted);

        // Prevent multiple cascade paths error
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
