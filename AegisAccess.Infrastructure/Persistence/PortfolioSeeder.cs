using AegisAccess.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Persistence;

public static class PortfolioSeeder
{
    public static async Task SeedAsync(
        AegisDbContext db,
        CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles.ToDictionaryAsync(role => role.Name, cancellationToken);

        if (roles.TryGetValue("SecurityAdmin", out var securityAdmin) &&
            roles.TryGetValue("Auditor", out var auditor) &&
            !await db.SodPolicies.AnyAsync(
                policy => policy.Name == "Administration and Audit Independence",
                cancellationToken))
        {
            db.SodPolicies.Add(new SodPolicy
            {
                Name = "Administration and Audit Independence",
                Description = "A user must not administer security controls and independently audit those same controls.",
                Severity = "Critical",
                FirstRoleId = securityAdmin.Id,
                SecondRoleId = auditor.Id
            });
        }

        if (roles.TryGetValue("SecurityAdmin", out securityAdmin) &&
            roles.TryGetValue("Approver", out var approver) &&
            !await db.SodPolicies.AnyAsync(
                policy => policy.Name == "Approval and Administration Separation",
                cancellationToken))
        {
            db.SodPolicies.Add(new SodPolicy
            {
                Name = "Approval and Administration Separation",
                Description = "A user should not both approve privileged access and execute administrative changes.",
                Severity = "High",
                FirstRoleId = securityAdmin.Id,
                SecondRoleId = approver.Id
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var hasApprovedAccess = await db.AccessRequests.AnyAsync(
            request =>
                request.Status == AccessRequestStatus.Approved &&
                request.AccessExpiresAtUtc > now,
            cancellationToken);

        if (!hasApprovedAccess &&
            roles.TryGetValue("Auditor", out auditor))
        {
            var auditUser = await db.Users.SingleOrDefaultAsync(
                user => user.Username == "auditor",
                cancellationToken);
            var hrPortal = await db.Resources.SingleOrDefaultAsync(
                resource => resource.Name == "HR Portal",
                cancellationToken);
            var adminUser = await db.Users.SingleOrDefaultAsync(
                user => user.Username == "securityadmin",
                cancellationToken);

            if (auditUser is not null && hrPortal is not null && adminUser is not null)
            {
                db.AccessRequests.Add(new AccessRequest
                {
                    CorrelationId = $"ACC-ENT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    RequesterId = auditUser.Id,
                    ResourceId = hrPortal.Id,
                    RequestedRoleId = auditor.Id,
                    BusinessJustification = "Approved compliance evidence access for the quarterly audit cycle.",
                    DurationDays = 90,
                    RequestedAtUtc = now.AddDays(-5),
                    AccessExpiresAtUtc = now.AddDays(85),
                    Status = AccessRequestStatus.Approved,
                    RiskScore = 38,
                    RiskLevel = AccessRiskLevel.Medium,
                    RiskReasons = "Target resource has Medium sensitivity.|Auditor can view sensitive governance evidence.",
                    Recommendation = "Include in the next access certification campaign.",
                    DecidedById = adminUser.Id,
                    DecidedAtUtc = now.AddDays(-4),
                    DecisionComment = "Approved for the quarterly compliance audit."
                });

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        if (!await db.AccessReviewCampaigns.AnyAsync(cancellationToken))
        {
            var entitlements = await db.AccessRequests
                .Where(request =>
                    request.Status == AccessRequestStatus.Approved &&
                    request.AccessExpiresAtUtc > now)
                .ToListAsync(cancellationToken);

            if (entitlements.Count > 0)
            {
                var campaign = new AccessReviewCampaign
                {
                    Name = "Q3 Privileged Access Certification",
                    Description = "Periodic review of active access to confirm business need and least privilege.",
                    CreatedByUsername = "securityadmin",
                    CreatedAtUtc = now.AddDays(-1),
                    DueAtUtc = now.AddDays(13),
                    Status = AccessReviewCampaignStatus.Active
                };

                foreach (var entitlement in entitlements)
                {
                    campaign.Items.Add(new AccessReviewItem
                    {
                        AccessRequestId = entitlement.Id,
                        RiskAtReview = entitlement.RiskScore
                    });
                }

                db.AccessReviewCampaigns.Add(campaign);
                db.AuditEvents.Add(new AuditEvent
                {
                    Action = "PortfolioGovernanceSeeded",
                    Actor = "System",
                    Target = campaign.Name,
                    Details = $"Policies=2; ReviewItems={campaign.Items.Count}"
                });

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
