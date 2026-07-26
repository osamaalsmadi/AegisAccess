using System.Net;
using System.Net.Http.Json;
using AegisAccess.Application.AccessGovernance;
using Xunit;

namespace AegisAccess.IntegrationTests;

public sealed class AccessWorkflowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccessWorkflowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Employee_Can_Create_Request_And_Approver_Can_Approve()
    {
        var catalogRequest = new HttpRequestMessage(HttpMethod.Get, "/api/accessrequests/catalog");
        catalogRequest.Headers.Add("X-Demo-User", "employee");
        var catalogResponse = await _client.SendAsync(catalogRequest);
        catalogResponse.EnsureSuccessStatusCode();

        var catalog = await catalogResponse.Content.ReadFromJsonAsync<AccessCatalogDto>();
        Assert.NotNull(catalog);

        var resource = catalog.Resources.Single(item => item.Name == "HR Portal");
        var role = catalog.Roles.Single(item => item.Name == "Auditor");

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/accessrequests")
        {
            Content = JsonContent.Create(new CreateAccessRequestDto
            {
                ResourceId = resource.Id,
                RequestedRoleId = role.Id,
                BusinessJustification = "Temporary audit evidence review for the approved compliance assignment.",
                DurationDays = 14
            })
        };
        createRequest.Headers.Add("X-Demo-User", "employee");

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<AccessRequestDto>();
        Assert.NotNull(created);
        Assert.Equal("Pending", created.Status);

        var decisionRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/accessrequests/{created.Id}/decision")
        {
            Content = JsonContent.Create(new AccessDecisionDto
            {
                Decision = "Approve",
                Comment = "Approved for the stated compliance review period."
            })
        };
        decisionRequest.Headers.Add("X-Demo-User", "approver");

        var decisionResponse = await _client.SendAsync(decisionRequest);
        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);

        var decided = await decisionResponse.Content.ReadFromJsonAsync<AccessRequestDto>();
        Assert.NotNull(decided);
        Assert.Equal("Approved", decided.Status);
        Assert.Equal("approver", decided.DecidedByUsername);
    }

    [Fact]
    public async Task Employee_Cannot_Use_Decision_Endpoint()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/accessrequests/{Guid.NewGuid()}/decision")
        {
            Content = JsonContent.Create(new AccessDecisionDto
            {
                Decision = "Approve",
                Comment = "Self approval attempt must be blocked."
            })
        };
        request.Headers.Add("X-Demo-User", "employee");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_User_Cannot_Read_Access_Requests()
    {
        var response = await _client.GetAsync("/api/accessrequests");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
