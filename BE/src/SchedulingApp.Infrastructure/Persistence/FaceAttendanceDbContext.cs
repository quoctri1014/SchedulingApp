using Microsoft.EntityFrameworkCore;
using SchedulingApp.Domain.Entities.FaceAttendance;

namespace SchedulingApp.Infrastructure.Persistence;

public class FaceAttendanceDbContext : DbContext
{
    public FaceAttendanceDbContext(DbContextOptions<FaceAttendanceDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserCompanyAssignment> UserCompanyAssignments { get; set; } = null!;
    public DbSet<UserDepartmentAssignment> UserDepartmentAssignments { get; set; } = null!;
    public DbSet<FaceCompany> Companies { get; set; } = null!;
    public DbSet<FaceDepartment> Departments { get; set; } = null!;
    public DbSet<FaceJobTitle> JobTitles { get; set; } = null!;
    public DbSet<FaceCompanyShift> CompanyShifts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<UserCompanyAssignment>(entity =>
        {
            entity.ToTable("UserCompanyAssignments");
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.User)
                .WithMany(p => p.CompanyAssignments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserDepartmentAssignment>(entity =>
        {
            entity.ToTable("UserDepartmentAssignments");
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.User)
                .WithMany(p => p.DepartmentAssignments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FaceCompany>(entity =>
        {
            entity.ToTable("Companies");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<FaceDepartment>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<FaceJobTitle>(entity =>
        {
            entity.ToTable("JobTitles");
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<FaceCompanyShift>(entity =>
        {
            entity.ToTable("CompanyShifts");
            entity.HasKey(e => e.Id);
        });
    }
}
