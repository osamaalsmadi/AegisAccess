using AegisAccess.Application.AccessGovernance;
using AegisAccess.Domain.Entities;
using AegisAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Services;

public sealed class AccessRequestService : IAccessRequestService
{
    private readonly AegisDbContext _db;
    private readonly IAccessRiskEngine _riskEngine;

    public AccessRequestService(AegisDbContext db, IAccessRiskEngine riskEngine)
    {
        _db = db;
        _riskEngine = riskEngine;
    }

    public async Task<IReadOnlyList<AccessRequestDto>> GetRequestsAsync(
        string actorUsername,
        bool canReviewAll,
        CancellationToken cancellationToken = default)
    {
        var normalizedActor = NormalizeUsername(actorUsername);

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
            query = query.Where(request => request.Requester!.Username == normalizedActor);
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
        var normalizedActor = NormalizeUsername(actorUsername);

        if (input.ResourceId == Guid.Empty || input.RequestedRoleId == Guid.Empty)
        {
            throw new ArgumentException("A resource and requested role are required.");
        }

        var justification = input.BusinessJustification?.Trim() ?? string.Empty;
        if (justification.Length < 15)
        {
            throw new ArgumentException("Business justification must contain at least 15 characters.");
        }

        if (input.DurationDays is < 1 or > 365)
        {
            throw new ArgumentException("Access duration must be between 1 and 365 days.");
        }

        var requester = await _db.Users
            .SingleOrDefaultAsync(user => user.Username == normalizedActor, cancellationToken)
            ?? throw new KeyNotFoundException("The authenticated demo user was not found.");

        var resource = await _db.Resources
            .SingleOrDefaultAsync(item => item.Id == input.ResourceId, cancellationToken)
            ?? throw new KeyNotFoundException("The selected resource was not found.");

        var requestedRole = await _db.Roles
            .SingleOrDefaultAsync(item => item.Id == input.RequestedRoleId, cancellationToken)
            ?? throw new KeyNotFoundException("The selected role was not found.");

        var duplicateExists = await _db.AccessRequests.AnyAsync(
            request =>
                request.RequesterId == requester.Id &&
                request.ResourceId == resource.Id &&
                request.RequestedRoleId == requestedRole.Id &&
                (request.Status == AccessRequestStatus.Pending ||
                 request.Status == AccessRequestStatus.Escalated),
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException("An active request already exists for the same user, resource, and role.");
        }

        var now = DateTime.UtcNow;
        var privilegedAccessCount = await _db.AccessRequests.CountAsync(
            request =>
                request.RequesterId == requester.Id &&
                request.Status == AccessRequestStatus.Approved &&
                request.AccessExpiresAtUtc > now &&
                (request.RequestedRole!.Name == "SecurityAdmin" ||
                 request.RequestedRole.Name == "Approver"),
            cancellationToken);

        var risk = _riskEngine.Evaluate(new AccessRiskContext(
            resource.SensitivityLevel,
            requestedRole.Name,
            input.DurationDays,
            justification,
            privilegedAccessCount));

        var request = new AccessRequest
        {
            CorrelationId = $"ACC-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            RequesterId = requester.Id,
            ResourceId = resource.Id,
            RequestedRoleId = requestedRole.Id,
            BusinessJustification = justification,
            DurationDays = input.DurationDays,
            RequestedAtUtc = now,
            AccessExpiresAtUtc = now.AddDays(input.DurationDays),
            Status = risk.RequiresSecurityAdmin ? AccessRequestStatus.Escalated : AccessRequestStatus.Pending,
            RiskScore = risk.Score,
            RiskLevel = Enum.Parse<AccessRiskLevel>(risk.Level),
            RiskReasons = string.Join("|", risk.Reasons),
            Recommendation = risk.Recommendation
        };

        _db.AccessRequests.Add(request);
        _db.AuditEvents.Add(new AuditEvent
        {
            Action = "AccessRequestCreated",
            Actor = normalizedActor,
            Target = request.CorrelationId,
            Details = $"Resource={resource.Name}; Role={requestedRole.Name}; Risk={risk.Score}; Status={request.Status}"
        });

        await _db.SaveChangesAsync(cancellationToken);

        request.Requester = requester;
        request.Resource = resource;
        request.RequestedRole = requestedRole;
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
        var normalizedActor = NormalizeUsername(actorUsername);

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
            .SingleOrDefaultAsync(user => user.Username == normalizedActor, cancellationToken)
            ?? throw new KeyNotFoundException("The decision user was not found.");

        if (decider.Id == request.RequesterId)
        {
            throw new UnauthorizedAccessException("Segregation of duties prevents self-approval.");
        }

        var normalizedDecision = input.Decision?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedDecision is not "approve" and not "reject")
        {
            throw new ArgumentException("Decision must be Approve or Reject.");
        }

        var comment = input.Comment?.Trim() ?? string.Empty;
        if (comment.Length < 5)
        {
            throw new ArgumentException("Decision comment must contain at least 5 characters.");
        }

        if (normalizedDecision == "approve" &&
            request.RiskLevel == AccessRiskLevel.Critical &&
            !isSecurityAdmin)
        {
            throw new UnauthorizedAccessException("Critical requests require SecurityAdmin approval.");
        }

        request.Status = normalizedDecision == "approve"
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
            Actor = normalizedActor,
            Target = request.CorrelationId,
            Details = $"Requester={request.Requester!.Username}; Resource={request.Resource!.Name}; Role={request.RequestedRole!.Name}; Comment={comment}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Map(request);
    }

    private static string NormalizeUsername(string username)
    {
        var normalized = username?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new UnauthorizedAccessException("Authenticated username is missing.");
        }

        return normalized;
    }

    private static AccessRequestDto Map(AccessRequest request)
    {
        var reasons = string.IsNullOrWhiteSpace(request.RiskReasons)
            ? Array.Empty<string>()
            : request.RiskReasons.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
