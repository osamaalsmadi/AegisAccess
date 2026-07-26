using AegisAccess.Application.AccessGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisAccess.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AccessRequestsController : ControllerBase
{
    private readonly IAccessRequestService _service;

    public AccessRequestsController(IAccessRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetRequests(CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var canReviewAll =
            User.IsInRole("SecurityAdmin") ||
            User.IsInRole("Approver") ||
            User.IsInRole("Auditor");

        var requests = await _service.GetRequestsAsync(actor, canReviewAll, cancellationToken);
        return Ok(requests);
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetCatalogAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateAccessRequestDto input,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateRequestAsync(GetActor(), input, cancellationToken);
            return Created($"/api/accessrequests/{created.Id}", created);
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid access request", detail: exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Governance item not found", detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Access request conflict", detail: exception.Message);
        }
    }

    [HttpPost("{requestId:guid}/decision")]
    [Authorize(Policy = "RequireApprover")]
    public async Task<IActionResult> DecideRequest(
        Guid requestId,
        [FromBody] AccessDecisionDto input,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.DecideRequestAsync(
                requestId,
                GetActor(),
                User.IsInRole("SecurityAdmin"),
                input,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid decision", detail: exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Access request not found", detail: exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Decision blocked by policy", detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Decision conflict", detail: exception.Message);
        }
    }

    private string GetActor()
    {
        return User.Identity?.Name?.Trim().ToLowerInvariant()
            ?? throw new UnauthorizedAccessException("Authenticated identity is missing.");
    }
}
