using AegisAccess.Application.AccessGovernance;
using AegisAccess.Application.Interfaces;
using AegisAccess.Application.Portfolio;
using AegisAccess.Infrastructure.Persistence;
using AegisAccess.Infrastructure.Services;
using AegisAccess.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException("Demo authentication cannot run in Production.");
}

var databaseDirectory = Path.Combine(builder.Environment.ContentRootPath, "..", "data");
Directory.CreateDirectory(databaseDirectory);
var databasePath = Path.Combine(databaseDirectory, "aegisaccess.dev.db");
builder.Services.AddDbContext<AegisDbContext>(
    options => options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IAccessRiskEngine, AccessRiskEngine>();
builder.Services.AddScoped<ISodPolicyEvaluator, SodPolicyEvaluator>();
builder.Services.AddScoped<IAccessRequestService, AccessRequestService>();
builder.Services.AddScoped<IPortfolioGovernanceService, PortfolioGovernanceService>();

builder.Services
    .AddAuthentication("DemoAuth")
    .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>("DemoAuth", null);

builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictReact", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("Api", limiter =>
    {
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.PermitLimit = 100;
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("SecurityAdmin"));
    options.AddPolicy("RequireAudit", policy => policy.RequireRole("SecurityAdmin", "Auditor"));
    options.AddPolicy("RequireApprover", policy => policy.RequireRole("SecurityAdmin", "Approver"));
    options.AddPolicy(
        "RequireGovernanceRead",
        policy => policy.RequireRole("SecurityAdmin", "Approver", "Auditor"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<AegisDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("StrictReact");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("Api");
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<AegisDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await database.Database.EnsureCreatedAsync();
    }

    try
    {
        await DataSeeder.SeedAsync(database);
        await AccessGovernanceSeeder.SeedAsync(database);
        await PortfolioSeeder.SeedAsync(database);
    }
    catch (Exception exception)
    {
        throw new Exception("Database seeding failed.", exception);
    }
}

app.Run();
public partial class Program { }
