using Microsoft.EntityFrameworkCore;
using VacationPlanner.Api.Models;

namespace VacationPlanner.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<EmployeeProject> EmployeeProjects => Set<EmployeeProject>();
    public DbSet<Vacation> Vacations => Set<Vacation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeProject>(entity =>
        {
            entity.HasKey(ep => new { ep.EmployeeId, ep.ProjectId });

            entity.HasOne(ep => ep.Employee)
                .WithMany(e => e.EmployeeProjects)
                .HasForeignKey(ep => ep.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ep => ep.Project)
                .WithMany(p => p.EmployeeProjects)
                .HasForeignKey(ep => ep.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.FullName);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.KeycloakUserId);

        modelBuilder.Entity<Vacation>()
            .HasOne(v => v.Employee)
            .WithMany(e => e.Vacations)
            .HasForeignKey(v => v.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vacation>()
            .HasIndex(v => new { v.EmployeeId, v.StartDate, v.EndDate });
    }
}
