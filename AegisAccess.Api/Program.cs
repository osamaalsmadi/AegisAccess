using AegisAccess.Application.AccessGovernance;
using AegisAccess.Application.Interfaces;
using AegisAccess.Infrastructure.Persistence;
using AegisAccess.Infrastructure.Services;
using AegisAccess.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException("Application is set to Production but uses Demo configuration. Halting startup.");
}

var dbFolder = Path.Combine(builder.Environment.ContentRootPath, "..", "data");
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "aegisaccess.dev.db");
builder.Services.AddDbContext<AegisDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<IAccessRiskEngine, AccessRiskEngine>();
builder.Services.AddScoped<IAccessRequestService, AccessRequestService>();

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
    options.AddFixedWindowLimiter("Api", limiter =>
    {
        limiter.Window = TimeSpan.FromSeconds(10);
        limiter.PermitLimit = 100;
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("SecurityAdmin"));
    options.AddPolicy("RequireAudit", policy => policy.RequireRole("SecurityAdmin", "Auditor"));
    options.AddPolicy("RequireApprover", policy => policy.RequireRole("SecurityAdmin", "Approver"));
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
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.EnsureCreatedAsync();
    }

    try
    {
        await DataSeeder.SeedAsync(db);
        await AccessGovernanceSeeder.SeedAsync(db);
    }
    catch (Exception exception)
    {
        throw new Exception("Database seeding failed.", exception);
    }
}

app.Run();
public partial class Program { }
