using AegisAccess.Application.Portfolio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisAccess.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class GovernanceController : ControllerBase
{
    private readonly IPortfolioGovernanceService _service;

    public GovernanceController(IPortfolioGovernanceService service)
    {
        _service = service;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetOverviewAsync(cancellationToken));
    }

    [HttpGet("sod-policies")]
    [Authorize(Policy = "RequireGovernanceRead")]
    public async Task<IActionResult> GetPolicies(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetPoliciesAsync(cancellationToken));
    }

    [HttpGet("violations")]
    [Authorize(Policy = "RequireGovernanceRead")]
    public async Task<IActionResult> GetViolations(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetViolationsAsync(cancellationToken));
    }
}
