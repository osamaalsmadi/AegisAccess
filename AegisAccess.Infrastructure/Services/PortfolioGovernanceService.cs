using System.Text;
using AegisAccess.Application.Portfolio;
using AegisAccess.Domain.Entities;
using AegisAccess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AegisAccess.Infrastructure.Services;

public sealed class PortfolioGovernanceService : IPortfolioGovernanceService
{
    private readonly AegisDbContext _db;

    public PortfolioGovernanceService(AegisDbContext db)
    {
        _db = db;
    }

    public async Task<GovernanceOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingRequests = await _db.AccessRequests.CountAsync(
            request => request.Status == AccessRequestStatus.Pending ||
                       request.Status == AccessRequestStatus.Escalated,
            cancellationToken);
        var criticalRequests = await _db.AccessRequests.CountAsync(
            request =>
                (request.Status == AccessRequestStatus.Pending ||
                 request.Status == AccessRequestStatus.Escalated) &&
                request.RiskLevel == AccessRiskLevel.Critical,
            cancellationToken);
        var activeCampaigns = await _db.AccessReviewCampaigns.CountAsync(
            campaign => campaign.Status == AccessReviewCampaignStatus.Active,
            cancellationToken);
        var pendingReviewItems = await _db.AccessReviewItems.CountAsync(
            item => item.Decision == AccessReviewDecision.Pending,
            cancellationToken);
        var overdueReviewItems = await _db.AccessReviewItems.CountAsync(
            item =>
                item.Decision == AccessReviewDecision.Pending &&
                item.Campaign!.DueAtUtc < now,
            cancellationToken);

        var violations = await GetViolationsAsync(cancellationToken);

