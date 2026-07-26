using AegisAccess.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Persistence;

public static class AccessGovernanceSeeder
{
    public static async Task SeedAsync(AegisDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.AccessRequests.AnyAsync(cancellationToken))
        {
            return;
        }

        var employee = await db.Users.SingleOrDefaultAsync(user => user.Username == "employee", cancellationToken);
        var approver = await db.Users.SingleOrDefaultAsync(user => user.Username == "approver", cancellationToken);
        var finance = await db.Resources.SingleOrDefaultAsync(resource => resource.Name == "Finance Database", cancellationToken);
        var itAdmin = await db.Resources.SingleOrDefaultAsync(resource => resource.Name == "IT Administration", cancellationToken);
        var employeeRole = await db.Roles.SingleOrDefaultAsync(role => role.Name == "Employee", cancellationToken);
        var securityAdminRole = await db.Roles.SingleOrDefaultAsync(role => role.Name == "SecurityAdmin", cancellationToken);

        if (employee is null || approver is null || finance is null || itAdmin is null ||
            employeeRole is null || securityAdminRole is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        db.AccessRequests.AddRange(
            new AccessRequest
            {
                CorrelationId = $"ACC-SEED-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                RequesterId = employee.Id,
                ResourceId = finance.Id,
                RequestedRoleId = employeeRole.Id,
                BusinessJustification = "Quarterly finance reporting duties require time-limited read access.",
                DurationDays = 30,
                RequestedAtUtc = now.AddHours(-4),
                AccessExpiresAtUtc = now.AddDays(30),
                Status = AccessRequestStatus.Pending,
                RiskScore = 45,
                RiskLevel = AccessRiskLevel.Medium,
                RiskReasons = "Target resource is classified as High sensitivity.",
                Recommendation = "Approver must verify business need and least privilege."
            },
            new AccessRequest
            {
                CorrelationId = $"ACC-SEED-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                RequesterId = approver.Id,
                ResourceId = itAdmin.Id,
                RequestedRoleId = securityAdminRole.Id,
                BusinessJustification = "Temporary privileged access requested for emergency infrastructure maintenance.",
                DurationDays = 7,
                RequestedAtUtc = now.AddHours(-2),
                AccessExpiresAtUtc = now.AddDays(7),
                Status = AccessRequestStatus.Escalated,
                RiskScore = 100,
                RiskLevel = AccessRiskLevel.Critical,
                RiskReasons = "Target resource is classified as Critical.|SecurityAdmin grants privileged administrative access.",
                Recommendation = "SecurityAdmin review is mandatory before approval."
            });

        db.AuditEvents.Add(new AuditEvent
        {
            Action = "AccessGovernanceSeeded",
            Actor = "System",
            Target = "AccessRequests",
            Details = "Created demonstration Pending and Escalated access requests."
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
