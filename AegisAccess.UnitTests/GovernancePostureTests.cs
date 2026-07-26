using AegisAccess.Application.Portfolio;
using Xunit;

namespace AegisAccess.UnitTests;

public sealed class GovernancePostureTests
{
    [Fact]
    public void Healthy_Posture_Remains_At_One_Hundred()
    {
        var score = GovernancePostureCalculator.Calculate(0, 0, 0, 0);
        Assert.Equal(100, score);
    }

    [Fact]
    public void Security_Issues_Reduce_Posture_Deterministically()
    {
        var score = GovernancePostureCalculator.Calculate(
            criticalRequests: 2,
            sodViolations: 1,
            overdueReviews: 1,
            pendingRequests: 3);

        Assert.Equal(53, score);
    }

    [Fact]
    public void Posture_Never_Becomes_Negative()
    {
        var score = GovernancePostureCalculator.Calculate(99, 99, 99, 99);
        Assert.Equal(16, score);
    }
}
