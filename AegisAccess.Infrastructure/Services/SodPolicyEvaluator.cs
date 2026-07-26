using AegisAccess.Application.Portfolio;
using AegisAccess.Domain.Entities;
using AegisAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Services;

public sealed class SodPolicyEvaluator : ISodPolicyEvaluator
{
    private readonly AegisDbContext _db;

    public SodPolicyEvaluator(AegisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SodConflict>> FindConflictsAsync(
        Guid userId,
        Guid requestedRoleId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var existingRoleIds = await _db.AccessRequests
            .AsNoTracking()
            .Where(request =>
                request.RequesterId == userId &&
                request.Status == AccessRequestStatus.Approved &&
                request.AccessExpiresAtUtc > now)
            .Select(request => request.RequestedRoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (existingRoleIds.Count == 0)
        {
            return Array.Empty<SodConflict>();
        }

        var policies = await _db.SodPolicies
            .AsNoTracking()
            .Include(policy => policy.FirstRole)
            .Include(policy => policy.SecondRole)
            .Where(policy =>
                policy.IsActive &&
                ((policy.FirstRoleId == requestedRoleId && existingRoleIds.Contains(policy.SecondRoleId)) ||
                 (policy.SecondRoleId == requestedRoleId && existingRoleIds.Contains(policy.FirstRoleId))))
            .ToListAsync(cancellationToken);

        return policies.Select(policy =>
        {
            var requestedIsFirst = policy.FirstRoleId == requestedRoleId;
            return new SodConflict(
                policy.Id,
                policy.Name,
                policy.Severity,
                requestedIsFirst ? policy.SecondRole!.Name : policy.FirstRole!.Name,
                requestedIsFirst ? policy.FirstRole!.Name : policy.SecondRole!.Name,
                policy.Description);
        }).ToList();
    }
}
