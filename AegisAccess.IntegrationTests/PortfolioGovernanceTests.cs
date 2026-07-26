using System.Net;
using System.Net.Http.Json;
using AegisAccess.Application.Portfolio;
using Xunit;

namespace AegisAccess.IntegrationTests;

public sealed class PortfolioGovernanceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PortfolioGovernanceTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SecurityAdmin_Can_Read_Dynamic_Overview()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/governance/overview");
        request.Headers.Add("X-Demo-User", "securityadmin");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var overview = await response.Content.ReadFromJsonAsync<GovernanceOverviewDto>();
        Assert.NotNull(overview);
        Assert.InRange(overview.PostureScore, 0, 100);
        Assert.True(overview.TotalUsers > 0);
        Assert.True(overview.RiskDistribution.Count >= 4);
    }

    [Fact]
    public async Task Employee_Cannot_Read_Sod_Policies()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/governance/sod-policies");
        request.Headers.Add("X-Demo-User", "employee");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SecurityAdmin_Can_Create_And_Export_Review_Campaign()
    {
        var uniqueName = $"Integration Review {Guid.NewGuid():N}";
        var create = new HttpRequestMessage(HttpMethod.Post, "/api/accessreviews")
        {
            Content = JsonContent.Create(new CreateReviewCampaignDto
            {
                Name = uniqueName,
                Description = "Integration test campaign for active portfolio entitlements.",
                DueInDays = 10
            })
        };
        create.Headers.Add("X-Demo-User", "securityadmin");

        var createResponse = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var campaign = await createResponse.Content.ReadFromJsonAsync<ReviewCampaignDto>();
        Assert.NotNull(campaign);
        Assert.True(campaign.TotalItems > 0);

        var export = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/accessreviews/{campaign.Id}/export");
        export.Headers.Add("X-Demo-User", "securityadmin");

        var exportResponse = await _client.SendAsync(export);
        exportResponse.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", exportResponse.Content.Headers.ContentType?.MediaType);
    }
}
