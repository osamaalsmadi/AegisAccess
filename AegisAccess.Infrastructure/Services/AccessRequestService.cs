using AegisAccess.Application.AccessGovernance;
using AegisAccess.Application.Portfolio;
using AegisAccess.Domain.Entities;
using AegisAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Services;

public sealed class AccessRequestService : IAccessRequestService
{
    private readonly AegisDbContext _db;
    private readonly IAccessRiskEngine _riskEngine;
    private readonly ISodPolicyEvaluator _sodEvaluator;

    public AccessRequestService(
        AegisDbContext db,
        IAccessRiskEngine riskEngine,
        ISodPolicyEvaluator sodEvaluator)
    {
        _db = db;
        _riskEngine = riskEngine;
        _sodEvaluator = sodEvaluator;
    }

    public async Task<IReadOnlyList<AccessRequestDto>> GetRequestsAsync(
        string actorUsername,
        bool canReviewAll,
        CancellationToken cancellationToken = default)
    {
        var actor = NormalizeUsername(actorUsername);

        var query = _db.AccessRequests
            .AsNoTracking()
            .Include(request => request.Requester)
                .ThenInclude(user => user!.Department)
            .Include(request => request.Resource)
            .Include(request => request.RequestedRole)
            .Include(request => request.DecidedBy)
            .AsQueryable();

        if (!canReviewAll)
        {
            query = query.Where(request => request.Requester!.Username == actor);
        }

        var requests = await query
            .OrderByDescending(request => request.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        return requests.Select(Map).ToList();
    }

    public async Task<AccessCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var resources = await _db.Resources
            .AsNoTracking()
            .OrderBy(resource => resource.Name)
            .Select(resource => new AccessCatalogItemDto
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = resource.Type,
                Metadata = resource.SensitivityLevel
            })
            .ToListAsync(cancellationToken);

