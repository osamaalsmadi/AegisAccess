using AegisAccess.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisAccess.Api.Controllers;

[ApiController] [Route("api/[controller]")] [Authorize]
public class DashboardController : ControllerBase {
    private readonly IDashboardService _service;
    public DashboardController(IDashboardService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetSummaryAsync());
}

[ApiController] [Route("api/[controller]")] [Authorize(Policy = "RequireAdmin")]
public class UsersController : ControllerBase {
    private readonly IUserService _service;
    public UsersController(IUserService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetAllUsersAsync());
}

[ApiController] [Route("api/[controller]")] [Authorize(Policy = "RequireAdmin")]
public class DepartmentsController : ControllerBase {
    private readonly IDepartmentService _service;
    public DepartmentsController(IDepartmentService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetAllDepartmentsAsync());
}

[ApiController] [Route("api/[controller]")] [Authorize(Policy = "RequireAdmin")]
public class RolesController : ControllerBase {
    private readonly IRoleService _service;
    public RolesController(IRoleService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetAllRolesAsync());
}

[ApiController] [Route("api/[controller]")] [Authorize(Policy = "RequireAdmin")]
public class PermissionsController : ControllerBase {
    private readonly IPermissionService _service;
    public PermissionsController(IPermissionService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetAllPermissionsAsync());
}

[ApiController] [Route("api/[controller]")] [Authorize]
public class ResourcesController : ControllerBase {
    private readonly IResourceService _service;
    public ResourcesController(IResourceService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetAllResourcesAsync());
}

[ApiController] [Route("api/[controller]")] [Authorize(Policy = "RequireAudit")]
public class AuditEventsController : ControllerBase {
    private readonly IAuditService _service;
    public AuditEventsController(IAuditService service) { _service = service; }
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetRecentEventsAsync());
}