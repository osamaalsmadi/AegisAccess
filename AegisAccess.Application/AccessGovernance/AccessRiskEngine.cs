namespace AegisAccess.Application.AccessGovernance;

public sealed class AccessRiskEngine : IAccessRiskEngine
{
    public AccessRiskAssessment Evaluate(AccessRiskContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var score = 0;
        var reasons = new List<string>();

        switch (context.ResourceSensitivity.Trim().ToLowerInvariant())
        {
            case "critical":
                score += 65;
                reasons.Add("Target resource is classified as Critical.");
                break;
            case "high":
                score += 45;
                reasons.Add("Target resource is classified as High sensitivity.");
                break;
            case "medium":
                score += 25;
                reasons.Add("Target resource has Medium sensitivity.");
                break;
            default:
                score += 10;
                reasons.Add("Target resource has Low sensitivity.");
                break;
        }

        switch (context.RequestedRole.Trim().ToLowerInvariant())
        {
            case "securityadmin":
                score += 25;
                reasons.Add("SecurityAdmin grants privileged administrative access.");
                break;
            case "approver":
                score += 15;
                reasons.Add("Approver can authorize other users' access.");
                break;
            case "auditor":
                score += 8;
                reasons.Add("Auditor can view sensitive governance evidence.");
                break;
        }

        if (context.DurationDays > 90)
        {
            score += 15;
            reasons.Add("Requested duration exceeds 90 days.");
        }
        else if (context.DurationDays > 30)
        {
            score += 5;
            reasons.Add("Requested duration exceeds the 30-day baseline.");
        }

        if (context.BusinessJustification.Trim().Length < 30)
        {
            score += 10;
            reasons.Add("Business justification is short and requires clarification.");
        }

        if (context.ExistingPrivilegedAccessCount > 0)
        {
            score += 10;
            reasons.Add("Requester already has active privileged access.");
        }

        score = Math.Clamp(score, 0, 100);

        var level = score switch
        {
            < 30 => "Low",
            < 55 => "Medium",
            < 75 => "High",
            _ => "Critical"
        };

        var requiresSecurityAdmin = score >= 75;
        var recommendation = requiresSecurityAdmin
            ? "SecurityAdmin review is mandatory before approval."
            : score >= 55
                ? "Approver must verify business need and least privilege."
                : "Standard approval workflow is sufficient.";

        return new AccessRiskAssessment(score, level, reasons, recommendation, requiresSecurityAdmin);
    }
}
