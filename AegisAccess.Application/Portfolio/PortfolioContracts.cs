using System.ComponentModel.DataAnnotations;

namespace AegisAccess.Application.Portfolio;

public sealed class GovernanceOverviewDto
{
    public int PostureScore { get; set; }
    public int TotalUsers { get; set; }
    public int TotalRoles { get; set; }
    public int TotalResources { get; set; }
    public int PendingRequests { get; set; }
    public int CriticalRequests { get; set; }
    public int ActiveCampaigns { get; set; }
    public int PendingReviewItems { get; set; }
    public int OverdueReviewItems { get; set; }
    public int SodViolations { get; set; }
    public IReadOnlyList<RiskBucketDto> RiskDistribution { get; set; } = Array.Empty<RiskBucketDto>();
    public IReadOnlyList<RecentGovernanceEventDto> RecentEvents { get; set; } = Array.Empty<RecentGovernanceEventDto>();
}

public sealed class RiskBucketDto
{
    public string Level { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class RecentGovernanceEventDto
{
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}

public sealed class SodPolicyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string FirstRole { get; set; } = string.Empty;
    public string SecondRole { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class SodViolationDto
{
    public Guid PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string FirstRole { get; set; } = string.Empty;
    public string SecondRole { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
}

public sealed record SodConflict(
    Guid PolicyId,
    string PolicyName,
    string Severity,
    string ExistingRole,
    string RequestedRole,
    string Description);

public sealed class CreateReviewCampaignDto
{
    [Required]
    [StringLength(120, MinimumLength = 5)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 90)]
    public int DueInDays { get; set; } = 14;
}

public sealed class ReviewItemDecisionDto
{
    [Required]
    [RegularExpression("^(Certify|Revoke|Escalate)$")]
    public string Decision { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Comment { get; set; } = string.Empty;
}

public sealed class ReviewItemDto
{
    public Guid Id { get; set; }
    public Guid AccessRequestId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceSensitivity { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int RiskAtReview { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string ReviewerUsername { get; set; } = string.Empty;
    public string ReviewComment { get; set; } = string.Empty;
    public DateTime? ReviewedAtUtc { get; set; }
}

public sealed class ReviewCampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int PendingItems { get; set; }
    public int CertifiedItems { get; set; }
    public int RevokedItems { get; set; }
    public int EscalatedItems { get; set; }
    public IReadOnlyList<ReviewItemDto> Items { get; set; } = Array.Empty<ReviewItemDto>();
}

public interface ISodPolicyEvaluator
{
    Task<IReadOnlyList<SodConflict>> FindConflictsAsync(
        Guid userId,
        Guid requestedRoleId,
        CancellationToken cancellationToken = default);
}

public interface IPortfolioGovernanceService
{
    Task<GovernanceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SodPolicyDto>> GetPoliciesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SodViolationDto>> GetViolationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewCampaignDto>> GetCampaignsAsync(CancellationToken cancellationToken = default);
    Task<ReviewCampaignDto> CreateCampaignAsync(
        string actorUsername,
        CreateReviewCampaignDto input,
        CancellationToken cancellationToken = default);
    Task<ReviewCampaignDto> DecideItemAsync(
        Guid campaignId,
        Guid itemId,
        string actorUsername,
        ReviewItemDecisionDto input,
        CancellationToken cancellationToken = default);
    Task<string> ExportCampaignCsvAsync(Guid campaignId, CancellationToken cancellationToken = default);
}

public static class GovernancePostureCalculator
{
    public static int Calculate(
        int criticalRequests,
        int sodViolations,
        int overdueReviews,
        int pendingRequests)
    {
        var deductions =
            Math.Min(criticalRequests * 8, 24) +
            Math.Min(sodViolations * 15, 30) +
            Math.Min(overdueReviews * 10, 20) +
            Math.Min(pendingRequests * 2, 10);

        return Math.Clamp(100 - deductions, 0, 100);
    }
}
