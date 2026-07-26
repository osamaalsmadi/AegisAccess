using AegisAccess.Application.AccessGovernance;
using Xunit;

namespace AegisAccess.UnitTests;

public sealed class AccessRiskEngineTests
{
    private readonly AccessRiskEngine _engine = new();

    [Fact]
    public void Critical_Admin_Request_Requires_SecurityAdmin()
    {
        var result = _engine.Evaluate(new AccessRiskContext(
            "Critical",
            "SecurityAdmin",
            180,
            "Short emergency reason",
            1));

        Assert.Equal("Critical", result.Level);
        Assert.True(result.Score >= 75);
        Assert.True(result.RequiresSecurityAdmin);
    }

    [Fact]
    public void Low_Risk_Request_Uses_Standard_Workflow()
    {
        var result = _engine.Evaluate(new AccessRiskContext(
            "Low",
            "Employee",
            14,
            "Temporary read-only access for an approved reporting assignment.",
            0));

        Assert.Equal("Low", result.Level);
        Assert.False(result.RequiresSecurityAdmin);
    }
}