        var roles = await _db.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new AccessCatalogItemDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Metadata = role.Name == "SecurityAdmin" || role.Name == "Approver"
                    ? "Privileged"
                    : "Standard"
            })
            .ToListAsync(cancellationToken);

        return new AccessCatalogDto { Resources = resources, Roles = roles };
    }

    public async Task<AccessRequestDto> CreateRequestAsync(
        string actorUsername,
        CreateAccessRequestDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var actor = NormalizeUsername(actorUsername);
        var justification = input.BusinessJustification?.Trim() ?? string.Empty;

        if (input.ResourceId == Guid.Empty || input.RequestedRoleId == Guid.Empty)
        {
            throw new ArgumentException("A resource and role are required.");
        }
        if (justification.Length < 15)
        {
            throw new ArgumentException("Business justification requires at least 15 characters.");
        }
        if (input.DurationDays is < 1 or > 365)
        {
            throw new ArgumentException("Duration must be between 1 and 365 days.");
        }

        var requester = await _db.Users
            .SingleOrDefaultAsync(user => user.Username == actor, cancellationToken)
            ?? throw new KeyNotFoundException("Authenticated user was not found.");

        var resource = await _db.Resources
            .SingleOrDefaultAsync(item => item.Id == input.ResourceId, cancellationToken)
            ?? throw new KeyNotFoundException("Selected resource was not found.");

        var role = await _db.Roles
            .SingleOrDefaultAsync(item => item.Id == input.RequestedRoleId, cancellationToken)
            ?? throw new KeyNotFoundException("Selected role was not found.");

        var duplicate = await _db.AccessRequests.AnyAsync(
            request =>
                request.RequesterId == requester.Id &&
                request.ResourceId == resource.Id &&
                request.RequestedRoleId == role.Id &&
                (request.Status == AccessRequestStatus.Pending ||
                 request.Status == AccessRequestStatus.Escalated),
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("An active duplicate request already exists.");
        }

        var now = DateTime.UtcNow;
        var privilegedCount = await _db.AccessRequests.CountAsync(
            request =>
                request.RequesterId == requester.Id &&
                request.Status == AccessRequestStatus.Approved &&
                request.AccessExpiresAtUtc > now &&
                (request.RequestedRole!.Name == "SecurityAdmin" ||
                 request.RequestedRole.Name == "Approver"),
            cancellationToken);

        var baseRisk = _riskEngine.Evaluate(new AccessRiskContext(
            resource.SensitivityLevel,
            role.Name,
            input.DurationDays,
            justification,
            privilegedCount));

        var conflicts = await _sodEvaluator.FindConflictsAsync(
            requester.Id,
            role.Id,
            cancellationToken);

        var conflictPenalty = conflicts.Count > 0 ? 35 : 0;
        var finalScore = Math.Clamp(baseRisk.Score + conflictPenalty, 0, 100);
        var finalLevel = finalScore switch
        {
            < 30 => "Low",
            < 55 => "Medium",
            < 75 => "High",
            _ => "Critical"
        };

        var reasons = baseRisk.Reasons.ToList();
        reasons.AddRange(conflicts.Select(conflict =>
            $"SoD conflict: {conflict.ExistingRole} + {conflict.RequestedRole} violates {conflict.PolicyName}."));

        var requiresSecurityAdmin = baseRisk.RequiresSecurityAdmin || conflicts.Count > 0;
        var recommendation = conflicts.Count > 0
            ? "Toxic role combination detected. SecurityAdmin exception review is mandatory."
            : baseRisk.Recommendation;

        var request = new AccessRequest
        {
            CorrelationId = $"ACC-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            RequesterId = requester.Id,
            ResourceId = resource.Id,
            RequestedRoleId = role.Id,
            BusinessJustification = justification,
            DurationDays = input.DurationDays,
            RequestedAtUtc = now,
            AccessExpiresAtUtc = now.AddDays(input.DurationDays),
            Status = requiresSecurityAdmin ? AccessRequestStatus.Escalated : AccessRequestStatus.Pending,
            RiskScore = finalScore,
            RiskLevel = Enum.Parse<AccessRiskLevel>(finalLevel),
            RiskReasons = string.Join("|", reasons),
            Recommendation = recommendation
        };

        _db.AccessRequests.Add(request);
        _db.AuditEvents.Add(new AuditEvent
        {
            Action = conflicts.Count > 0 ? "AccessRequestEscalatedBySoD" : "AccessRequestCreated",
            Actor = actor,
            Target = request.CorrelationId,
            Details = $"Resource={resource.Name}; Role={role.Name}; Risk={finalScore}; Conflicts={conflicts.Count}"
        });

        await _db.SaveChangesAsync(cancellationToken);

        request.Requester = requester;
        request.Resource = resource;
        request.RequestedRole = role;
        return Map(request);
    }

    public async Task<AccessRequestDto> DecideRequestAsync(
        Guid requestId,
        string actorUsername,
        bool isSecurityAdmin,
        AccessDecisionDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var actor = NormalizeUsername(actorUsername);

        var request = await _db.AccessRequests
            .Include(item => item.Requester)
                .ThenInclude(user => user!.Department)
            .Include(item => item.Resource)
            .Include(item => item.RequestedRole)
            .Include(item => item.DecidedBy)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Access request was not found.");

        if (request.Status is not AccessRequestStatus.Pending and not AccessRequestStatus.Escalated)
        {
            throw new InvalidOperationException("Only Pending or Escalated requests can be decided.");
        }

        var decider = await _db.Users
            .SingleOrDefaultAsync(user => user.Username == actor, cancellationToken)
            ?? throw new KeyNotFoundException("Decision user was not found.");

        if (decider.Id == request.RequesterId)
        {
            throw new UnauthorizedAccessException("Segregation of duties prevents self-approval.");
        }

        var decision = input.Decision?.Trim().ToLowerInvariant() ?? string.Empty;
        var comment = input.Comment?.Trim() ?? string.Empty;

        if (decision is not "approve" and not "reject")
        {
            throw new ArgumentException("Decision must be Approve or Reject.");
        }
        if (comment.Length < 5)
        {
            throw new ArgumentException("Decision comment requires at least 5 characters.");
        }
        if (decision == "approve" &&
            request.RiskLevel == AccessRiskLevel.Critical &&
            !isSecurityAdmin)
        {
            throw new UnauthorizedAccessException("Critical and SoD-escalated requests require SecurityAdmin approval.");
        }

        request.Status = decision == "approve"
            ? AccessRequestStatus.Approved
            : AccessRequestStatus.Rejected;
        request.DecidedById = decider.Id;
        request.DecidedBy = decider;
        request.DecidedAtUtc = DateTime.UtcNow;
        request.DecisionComment = comment;

        _db.AuditEvents.Add(new AuditEvent
        {
            Action = request.Status == AccessRequestStatus.Approved
                ? "AccessRequestApproved"
                : "AccessRequestRejected",
            Actor = actor,
            Target = request.CorrelationId,
            Details = $"Requester={request.Requester!.Username}; Resource={request.Resource!.Name}; Role={request.RequestedRole!.Name}; Comment={comment}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Map(request);
    }

    private static string NormalizeUsername(string username)
    {
        var value = username?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new UnauthorizedAccessException("Authenticated identity is missing.");
        }
        return value;
    }

    private static AccessRequestDto Map(AccessRequest request)
    {
        var reasons = string.IsNullOrWhiteSpace(request.RiskReasons)
            ? Array.Empty<string>()
            : request.RiskReasons.Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new AccessRequestDto
        {
            Id = request.Id,
            CorrelationId = request.CorrelationId,
            RequesterUsername = request.Requester?.Username ?? string.Empty,
            RequesterDepartment = request.Requester?.Department?.Name ?? string.Empty,
            ResourceId = request.ResourceId,
            ResourceName = request.Resource?.Name ?? string.Empty,
            ResourceSensitivity = request.Resource?.SensitivityLevel ?? string.Empty,
            RequestedRoleId = request.RequestedRoleId,
            RequestedRoleName = request.RequestedRole?.Name ?? string.Empty,
            BusinessJustification = request.BusinessJustification,
            DurationDays = request.DurationDays,
            RequestedAtUtc = request.RequestedAtUtc,
            AccessExpiresAtUtc = request.AccessExpiresAtUtc,
            Status = request.Status.ToString(),
            RiskScore = request.RiskScore,
            RiskLevel = request.RiskLevel.ToString(),
            RiskReasons = reasons,
            Recommendation = request.Recommendation,
            DecidedByUsername = request.DecidedBy?.Username ?? string.Empty,
            DecidedAtUtc = request.DecidedAtUtc,
            DecisionComment = request.DecisionComment
        };
    }
}