        var riskGroups = await _db.AccessRequests
            .AsNoTracking()
            .GroupBy(request => request.RiskLevel)
            .Select(group => new { Level = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var riskDistribution = Enum.GetValues<AccessRiskLevel>()
            .Select(level => new RiskBucketDto
            {
                Level = level.ToString(),
                Count = riskGroups.SingleOrDefault(item => item.Level == level)?.Count ?? 0
            })
            .ToList();

        var recentEvents = await _db.AuditEvents
            .AsNoTracking()
            .OrderByDescending(item => item.TimestampUtc)
            .Take(8)
            .Select(item => new RecentGovernanceEventDto
            {
                Action = item.Action,
                Actor = item.Actor,
                Target = item.Target,
                TimestampUtc = item.TimestampUtc
            })
            .ToListAsync(cancellationToken);

        return new GovernanceOverviewDto
        {
            PostureScore = GovernancePostureCalculator.Calculate(
                criticalRequests,
                violations.Count,
                overdueReviewItems,
                pendingRequests),
            TotalUsers = await _db.Users.CountAsync(cancellationToken),
            TotalRoles = await _db.Roles.CountAsync(cancellationToken),
            TotalResources = await _db.Resources.CountAsync(cancellationToken),
            PendingRequests = pendingRequests,
            CriticalRequests = criticalRequests,
            ActiveCampaigns = activeCampaigns,
            PendingReviewItems = pendingReviewItems,
            OverdueReviewItems = overdueReviewItems,
            SodViolations = violations.Count,
            RiskDistribution = riskDistribution,
            RecentEvents = recentEvents
        };
    }

    public async Task<IReadOnlyList<SodPolicyDto>> GetPoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.SodPolicies
            .AsNoTracking()
            .Include(policy => policy.FirstRole)
            .Include(policy => policy.SecondRole)
            .OrderBy(policy => policy.Name)
            .Select(policy => new SodPolicyDto
            {
                Id = policy.Id,
                Name = policy.Name,
                Description = policy.Description,
                Severity = policy.Severity,
                FirstRole = policy.FirstRole!.Name,
                SecondRole = policy.SecondRole!.Name,
                IsActive = policy.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SodViolationDto>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var entitlements = await _db.AccessRequests
            .AsNoTracking()
            .Include(request => request.Requester)
                .ThenInclude(user => user!.Department)
            .Include(request => request.RequestedRole)
            .Where(request =>
                request.Status == AccessRequestStatus.Approved &&
                request.AccessExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        var policies = await _db.SodPolicies
            .AsNoTracking()
            .Include(policy => policy.FirstRole)
            .Include(policy => policy.SecondRole)
            .Where(policy => policy.IsActive)
            .ToListAsync(cancellationToken);

        var violations = new List<SodViolationDto>();

        foreach (var userGroup in entitlements.GroupBy(item => item.RequesterId))
        {
            var roleIds = userGroup.Select(item => item.RequestedRoleId).Distinct().ToHashSet();
            var evidence = userGroup.First();

            foreach (var policy in policies)
            {
                if (roleIds.Contains(policy.FirstRoleId) && roleIds.Contains(policy.SecondRoleId))
                {
                    violations.Add(new SodViolationDto
                    {
                        PolicyId = policy.Id,
                        PolicyName = policy.Name,
                        Severity = policy.Severity,
                        Username = evidence.Requester?.Username ?? string.Empty,
                        Department = evidence.Requester?.Department?.Name ?? string.Empty,
                        FirstRole = policy.FirstRole?.Name ?? string.Empty,
                        SecondRole = policy.SecondRole?.Name ?? string.Empty,
                        Evidence = $"Active approved access includes both {policy.FirstRole?.Name} and {policy.SecondRole?.Name}."
                    });
                }
            }
        }

        return violations;
    }

    public async Task<IReadOnlyList<ReviewCampaignDto>> GetCampaignsAsync(
        CancellationToken cancellationToken = default)
    {
        var campaigns = await CampaignQuery()
            .OrderByDescending(campaign => campaign.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return campaigns.Select(MapCampaign).ToList();
    }

    public async Task<ReviewCampaignDto> CreateCampaignAsync(
        string actorUsername,
        CreateReviewCampaignDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var actor = NormalizeUsername(actorUsername);
        var name = input.Name?.Trim() ?? string.Empty;
        var description = input.Description?.Trim() ?? string.Empty;

        if (name.Length < 5 || description.Length < 10)
        {
            throw new ArgumentException("Campaign name or description is too short.");
        }
        if (input.DueInDays is < 1 or > 90)
        {
            throw new ArgumentException("Campaign due period must be between 1 and 90 days.");
        }

        var duplicate = await _db.AccessReviewCampaigns.AnyAsync(
            campaign =>
                campaign.Status == AccessReviewCampaignStatus.Active &&
                campaign.Name == name,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("An active campaign with this name already exists.");
        }

        var now = DateTime.UtcNow;
        var entitlements = await _db.AccessRequests
            .Where(request =>
                request.Status == AccessRequestStatus.Approved &&
                request.AccessExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        if (entitlements.Count == 0)
        {
            throw new InvalidOperationException("No active approved entitlements are available for review.");
        }

        var campaign = new AccessReviewCampaign
        {
            Name = name,
            Description = description,
            CreatedByUsername = actor,
            CreatedAtUtc = now,
            DueAtUtc = now.AddDays(input.DueInDays),
            Status = AccessReviewCampaignStatus.Active
        };

        foreach (var entitlement in entitlements)
        {
            campaign.Items.Add(new AccessReviewItem
            {
                AccessRequestId = entitlement.Id,
                RiskAtReview = entitlement.RiskScore,
                Decision = AccessReviewDecision.Pending
            });
        }

        _db.AccessReviewCampaigns.Add(campaign);
        _db.AuditEvents.Add(new AuditEvent
        {
            Action = "AccessReviewCampaignCreated",
            Actor = actor,
            Target = campaign.Name,
            Details = $"Items={campaign.Items.Count}; Due={campaign.DueAtUtc:O}"
        });

        await _db.SaveChangesAsync(cancellationToken);

        var created = await CampaignQuery()
            .SingleAsync(item => item.Id == campaign.Id, cancellationToken);
        return MapCampaign(created);
    }

    public async Task<ReviewCampaignDto> DecideItemAsync(
        Guid campaignId,
        Guid itemId,
        string actorUsername,
        ReviewItemDecisionDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var actor = NormalizeUsername(actorUsername);
        var comment = input.Comment?.Trim() ?? string.Empty;

        var campaign = await CampaignQuery()
            .SingleOrDefaultAsync(item => item.Id == campaignId, cancellationToken)
            ?? throw new KeyNotFoundException("Review campaign was not found.");

        if (campaign.Status != AccessReviewCampaignStatus.Active)
        {
            throw new InvalidOperationException("Only Active campaigns can be reviewed.");
        }

        var item = campaign.Items.SingleOrDefault(reviewItem => reviewItem.Id == itemId)
            ?? throw new KeyNotFoundException("Review item was not found.");

        if (item.Decision != AccessReviewDecision.Pending)
        {
            throw new InvalidOperationException("This entitlement already has a review decision.");
        }
        if (item.AccessRequest!.Requester!.Username == actor)
        {
            throw new UnauthorizedAccessException("A user cannot certify their own access.");
        }
        if (comment.Length < 5)
        {
            throw new ArgumentException("Review comment requires at least 5 characters.");
        }

        if (!Enum.TryParse<AccessReviewDecision>(input.Decision, true, out var decision) ||
            decision == AccessReviewDecision.Pending)
        {
            throw new ArgumentException("Decision must be Certify, Revoke, or Escalate.");
        }

        item.Decision = decision;
        item.ReviewerUsername = actor;
        item.ReviewComment = comment;
        item.ReviewedAtUtc = DateTime.UtcNow;

        if (decision == AccessReviewDecision.Revoke)
        {
            item.AccessRequest.Status = AccessRequestStatus.Revoked;
        }

        if (campaign.Items.All(reviewItem => reviewItem.Decision != AccessReviewDecision.Pending))
        {
            campaign.Status = AccessReviewCampaignStatus.Completed;
            campaign.CompletedAtUtc = DateTime.UtcNow;
        }

        _db.AuditEvents.Add(new AuditEvent
        {
            Action = $"AccessReview{decision}",
            Actor = actor,
            Target = item.AccessRequest.CorrelationId,
            Details = $"Campaign={campaign.Name}; User={item.AccessRequest.Requester.Username}; Comment={comment}"
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapCampaign(campaign);
    }

    public async Task<string> ExportCampaignCsvAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var campaign = await CampaignQuery()
            .SingleOrDefaultAsync(item => item.Id == campaignId, cancellationToken)
            ?? throw new KeyNotFoundException("Review campaign was not found.");

        static string Escape(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        var csv = new StringBuilder();
        csv.AppendLine("Campaign,Status,DueAtUtc,CorrelationId,Username,Department,Resource,Role,Risk,Decision,Reviewer,Comment");

        foreach (var item in campaign.Items.OrderBy(item => item.AccessRequest!.Requester!.Username))
        {
            var access = item.AccessRequest!;
            csv.AppendLine(string.Join(",",
                Escape(campaign.Name),
                Escape(campaign.Status.ToString()),
                Escape(campaign.DueAtUtc.ToString("O")),
                Escape(access.CorrelationId),
                Escape(access.Requester?.Username ?? string.Empty),
                Escape(access.Requester?.Department?.Name ?? string.Empty),
                Escape(access.Resource?.Name ?? string.Empty),
                Escape(access.RequestedRole?.Name ?? string.Empty),
                item.RiskAtReview,
                Escape(item.Decision.ToString()),
                Escape(item.ReviewerUsername),
                Escape(item.ReviewComment)));
        }

        return csv.ToString();
    }

    private IQueryable<AccessReviewCampaign> CampaignQuery()
    {
        return _db.AccessReviewCampaigns
            .Include(campaign => campaign.Items)
                .ThenInclude(item => item.AccessRequest)
                    .ThenInclude(request => request!.Requester)
                        .ThenInclude(user => user!.Department)
            .Include(campaign => campaign.Items)
                .ThenInclude(item => item.AccessRequest)
                    .ThenInclude(request => request!.Resource)
            .Include(campaign => campaign.Items)
                .ThenInclude(item => item.AccessRequest)
                    .ThenInclude(request => request!.RequestedRole);
    }

    private static ReviewCampaignDto MapCampaign(AccessReviewCampaign campaign)
    {
        var items = campaign.Items
            .OrderByDescending(item => item.RiskAtReview)
            .Select(item => new ReviewItemDto
            {
                Id = item.Id,
                AccessRequestId = item.AccessRequestId,
                CorrelationId = item.AccessRequest?.CorrelationId ?? string.Empty,
                Username = item.AccessRequest?.Requester?.Username ?? string.Empty,
                Department = item.AccessRequest?.Requester?.Department?.Name ?? string.Empty,
                ResourceName = item.AccessRequest?.Resource?.Name ?? string.Empty,
                ResourceSensitivity = item.AccessRequest?.Resource?.SensitivityLevel ?? string.Empty,
                RoleName = item.AccessRequest?.RequestedRole?.Name ?? string.Empty,
                RiskAtReview = item.RiskAtReview,
                Decision = item.Decision.ToString(),
                ReviewerUsername = item.ReviewerUsername,
                ReviewComment = item.ReviewComment,
                ReviewedAtUtc = item.ReviewedAtUtc
            })
            .ToList();

        return new ReviewCampaignDto
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            CreatedByUsername = campaign.CreatedByUsername,
            CreatedAtUtc = campaign.CreatedAtUtc,
            DueAtUtc = campaign.DueAtUtc,
            CompletedAtUtc = campaign.CompletedAtUtc,
            Status = campaign.Status.ToString(),
            TotalItems = items.Count,
            PendingItems = items.Count(item => item.Decision == "Pending"),
            CertifiedItems = items.Count(item => item.Decision == "Certify"),
            RevokedItems = items.Count(item => item.Decision == "Revoke"),
            EscalatedItems = items.Count(item => item.Decision == "Escalate"),
            Items = items
        };
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
}
