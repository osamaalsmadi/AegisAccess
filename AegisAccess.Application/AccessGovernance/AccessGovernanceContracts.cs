using System.ComponentModel.DataAnnotations;

namespace AegisAccess.Application.AccessGovernance;

public sealed class CreateAccessRequestDto
{
    public Guid ResourceId { get; set; }
    public Guid RequestedRoleId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 15)]
    public string BusinessJustification { get; set; } = string.Empty;

    [Range(1, 365)]
    public int DurationDays { get; set; } = 30;
}

public sealed class AccessDecisionDto
{
    [Required]
    [RegularExpression("^(Approve|Reject)$", ErrorMessage = "Decision must be Approve or Reject.")]
    public string Decision { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Comment { get; set; } = string.Empty;
}

public sealed class AccessRequestDto
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string RequesterUsername { get; set; } = string.Empty;
    public string RequesterDepartment { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceSensitivity { get; set; } = string.Empty;
    public Guid RequestedRoleId { get; set; }
    public string RequestedRoleName { get; set; } = string.Empty;
    public string BusinessJustification { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime AccessExpiresAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string[] RiskReasons { get; set; } = Array.Empty<string>();
    public string Recommendation { get; set; } = string.Empty;
    public string DecidedByUsername { get; set; } = string.Empty;
    public DateTime? DecidedAtUtc { get; set; }
    public string DecisionComment { get; set; } = string.Empty;
}

public sealed class AccessCatalogItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
}

public sealed class AccessCatalogDto
{
    public IReadOnlyList<AccessCatalogItemDto> Resources { get; set; } = Array.Empty<AccessCatalogItemDto>();
    public IReadOnlyList<AccessCatalogItemDto> Roles { get; set; } = Array.Empty<AccessCatalogItemDto>();
}

public sealed record AccessRiskContext(
    string ResourceSensitivity,
    string RequestedRole,
    int DurationDays,
    string BusinessJustification,
    int ExistingPrivilegedAccessCount);

public sealed record AccessRiskAssessment(
    int Score,
    string Level,
    IReadOnlyList<string> Reasons,
    string Recommendation,
    bool RequiresSecurityAdmin);

public interface IAccessRiskEngine
{
    AccessRiskAssessment Evaluate(AccessRiskContext context);
}

public interface IAccessRequestService
{
    Task<IReadOnlyList<AccessRequestDto>> GetRequestsAsync(
        string actorUsername,
        bool canReviewAll,
        CancellationToken cancellationToken = default);

    Task<AccessCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<AccessRequestDto> CreateRequestAsync(
        string actorUsername,
        CreateAccessRequestDto input,
        CancellationToken cancellationToken = default);

    Task<AccessRequestDto> DecideRequestAsync(
        Guid requestId,
        string actorUsername,
        bool isSecurityAdmin,
        AccessDecisionDto input,
        CancellationToken cancellationToken = default);
}
