namespace AegisAccess.Domain.Entities;

public enum AccessRequestStatus
{
    Pending = 1,
    Escalated = 2,
    Approved = 3,
    Rejected = 4,
    Revoked = 5,
    Expired = 6
}

public enum AccessRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public sealed class AccessRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = string.Empty;

    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    public Guid ResourceId { get; set; }
    public Resource? Resource { get; set; }

    public Guid RequestedRoleId { get; set; }
    public Role? RequestedRole { get; set; }

    public string BusinessJustification { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime AccessExpiresAtUtc { get; set; }

    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
    public int RiskScore { get; set; }
    public AccessRiskLevel RiskLevel { get; set; } = AccessRiskLevel.Low;
    public string RiskReasons { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;

    public Guid? DecidedById { get; set; }
    public User? DecidedBy { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string DecisionComment { get; set; } = string.Empty;
}
