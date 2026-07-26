using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AegisAccess.Infrastructure.Persistence;
using Xunit;

namespace AegisAccess.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program> {
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<AegisDbContext>>();
            services.RemoveAll<AegisDbContext>();

            // One open connection keeps the in-memory database alive for the
            // application startup, seeder, health check, and every test.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            services.AddSingleton<DbConnection>(_connection);

            services.AddDbContext<AegisDbContext>((container, options) =>
                options.UseSqlite(container.GetRequiredService<DbConnection>()));
        });
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (disposing) _connection?.Dispose();
    }
}

public class ApiTests : IClassFixture<CustomWebApplicationFactory> {
    private readonly HttpClient _client;
    public ApiTests(CustomWebApplicationFactory factory) { _client = factory.CreateClient(); }

    [Fact] public async Task HealthCheck_Returns200() {
        var res = await _client.GetAsync("/health");
        res.EnsureSuccessStatusCode();
    }
    [Fact] public async Task NoHeader_Returns401() {
        var res = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
    [Fact] public async Task UnknownPersona_Returns401() {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        req.Headers.Add("X-Demo-User", "hacker");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
    [Fact] public async Task Employee_Users_Returns403() {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        req.Headers.Add("X-Demo-User", "employee");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
    [Fact] public async Task SecurityAdmin_Users_Returns200() {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        req.Headers.Add("X-Demo-User", "securityadmin");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
    [Fact] public async Task Auditor_AuditEvents_Returns200() {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auditevents");
        req.Headers.Add("X-Demo-User", "auditor");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
    [Fact] public async Task Employee_AuditEvents_Returns403() {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auditevents");
        req.Headers.Add("X-Demo-User", "employee");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}