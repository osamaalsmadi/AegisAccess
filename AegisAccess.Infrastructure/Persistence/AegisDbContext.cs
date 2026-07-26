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
    public DbSet<SodPolicy> SodPolicies => Set<SodPolicy>();
    public DbSet<AccessReviewCampaign> AccessReviewCampaigns => Set<AccessReviewCampaign>();
    public DbSet<AccessReviewItem> AccessReviewItems => Set<AccessReviewItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserRole>().HasKey(item => new { item.UserId, item.RoleId });
        builder.Entity<RolePermission>().HasKey(item => new { item.RoleId, item.PermissionId });

        builder.Entity<User>(entity =>
        {
            entity.HasIndex(item => item.Email).IsUnique();
            entity.HasIndex(item => item.Username).IsUnique();
        });

        builder.Entity<Department>().HasIndex(item => item.Name).IsUnique();
        builder.Entity<Role>().HasIndex(item => item.Name).IsUnique();
        builder.Entity<Permission>().HasIndex(item => item.Name).IsUnique();

        builder.Entity<AccessRequest>(entity =>
        {
            entity.HasIndex(item => item.CorrelationId).IsUnique();
            entity.HasIndex(item => new { item.RequesterId, item.Status });
            entity.HasIndex(item => new { item.ResourceId, item.RequestedRoleId });
            entity.Property(item => item.BusinessJustification).HasMaxLength(500);
            entity.Property(item => item.DecisionComment).HasMaxLength(500);
            entity.Property(item => item.RiskReasons).HasMaxLength(2000);
            entity.Property(item => item.Recommendation).HasMaxLength(500);

            entity.HasOne(item => item.Requester)
                .WithMany()
                .HasForeignKey(item => item.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.DecidedBy)
                .WithMany()
                .HasForeignKey(item => item.DecidedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Resource)
                .WithMany()
                .HasForeignKey(item => item.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.RequestedRole)
                .WithMany()
                .HasForeignKey(item => item.RequestedRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SodPolicy>(entity =>
        {
            entity.HasIndex(item => item.Name).IsUnique();
            entity.HasIndex(item => new { item.FirstRoleId, item.SecondRoleId }).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.Property(item => item.Severity).HasMaxLength(20);

            entity.HasOne(item => item.FirstRole)
                .WithMany()
                .HasForeignKey(item => item.FirstRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.SecondRole)
                .WithMany()
                .HasForeignKey(item => item.SecondRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AccessReviewCampaign>(entity =>
        {
            entity.HasIndex(item => item.Name);
            entity.HasIndex(item => new { item.Status, item.DueAtUtc });
            entity.Property(item => item.Name).HasMaxLength(120);
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.Property(item => item.CreatedByUsername).HasMaxLength(100);
        });

        builder.Entity<AccessReviewItem>(entity =>
        {
            entity.HasIndex(item => new { item.CampaignId, item.AccessRequestId }).IsUnique();
            entity.Property(item => item.ReviewerUsername).HasMaxLength(100);
            entity.Property(item => item.ReviewComment).HasMaxLength(500);

            entity.HasOne(item => item.Campaign)
                .WithMany(campaign => campaign.Items)
                .HasForeignKey(item => item.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.AccessRequest)
                .WithMany()
                .HasForeignKey(item => item.AccessRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
