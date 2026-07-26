namespace AegisAccess.Domain.Entities;

public sealed class SodPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "High";
    public Guid FirstRoleId { get; set; }
    public Role? FirstRole { get; set; }
    public Guid SecondRoleId { get; set; }
    public Role? SecondRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public enum AccessReviewCampaignStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum AccessReviewDecision
{
    Pending = 1,
    Certify = 2,
    Revoke = 3,
    Escalate = 4
}

public sealed class AccessReviewCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public AccessReviewCampaignStatus Status { get; set; } = AccessReviewCampaignStatus.Active;
    public ICollection<AccessReviewItem> Items { get; set; } = new List<AccessReviewItem>();
}

public sealed class AccessReviewItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public AccessReviewCampaign? Campaign { get; set; }
    public Guid AccessRequestId { get; set; }
    public AccessRequest? AccessRequest { get; set; }
    public AccessReviewDecision Decision { get; set; } = AccessReviewDecision.Pending;
    public string ReviewerUsername { get; set; } = string.Empty;
    public string ReviewComment { get; set; } = string.Empty;
    public DateTime? ReviewedAtUtc { get; set; }
    public int RiskAtReview { get; set; }
}
