using System.Text;
using AegisAccess.Application.Portfolio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisAccess.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireGovernanceRead")]
public sealed class AccessReviewsController : ControllerBase
{
    private readonly IPortfolioGovernanceService _service;

    public AccessReviewsController(IPortfolioGovernanceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetCampaigns(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetCampaignsAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateCampaign(
        [FromBody] CreateReviewCampaignDto input,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateCampaignAsync(
                GetActor(),
                input,
                cancellationToken);
            return Created($"/api/accessreviews/{created.Id}", created);
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: 400, title: "Invalid campaign", detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: 409, title: "Campaign conflict", detail: exception.Message);
        }
    }

    [HttpPost("{campaignId:guid}/items/{itemId:guid}/decision")]
    [Authorize(Policy = "RequireApprover")]
    public async Task<IActionResult> DecideItem(
        Guid campaignId,
        Guid itemId,
        [FromBody] ReviewItemDecisionDto input,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.DecideItemAsync(
                campaignId,
                itemId,
                GetActor(),
                input,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: 400, title: "Invalid review decision", detail: exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(statusCode: 404, title: "Review item not found", detail: exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: 403, title: "Review blocked by policy", detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: 409, title: "Review conflict", detail: exception.Message);
        }
    }

    [HttpGet("{campaignId:guid}/export")]
    public async Task<IActionResult> ExportCampaign(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        try
        {
            var csv = await _service.ExportCampaignCsvAsync(campaignId, cancellationToken);
            return File(
                Encoding.UTF8.GetBytes(csv),
                "text/csv",
                $"aegisaccess-review-{campaignId:N}.csv");
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(statusCode: 404, title: "Campaign not found", detail: exception.Message);
        }
    }

    private string GetActor()
    {
        return User.Identity?.Name?.Trim().ToLowerInvariant()
            ?? throw new UnauthorizedAccessException("Authenticated identity is missing.");
    }
}
