using AegisAccess.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Persistence;

public class AegisDbContext : DbContext {
    public AegisDbContext(DbContextOptions<AegisDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
        builder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.Entity<User>(e => { e.HasIndex(u => u.Email).IsUnique(); e.HasIndex(u => u.Username).IsUnique(); });
        builder.Entity<Department>().HasIndex(d => d.Name).IsUnique();
        builder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        builder.Entity<Permission>().HasIndex(p => p.Name).IsUnique();
    }
}