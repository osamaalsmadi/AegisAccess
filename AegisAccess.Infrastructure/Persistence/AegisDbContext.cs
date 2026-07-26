using AegisAccess.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Persistence;

public class AegisDbContext : DbContext
{
    public AegisDbContext(DbContextOptions<AegisDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.Username).IsUnique();
        });

        builder.Entity<Department>().HasIndex(department => department.Name).IsUnique();
        builder.Entity<Role>().HasIndex(role => role.Name).IsUnique();
        builder.Entity<Permission>().HasIndex(permission => permission.Name).IsUnique();

        builder.Entity<AccessRequest>(entity =>
        {
            entity.HasIndex(request => request.CorrelationId).IsUnique();
            entity.HasIndex(request => new { request.RequesterId, request.Status });
            entity.HasIndex(request => new { request.ResourceId, request.RequestedRoleId });

            entity.Property(request => request.BusinessJustification).HasMaxLength(500);
            entity.Property(request => request.DecisionComment).HasMaxLength(500);
            entity.Property(request => request.RiskReasons).HasMaxLength(2000);
            entity.Property(request => request.Recommendation).HasMaxLength(500);

            entity.HasOne(request => request.Requester)
                .WithMany()
                .HasForeignKey(request => request.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.DecidedBy)
                .WithMany()
                .HasForeignKey(request => request.DecidedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.Resource)
                .WithMany()
                .HasForeignKey(request => request.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.RequestedRole)
                .WithMany()
                .HasForeignKey(request => request.RequestedRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
