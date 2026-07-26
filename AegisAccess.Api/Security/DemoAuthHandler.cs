using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AegisAccess.Api.Security;

public class DemoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
    private readonly IWebHostEnvironment _env;

    public DemoAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, 
        UrlEncoder encoder, IWebHostEnvironment env) 
        : base(options, logger, encoder) {
        _env = env;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!_env.IsDevelopment() && _env.EnvironmentName != "Testing") {
            throw new InvalidOperationException("CRITICAL SECURITY ERROR: Demo Authentication is active in Production environment.");
        }

        if (!Request.Headers.TryGetValue("X-Demo-User", out var headerValue)) {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Demo-User header."));
        }

        var persona = headerValue.ToString().ToLower();
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, persona) };
        
        switch (persona) {
            case "securityadmin": claims.Add(new Claim(ClaimTypes.Role, "SecurityAdmin")); break;
            case "employee": claims.Add(new Claim(ClaimTypes.Role, "Employee")); break;
            case "approver": claims.Add(new Claim(ClaimTypes.Role, "Approver")); break;
            case "auditor": claims.Add(new Claim(ClaimTypes.Role, "Auditor")); break;
            default: return Task.FromResult(AuthenticateResult.Fail("Invalid Demo Persona."));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}